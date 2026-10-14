using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A reusable, ordered image transform specification.
/// </summary>
/// <remarks>
/// Fluent calls append stages without reading streams.
/// Source admission is configured on <see cref="Create"/>; resize and rotate options use the same
/// lambda shape on <see cref="Resize(int?, int?, ResizeMode, Action{ImagePipelineResize})"/> and
/// <see cref="Rotate(ImageRotation, Action{ImagePipelineRotate})"/>.
/// Metadata and format options live on <see cref="Encoding"/>.
/// Native stages share one libvips graph and encode once at <see cref="Run"/> or <see cref="RunAsync"/>.
/// Example:
/// <c>ImagePipeline.Create().Resize(256, 256, ResizeMode.Crop, r => r.Focus(...)).EncodingWebP().Run(...)</c>.
/// Resize modes are Fit / Pad / Crop / Stretch.
/// </remarks>
public sealed class ImagePipeline
{
    private static readonly ImagePipeline Empty = new(
        Array.Empty<PipelineOp>(),
        AlphaBehavior.Preserve,
        autoOrient: true,
        encoding: null);

    private readonly PipelineOp[] _ops;
    private readonly AlphaBehavior _sourceAlpha;
    private readonly bool _autoOrient;
    private readonly ImageEncoding? _encoding;

    private ImagePipeline(
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        bool autoOrient,
        ImageEncoding? encoding)
    {
        _ops = ops;
        _sourceAlpha = sourceAlpha;
        _autoOrient = autoOrient;
        _encoding = encoding;
    }

    /// <summary>
    /// Starts a reusable pipeline. Auto-orient still runs as a preamble at <see cref="Run"/> unless turned off.
    /// </summary>
    /// <param name="configure">
    /// Optional source admission (<see cref="ImagePipelineSource.AutoOrient"/>,
    /// <see cref="ImagePipelineSource.SourceAlpha"/>).
    /// Omit it to auto-orient and preserve source transparency.
    /// </param>
    /// <returns>A pipeline with no geometry stages yet.</returns>
    public static ImagePipeline Create(Action<ImagePipelineSource>? configure = null)
    {
        if (configure is null)
        {
            return Empty;
        }

        var source = new ImagePipelineSource();
        configure(source);
        return new ImagePipeline(
            Array.Empty<PipelineOp>(),
            source.Behavior,
            source.AutoOrientEnabled,
            encoding: null);
    }

    /// <summary>Starts from a preset pipeline built from the same primitives.</summary>
    /// <param name="preset">A pipeline such as <see cref="ImagePresets.ThumbnailWebP"/>.</param>
    /// <returns><paramref name="preset"/>.</returns>
    public static ImagePipeline Preset(ImagePipeline preset)
    {
        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        return preset;
    }

    /// <summary>
    /// Appends a caller rotation after auto-orient. Bounds default to <see cref="RotationBounds.Expand"/>.
    /// </summary>
    /// <param name="rotation">The turn in degrees.</param>
    /// <param name="configure">
    /// Optional bounds and plate (<see cref="ImagePipelineRotate.Trim"/>,
    /// <see cref="ImagePipelineRotate.Expand"/>, <see cref="ImagePipelineRotate.Aspect"/>,
    /// <see cref="ImagePipelineRotate.Canvas"/>).
    /// </param>
    /// <returns>A pipeline that includes the rotation.</returns>
    public ImagePipeline Rotate(ImageRotation rotation, Action<ImagePipelineRotate>? configure = null)
    {
        var options = new ImagePipelineRotate();
        configure?.Invoke(options);
        var op = new PipelineOp.RotateOp(
            rotation,
            options.Bounds,
            options.AspectRatio,
            options.CanvasPlate);
        ValidateRotate(op);
        return Append(op);
    }

