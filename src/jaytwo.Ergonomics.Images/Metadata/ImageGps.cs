namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// Decimal-degree coordinates from the EXIF GPS IFD.
/// </summary>
public sealed class ImageGps
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageGps"/> class.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (positive north).</param>
    /// <param name="longitude">Longitude in decimal degrees (positive east).</param>
    public ImageGps(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Gets latitude in decimal degrees (positive north).</summary>
    public double Latitude { get; }

    /// <summary>Gets longitude in decimal degrees (positive east).</summary>
    public double Longitude { get; }
}
