using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using jaytwo.Ergonomics.Images.Metadata;
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
        WebP,
    }

    public static ImageMetadata Identify(Stream source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        VipsRuntime.EnsureInitialized();

        try
        {
            var owned = new List<IDisposable>();
            try
            {
                var input = PrepareInput(source, out var loaderOptions, out var headerPeek);
                var vipsSource = new VipsStreamSource(input);
                owned.Add(vipsSource);
                var image = Image.NewFromSource(
                    vipsSource,
                    loaderOptions ?? string.Empty,
                    access: Enums.Access.Sequential,
                    failOn: Enums.FailOn.Error);
                owned.Add(image);
                return MetadataReader.FromImage(image, headerPeek);
            }
            finally
            {
                for (var i = owned.Count - 1; i >= 0; i--)
                {
                    owned[i].Dispose();
                }
            }
        }
        catch (ImageException)
        {
            throw;
        }
        catch (Exception ex) when (ex is VipsException or DllNotFoundException or TypeInitializationException)
        {
            throw new ImageException("Image identify failed. " + ex.Message, ex);
        }
    }

    public static ImageSize ExecutePipeline(
        Stream source,
        Stream destination,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        bool autoOrient,
        ImageEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        var format = encoding.Format;

        if (ops.Length == 1 && ops[0] is PipelineOp.ResizeOp resize)
        {
            return Execute(
                source,
                destination,
                format,
                resize.Mode,
                resize.Width,
                resize.Height,
                resize.Aspect,
                resize.Enlarge,
                resize.Canvas,
                sourceAlpha,
                autoOrient,
                resize.Focus,
                encoding,
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
                autoOrient,
                encoding,
                cancellationToken);
        }

        if (ops.Length == 1 && ops[0] is PipelineOp.CropOp crop)
        {
            return ExecuteCrop(
                source,
                destination,
                format,
                crop.X,
                crop.Y,
                crop.Width,
                crop.Height,
                sourceAlpha,
                autoOrient,
                encoding,
                cancellationToken);
        }

        if (ops.Length == 0)
        {
            return Execute(
                source,
                destination,
                format,
                ResizeMode.Fit,
                width: null,
                height: null,
                aspect: null,
                enlarge: false,
                canvas: null,
                sourceAlpha,
                autoOrient,
                focus: null,
                encoding,
                cancellationToken);
        }

        return ExecuteOrdered(
            source,
            destination,
            ops,
            sourceAlpha,
            autoOrient,
            format,
            encoding,
            cancellationToken);
    }

    public static ImageSize Execute(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        ResizeMode mode,
        int? width,
        int? height,
        ImageAspectRatio? aspect,
        bool enlarge,
        ImageCanvas? canvas,
        AlphaBehavior sourceAlpha,
        bool autoOrient,
        ImageFocus? focus,
        ImageEncoding encoding,
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
                    Mode = mode,
                    Width = width,
                    Height = height,
                    Aspect = aspect,
                    Enlarge = enlarge,
                    Canvas = canvas,
                    Focus = focus,
                    Metadata = encoding.Metadata,
                    Quality = encoding.QualityOrDefault(),
                    Fallback = encoding.AlphaFallbackOrDefault(),
                };
                var image = LoadOriented(input, autoOrient, loaderOptions, owned);
                image = ApplySourceAlpha(image, sourceAlpha, owned);
                image = Finish(BuildLoaded(image, plan, owned), plan, owned);
                var size = new ImageSize(image.Width, image.Height);
                Write(image, destination, format, WritePlan(encoding), cancellationToken);
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
        ImageEncoding encoding,
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
                var writePlan = WritePlan(encoding);
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
        bool autoOrient,
        ImageEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        return ExecuteBuilt(
            source,
            destination,
            format,
            encoding,
            (input, loaderOptions, owned) =>
            {
                var image = LoadOriented(input, autoOrient, loaderOptions, owned);
                image = ApplySourceAlpha(image, sourceAlpha, owned);
                image = Turn(image, rotation, bounds, aspect, canvas ?? ImageCanvas.Transparent, owned);
                return ClearOrientation(image, owned);
            },
            cancellationToken);
    }

    internal static ImageSize ExecuteCrop(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        double x,
        double y,
        double width,
        double height,
        AlphaBehavior sourceAlpha,
        bool autoOrient,
        ImageEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        return ExecuteBuilt(
            source,
            destination,
            format,
            encoding,
            (input, loaderOptions, owned) =>
            {
                var image = LoadOriented(input, autoOrient, loaderOptions, owned);
                image = ApplySourceAlpha(image, sourceAlpha, owned);
                image = CropRelative(image, x, y, width, height, owned);
                return image;
            },
            cancellationToken);
    }

    /// <summary>
    /// Opens with sequential access (works for non-seekable <see cref="PrefixStream"/> /
    /// ASP.NET bodies). Autorot may transpose EXIF tags 5–8; libvips buffers as needed.
    /// Covered by non-seekable orientation tests.
    /// </summary>
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

    private static Plan WritePlan(ImageEncoding encoding)
    {
        return new Plan
        {
            Metadata = encoding.Metadata,
            Quality = encoding.QualityOrDefault(),
            Lossless = encoding.WebPLosslessOrDefault(),
            Fallback = encoding.AlphaFallbackOrDefault(),
        };
    }

    private static ImageSize ExecuteOrdered(
        Stream source,
        Stream destination,
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        bool autoOrient,
        ImageOutputFormat format,
        ImageEncoding encoding,
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
                var image = LoadOriented(input, autoOrient, loaderOptions, owned);
                image = ApplySourceAlpha(image, sourceAlpha, owned);
                for (var i = 0; i < ops.Length; i++)
                {
                    switch (ops[i])
                    {
                        case PipelineOp.RotateOp rotate:
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
                                Mode = resize.Mode,
                                Width = resize.Width,
                                Height = resize.Height,
                                Aspect = resize.Aspect,
                                Enlarge = resize.Enlarge,
                                Canvas = resize.Canvas,
                                Focus = resize.Focus,
                                Metadata = encoding.Metadata,
                                Quality = encoding.QualityOrDefault(),
                                Fallback = encoding.AlphaFallbackOrDefault(),
                            };
                            image = BuildLoaded(image, plan, owned);
                            image = Finish(image, plan, owned);
                            break;

                        case PipelineOp.CropOp crop:
                            image = CropRelative(image, crop.X, crop.Y, crop.Width, crop.Height, owned);
                            break;
                    }
                }

                var size = new ImageSize(image.Width, image.Height);
                Write(image, destination, format, WritePlan(encoding), cancellationToken);
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

    private static Image BuildLoaded(Image image, Plan transform, List<IDisposable> owned)
    {
        if (transform.Width is null && transform.Height is null)
        {
            if (transform.Aspect is ImageAspectRatio aspect && transform.Mode == ResizeMode.Crop)
            {
                return transform.Focus is null
                    ? NativeZoomLoaded(image, aspect, owned)
                    : CropToFocusLoaded(image, transform, owned);
            }

            if (transform.Aspect is not null
                && transform.Mode != ResizeMode.Fit
                && transform.Mode != ResizeMode.Pad)
            {
                throw new ArgumentOutOfRangeException(nameof(transform));
            }

            return image;
        }

        switch (transform.Mode)
        {
            case ResizeMode.Fit:
            case ResizeMode.Pad:
                return FitLoaded(
                    image,
                    transform.Width ?? UnboundedDimension,
                    transform.Height ?? UnboundedDimension,
                    transform.Enlarge ? Enums.Size.Both : Enums.Size.Down,
                    crop: null,
                    owned);

            case ResizeMode.Stretch:
                return FitLoaded(
                    image,
                    transform.Width!.Value,
                    transform.Height!.Value,
                    Enums.Size.Force,
                    crop: null,
                    owned);

            case ResizeMode.Crop:
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

    private static Image NativeZoomLoaded(Image oriented, ImageAspectRatio aspect, List<IDisposable> owned)
    {
        var (cropWidth, cropHeight) = AspectWindow(oriented.Width, oriented.Height, aspect.WidthOverHeight);
        return TakeCenter(oriented, cropWidth, cropHeight, owned);
    }

    private static Image MinLoaded(Image oriented, Plan transform, List<IDisposable> owned)
    {
        var width = transform.Width!.Value;
        var height = transform.Height!.Value;
        var (cropWidth, cropHeight) = AspectWindow(oriented.Width, oriented.Height, width, height);
        var window = TakeCenter(oriented, cropWidth, cropHeight, owned);
        if (window.Width <= width && window.Height <= height)
        {
            // Crop always writes the frame; Finish letterboxes the underfill.
            transform.UnderfillsFrame = window.Width < width || window.Height < height;
            return window;
        }

        var resized = window.ThumbnailImage(width, height: height, size: Enums.Size.Down, noRotate: true);
        owned.Add(resized);
        return resized;
    }

    private static Image CropToFocusLoaded(Image oriented, Plan plan, List<IDisposable> owned)
    {
        var ratio = plan.Width is int frameWidth && plan.Height is int frameHeight
            ? frameWidth / (double)frameHeight
            : plan.Aspect!.Value.WidthOverHeight;
        var window = FocusMath.Window(oriented.Width, oriented.Height, ratio, plan.Focus!.Value);
        var cropped = Take(oriented, window.Left, window.Top, window.Width, window.Height, owned);
        plan.UnderfillsFrame = window.UnderfillsFrame;
        if (plan.Width is not int width || plan.Height is not int height)
        {
            return cropped;
        }

        if (!plan.Enlarge && cropped.Width <= width && cropped.Height <= height)
        {
            plan.UnderfillsFrame = plan.UnderfillsFrame
                || cropped.Width < width
                || cropped.Height < height;
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

    private static Image CropRelative(
        Image image,
        double x,
        double y,
        double width,
        double height,
        List<IDisposable> owned)
    {
        var (left, top, cropWidth, cropHeight) = CropRect.ToPixels(image.Width, image.Height, x, y, width, height);
        return Take(image, left, top, cropWidth, cropHeight, owned);
    }

    private static Image Finish(Image image, Plan plan, List<IDisposable> owned)
    {
        var canvas = plan.Canvas;
        if (plan.Mode == ResizeMode.Fit || plan.Mode == ResizeMode.Stretch)
        {
            return image;
        }

        if (plan.Mode == ResizeMode.Crop)
        {
            if (!plan.UnderfillsFrame)
            {
                // Checkered always underplates; solid underplates preserved content alpha
                // even when the cover window already fills the frame (no invented gap).
                if (canvas is ImageCanvas.Checkered or ImageCanvas.Paint)
                {
                    return PlaceOnCanvas(image, image.Width, image.Height, canvas, owned);
                }

                return image;
            }

            canvas ??= ImageCanvas.Transparent;
        }
        else if (plan.Mode == ResizeMode.Pad)
        {
            // Pad always letterboxes to the frame; canvas is paint only.
            canvas ??= ImageCanvas.Transparent;
        }
        else if (canvas is null)
        {
            return image;
        }

        if (canvas is ImageCanvas.Checkered
            && plan.Width is null
            && plan.Height is null
            && plan.Aspect is null)
        {
            return PlaceOnCanvas(image, image.Width, image.Height, canvas, owned);
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
        else if (canvas is ImageCanvas.Checkered)
        {
            return PlaceOnCanvas(image, image.Width, image.Height, canvas, owned);
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
        var checkered = canvas is ImageCanvas.Checkered;
        if (image.Width == width && image.Height == height && !checkered && (clear || !image.HasAlpha()))
        {
            return image;
        }

        if (checkered && image.Width == width && image.Height == height && !image.HasAlpha())
        {
            return image;
        }

        RequireRgbForCanvas(image);

        Image backdrop;
        if (clear)
        {
            backdrop = SolidRgba(width, height, 0, 0, 0, 0, owned);
        }
        else if (canvas is ImageCanvas.Paint paint)
        {
            backdrop = SolidRgb(width, height, paint.Color.R, paint.Color.G, paint.Color.B, owned);
        }
        else if (checkered)
        {
            backdrop = CheckerPlate(width, height, owned);
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

    /// <summary>
    /// Canvas plates are built from <see cref="ImageColor"/> as three RGB bands. Non-RGB
    /// sources (for example greyscale) are rejected here — no silent colorspace conversion.
    /// </summary>
    private static void RequireRgbForCanvas(Image image)
    {
        var colorBands = image.HasAlpha() ? image.Bands - 1 : image.Bands;
        if (colorBands == 3)
        {
            return;
        }

        throw new ImageException(
            "Canvas plates are RGB (three color bands). This source has "
            + colorBands
            + " color band(s). Convert the source to RGB before placing it on a canvas, or omit the canvas.");
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
                    working.WebpsaveStream(
                        destination,
                        q: options.Lossless ? null : options.Quality,
                        lossless: options.Lossless,
                        keep: keep);
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
        return PrepareInput(source, out loaderOptions, out _);
    }

    private static Stream PrepareInput(Stream source, out string? loaderOptions, out byte[] headerPeek)
    {
        var header = new byte[16];
        if (source.CanSeek)
        {
            var origin = source.Position;
            var read = ReadSome(source, header);
            source.Position = origin;
            loaderOptions = LoaderOptions(Classify(header, read));
            headerPeek = TrimHeader(header, read);
            return source;
        }

        var count = ReadSome(source, header);
        loaderOptions = LoaderOptions(Classify(header, count));
        headerPeek = TrimHeader(header, count);
        return new PrefixStream(header, count, source);
    }

    private static byte[] TrimHeader(byte[] header, int length)
    {
        if (length <= 0)
        {
            return Array.Empty<byte>();
        }

        if (length == header.Length)
        {
            return header;
        }

        var trimmed = new byte[length];
        Buffer.BlockCopy(header, 0, trimmed, 0, length);
        return trimmed;
    }

    private static string? LoaderOptions(ContainerKind kind)
    {
        // GIF, TIFF, and animated WebP load every frame as a tall strip unless the caller
        // limits the page count. HEIF is left alone: a still HEIC can be stored as a tile
        // grid, and n=1 would keep one tile.
        if (kind is ContainerKind.Gif or ContainerKind.Tiff or ContainerKind.WebP)
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

        if (length >= 12
            && header[0] == (byte)'R'
            && header[1] == (byte)'I'
            && header[2] == (byte)'F'
            && header[3] == (byte)'F'
            && header[8] == (byte)'W'
            && header[9] == (byte)'E'
            && header[10] == (byte)'B'
            && header[11] == (byte)'P')
        {
            return ContainerKind.WebP;
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
        // Trim invents transparent corners that the inscribed crop removes. Black fill
        // would show as a dark edge if resample/math leaves a residual pixel.
        var expandCanvas = bounds == RotationBounds.Expand ? canvas : ImageCanvas.Transparent;

        // Solid and checkerboard Expand share one compositing model: invent transparent
        // corners, then Over the full frame so preserved content alpha sees the plate.
        // Transparent Expand leaves holes alone (no underplate).
        var underplate = bounds == RotationBounds.Expand
            && expandCanvas is ImageCanvas.Paint or ImageCanvas.Checkered;
        var freeCorners = underplate ? ImageCanvas.Transparent : expandCanvas;
        var turned = rotation.ClockwiseDegrees switch
        {
            0d => image,
            90d => Track(image.Rot90(), owned),
            180d => Track(image.Rot180(), owned),
            270d => Track(image.Rot270(), owned),
            _ => RotateFree(
                image,
                rotation.ClockwiseDegrees,
                freeCorners,
                owned),
        };

        if (bounds != RotationBounds.Trim)
        {
            if (underplate)
            {
                return PlaceOnCanvas(turned, turned.Width, turned.Height, expandCanvas, owned);
            }

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
        public ResizeMode Mode { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public ImageAspectRatio? Aspect { get; init; }

        public bool Enlarge { get; init; }

        public ImageCanvas? Canvas { get; init; }

        public ImageFocus? Focus { get; init; }

        public bool UnderfillsFrame { get; set; }

        public MetadataPolicy Metadata { get; init; }

        public int Quality { get; init; }

        public bool Lossless { get; init; }

        public ImageColor Fallback { get; init; }
    }
}