    /// <summary>
    /// Appends an explicit crop of a relative rectangle on the upright image.
    /// </summary>
    /// <param name="x">Left edge, as a fraction of the width.</param>
    /// <param name="y">Top edge, as a fraction of the height.</param>
    /// <param name="width">Width, as a fraction of the image width.</param>
    /// <param name="height">Height, as a fraction of the image height.</param>
    /// <returns>A pipeline that includes the crop.</returns>
    /// <remarks>
    /// Coordinates match <see cref="ImageFocus"/>: fractions from 0 to 1, origin at the top left, after auto-orient.
    /// The box must stay inside the image and have positive area. This extracts that rectangle; it is not
    /// <see cref="ResizeMode.Crop"/> (cover the frame and discard overflow).
    /// </remarks>
    public ImagePipeline Crop(double x, double y, double width, double height)
    {
        (x, y, width, height) = CropRect.Normalize(x, y, width, height);
        return Append(new PipelineOp.CropOp(x, y, width, height));
    }

    /// <summary>Appends a resize frame. Mode defaults to <see cref="ResizeMode.Fit"/>.</summary>
    /// <param name="size">Width and height of the frame.</param>
    /// <param name="mode">How the source meets the frame. Pass <see cref="ResizeMode.Crop"/> when using Focus.</param>
    /// <param name="configure">
    /// Optional aspect, focus, enlarge, and canvas (<see cref="ImagePipelineResize"/>).
    /// </param>
    /// <returns>A pipeline that includes the resize.</returns>
    /// <remarks>
    /// There is no <c>Resize(ImageSize, Action)</c> or <c>Resize(width, height, Action)</c> overload
    /// that silently picks a mode: Focus, canvas, and other mode-specific options require an explicit
    /// <paramref name="mode"/>.
    /// </remarks>
    public ImagePipeline Resize(
        ImageSize size,
        ResizeMode mode = ResizeMode.Fit,
        Action<ImagePipelineResize>? configure = null)
        => Resize(size.Width, size.Height, mode, configure);

