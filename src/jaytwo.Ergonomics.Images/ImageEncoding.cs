using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Configured output encoding for <see cref="ImagePipeline.Encoding"/>.
/// </summary>
/// <remarks>
/// Each format exposes only the options that apply to it.
/// JPEG, WebP, AVIF, and HEIC take a quality. PNG, GIF, and TIFF do not.
/// Formats that cannot store alpha (JPEG) also take an <see cref="JpegEncoding.AlphaFallbackColor"/>.
/// </remarks>
public abstract record ImageEncoding
{
    private ImageEncoding()
    {
    }

    /// <summary>
    /// Gets a value indicating whether EXIF/XMP orientation is applied before geometry.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Width and height then describe the visually upright image.
    /// </remarks>
    public bool AutoOrient { get; init; } = true;

    /// <summary>
    /// Gets which metadata is written. Defaults to <see cref="MetadataPolicy.PreserveColorProfileOnly"/>.
    /// </summary>
    public MetadataPolicy Metadata { get; init; } = MetadataPolicy.PreserveColorProfileOnly;

    /// <summary>Gets the destination codec.</summary>
    public abstract ImageOutputFormat Format { get; }

    /// <summary>JPEG with lossy quality and an alpha fallback for remaining transparency.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 80.</param>
    /// <param name="alphaFallbackColor">Flatten color when alpha remains. Defaults to white.</param>
    /// <returns>A JPEG encoding.</returns>
    public static JpegEncoding Jpeg(int quality = 80, ImageColor? alphaFallbackColor = null)
        => new(quality, alphaFallbackColor ?? ImageColor.White);

    /// <summary>Lossless PNG.</summary>
    /// <returns>A PNG encoding.</returns>
    public static PngEncoding Png() => new();

    /// <summary>WebP with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 60.</param>
    /// <returns>A WebP encoding.</returns>
    public static WebPEncoding WebP(int quality = 60) => new(quality);

    /// <summary>AVIF with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 85.</param>
    /// <returns>An AVIF encoding.</returns>
    public static AvifEncoding Avif(int quality = 85) => new(quality);

    /// <summary>GIF. Animated sources are reduced to the first frame.</summary>
    /// <returns>A GIF encoding.</returns>
    public static GifEncoding Gif() => new();

    /// <summary>TIFF. Multi-page sources are reduced to the first page.</summary>
    /// <returns>A TIFF encoding.</returns>
    public static TiffEncoding Tiff() => new();

    /// <summary>HEIC with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to 85.</param>
    /// <returns>A HEIC encoding.</returns>
    public static HeicEncoding Heic(int quality = 85) => new(quality);

    internal static ImageEncoding From(ImageOutputFormat format, ImageEncode? encode)
    {
        if (encode is null)
        {
            return format switch
            {
                ImageOutputFormat.Jpeg => Jpeg(),
                ImageOutputFormat.Png => Png(),
                ImageOutputFormat.WebP => WebP(),
                ImageOutputFormat.Avif => Avif(),
                ImageOutputFormat.Gif => Gif(),
                ImageOutputFormat.Tiff => Tiff(),
                ImageOutputFormat.Heic => Heic(),
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            };
        }

        var settings = ImageTransforms.Settings(encode, nameof(encode));
        return format switch
        {
            ImageOutputFormat.Jpeg => Jpeg(settings.Quality, settings.AlphaFallbackColor) with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.Png => Png() with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.WebP => WebP(settings.Quality) with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.Avif => Avif(settings.Quality) with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.Gif => Gif() with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.Tiff => Tiff() with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            ImageOutputFormat.Heic => Heic(settings.Quality) with
            {
                AutoOrient = settings.AutoOrient,
                Metadata = settings.Metadata,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
    }

    internal void Validate()
    {
        if (!Enum.IsDefined(typeof(MetadataPolicy), Metadata))
        {
            throw new ArgumentOutOfRangeException(nameof(Metadata), "Metadata policy is not a known value.");
        }

        switch (this)
        {
            case JpegEncoding jpeg:
                RequireQuality(jpeg.Quality);
                break;
            case WebPEncoding webP:
                RequireQuality(webP.Quality);
                break;
            case AvifEncoding avif:
                RequireQuality(avif.Quality);
                break;
            case HeicEncoding heic:
                RequireQuality(heic.Quality);
                break;
        }
    }

    internal int QualityOrDefault()
    {
        return this switch
        {
            JpegEncoding jpeg => jpeg.Quality,
            WebPEncoding webP => webP.Quality,
            AvifEncoding avif => avif.Quality,
            HeicEncoding heic => heic.Quality,
            _ => 85,
        };
    }

    internal ImageColor AlphaFallbackOrDefault()
    {
        return this is JpegEncoding jpeg ? jpeg.AlphaFallbackColor : ImageColor.White;
    }

    private static void RequireQuality(int quality)
    {
        if (quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be from 1 to 100.");
        }
    }

    /// <summary>JPEG output encoding.</summary>
    public sealed record JpegEncoding : ImageEncoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JpegEncoding"/> class.
        /// </summary>
        public JpegEncoding(int quality = 80, ImageColor? alphaFallbackColor = null)
        {
            RequireQuality(quality);
            Quality = quality;
            AlphaFallbackColor = alphaFallbackColor ?? ImageColor.White;
        }

        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Jpeg;

        /// <summary>Gets the lossy quality, from 1 to 100.</summary>
        public int Quality { get; init; }

        /// <summary>
        /// Gets the color used when remaining alpha cannot be stored in JPEG.
        /// </summary>
        /// <remarks>Defaults to white.</remarks>
        public ImageColor AlphaFallbackColor { get; init; }
    }

    /// <summary>PNG output encoding.</summary>
    public sealed record PngEncoding : ImageEncoding
    {
        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Png;
    }

    /// <summary>WebP output encoding.</summary>
    public sealed record WebPEncoding : ImageEncoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebPEncoding"/> class.
        /// </summary>
        public WebPEncoding(int quality = 60)
        {
            RequireQuality(quality);
            Quality = quality;
        }

        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.WebP;

        /// <summary>Gets the lossy quality, from 1 to 100.</summary>
        public int Quality { get; init; }
    }

    /// <summary>AVIF output encoding.</summary>
    public sealed record AvifEncoding : ImageEncoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvifEncoding"/> class.
        /// </summary>
        public AvifEncoding(int quality = 85)
        {
            RequireQuality(quality);
            Quality = quality;
        }

        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Avif;

        /// <summary>Gets the lossy quality, from 1 to 100.</summary>
        public int Quality { get; init; }
    }

    /// <summary>GIF output encoding.</summary>
    public sealed record GifEncoding : ImageEncoding
    {
        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Gif;
    }

    /// <summary>TIFF output encoding.</summary>
    public sealed record TiffEncoding : ImageEncoding
    {
        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Tiff;
    }

    /// <summary>HEIC output encoding.</summary>
    public sealed record HeicEncoding : ImageEncoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeicEncoding"/> class.
        /// </summary>
        public HeicEncoding(int quality = 85)
        {
            RequireQuality(quality);
            Quality = quality;
        }

        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.Heic;

        /// <summary>Gets the lossy quality, from 1 to 100.</summary>
        public int Quality { get; init; }
    }
}
