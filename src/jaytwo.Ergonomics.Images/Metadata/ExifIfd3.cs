using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// IFD3 — GPS tags.
/// </summary>
public sealed class ExifIfd3 : ExifIfd
{
    internal static readonly ExifIfd3 Empty = new(EmptyFields);

    internal ExifIfd3(IReadOnlyDictionary<string, string> fields)
        : base(fields)
    {
    }

    /// <summary>Gets <c>GPSLatitude</c> as a raw string, when present.</summary>
    public string? GPSLatitude => Get("GPSLatitude");

    /// <summary>Gets <c>GPSLatitudeRef</c> as a raw string, when present.</summary>
    public string? GPSLatitudeRef => Get("GPSLatitudeRef");

    /// <summary>Gets <c>GPSLongitude</c> as a raw string, when present.</summary>
    public string? GPSLongitude => Get("GPSLongitude");

    /// <summary>Gets <c>GPSLongitudeRef</c> as a raw string, when present.</summary>
    public string? GPSLongitudeRef => Get("GPSLongitudeRef");

    /// <summary>Gets <c>GPSAltitude</c> as a raw string, when present.</summary>
    public string? GPSAltitude => Get("GPSAltitude");

    /// <summary>Gets <c>GPSAltitudeRef</c> as a raw string, when present.</summary>
    public string? GPSAltitudeRef => Get("GPSAltitudeRef");

    /// <summary>
    /// Gets decimal-degree coordinates when latitude and longitude both parse.
    /// Same as <see cref="ImageMetadata.Gps"/> (requires value + hemisphere Ref on both axes).
    /// </summary>
    public ImageGps? Gps => ExifConvenience.ReadGps(this);
}