    /// <summary>Appends a resize frame.</summary>
    /// <param name="width">Requested width in pixels.</param>
    /// <param name="height">Requested height in pixels.</param>
    /// <param name="mode">How the source meets the frame. Defaults to <see cref="ResizeMode.Fit"/>.</param>
    /// <param name="configure">
    /// Optional aspect, focus, enlarge, and canvas (<see cref="ImagePipelineResize"/>).
    /// Omit it for a plain fit/pad/crop/stretch with library defaults.
    /// When <paramref name="configure"/> is used, pass an explicit <paramref name="mode"/>
    /// (there is no Action-only overload that forces Fit).
    /// </param>
    /// <returns>A pipeline that includes the resize.</returns>
    public ImagePipeline Resize(
        int? width = null,
        int? height = null,
        ResizeMode mode = ResizeMode.Fit,
        Action<ImagePipelineResize>? configure = null)
    {
        if (width is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        if (!Enum.IsDefined(typeof(ResizeMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), "Mode is not a known value.");
        }

        var options = new ImagePipelineResize();
        configure?.Invoke(options);

        // Resolve FitInside's both-sides+aspect and clears NativeAspect; reject Stretch first.
        RejectStretchBoxWithAspect(mode, width, height, options.AspectRatio);

        var frame = options.AspectRatio is ImageAspectRatio aspect
            ? ImageTransforms.Resolve(width, height, aspect)
            : new ImageTransforms.Frame(width, height, null);

        // Stretch always writes the requested size via Size.Force; enlarge does not apply.
        var enlarge = mode != ResizeMode.Stretch && options.EnlargeEnabled;
        var op = new PipelineOp.ResizeOp(
            frame.Width,
            frame.Height,
            frame.NativeAspect,
            mode,
            options.FocusBox,
            options.CanvasPlate,
            enlarge);
        ValidateResize(op);
        return Append(op);
    }

    /// <summary>Sets the configured output encoding. Each format exposes only its valid options.</summary>
    /// <param name="encoding">Format-specific encoding, such as <see cref="ImageEncoding.Jpeg"/>.</param>
    /// <returns>A pipeline that encodes with <paramref name="encoding"/>.</returns>
    public ImagePipeline Encoding(ImageEncoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        encoding.Validate();
        return new ImagePipeline(_ops, _sourceAlpha, _autoOrient, encoding);
    }

    /// <summary>Encodes JPEG at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="ImageEncoding.DefaultJpegQuality"/>.</param>
    /// <param name="alphaFallbackColor">Flatten color when alpha remains. Defaults to white.</param>
    /// <returns>A pipeline that encodes JPEG.</returns>
    public ImagePipeline EncodingJpeg(int quality = ImageEncoding.DefaultJpegQuality, ImageColor? alphaFallbackColor = null)
        => Encoding(ImageEncoding.Jpeg(quality, alphaFallbackColor));

    /// <summary>Encodes PNG.</summary>
    /// <returns>A pipeline that encodes PNG.</returns>
    public ImagePipeline EncodingPng() => Encoding(ImageEncoding.Png());

    /// <summary>Encodes lossy WebP at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="ImageEncoding.DefaultWebPQuality"/>.</param>
    /// <returns>A pipeline that encodes WebP.</returns>
    public ImagePipeline EncodingWebP(int quality = ImageEncoding.DefaultWebPQuality) => Encoding(ImageEncoding.WebP(quality));

    /// <summary>Encodes WebP with the given compression (lossy quality or lossless).</summary>
    /// <param name="compression">Lossy or lossless.</param>
    /// <returns>A pipeline that encodes WebP.</returns>
    public ImagePipeline EncodingWebP(WebPCompression compression)
    {
        if (compression is null)
        {
            throw new ArgumentNullException(nameof(compression));
        }

        return Encoding(ImageEncoding.WebP(compression));
    }

    /// <summary>Encodes AVIF at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="ImageEncoding.DefaultAvifQuality"/>.</param>
    /// <returns>A pipeline that encodes AVIF.</returns>
    public ImagePipeline EncodingAvif(int quality = ImageEncoding.DefaultAvifQuality) => Encoding(ImageEncoding.Avif(quality));

    /// <summary>Encodes GIF.</summary>
    /// <returns>A pipeline that encodes GIF.</returns>
    public ImagePipeline EncodingGif() => Encoding(ImageEncoding.Gif());

    /// <summary>Encodes TIFF.</summary>
    /// <returns>A pipeline that encodes TIFF.</returns>
    public ImagePipeline EncodingTiff() => Encoding(ImageEncoding.Tiff());

    /// <summary>Encodes HEIC at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="ImageEncoding.DefaultHeicQuality"/>.</param>
    /// <returns>A pipeline that encodes HEIC.</returns>
    public ImagePipeline EncodingHeic(int quality = ImageEncoding.DefaultHeicQuality) => Encoding(ImageEncoding.Heic(quality));

    /// <summary>
    /// Runs the pipeline: decode, auto-orient, ordered native stages, encode once.
    /// </summary>
    /// <param name="source">Readable image stream.</param>
    /// <param name="destination">Writable destination stream.</param>
    /// <returns>The width and height written.</returns>
    public ImageSize Run(Stream source, Stream destination)
    {
        if (_encoding is null)
        {
            throw new InvalidOperationException("Set Encoding before Run.");
        }

        Validate();
        RequireStreams(source, destination);
        return ImageEngine.ExecutePipeline(source, destination, _ops, _sourceAlpha, _autoOrient, _encoding);
    }

    /// <summary>
    /// Runs the pipeline for streams that require async I/O (for example ASP.NET request/response bodies).
    /// </summary>
    /// <remarks>
    /// Native libvips work stays synchronous on a worker. Pipes bridge
    /// <paramref name="source"/> <c>ReadAsync</c> and <paramref name="destination"/> <c>WriteAsync</c>
    /// to sync NetVips reads and writes. <paramref name="cancellationToken"/> aborts evaluation via NetVips kill.
    /// </remarks>
    /// <param name="source">Readable image stream.</param>
    /// <param name="destination">Writable destination stream.</param>
    /// <param name="cancellationToken">Cancels pumps and kills the native encode.</param>
    /// <returns>The width and height written.</returns>
    public Task<ImageSize> RunAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (_encoding is null)
        {
            throw new InvalidOperationException("Set Encoding before Run.");
        }

        Validate();
        RequireStreams(source, destination);
        return AsyncStreamBridge.RunAsync(
            source,
            destination,
            _ops,
            _sourceAlpha,
            _autoOrient,
            _encoding,
            cancellationToken);
    }

