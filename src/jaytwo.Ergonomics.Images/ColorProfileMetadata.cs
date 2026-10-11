namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Embedded ICC color profile presence.
/// </summary>
/// <remarks>
/// Not descriptive metadata. Encode policy treats the profile separately from EXIF/XMP/IPTC
/// via <see cref="MetadataPolicy.PreserveColorProfileOnly"/>.
/// </remarks>
public sealed class ColorProfileMetadata
{
    internal ColorProfileMetadata(bool present)
    {
        Present = present;
    }

    /// <summary>Gets a value indicating whether an ICC profile blob is present.</summary>
    public bool Present { get; }
}
