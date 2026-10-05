namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Orientation and encode overrides for <see cref="ImageTransforms"/> shortcuts.
/// </summary>
/// <remarks>
/// Prefer <see cref="ImageEncoding"/> on <see cref="ImagePipeline"/>, where each format
/// only exposes the options that apply to it.
/// Omit this on shortcuts to use format defaults from <see cref="ImageEncoding"/> (JPEG 80, WebP 60, AVIF/HEIC 85).
/// When set, <see cref="Quality"/> applies to JPEG, WebP, AVIF, and HEIC. PNG ignores it.
/// <see cref="AlphaFallbackColor"/> applies only when the chosen format cannot store alpha (JPEG).
/// Prefer <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/> on the pipeline.
/// </remarks>
public sealed record ImageEncode
{
    /// <summary>
    /// Gets a value indicating whether EXIF/XMP orientation is applied before geometry.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Width and height then describe the visually upright image.
    /// Set this to <c>false</c> only when the caller intentionally wants the stored raster.
    /// </remarks>
    public bool AutoOrient { get; init; } = true;

    /// <summary>
    /// Gets which metadata is written. Defaults to <see cref="MetadataPolicy.PreserveColorProfileOnly"/>.
    /// </summary>
    public MetadataPolicy Metadata { get; init; } = MetadataPolicy.PreserveColorProfileOnly;

    /// <summary>
    /// Gets the lossy quality, from 1 to 100. Defaults to 85.
    /// </summary>
    /// <remarks>
    /// Applies to JPEG, WebP, AVIF, and HEIC. PNG is lossless and ignores this value.
    /// </remarks>
    public int Quality { get; init; } = 85;

    /// <summary>
    /// Gets the color used when the output format cannot store remaining alpha.
    /// </summary>
    /// <remarks>
    /// Defaults to white. Used for JPEG. A format that can store alpha leaves the pixels alone.
    /// </remarks>
    public ImageColor AlphaFallbackColor { get; init; } = ImageColor.White;
}