    internal static ImagePipeline FromResize(ImageResize resize)
    {
        if (resize is null)
        {
            throw new ArgumentNullException(nameof(resize));
        }

        // Same order as Resize: reject Stretch+box+aspect before Resolve clears aspect.
        RejectStretchBoxWithAspect(resize.Mode, resize.Width, resize.Height, resize.Aspect);

        var frame = ImageTransforms.Resolve(resize);
        return Create(s => s.AutoOrient(resize.AutoOrient).SourceAlpha(resize.SourceAlpha))
            .Resize(frame.Width, frame.Height, resize.Mode, r =>
            {
                if (frame.NativeAspect is ImageAspectRatio aspect)
                {
                    r.Aspect(aspect);
                }

                if (resize.Focus is ImageFocus focus)
                {
                    r.Focus(focus);
                }

                if (resize.Canvas is ImageCanvas canvas)
                {
                    r.Canvas(canvas);
                }

                if (resize.Mode != ResizeMode.Stretch && resize.Enlarge)
                {
                    r.Enlarge(true);
                }
            })
            .Encoding(resize.Encoding);
    }

    internal static ImagePipeline FromRotate(ImageRotate rotate)
    {
        if (rotate is null)
        {
            throw new ArgumentNullException(nameof(rotate));
        }

        return Create(s => s.AutoOrient(rotate.AutoOrient).SourceAlpha(rotate.SourceAlpha))
            .Rotate(rotate.Rotation, r =>
            {
                if (rotate.Bounds == RotationBounds.Trim)
                {
                    r.Trim(rotate.Aspect);
                }
                else
                {
                    r.Expand();
                    if (rotate.Aspect is ImageAspectRatio aspect)
                    {
                        // Surface Expand+Aspect as the same failure Trim requires.
                        r.Aspect(aspect);
                    }
                }

                if (rotate.Canvas is ImageCanvas canvas)
                {
                    r.Canvas(canvas);
                }
            })
            .Encoding(rotate.Encoding);
    }

    internal static ImagePipeline FromCrop(ImageCrop crop)
    {
        if (crop is null)
        {
            throw new ArgumentNullException(nameof(crop));
        }

        return Create(s => s.AutoOrient(crop.AutoOrient).SourceAlpha(crop.SourceAlpha))
            .Crop(crop.X, crop.Y, crop.Width, crop.Height)
            .Encoding(crop.Encoding);
    }

    private static void ValidateRotate(PipelineOp.RotateOp rotate)
    {
        if (!Enum.IsDefined(typeof(RotationBounds), rotate.Bounds))
        {
            throw new ArgumentOutOfRangeException(nameof(rotate), "Bounds is not a known value.");
        }

        if (rotate.Aspect is ImageAspectRatio aspect && !aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(rotate), "Aspect ratio must be finite and positive.");
        }

        if (rotate.Aspect is not null && rotate.Bounds != RotationBounds.Trim)
        {
            throw new ArgumentException("An aspect ratio applies with Trim.", nameof(rotate));
        }

