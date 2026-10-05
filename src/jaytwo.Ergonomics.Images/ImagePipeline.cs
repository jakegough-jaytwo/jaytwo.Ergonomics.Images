using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A reusable, ordered image transform specification.
/// </summary>
/// <remarks>
/// Fluent calls append or adjust stages without reading streams.
/// Auto-orient runs first unless <see cref="ImageEncoding.AutoOrient"/> is false.
/// <see cref="SourceAlpha"/> is a policy: its place in the chain does not change when it runs.
/// Native stages share one libvips graph and encode once at <see cref="Run"/> or <see cref="RunAsync"/>.
/// Start with <see cref="Create"/>, then chain stages:
/// <c>ImagePipeline.Create().Rotate(...).Trim().Resize(...).Fit(ImageFit.Zoom).Encoding(ImageEncoding.WebP()).Run(...)</c>.
/// </remarks>
public sealed class ImagePipeline
{
    private static readonly ImagePipeline Empty = new(
        Array.Empty<PipelineOp>(),
        AlphaBehavior.Preserve,
        encoding: null);

    private readonly PipelineOp[] _ops;
    private readonly AlphaBehavior _sourceAlpha;
    private readonly ImageEncoding? _encoding;

    private ImagePipeline(
        PipelineOp[] ops,
        AlphaBehavior sourceAlpha,
        ImageEncoding? encoding)
    {
        _ops = ops;
        _sourceAlpha = sourceAlpha;
        _encoding = encoding;
    }

    /// <summary>Starts an empty reusable pipeline. Auto-orient still runs as a preamble at <see cref="Run"/>.</summary>
    /// <returns>A pipeline with no stages yet.</returns>
    public static ImagePipeline Create() => Empty;

