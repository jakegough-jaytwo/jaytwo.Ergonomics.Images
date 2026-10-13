namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// Container format of a source image, as detected on open.
/// </summary>
public enum ImageFormat
{
    /// <summary>Unrecognized or unsupported loader.</summary>
    Unknown,

    /// <summary>JPEG.</summary>
    Jpeg,

    /// <summary>PNG.</summary>
    Png,

    /// <summary>WebP.</summary>
    WebP,

    /// <summary>AVIF (AV1 in a HEIF container).</summary>
    Avif,

    /// <summary>GIF.</summary>
    Gif,

    /// <summary>TIFF.</summary>
    Tiff,

    /// <summary>HEIC (HEVC in a HEIF container), or another non-AVIF HEIF brand.</summary>
    Heic,
}