        if (rotate.Canvas is not null && rotate.Bounds != RotationBounds.Expand)
        {
            throw new ArgumentException("A canvas applies with Expand.", nameof(rotate));
        }
    }

    private static void ValidateResize(PipelineOp.ResizeOp resize)
    {
        if (resize.Width is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Width must be positive.");
        }

        if (resize.Height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Height must be positive.");
        }

        if (!Enum.IsDefined(typeof(ResizeMode), resize.Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Mode is not a known value.");
        }

        if (resize.Focus is not null && resize.Mode != ResizeMode.Crop)
        {
            throw new ArgumentException("A focus box applies with ResizeMode.Crop.", nameof(resize));
        }

        if (resize.Aspect is ImageAspectRatio aspect && !aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Aspect ratio must be finite and positive.");
        }

        var hasWidth = resize.Width is not null;
        var hasHeight = resize.Height is not null;
        var hasAspect = resize.Aspect is not null;
        var hasCanvas = resize.Canvas is not null;

        switch (resize.Mode)
        {
            case ResizeMode.Fit:
                if (hasCanvas)
                {
                    throw new ArgumentException(
                        "A canvas applies with Pad, or with Crop for gaps. Use Pad for an exact letterboxed frame.",
                        nameof(resize));
                }

                if (hasAspect && !hasWidth && !hasHeight)
                {
                    throw new ArgumentException("Fit to an aspect ratio needs a pixel size.", nameof(resize));
                }

                break;

            case ResizeMode.Pad:
                if (!hasAspect && (!hasWidth || !hasHeight))
                {
                    throw new ArgumentException("Pad needs a width and a height, or an aspect ratio.", nameof(resize));
                }

                break;

            case ResizeMode.Crop:
                if (!hasAspect && (!hasWidth || !hasHeight))
                {
                    throw new ArgumentException("Crop needs a width and a height, or an aspect ratio.", nameof(resize));
                }

                break;

            case ResizeMode.Stretch:
                if (hasCanvas)
                {
                    throw new ArgumentException(
                        "A canvas applies with Pad, or with Crop for gaps.",
                        nameof(resize));
                }

                RejectStretchBoxWithAspect(resize.Mode, resize.Width, resize.Height, resize.Aspect);

                if (!HasExactSize(hasWidth, hasHeight, hasAspect))
                {
                    throw new ArgumentException(
                        "Stretch needs a width and a height, or an aspect ratio and one side.",
                        nameof(resize));
                }

                break;
        }
    }

    /// <summary>
    /// Stretch takes an exact pixel box, or one side plus an aspect. Both sides plus an aspect
    /// must be rejected before frame resolve FitInside clears the aspect.
    /// </summary>
    private static void RejectStretchBoxWithAspect(
        ResizeMode mode,
        int? width,
        int? height,
        ImageAspectRatio? aspect)
    {
        if (mode == ResizeMode.Stretch && width is not null && height is not null && aspect is not null)
        {
            throw new ArgumentException("Stretch uses the width and height as the exact size.");
        }
    }

    private static bool HasExactSize(bool hasWidth, bool hasHeight, bool hasAspect)
    {
        if (hasWidth && hasHeight)
        {
            return true;
        }

        return hasAspect && (hasWidth || hasHeight);
    }

    private static void RequireStreams(Stream source, Stream destination)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (!source.CanRead)
        {
            throw new ArgumentException("The source stream must be readable.", nameof(source));
        }

        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }
    }

    private ImagePipeline Append(PipelineOp op)
    {
        var next = new PipelineOp[_ops.Length + 1];
        Array.Copy(_ops, next, _ops.Length);
        next[_ops.Length] = op;
        return new ImagePipeline(next, _sourceAlpha, _autoOrient, _encoding);
    }

    private void Validate()
    {
        foreach (var op in _ops)
        {
            switch (op)
            {
                case PipelineOp.RotateOp rotate:
                    ValidateRotate(rotate);
                    break;
                case PipelineOp.ResizeOp resize:
                    ValidateResize(resize);
                    break;
                case PipelineOp.CropOp crop:
                    CropRect.Normalize(crop.X, crop.Y, crop.Width, crop.Height);
                    break;
            }
        }
    }
}
