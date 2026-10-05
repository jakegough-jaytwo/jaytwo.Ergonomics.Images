using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NetVips;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// libvips pipeline: open, orient, fit or rotate, encode. Native images stay on the native heap.
/// </summary>
internal static class ImageEngine
{
    private const int UnboundedDimension = 1_000_000;
    private const int CheckerCell = 8;

    private enum ContainerKind
    {
        Other,
        Gif,
        Tiff,
    }

    public static ImageSize ExecutePipeline(
        Stream source,
        Stream destination,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        ImageEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        var encode = ToEncode(encoding);
        var format = encoding.Format;

        if (ops.Length == 1 && ops[0] is PipelineOp.ResizeOp resize)
        {
            return Execute(
                source,
                destination,
                format,
                resize.Fit,
                resize.Width,
                resize.Height,
                resize.Aspect,
                resize.Enlarge,
                resize.Canvas,
                sourceAlpha,
                resize.Focus,
                encode,
                cancellationToken);
        }

        if (ops.Length == 1 && ops[0] is PipelineOp.RotateOp rotate)
        {
            return ExecuteRotate(
                source,
                destination,
                format,
                rotate.Rotation,
                rotate.Bounds,
                rotate.Aspect,
                rotate.Canvas,
                sourceAlpha,
                encode,
                cancellationToken);
        }

        if (ops.Length == 0)
        {
            return Execute(
                source,
                destination,
                format,
                ImageFit.Fit,
                width: null,
                height: null,
                aspect: null,
                enlarge: true,
                canvas: null,
                sourceAlpha,
                focus: null,
                encode,
                cancellationToken);
        }

        return ExecuteOrdered(source, destination, ops, sourceAlpha, format, encode, cancellationToken);
    }

