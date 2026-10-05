namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Encoded format written to the destination stream.
/// </summary>
/// <remarks>
/// Stream destinations have no filename, so the caller chooses the format.
/// Source format is inferred from the input.
/// </remarks>
public enum ImageOutputFormat
{
    /// <summary>JPEG. Remaining alpha is flattened onto <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>.</summary>
    Jpeg,

    /// <summary>PNG.</summary>
    Png,

    /// <summary>WebP.</summary>
    WebP,

    /// <summary>AVIF (AV1 in a HEIF container). Requires an AV1 encoder in the native runtime.</summary>
    Avif,

    /// <summary>GIF. Animated sources are reduced to the first frame.</summary>
    Gif,

    /// <summary>TIFF. Multi-page sources are reduced to the first page.</summary>
    Tiff,

    /// <summary>
    /// HEIC (HEVC in a HEIF container). Requires an HEVC encoder in the native runtime.
    /// <c>NetVips.Native</c> does not include one.
    /// </summary>
    Heic,
}
