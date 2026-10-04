namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Which embedded metadata is written to the output.
/// </summary>
/// <remarks>
/// Auto-orientation rewrites pixels and clears the orientation tag so a later
/// viewer does not rotate the image again. <see cref="Preserve"/> keeps the
/// remaining EXIF, XMP, IPTC, and ICC data.
/// </remarks>
public enum MetadataPolicy
{
    /// <summary>Keep EXIF, XMP, IPTC, ICC, and other container metadata.</summary>
    Preserve,

    /// <summary>Drop metadata, including the color profile.</summary>
    Strip,

    /// <summary>Keep the ICC profile and drop camera, GPS, and other descriptive metadata.</summary>
    PreserveColorProfileOnly,
}