    /// <summary>Starts from a preset pipeline built from the same primitives.</summary>
    /// <param name="preset">A pipeline such as <see cref="ImagePresets.Thumbnail"/>.</param>
    /// <returns><paramref name="preset"/>.</returns>
    public static ImagePipeline Preset(ImagePipeline preset)
    {
        if (preset is null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        return preset;
    }

    /// <summary>Appends a caller rotation after auto-orient. Bounds default to <see cref="RotationBounds.Expand"/>.</summary>
    /// <param name="rotation">The turn in degrees.</param>
    /// <returns>A pipeline that includes the rotation.</returns>
    public ImagePipeline Rotate(ImageRotation rotation)
    {
        return Append(new PipelineOp.RotateOp(rotation, RotationBounds.Expand, Aspect: null, Canvas: null));
    }

    /// <summary>Sets the preceding rotation to <see cref="RotationBounds.Expand"/>.</summary>
    /// <returns>A pipeline with expand bounds on the last rotation.</returns>
    public ImagePipeline Expand() => PatchLastRotate((rotate) => rotate with
    {
        Bounds = RotationBounds.Expand,
        Aspect = null,
    });

    /// <summary>Sets the preceding rotation to <see cref="RotationBounds.Trim"/>.</summary>
    /// <param name="aspect">Optional ratio of the centered trim rectangle.</param>
    /// <returns>A pipeline with trim bounds on the last rotation.</returns>
    public ImagePipeline Trim(ImageAspectRatio? aspect = null) => PatchLastRotate((rotate) => rotate with
    {
        Bounds = RotationBounds.Trim,
        Aspect = aspect,
        Canvas = null,
    });

    /// <summary>Appends a resize frame. Mode defaults to <see cref="ImageFit.Fit"/> until <see cref="Fit"/> is called.</summary>
    /// <param name="size">Width and height of the frame.</param>
    /// <returns>A pipeline that includes the resize.</returns>
    public ImagePipeline Resize(ImageSize size) => Resize(size.Width, size.Height);

    /// <summary>Appends a resize frame.</summary>
    /// <param name="width">Requested width in pixels.</param>
    /// <param name="height">Requested height in pixels.</param>
    /// <returns>A pipeline that includes the resize.</returns>
    public ImagePipeline Resize(int? width = null, int? height = null)
    {
        if (width is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        return Append(new PipelineOp.ResizeOp(
            width,
            height,
            Aspect: null,
            ImageFit.Fit,
            Focus: null,
            Canvas: null,
            Enlarge: true));
    }

    /// <summary>Sets how the preceding resize meets its frame.</summary>
    /// <param name="fit">Fit, zoom, or stretch.</param>
    /// <returns>A pipeline with that mode on the last resize.</returns>
    public ImagePipeline Fit(ImageFit fit)
    {
        if (!Enum.IsDefined(typeof(ImageFit), fit))
        {
            throw new ArgumentOutOfRangeException(nameof(fit), "Fit is not a known value.");
        }

        return PatchLastResize((resize) => resize with { Fit = fit });
    }

    /// <summary>Sets the output ratio on the preceding resize.</summary>
    /// <param name="aspect">Width over height.</param>
    /// <returns>A pipeline with that aspect on the last resize.</returns>
    public ImagePipeline Aspect(ImageAspectRatio aspect)
    {
        if (!aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect ratio must be finite and positive.");
        }

        return PatchLastResize((resize) => resize with { Aspect = aspect });
    }

    /// <summary>Sets the relative focus rectangle for a preceding <see cref="ImageFit.Zoom"/>.</summary>
    /// <param name="focus">Rectangle on the upright image, edges from 0 to 1.</param>
    /// <returns>A pipeline with that focus on the last resize.</returns>
    public ImagePipeline Focus(ImageFocus focus) => PatchLastResize((resize) => resize with { Focus = focus });

    /// <summary>Sets whether the preceding resize may enlarge the source.</summary>
    /// <param name="enlarge">True to allow upscaling.</param>
    /// <returns>A pipeline with that enlarge flag on the last resize.</returns>
    public ImagePipeline Enlarge(bool enlarge) => PatchLastResize((resize) => resize with { Enlarge = enlarge });

    /// <summary>
    /// Sets the canvas for the preceding fit resize, zoom focus gap, or expanded rotation.
    /// </summary>
    /// <param name="canvas">Solid color or transparent gap.</param>
    /// <returns>A pipeline with that canvas on the last compatible stage.</returns>
    public ImagePipeline Canvas(ImageCanvas canvas)
    {
        if (canvas is null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        for (var i = _ops.Length - 1; i >= 0; i--)
        {
            if (_ops[i] is PipelineOp.ResizeOp resize)
            {
                return ReplaceAt(i, resize with { Canvas = canvas });
            }

            if (_ops[i] is PipelineOp.RotateOp rotate)
            {
                return ReplaceAt(i, rotate with { Canvas = canvas, Bounds = RotationBounds.Expand, Aspect = null });
            }
        }

        throw new InvalidOperationException("Canvas needs a preceding Resize or Rotate.");
    }

    /// <summary>Sets how source transparency is resolved. Last call wins; chain position does not change timing.</summary>
    /// <param name="sourceAlpha">Preserve, flatten, or checkerboard.</param>
    /// <returns>A pipeline with that source-alpha policy.</returns>
    public ImagePipeline SourceAlpha(AlphaBehavior sourceAlpha)
    {
        if (sourceAlpha is null)
        {
            throw new ArgumentNullException(nameof(sourceAlpha));
        }

        return new ImagePipeline(_ops, sourceAlpha, _encoding);
    }

    /// <summary>Sets the configured output encoding. Each format exposes only its valid options.</summary>
    /// <param name="encoding">Format-specific encoding, such as <see cref="ImageEncoding.WebP"/>.</param>
    /// <returns>A pipeline that encodes with <paramref name="encoding"/>.</returns>
    public ImagePipeline Encoding(ImageEncoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        encoding.Validate();
        return new ImagePipeline(_ops, _sourceAlpha, encoding);
    }

    /// <summary>Encodes JPEG at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 80.</param>
    /// <param name="alphaFallbackColor">Flatten color when alpha remains. Defaults to white.</param>
    /// <returns>A pipeline that encodes JPEG.</returns>
    public ImagePipeline EncodingJpeg(int quality = 80, ImageColor? alphaFallbackColor = null)
        => Encoding(ImageEncoding.Jpeg(quality, alphaFallbackColor));

    /// <summary>Encodes PNG.</summary>
    /// <returns>A pipeline that encodes PNG.</returns>
    public ImagePipeline EncodingPng() => Encoding(ImageEncoding.Png());

    /// <summary>Encodes WebP at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 60.</param>
    /// <returns>A pipeline that encodes WebP.</returns>
    public ImagePipeline EncodingWebP(int quality = 60) => Encoding(ImageEncoding.WebP(quality));

    /// <summary>Encodes AVIF at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 85.</param>
    /// <returns>A pipeline that encodes AVIF.</returns>
    public ImagePipeline EncodingAvif(int quality = 85) => Encoding(ImageEncoding.Avif(quality));

    /// <summary>Encodes GIF.</summary>
    /// <returns>A pipeline that encodes GIF.</returns>
    public ImagePipeline EncodingGif() => Encoding(ImageEncoding.Gif());

    /// <summary>Encodes TIFF.</summary>
    /// <returns>A pipeline that encodes TIFF.</returns>
    public ImagePipeline EncodingTiff() => Encoding(ImageEncoding.Tiff());

    /// <summary>Encodes HEIC at <paramref name="quality"/>.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 85.</param>
    /// <returns>A pipeline that encodes HEIC.</returns>
    public ImagePipeline EncodingHeic(int quality = 85) => Encoding(ImageEncoding.Heic(quality));

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
        return ImageEngine.ExecutePipeline(source, destination, _ops, _sourceAlpha, _encoding);
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
        return AsyncStreamBridge.RunAsync(source, destination, _ops, _sourceAlpha, _encoding, cancellationToken);
    }

    internal static ImagePipeline FromResize(ImageResize resize)
    {
        if (resize is null)
        {
            throw new ArgumentNullException(nameof(resize));
        }

        var frame = ImageTransforms.Resolve(resize);
        var pipeline = Empty.Resize(frame.Width, frame.Height);
        if (frame.NativeAspect is ImageAspectRatio aspect)
        {
            pipeline = pipeline.Aspect(aspect);
        }

        pipeline = pipeline.Fit(resize.Fit);
        if (resize.Focus is ImageFocus focus)
        {
            pipeline = pipeline.Focus(focus);
        }

        if (resize.Canvas is ImageCanvas canvas)
        {
            pipeline = pipeline.Canvas(canvas);
        }

        if (!resize.Enlarge)
        {
            pipeline = pipeline.Enlarge(false);
        }

        return pipeline
            .SourceAlpha(resize.SourceAlpha)
            .Encoding(ImageEncoding.From(resize.Format, resize.Encode));
    }

    internal static ImagePipeline FromRotate(ImageRotate rotate)
    {
        if (rotate is null)
        {
            throw new ArgumentNullException(nameof(rotate));
        }

        // Keep the caller's combination intact so Run validation can reject Expand+Aspect / Trim+Canvas.
        var pipeline = new ImagePipeline(
            new PipelineOp[]
            {
                new PipelineOp.RotateOp(rotate.Rotation, rotate.Bounds, rotate.Aspect, rotate.Canvas),
            },
            AlphaBehavior.Preserve,
            encoding: null);

        return pipeline
            .SourceAlpha(rotate.SourceAlpha)
            .Encoding(ImageEncoding.From(rotate.Format, rotate.Encode));
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

        if (!Enum.IsDefined(typeof(ImageFit), resize.Fit))
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Fit is not a known value.");
        }

        if (resize.Focus is not null && resize.Fit != ImageFit.Zoom)
        {
            throw new ArgumentException("A focus box applies with Zoom.", nameof(resize));
        }

        if (resize.Aspect is ImageAspectRatio aspect && !aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(resize), "Aspect ratio must be finite and positive.");
        }

        var hasWidth = resize.Width is not null;
        var hasHeight = resize.Height is not null;
        var hasAspect = resize.Aspect is not null;
        var hasCanvas = resize.Canvas is not null;

        switch (resize.Fit)
        {
            case ImageFit.Fit:
                if (hasCanvas && !hasAspect && (!hasWidth || !hasHeight))
                {
                    throw new ArgumentException("A canvas needs a width and a height, or an aspect ratio.", nameof(resize));
                }

                if (!hasCanvas && hasAspect && !hasWidth && !hasHeight)
                {
                    throw new ArgumentException("Fit to an aspect ratio needs a canvas or a pixel size.", nameof(resize));
                }

                break;

            case ImageFit.Zoom:
                if (hasCanvas && resize.Focus is null)
                {
                    throw new ArgumentException("A canvas applies with Fit.", nameof(resize));
                }

                if (!hasAspect && (!hasWidth || !hasHeight))
                {
                    throw new ArgumentException("Zoom needs a width and a height, or an aspect ratio.", nameof(resize));
                }

                break;

            case ImageFit.Stretch:
                if (!resize.Enlarge)
                {
                    throw new ArgumentException("Stretch writes the requested size.", nameof(resize));
                }

                if (hasCanvas)
                {
                    throw new ArgumentException("A canvas applies with Fit.", nameof(resize));
                }

                if (hasWidth && hasHeight && hasAspect)
                {
                    throw new ArgumentException("Stretch uses the width and height as the exact size.", nameof(resize));
                }

                if (!HasExactSize(hasWidth, hasHeight, hasAspect))
                {
                    throw new ArgumentException("Stretch needs a width and a height.", nameof(resize));
                }

                break;
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
        return new ImagePipeline(next, _sourceAlpha, _encoding);
    }

    private ImagePipeline ReplaceAt(int index, PipelineOp op)
    {
        var next = new PipelineOp[_ops.Length];
        Array.Copy(_ops, next, _ops.Length);
        next[index] = op;
        return new ImagePipeline(next, _sourceAlpha, _encoding);
    }

    private ImagePipeline PatchLastRotate(Func<PipelineOp.RotateOp, PipelineOp.RotateOp> patch)
    {
        for (var i = _ops.Length - 1; i >= 0; i--)
        {
            if (_ops[i] is PipelineOp.RotateOp rotate)
            {
                return ReplaceAt(i, patch(rotate));
            }
        }

        throw new InvalidOperationException("Trim or Expand needs a preceding Rotate.");
    }

    private ImagePipeline PatchLastResize(Func<PipelineOp.ResizeOp, PipelineOp.ResizeOp> patch)
    {
        for (var i = _ops.Length - 1; i >= 0; i--)
        {
            if (_ops[i] is PipelineOp.ResizeOp resize)
            {
                return ReplaceAt(i, patch(resize));
            }
        }

        throw new InvalidOperationException("This adjustment needs a preceding Resize.");
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
            }
        }
    }
}