    public static ImageSize Execute(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        ImageFit fit,
        int? width,
        int? height,
        ImageAspectRatio? aspect,
        bool enlarge,
        ImageCanvas? canvas,
        AlphaBehavior sourceAlpha,
        ImageFocus? focus,
        ImageEncode encode,
        CancellationToken cancellationToken = default)
    {
        VipsRuntime.EnsureInitialized();

        try
        {
            var owned = new List<IDisposable>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = PrepareInput(source, out var loaderOptions);
                var plan = new Plan
                {
                    Fit = fit,
                    Width = width,
                    Height = height,
                    Aspect = aspect,
                    Enlarge = enlarge,
                    Canvas = canvas,
                    SourceAlpha = sourceAlpha,
                    Focus = focus,
                    AutoOrient = encode.AutoOrient,
                    Metadata = encode.Metadata,
                    Quality = encode.Quality,
                    Fallback = encode.AlphaFallbackColor,
                };
                var image = Finish(Build(input, plan, loaderOptions, owned), plan, owned);
                var size = new ImageSize(image.Width, image.Height);
                Write(image, destination, format, plan, cancellationToken);
                return size;
            }
            finally
            {
                for (var i = owned.Count - 1; i >= 0; i--)
                {
                    owned[i].Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (ImageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is VipsException or DllNotFoundException or TypeInitializationException)
        {
            throw new ImageException("Image transform failed. " + ex.Message, ex);
        }
    }

    internal static ImageSize ExecuteBuilt(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        ImageEncode encode,
        Func<Stream, string?, List<IDisposable>, Image> build,
        CancellationToken cancellationToken = default)
    {
        VipsRuntime.EnsureInitialized();

        try
        {
            var owned = new List<IDisposable>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = PrepareInput(source, out var loaderOptions);
                var image = build(input, loaderOptions, owned);
                var size = new ImageSize(image.Width, image.Height);
                var writePlan = new Plan
                {
                    AutoOrient = encode.AutoOrient,
                    Metadata = encode.Metadata,
                    Quality = encode.Quality,
                    Fallback = encode.AlphaFallbackColor,
                };
                Write(image, destination, format, writePlan, cancellationToken);
                return size;
            }
            finally
            {
                for (var i = owned.Count - 1; i >= 0; i--)
                {
                    owned[i].Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (ImageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is VipsException or DllNotFoundException or TypeInitializationException)
        {
            throw new ImageException("Image transform failed. " + ex.Message, ex);
        }
    }

    internal static ImageSize ExecuteRotate(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        ImageRotation rotation,
        RotationBounds bounds,
        ImageAspectRatio? aspect,
        ImageCanvas? canvas,
        AlphaBehavior sourceAlpha,
        ImageEncode encode,
        CancellationToken cancellationToken = default)
    {
        return ExecuteBuilt(
            source,
            destination,
            format,
            encode,
            (input, loaderOptions, owned) =>
            {
                var image = LoadOriented(input, encode.AutoOrient, loaderOptions, owned);
                image = ApplySourceAlpha(image, sourceAlpha, owned);
                image = Turn(image, rotation, bounds, aspect, canvas ?? ImageCanvas.Transparent, owned);
                return ClearOrientation(image, owned);
            },
            cancellationToken);
    }

    internal static Image LoadOriented(Stream source, bool autoOrient, string? loaderOptions, List<IDisposable> owned)
    {
        var vipsSource = new VipsStreamSource(source);
        owned.Add(vipsSource);
        var image = Image.NewFromSource(
            vipsSource,
            loaderOptions ?? string.Empty,
            access: Enums.Access.Sequential,
            failOn: Enums.FailOn.Error);
        owned.Add(image);
        if (!autoOrient)
        {
            return image;
        }

        var oriented = image.Autorot();
        owned.Add(oriented);
        return oriented;
    }

    internal static Image FitLoaded(
        Image image,
        int width,
        int height,
        Enums.Size size,
        Enums.Interesting? crop,
        List<IDisposable> owned)
    {
        var thumb = image.ThumbnailImage(width, height: height, size: size, crop: crop, noRotate: true);
        owned.Add(thumb);
        return thumb;
    }

    internal static Image Flatten(Image image, ImageColor color, List<IDisposable> owned)
    {
        if (!image.HasAlpha())
        {
            return image;
        }

        var flat = image.Flatten(background: new[] { (double)color.R, color.G, color.B });
        owned.Add(flat);
        return flat;
    }

    internal static Image ApplySourceAlpha(Image image, AlphaBehavior behavior, List<IDisposable> owned)
    {
        return behavior switch
        {
            AlphaBehavior.Keep => image,
            AlphaBehavior.Flat flat => Flatten(image, flat.Color, owned),
            AlphaBehavior.Grid => UnderCheckerboard(image, owned),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior)),
        };
    }

    internal static (int Width, int Height) ContainingFrame(int sourceWidth, int sourceHeight, ImageAspectRatio aspect)
    {
        var target = aspect.WidthOverHeight;
        int width;
        int height;
        if (sourceWidth / (double)sourceHeight > target)
        {
            width = sourceWidth;
            height = (int)Math.Round(sourceWidth / target);
        }
        else
        {
            height = sourceHeight;
            width = (int)Math.Round(sourceHeight * target);
        }

        if (width < sourceWidth)
        {
            width = sourceWidth;
        }

        if (height < sourceHeight)
        {
            height = sourceHeight;
        }

        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        return (width, height);
    }

    private static ImageEncode ToEncode(ImageEncoding encoding)
    {
        return new ImageEncode
        {
            AutoOrient = encoding.AutoOrient,
            Metadata = encoding.Metadata,
            Quality = encoding.QualityOrDefault(),
            AlphaFallbackColor = encoding.AlphaFallbackOrDefault(),
        };
    }

    private static ImageSize ExecuteOrdered(
        Stream source,
        Stream destination,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        ImageOutputFormat format,
        ImageEncode encode,
        CancellationToken cancellationToken = default)
    {
        VipsRuntime.EnsureInitialized();

        try
        {
            var owned = new List<IDisposable>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = PrepareInput(source, out var loaderOptions);
                var image = LoadOriented(input, encode.AutoOrient, loaderOptions, owned);
                var alphaApplied = false;
                for (var i = 0; i < ops.Length; i++)
                {
                    switch (ops[i])
                    {
                        case PipelineOp.RotateOp rotate:
                            if (rotate.Bounds == RotationBounds.Expand && !alphaApplied)
                            {
                                image = ApplySourceAlpha(image, sourceAlpha, owned);
                                alphaApplied = true;
                            }

                            image = Turn(
                                image,
                                rotate.Rotation,
                                rotate.Bounds,
                                rotate.Aspect,
                                rotate.Canvas ?? ImageCanvas.Transparent,
                                owned);
                            image = ClearOrientation(image, owned);
                            break;

                        case PipelineOp.ResizeOp resize:
                            var plan = new Plan
                            {
                                Fit = resize.Fit,
                                Width = resize.Width,
                                Height = resize.Height,
                                Aspect = resize.Aspect,
                                Enlarge = resize.Enlarge,
                                Canvas = resize.Canvas,
                                SourceAlpha = alphaApplied ? AlphaBehavior.Preserve : sourceAlpha,
                                Focus = resize.Focus,
                                AutoOrient = false,
                                Metadata = encode.Metadata,
                                Quality = encode.Quality,
                                Fallback = encode.AlphaFallbackColor,
                            };
                            image = BuildLoaded(image, plan, owned);
                            image = Finish(image, plan, owned);
                            alphaApplied = true;
                            break;
                    }
                }

                if (!alphaApplied)
                {
                    image = ApplySourceAlpha(image, sourceAlpha, owned);
                }

                var size = new ImageSize(image.Width, image.Height);
                var writePlan = new Plan
                {
                    AutoOrient = encode.AutoOrient,
                    Metadata = encode.Metadata,
                    Quality = encode.Quality,
                    Fallback = encode.AlphaFallbackColor,
                };
                Write(image, destination, format, writePlan, cancellationToken);
                return size;
            }
            finally
            {
                for (var i = owned.Count - 1; i >= 0; i--)
                {
                    owned[i].Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (ImageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is VipsException or DllNotFoundException or TypeInitializationException)
        {
            throw new ImageException("Image transform failed. " + ex.Message, ex);
        }
    }

    private static Image Build(
        Stream source,
        Plan transform,
        string? loaderOptions,
        List<IDisposable> owned)
    {
        if (transform.Width is null && transform.Height is null)
        {
            if (transform.Aspect is ImageAspectRatio aspect && transform.Fit == ImageFit.Zoom)
            {
                return transform.Focus is null
                    ? NativeZoom(source, aspect, transform, loaderOptions, owned)
                    : CropToFocus(source, transform, loaderOptions, owned);
            }

            if (transform.Aspect is not null && transform.Fit != ImageFit.Fit)
            {
                throw new ArgumentOutOfRangeException(nameof(transform));
            }

            return LoadOriented(source, transform.AutoOrient, loaderOptions, owned);
        }

        switch (transform.Fit)
        {
            case ImageFit.Fit:
                return Fit(source, transform, loaderOptions, owned);

            case ImageFit.Stretch:
                return Thumbnail(
                    source,
                    transform.Width!.Value,
                    transform.Height!.Value,
                    Enums.Size.Force,
                    crop: null,
                    transform.AutoOrient,
                    loaderOptions,
                    owned);

            case ImageFit.Zoom:
                if (transform.Focus is not null)
                {
                    return CropToFocus(source, transform, loaderOptions, owned);
                }

                return transform.Enlarge
                    ? Zoom(source, transform, loaderOptions, owned)
                    : Min(source, transform, loaderOptions, owned);

            default:
                throw new ArgumentOutOfRangeException(nameof(transform));
        }
    }

    private static Image BuildLoaded(Image image, Plan transform, List<IDisposable> owned)
    {
        if (transform.Width is null && transform.Height is null)
        {
            if (transform.Aspect is ImageAspectRatio aspect && transform.Fit == ImageFit.Zoom)
            {
                return transform.Focus is null
                    ? NativeZoomLoaded(image, aspect, owned)
                    : CropToFocusLoaded(image, transform, owned);
            }

            if (transform.Aspect is not null && transform.Fit != ImageFit.Fit)
            {
                throw new ArgumentOutOfRangeException(nameof(transform));
            }

            return image;
        }

        switch (transform.Fit)
        {
            case ImageFit.Fit:
                return FitLoaded(
                    image,
                    transform.Width ?? UnboundedDimension,
                    transform.Height ?? UnboundedDimension,
                    transform.Enlarge ? Enums.Size.Both : Enums.Size.Down,
                    crop: null,
                    owned);

            case ImageFit.Stretch:
                return FitLoaded(
                    image,
                    transform.Width!.Value,
                    transform.Height!.Value,
                    Enums.Size.Force,
                    crop: null,
                    owned);

            case ImageFit.Zoom:
                if (transform.Focus is not null)
                {
                    return CropToFocusLoaded(image, transform, owned);
                }

                return transform.Enlarge
                    ? FitLoaded(
                        image,
                        transform.Width!.Value,
                        transform.Height!.Value,
                        Enums.Size.Both,
                        Enums.Interesting.Centre,
                        owned)
                    : MinLoaded(image, transform, owned);

            default:
                throw new ArgumentOutOfRangeException(nameof(transform));
        }
    }

    private static Image Fit(
        Stream source,
        Plan transform,
        string? loaderOptions,
        List<IDisposable> owned)
    {
        return Thumbnail(
            source,
            transform.Width ?? UnboundedDimension,
            transform.Height ?? UnboundedDimension,
            transform.Enlarge ? Enums.Size.Both : Enums.Size.Down,
            crop: null,
            transform.AutoOrient,
            loaderOptions,
            owned);
    }

    private static Image Zoom(Stream source, Plan transform, string? loaderOptions, List<IDisposable> owned)
    {
        return Thumbnail(
            source,
            transform.Width!.Value,
            transform.Height!.Value,
            Enums.Size.Both,
            Enums.Interesting.Centre,
            transform.AutoOrient,
            loaderOptions,
            owned);
    }

    private static Image NativeZoom(
        Stream source,
        ImageAspectRatio aspect,
        Plan transform,
        string? loaderOptions,
        List<IDisposable> owned)
    {
        var oriented = LoadOriented(source, transform.AutoOrient, loaderOptions, owned);
        return NativeZoomLoaded(oriented, aspect, owned);
    }

    private static Image NativeZoomLoaded(Image oriented, ImageAspectRatio aspect, List<IDisposable> owned)
    {
        var (cropWidth, cropHeight) = AspectWindow(oriented.Width, oriented.Height, aspect.WidthOverHeight);
        return TakeCenter(oriented, cropWidth, cropHeight, owned);
    }

    private static Image Min(Stream source, Plan transform, string? loaderOptions, List<IDisposable> owned)
    {
        var oriented = LoadOriented(source, transform.AutoOrient, loaderOptions, owned);
        return MinLoaded(oriented, transform, owned);
    }

    private static Image MinLoaded(Image oriented, Plan transform, List<IDisposable> owned)
    {
        var width = transform.Width!.Value;
        var height = transform.Height!.Value;
        var (cropWidth, cropHeight) = AspectWindow(oriented.Width, oriented.Height, width, height);
        var window = TakeCenter(oriented, cropWidth, cropHeight, owned);
        if (window.Width <= width && window.Height <= height)
        {
            return window;
        }

        var resized = window.ThumbnailImage(width, height: height, size: Enums.Size.Down, noRotate: true);
        owned.Add(resized);
        return resized;
    }

    private static Image CropToFocus(Stream source, Plan plan, string? loaderOptions, List<IDisposable> owned)
    {
        var oriented = LoadOriented(source, plan.AutoOrient, loaderOptions, owned);
        return CropToFocusLoaded(oriented, plan, owned);
    }

    private static Image CropToFocusLoaded(Image oriented, Plan plan, List<IDisposable> owned)
    {
        var ratio = plan.Width is int frameWidth && plan.Height is int frameHeight
            ? frameWidth / (double)frameHeight
            : plan.Aspect!.Value.WidthOverHeight;
        var window = FocusMath.Window(oriented.Width, oriented.Height, ratio, plan.Focus!.Value);
        var cropped = Take(oriented, window.Left, window.Top, window.Width, window.Height, owned);
        plan.Pad = window.Pad;
        if (plan.Width is not int width || plan.Height is not int height)
        {
            return cropped;
        }

        if (!plan.Enlarge && cropped.Width <= width && cropped.Height <= height)
        {
            return cropped;
        }

        var size = plan.Enlarge ? Enums.Size.Both : Enums.Size.Down;
        return Scale(cropped, width, height, size, owned);
    }

    private static Image Scale(Image image, int width, int height, Enums.Size size, List<IDisposable> owned)
    {
        if (image.Width == width && image.Height == height)
        {
            return image;
        }

        var resized = image.ThumbnailImage(width, height: height, size: size, noRotate: true);
        owned.Add(resized);
        return resized;
    }

    private static Image Thumbnail(
        Stream source,
        int width,
        int height,
        Enums.Size size,
        Enums.Interesting? crop,
        bool autoOrient,
        string? loaderOptions,
        List<IDisposable> owned)
    {
        var vipsSource = new VipsStreamSource(source);
        owned.Add(vipsSource);
        var image = Image.ThumbnailSource(
            vipsSource,
            width,
            optionString: loaderOptions,
            height: height,
            size: size,
            noRotate: !autoOrient,
            crop: crop,
            failOn: Enums.FailOn.Error);
        owned.Add(image);
        return image;
    }

    private static Image TakeCenter(Image image, int cropWidth, int cropHeight, List<IDisposable> owned)
    {
        var left = (image.Width - cropWidth) / 2;
        var top = (image.Height - cropHeight) / 2;
        return Take(image, left, top, cropWidth, cropHeight, owned);
    }

    private static Image Take(Image image, int left, int top, int width, int height, List<IDisposable> owned)
    {
        if (left == 0 && top == 0 && width == image.Width && height == image.Height)
        {
            return image;
        }

        var cropped = image.ExtractArea(left, top, width, height);
        owned.Add(cropped);
        return cropped;
    }

    private static Image Finish(Image image, Plan plan, List<IDisposable> owned)
    {
        image = ApplySourceAlpha(image, plan.SourceAlpha ?? AlphaBehavior.Preserve, owned);
        var canvas = plan.Canvas;
        if (plan.Fit == ImageFit.Zoom)
        {
            if (!plan.Pad)
            {
                return image;
            }

            canvas ??= ImageCanvas.Transparent;
        }
        else if (canvas is null)
        {
            return image;
        }

        int width;
        int height;
        if (plan.Width is int frameWidth && plan.Height is int frameHeight)
        {
            width = frameWidth;
            height = frameHeight;
        }
        else if (plan.Aspect is ImageAspectRatio aspect)
        {
            (width, height) = ContainingFrame(image.Width, image.Height, aspect);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(plan));
        }

        return PlaceOnCanvas(image, width, height, canvas, owned);
    }

    private static Image PlaceOnCanvas(Image image, int width, int height, ImageCanvas canvas, List<IDisposable> owned)
    {
        var clear = canvas is ImageCanvas.Clear;
        if (image.Width == width && image.Height == height && (clear || !image.HasAlpha()))
        {
            return image;
        }

        Image backdrop;
        if (clear)
        {
            backdrop = SolidRgba(width, height, 0, 0, 0, 0, owned);
        }
        else if (canvas is ImageCanvas.Paint paint)
        {
            backdrop = SolidRgb(width, height, paint.Color.R, paint.Color.G, paint.Color.B, owned);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(canvas));
        }

        backdrop = Adopt(backdrop, image, owned);
        if (!image.HasAlpha() && backdrop.HasAlpha())
        {
            image = image.Bandjoin(new[] { 255d });
            owned.Add(image);
        }

        var x = (width - image.Width) / 2;
        var y = (height - image.Height) / 2;
        var composed = backdrop.Composite2(image, Enums.BlendMode.Over, x, y);
        owned.Add(composed);
        return composed;
    }

    private static Image UnderCheckerboard(Image image, List<IDisposable> owned)
    {
        if (!image.HasAlpha())
        {
            return image;
        }

        var plate = CheckerPlate(image.Width, image.Height, owned);
        plate = Adopt(plate, image, owned);
        var composed = plate.Composite2(image, Enums.BlendMode.Over, 0, 0);
        owned.Add(composed);
        return composed;
    }

    private static Image CheckerPlate(int width, int height, List<IDisposable> owned)
    {
        var light = SolidRgb(CheckerCell, CheckerCell, 255, 255, 255, owned);
        var dark = SolidRgb(CheckerCell, CheckerCell, 204, 204, 204, owned);
        var top = light.Join(dark, Enums.Direction.Horizontal);
        owned.Add(top);
        var bottom = dark.Join(light, Enums.Direction.Horizontal);
        owned.Add(bottom);
        var tile = top.Join(bottom, Enums.Direction.Vertical);
        owned.Add(tile);
        var across = Math.Max(1, (width + tile.Width - 1) / tile.Width);
        var down = Math.Max(1, (height + tile.Height - 1) / tile.Height);
        var sheet = tile.Replicate(across, down);
        owned.Add(sheet);
        if (sheet.Width == width && sheet.Height == height)
        {
            return sheet;
        }

        var cropped = sheet.ExtractArea(0, 0, width, height);
        owned.Add(cropped);
        return cropped;
    }

    private static Image Adopt(Image plate, Image image, List<IDisposable> owned)
    {
        if (image.HasAlpha() && plate.Bands == image.Bands - 1)
        {
            plate = plate.Bandjoin(new[] { 255d });
            owned.Add(plate);
        }

        if (plate.Interpretation != image.Interpretation)
        {
            var tagged = plate.Copy(interpretation: image.Interpretation);
            owned.Add(tagged);
            plate = tagged;
        }

        return plate;
    }

    private static Image SolidRgb(int width, int height, byte red, byte green, byte blue, List<IDisposable> owned)
    {
        var black = Image.Black(width, height);
        owned.Add(black);
        var filled = black.NewFromImage(new[] { (double)red, green, blue });
        owned.Add(filled);
        return filled;
    }

    private static Image SolidRgba(int width, int height, byte red, byte green, byte blue, byte alpha, List<IDisposable> owned)
    {
        var rgb = SolidRgb(width, height, red, green, blue, owned);
        var rgba = rgb.Bandjoin(new[] { (double)alpha });
        owned.Add(rgba);
        return rgba;
    }

    private static void Write(
        Image image,
        Stream destination,
        ImageOutputFormat format,
        Plan options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keep = ToKeep(options.Metadata);
        var working = image;
        Image? flattened = null;
        try
        {
            if (format == ImageOutputFormat.Jpeg && working.HasAlpha())
            {
                flattened = working.Flatten(background: new[] { (double)options.Fallback.R, options.Fallback.G, options.Fallback.B });
                working = flattened;
            }

            if (cancellationToken.CanBeCanceled)
            {
                // NetVips returns early when progress is null, so a no-op is required to arm kill.
                working.SetProgress(NoOpProgress.Instance, cancellationToken);
            }

            switch (format)
            {
                case ImageOutputFormat.Jpeg:
                    working.JpegsaveStream(destination, q: options.Quality, keep: keep, optimizeCoding: true);
                    break;
                case ImageOutputFormat.Png:
                    working.PngsaveStream(destination, keep: keep);
                    break;
                case ImageOutputFormat.WebP:
                    working.WebpsaveStream(destination, q: options.Quality, keep: keep);
                    break;
                case ImageOutputFormat.Avif:
                    working.HeifsaveStream(
                        destination,
                        q: options.Quality,
                        compression: Enums.ForeignHeifCompression.Av1,
                        keep: keep);
                    break;
                case ImageOutputFormat.Heic:
                    working.HeifsaveStream(
                        destination,
                        q: options.Quality,
                        compression: Enums.ForeignHeifCompression.Hevc,
                        keep: keep);
                    break;
                case ImageOutputFormat.Gif:
                    working.GifsaveStream(destination, keep: keep);
                    break;
                case ImageOutputFormat.Tiff:
                    working.TiffsaveStream(destination, keep: keep);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
        finally
        {
            flattened?.Dispose();
        }
    }

    private static Stream PrepareInput(Stream source, out string? loaderOptions)
    {
        var header = new byte[16];
        if (source.CanSeek)
        {
            var origin = source.Position;
            var read = ReadSome(source, header);
            source.Position = origin;
            loaderOptions = LoaderOptions(Classify(header, read));
            return source;
        }

        var count = ReadSome(source, header);
        loaderOptions = LoaderOptions(Classify(header, count));
        return new PrefixStream(header, count, source);
    }

    private static string? LoaderOptions(ContainerKind kind)
    {
        // GIF and TIFF load every frame as a tall strip unless the caller limits the page count.
        // HEIF is left alone: a still HEIC can be stored as a tile grid, and n=1 would keep one tile.
        if (kind is ContainerKind.Gif or ContainerKind.Tiff)
        {
            return "[n=1]";
        }

        return null;
    }

    private static ContainerKind Classify(byte[] header, int length)
    {
        if (length >= 6
            && header[0] == (byte)'G'
            && header[1] == (byte)'I'
            && header[2] == (byte)'F')
        {
            return ContainerKind.Gif;
        }

        if (length >= 4
            && ((header[0] == (byte)'I' && header[1] == (byte)'I' && header[2] == 42 && header[3] == 0)
                || (header[0] == (byte)'M' && header[1] == (byte)'M' && header[2] == 0 && header[3] == 42)))
        {
            return ContainerKind.Tiff;
        }

        return ContainerKind.Other;
    }

    private static int ReadSome(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        return offset;
    }

    private static (int Width, int Height) AspectWindow(int sourceWidth, int sourceHeight, int boxWidth, int boxHeight)
    {
        return AspectWindow(sourceWidth, sourceHeight, boxWidth / (double)boxHeight);
    }

    private static (int Width, int Height) AspectWindow(int sourceWidth, int sourceHeight, double widthOverHeight)
    {
        return FocusMath.Cover(sourceWidth, sourceHeight, widthOverHeight);
    }

    private static Enums.ForeignKeep ToKeep(MetadataPolicy policy)
    {
        switch (policy)
        {
            case MetadataPolicy.Strip:
                return Enums.ForeignKeep.None;
            case MetadataPolicy.PreserveColorProfileOnly:
                return Enums.ForeignKeep.Icc;
            case MetadataPolicy.Preserve:
                return Enums.ForeignKeep.All;
            default:
                throw new ArgumentOutOfRangeException(nameof(policy));
        }
    }

    private static Image Turn(
        Image image,
        ImageRotation rotation,
        RotationBounds bounds,
        ImageAspectRatio? aspect,
        ImageCanvas canvas,
        List<IDisposable> owned)
    {
        var sourceWidth = image.Width;
        var sourceHeight = image.Height;
        var turned = rotation.ClockwiseDegrees switch
        {
            0d => image,
            90d => Track(image.Rot90(), owned),
            180d => Track(image.Rot180(), owned),
            270d => Track(image.Rot270(), owned),
            _ => RotateFree(
                image,
                rotation.ClockwiseDegrees,
                bounds == RotationBounds.Expand ? canvas : ImageCanvas.SolidBlack,
                owned),
        };

        if (bounds != RotationBounds.Trim)
        {
            return turned;
        }

        var (cropWidth, cropHeight) = RotationMath.Inscribed(
            sourceWidth,
            sourceHeight,
            rotation.ClockwiseDegrees,
            aspect?.WidthOverHeight);
        if (rotation.ClockwiseDegrees is not 0d and not 90d and not 180d and not 270d)
        {
            (cropWidth, cropHeight) = RotationMath.ResampledCrop(cropWidth, cropHeight, aspect?.WidthOverHeight);
        }

        if (cropWidth > turned.Width)
        {
            cropWidth = turned.Width;
        }

        if (cropHeight > turned.Height)
        {
            cropHeight = turned.Height;
        }

        return TakeCenter(turned, cropWidth, cropHeight, owned);
    }

    private static Image RotateFree(Image image, double degrees, ImageCanvas corners, List<IDisposable> owned)
    {
        if (corners is ImageCanvas.Clear)
        {
            image = EnsureAlpha(image, owned);
        }

        var premultiplied = false;
        if (image.HasAlpha())
        {
            var premultipliedImage = image.Premultiply();
            owned.Add(premultipliedImage);
            image = premultipliedImage;
            premultiplied = true;
        }

        var rotated = image.Rotate(degrees, background: CornerColor(image, corners));
        owned.Add(rotated);
        if (!premultiplied)
        {
            return rotated;
        }

        var unpremultiplied = rotated.Unpremultiply();
        owned.Add(unpremultiplied);
        return unpremultiplied;
    }

    private static Image EnsureAlpha(Image image, List<IDisposable> owned)
    {
        if (image.HasAlpha())
        {
            return image;
        }

        var interpretation = image.Interpretation;
        var withAlpha = image.Bandjoin(new[] { 255d });
        owned.Add(withAlpha);
        if (withAlpha.Interpretation == interpretation)
        {
            return withAlpha;
        }

        var tagged = withAlpha.Copy(interpretation: interpretation);
        owned.Add(tagged);
        return tagged;
    }

    private static double[] CornerColor(Image image, ImageCanvas corners)
    {
        var color = new double[image.Bands];
        if (corners is ImageCanvas.Clear)
        {
            return color;
        }

        if (corners is not ImageCanvas.Paint paint)
        {
            throw new ArgumentOutOfRangeException(nameof(corners));
        }

        var colorBands = image.HasAlpha() ? image.Bands - 1 : image.Bands;
        if (colorBands > 0)
        {
            color[0] = paint.Color.R;
        }

        if (colorBands > 1)
        {
            color[1] = paint.Color.G;
        }

        if (colorBands > 2)
        {
            color[2] = paint.Color.B;
        }

        if (image.HasAlpha())
        {
            color[image.Bands - 1] = 255d;
        }

        return color;
    }

    private static Image ClearOrientation(Image image, List<IDisposable> owned)
    {
        var cleared = image.Mutate(mutable => mutable.Set(GValue.GIntType, "orientation", 1));
        owned.Add(cleared);
        return cleared;
    }

    private static Image Track(Image image, List<IDisposable> owned)
    {
        owned.Add(image);
        return image;
    }

    private sealed class Plan
    {
        public ImageFit Fit { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public ImageAspectRatio? Aspect { get; init; }

        public bool Enlarge { get; init; }

        public ImageCanvas? Canvas { get; init; }

        public AlphaBehavior? SourceAlpha { get; init; }

        public ImageFocus? Focus { get; init; }

        public bool Pad { get; set; }

        public bool AutoOrient { get; init; }

        public MetadataPolicy Metadata { get; init; }

        public int Quality { get; init; }

        public ImageColor Fallback { get; init; }
    }
}
