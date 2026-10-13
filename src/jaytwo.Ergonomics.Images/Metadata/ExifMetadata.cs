namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// EXIF bags from the source image, partitioned by IFD.
/// </summary>
/// <remarks>
/// Each IFD exposes common tags as properties (for example <see cref="ExifIfd0.Make"/>) and arbitrary tags
/// via indexer (for example <c>Ifd0["Make"]</c>), with raw stored values. Prefer
/// <see cref="ImageMetadata"/> convenience properties for capture time, GPS, and camera when those suffice.
/// </remarks>
public sealed class ExifMetadata
{
    internal ExifMetadata(
        bool present,
        ExifIfd0 ifd0,
        ExifIfd1 ifd1,
        ExifIfd2 ifd2,
        ExifIfd3 ifd3,
        ExifIfd4 ifd4)
    {
        Present = present;
        Ifd0 = ifd0;
        Ifd1 = ifd1;
        Ifd2 = ifd2;
        Ifd3 = ifd3;
        Ifd4 = ifd4;
    }

    /// <summary>Gets a value indicating whether an EXIF blob or expanded EXIF fields are present.</summary>
    public bool Present { get; }

    /// <summary>Gets IFD0 — main image tags.</summary>
    public ExifIfd0 Ifd0 { get; }

    /// <summary>Gets IFD1 — embedded thumbnail tags. Often empty.</summary>
    public ExifIfd1 Ifd1 { get; }

    /// <summary>Gets IFD2 — EXIF sub-IFD tags.</summary>
    public ExifIfd2 Ifd2 { get; }

    /// <summary>Gets IFD3 — GPS tags.</summary>
    public ExifIfd3 Ifd3 { get; }

    /// <summary>Gets IFD4 — interoperability tags. Often empty.</summary>
    public ExifIfd4 Ifd4 { get; }
}
