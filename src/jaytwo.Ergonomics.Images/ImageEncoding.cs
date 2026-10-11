using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Configured output encoding for <see cref="ImagePipeline.Encoding"/>.
/// </summary>
/// <remarks>
/// Each format exposes only the options that apply to it.
/// JPEG, AVIF, and HEIC take a quality. WebP takes a <see cref="WebPCompression"/> (lossy quality or lossless).
/// PNG, GIF, and TIFF do not.
/// Formats that cannot store alpha (JPEG) also take an <see cref="JpegEncoding.AlphaFallbackColor"/>.
/// </remarks>
public abstract record ImageEncoding
{
    /// <summary>Default lossy quality for JPEG.</summary>
    public const int DefaultJpegQuality = 85;

    /// <summary>Default lossy quality for WebP.</summary>
    public const int DefaultWebPQuality = 85;

    /// <summary>Default lossy quality for AVIF.</summary>
    public const int DefaultAvifQuality = 85;

    /// <summary>Default lossy quality for HEIC.</summary>
    public const int DefaultHeicQuality = 85;

    private ImageEncoding()
    {
    }

    /// <summary>
    /// Gets which metadata is written. Defaults to <see cref="MetadataPolicy.PreserveColorProfileOnly"/>.
    /// </summary>
    /// <remarks>
    /// Encode policy only. Auto-orient is source admission on <see cref="ImagePipeline.Create"/>.
    /// </remarks>
    public MetadataPolicy Metadata { get; init; } = MetadataPolicy.PreserveColorProfileOnly;

    /// <summary>Gets the destination codec.</summary>
    public abstract ImageOutputFormat Format { get; }

    /// <summary>JPEG with lossy quality and an alpha fallback for remaining transparency.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="DefaultJpegQuality"/>.</param>
    /// <param name="alphaFallbackColor">Flatten color when alpha remains. Defaults to white.</param>
    /// <returns>A JPEG encoding.</returns>
    public static JpegEncoding Jpeg(int quality = DefaultJpegQuality, ImageColor? alphaFallbackColor = null)
        => new(quality, alphaFallbackColor ?? ImageColor.White);

    /// <summary>Lossless PNG.</summary>
    /// <returns>A PNG encoding.</returns>
    public static PngEncoding Png() => new();

    /// <summary>WebP with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="DefaultWebPQuality"/>.</param>
    /// <returns>A WebP encoding.</returns>
    public static WebPEncoding WebP(int quality = DefaultWebPQuality) => new(WebPCompression.Lossy(quality));

    /// <summary>WebP with the given compression (lossy quality or lossless).</summary>
    /// <param name="compression">Lossy or lossless.</param>
    /// <returns>A WebP encoding.</returns>
    public static WebPEncoding WebP(WebPCompression compression) => new(compression);

    /// <summary>AVIF with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="DefaultAvifQuality"/>.</param>
    /// <returns>An AVIF encoding.</returns>
    public static AvifEncoding Avif(int quality = DefaultAvifQuality) => new(quality);

    /// <summary>GIF. Animated sources are reduced to the first frame.</summary>
    /// <returns>A GIF encoding.</returns>
    public static GifEncoding Gif() => new();

    /// <summary>TIFF. Multi-page sources are reduced to the first page.</summary>
    /// <returns>A TIFF encoding.</returns>
    public static TiffEncoding Tiff() => new();

    /// <summary>HEIC with lossy quality.</summary>
    /// <param name="quality">Lossy quality from 1 to 100. Defaults to <see cref="DefaultHeicQuality"/>.</param>
    /// <returns>A HEIC encoding.</returns>
    public static HeicEncoding Heic(int quality = DefaultHeicQuality) => new(quality);

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
                switch (webP.Compression)
                {
                    case null:
                        throw new ArgumentException("WebP compression is required.");
                    case WebPCompression.LossyMode lossy:
                        RequireQuality(lossy.Quality);
                        break;
                }

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
            WebPEncoding { Compression: WebPCompression.LossyMode lossy } => lossy.Quality,
            AvifEncoding avif => avif.Quality,
            HeicEncoding heic => heic.Quality,
            _ => DefaultJpegQuality,
        };
    }

    internal bool WebPLosslessOrDefault() => this is WebPEncoding { Compression: WebPCompression.LosslessMode };

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
        public JpegEncoding(int quality = DefaultJpegQuality, ImageColor? alphaFallbackColor = null)
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
        /// <remarks>
        /// Defaults to white. This is the third alpha knob: encode-time only, and only for formats
        /// that cannot store alpha. It flattens whatever alpha remains after
        /// <see cref="AlphaBehavior"/> and <see cref="ImageCanvas"/> (transparent pad gaps,
        /// preserved content holes, expand corners). It does not replace a canvas plate, and a
        /// canvas does not replace this fallback when the encoder still cannot write alpha.
        /// </remarks>
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
        /// <param name="compression">Lossy quality or lossless. Defaults to lossy at <see cref="DefaultWebPQuality"/>.</param>
        public WebPEncoding(WebPCompression? compression = null)
        {
            Compression = compression ?? WebPCompression.Lossy();
        }

        /// <inheritdoc />
        public override ImageOutputFormat Format => ImageOutputFormat.WebP;

        /// <summary>Gets whether WebP is lossy or lossless.</summary>
        public WebPCompression Compression { get; init; }
    }

    /// <summary>AVIF output encoding.</summary>
    public sealed record AvifEncoding : ImageEncoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvifEncoding"/> class.
        /// </summary>
        public AvifEncoding(int quality = DefaultAvifQuality)
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
        public HeicEncoding(int quality = DefaultHeicQuality)
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
