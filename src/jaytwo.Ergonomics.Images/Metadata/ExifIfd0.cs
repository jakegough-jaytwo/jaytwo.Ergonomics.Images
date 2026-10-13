using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// IFD0 — main image tags (camera, modify date, orientation, resolution, …).
/// </summary>
public sealed class ExifIfd0 : ExifIfd
{
    internal static readonly ExifIfd0 Empty = new(EmptyFields);

    internal ExifIfd0(IReadOnlyDictionary<string, string> fields)
        : base(fields)
    {
    }

    /// <summary>Gets <c>Make</c>, when present.</summary>
    public string? Make => Get("Make");

    /// <summary>Gets <c>Model</c>, when present.</summary>
    public string? Model => Get("Model");

    /// <summary>
    /// Gets <c>DateTime</c> (file modify date), when present. Prefer
    /// <see cref="ImageMetadata.DateTaken"/> for when the image was taken.
    /// </summary>
    public string? DateTime => Get("DateTime");

    /// <summary>Gets <c>Software</c>, when present.</summary>
    public string? Software => Get("Software");

    /// <summary>Gets <c>ImageDescription</c>, when present.</summary>
    public string? ImageDescription => Get("ImageDescription");

    /// <summary>Gets <c>Artist</c>, when present.</summary>
    public string? Artist => Get("Artist");

    /// <summary>Gets <c>Copyright</c>, when present.</summary>
    public string? Copyright => Get("Copyright");

    /// <summary>Gets <c>Orientation</c> as a raw string, when present.</summary>
    public string? Orientation => Get("Orientation");

    /// <summary>Gets <c>XResolution</c> as a raw string, when present.</summary>
    public string? XResolution => Get("XResolution");

    /// <summary>Gets <c>YResolution</c> as a raw string, when present.</summary>
    public string? YResolution => Get("YResolution");

    /// <summary>Gets <c>ResolutionUnit</c> as a raw string, when present.</summary>
    public string? ResolutionUnit => Get("ResolutionUnit");
}
