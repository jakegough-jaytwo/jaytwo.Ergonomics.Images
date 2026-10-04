namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Orientation and encode settings shared by every <see cref="ImageTransforms"/> call.
/// </summary>
/// <remarks>
/// Omit it to keep the defaults: auto-orient, color profile only, quality 85, white alpha fallback.
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
    /// Defaults to white. JPEG uses it. A format that can store alpha leaves the pixels alone.
    /// </remarks>
    public ImageColor AlphaFallbackColor { get; init; } = ImageColor.White;
}
