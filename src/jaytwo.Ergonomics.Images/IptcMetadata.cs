namespace jaytwo.Ergonomics.Images;

/// <summary>
/// IPTC/IIM bag from the source image.
/// </summary>
/// <remarks>
/// Presence only in this version. Field dictionaries wait on an IIM parser.
/// </remarks>
public sealed class IptcMetadata
{
    internal IptcMetadata(bool present)
    {
        Present = present;
    }

    /// <summary>Gets a value indicating whether an IPTC blob is present.</summary>
    public bool Present { get; }
}
