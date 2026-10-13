using System;
using System.IO;
using System.Threading;
using jaytwo.Ergonomics.Images.Metadata;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Opinionated image transforms for application code.
/// </summary>
/// <remarks>
/// Prefer <see cref="ImagePipeline"/> for ordered composition.
/// These shortcuts compile into a pipeline and call sync <see cref="ImagePipeline.Run"/>.
/// For ASP.NET bodies use <see cref="ImagePipeline.RunAsync"/>.
/// The first call also sets the libvips operation cache size to 0 for this process. The cache retains
/// source images, which is the wrong default for unique uploads. Set it again after startup when
/// another part of the process depends on that cache.
/// </remarks>
public static class ImageTransforms
{
    private static readonly object CapabilityGate = new();
    private static ImageCapabilities? _capabilities;

    /// <summary>
    /// Gets the codecs the loaded libvips runtime can decode, and whether each
    /// <see cref="ImageOutputFormat"/> can be encoded.
    /// </summary>
    /// <remarks>
    /// Probed on first read, not on the first transform. A missing libvips throws
    /// <see cref="ImageException"/> from this getter and from Resize.
    /// </remarks>
    public static ImageCapabilities Capabilities
    {
        get
        {
            var snapshot = Volatile.Read(ref _capabilities);
            if (snapshot is not null)
            {
                return snapshot;
            }

            lock (CapabilityGate)
            {
                snapshot = _capabilities;
                if (snapshot is null)
                {
                    snapshot = ImageCapabilities.Probe();
                    Volatile.Write(ref _capabilities, snapshot);
                }

                return snapshot;
            }
        }
    }

    /// <summary>
    /// Reads structural header fields and metadata bags from <paramref name="source"/> without encoding.
    /// </summary>
    /// <remarks>
    /// Opens the image header only (no pixel decode for typical formats). Does not auto-orient;
    /// see <see cref="ImageStructure.VisualSize"/> for the size after an orientation swap.
    /// This is read-only: it does not edit EXIF, XMP, or IPTC.
    /// </remarks>
    /// <returns>Layered <see cref="ImageMetadata"/> for the source stream.</returns>
    public static ImageMetadata Identify(Stream source)
    {
        return ImageEngine.Identify(source);
    }

    /// <summary>
    /// Pads the image inside the requested size and keeps the aspect ratio (<see cref="ResizeMode.Pad"/>).
    /// </summary>
    /// <remarks>
    /// Omit <paramref name="width"/> and <paramref name="height"/> to keep the source size.
    /// Pass one side to derive the other from the aspect ratio. When both are set, the result
    /// scales inside the box and one side can be shorter. The whole image stays visible.
    /// <paramref name="enlarge"/> false (the default) leaves a source smaller than the box at its own size.
    /// Pass <paramref name="enlarge"/> true to allow upscaling.
    /// Crop, distort, a canvas, or an aspect ratio belongs on <see cref="ImageResize"/> or <see cref="ImagePipeline"/>.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Resize(
        Stream source,
        Stream destination,
        ImageEncoding encoding,
        int? width = null,
        int? height = null,
        bool enlarge = false)
    {
        return Resize(source, destination, new ImageResize(encoding)
        {
            Width = width,
            Height = height,
            Enlarge = enlarge,
        });
    }

    /// <summary>
    /// Pads the image inside <paramref name="size"/> and keeps the aspect ratio (<see cref="ResizeMode.Pad"/>).
    /// </summary>
    /// <remarks>
    /// One side of the result can be shorter. The whole image stays visible.
    /// <paramref name="enlarge"/> false (the default) leaves a source smaller than the frame at its own size.
    /// Pass <paramref name="enlarge"/> true to allow upscaling.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Resize(
        Stream source,
        Stream destination,
        ImageEncoding encoding,
        ImageSize size,
        bool enlarge = false)
    {
        return Resize(source, destination, encoding, size.Width, size.Height, enlarge);
    }

    /// <summary>
    /// Resizes with the mode, frame, and canvas on <paramref name="resize"/>.
    /// </summary>
    /// <returns>The width and height written.</returns>
    public static ImageSize Resize(Stream source, Stream destination, ImageResize resize)
    {
        if (resize is null)
        {
            throw new ArgumentNullException(nameof(resize));
        }

        return ImagePipeline.FromResize(resize).Run(source, destination);
    }

    /// <summary>
    /// Rotates the upright image and writes it to <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// Auto-orient runs first unless <see cref="ImageRotate.AutoOrient"/> is false.
    /// <paramref name="rotate"/> is then applied to that raster. The output orientation tag is cleared,
    /// so a viewer does not rotate the result again.
    /// A multiple of 90 degrees does not resample. Any other angle does.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Rotate(Stream source, Stream destination, ImageRotate rotate)
    {
        if (rotate is null)
        {
            throw new ArgumentNullException(nameof(rotate));
        }

        return ImagePipeline.FromRotate(rotate).Run(source, destination);
    }

    /// <summary>
    /// Crops a relative rectangle from the upright image and writes it to <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// Auto-orient runs first unless <see cref="ImageCrop.AutoOrient"/> is false.
    /// Coordinates match <see cref="ImageFocus"/>: fractions from 0 to 1, origin at the top left.
    /// The box must stay inside the image and have positive area.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Crop(Stream source, Stream destination, ImageCrop crop)
    {
        if (crop is null)
        {
            throw new ArgumentNullException(nameof(crop));
        }

        return ImagePipeline.FromCrop(crop).Run(source, destination);
    }

    internal static Frame Resolve(ImageResize resize) => Resolve(resize.Width, resize.Height, resize.Aspect);

    /// <summary>
    /// Resolves a pixel box and optional aspect into the frame the engine should use.
    /// Both sides plus an aspect become the largest rectangle of that ratio inside the box.
    /// </summary>
    internal static Frame Resolve(int? width, int? height, ImageAspectRatio? aspect)
    {
        if (aspect is not ImageAspectRatio ratio)
        {
            return new Frame(width, height, null);
        }

        if (width is int boxWidth && height is int boxHeight)
        {
            var fitted = FitInside(boxWidth, boxHeight, ratio);
            return new Frame(fitted.Width, fitted.Height, null);
        }

        if (width is int onlyWidth)
        {
            return new Frame(onlyWidth, Scale(onlyWidth, 1d / ratio.WidthOverHeight), null);
        }

        if (height is int onlyHeight)
        {
            return new Frame(Scale(onlyHeight, ratio.WidthOverHeight), onlyHeight, null);
        }

        return new Frame(null, null, ratio);
    }

    private static (int Width, int Height) FitInside(int boxWidth, int boxHeight, ImageAspectRatio aspect)
    {
        var target = aspect.WidthOverHeight;
        int width;
        int height;
        if (boxWidth / (double)boxHeight > target)
        {
            height = boxHeight;
            width = Scale(boxHeight, target);
        }
        else
        {
            width = boxWidth;
            height = Scale(boxWidth, 1d / target);
        }

        if (width > boxWidth)
        {
            width = boxWidth;
        }

        if (height > boxHeight)
        {
            height = boxHeight;
        }

        return (width, height);
    }

    private static int Scale(int side, double factor)
    {
        var scaled = side * factor;
        if (!double.IsFinite(scaled) || scaled < 1d || scaled >= int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(side), "The resulting side is out of range.");
        }

        return (int)Math.Round(scaled);
    }

    internal readonly record struct Frame(int? Width, int? Height, ImageAspectRatio? NativeAspect);
}
