using System;
using System.IO;
using System.Threading;

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
    /// Fits the image inside the requested size and keeps the aspect ratio.
    /// </summary>
    /// <remarks>
    /// Omit <paramref name="width"/> and <paramref name="height"/> to keep the source size.
    /// Pass one side to derive the other from the aspect ratio. When both are set, the result
    /// fits inside the box and one side can be shorter. The whole image stays visible.
    /// <paramref name="enlarge"/> false leaves a source smaller than the box at its own size.
    /// A fit, a canvas, or an aspect ratio belongs on <see cref="ImageResize"/> or <see cref="ImagePipeline"/>.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Resize(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        int? width = null,
        int? height = null,
        bool enlarge = true,
        ImageEncode? encode = null)
    {
        return Resize(source, destination, new ImageResize(format)
        {
            Width = width,
            Height = height,
            Enlarge = enlarge,
            Encode = encode,
        });
    }

    /// <summary>
    /// Fits the image inside <paramref name="size"/> and keeps the aspect ratio.
    /// </summary>
    /// <remarks>
    /// One side of the result can be shorter. The whole image stays visible.
    /// <paramref name="enlarge"/> false leaves a source smaller than the frame at its own size.
    /// </remarks>
    /// <returns>The width and height written.</returns>
    public static ImageSize Resize(
        Stream source,
        Stream destination,
        ImageOutputFormat format,
        ImageSize size,
        bool enlarge = true,
        ImageEncode? encode = null)
    {
        return Resize(source, destination, format, size.Width, size.Height, enlarge, encode);
    }

    /// <summary>
    /// Resizes with the fit, frame, and canvas on <paramref name="resize"/>.
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
    /// Auto-orient runs first unless <see cref="ImageEncode.AutoOrient"/> is false.
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

    internal static ImageEncode Settings(ImageEncode? encode, string paramName)
    {
        var settings = encode ?? new ImageEncode();
        if (!Enum.IsDefined(typeof(MetadataPolicy), settings.Metadata))
        {
            throw new ArgumentOutOfRangeException(paramName, "Metadata policy is not a known value.");
        }

        if (settings.Quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(paramName, "Quality must be from 1 to 100.");
        }

        return settings;
    }

    internal static Frame Resolve(ImageResize resize)
    {
        if (resize.Aspect is not ImageAspectRatio aspect)
        {
            return new Frame(resize.Width, resize.Height, null);
        }

        if (resize.Width is int width && resize.Height is int height)
        {
            var fitted = FitInside(width, height, aspect);
            return new Frame(fitted.Width, fitted.Height, null);
        }

        if (resize.Width is int onlyWidth)
        {
            return new Frame(onlyWidth, Scale(onlyWidth, 1d / aspect.WidthOverHeight), null);
        }

        if (resize.Height is int onlyHeight)
        {
            return new Frame(Scale(onlyHeight, aspect.WidthOverHeight), onlyHeight, null);
        }

        return new Frame(null, null, aspect);
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
