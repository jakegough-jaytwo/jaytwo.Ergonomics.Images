namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// XMP bag from the source image.
/// </summary>
/// <remarks>
/// Exposed as opaque XML. This is not flattened into a string dictionary in this version.
/// </remarks>
public sealed class XmpMetadata
{
    internal XmpMetadata(bool present, string? xml)
    {
        Present = present;
        Xml = xml;
    }

    /// <summary>Gets a value indicating whether an XMP packet is present.</summary>
    public bool Present { get; }

    /// <summary>Gets the XMP packet as UTF-8 XML, or null when absent.</summary>
    public string? Xml { get; }
}
