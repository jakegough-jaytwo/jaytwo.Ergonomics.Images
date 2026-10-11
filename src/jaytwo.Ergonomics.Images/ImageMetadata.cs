using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Layered read of a source image's structure and metadata bags.
/// </summary>
/// <remarks>
/// Read-only. This is not an EXIF editor. Convenience properties resolve common facts from
/// EXIF only in this version (see each property). Missing or malformed tags are null.
/// </remarks>
public sealed class ImageMetadata
{
    internal ImageMetadata(
        ImageStructure structure,
        ExifMetadata exif,
        XmpMetadata xmp,
        IptcMetadata iptc,
        ColorProfileMetadata colorProfile,
        DateTime? capturedAt,
        double? latitude,
        double? longitude,
        string? cameraMake,
        string? cameraModel)
    {
        Structure = structure;
        Exif = exif;
        Xmp = xmp;
        Iptc = iptc;
        ColorProfile = colorProfile;
        CapturedAt = capturedAt;
        Latitude = latitude;
        Longitude = longitude;
        CameraMake = cameraMake;
        CameraModel = cameraModel;
    }

    /// <summary>Gets structural header fields (size, format, alpha, orientation).</summary>
    public ImageStructure Structure { get; }

    /// <summary>Gets the EXIF bag.</summary>
    public ExifMetadata Exif { get; }

    /// <summary>Gets the XMP bag.</summary>
    public XmpMetadata Xmp { get; }

    /// <summary>Gets the IPTC bag.</summary>
    public IptcMetadata Iptc { get; }

    /// <summary>Gets ICC color profile presence.</summary>
    public ColorProfileMetadata ColorProfile { get; }

    /// <summary>
    /// Gets the capture time from EXIF, when parseable.
    /// </summary>
    /// <remarks>
    /// Prefers <c>DateTimeOriginal</c>, then <c>DateTimeDigitized</c>, then <c>DateTime</c>.
    /// Kind is <see cref="DateTimeKind.Unspecified"/> (EXIF has no timezone).
    /// </remarks>
    public DateTime? CapturedAt { get; }

    /// <summary>Gets GPS latitude in decimal degrees from the EXIF GPS IFD, when parseable.</summary>
    public double? Latitude { get; }

    /// <summary>Gets GPS longitude in decimal degrees from the EXIF GPS IFD, when parseable.</summary>
    public double? Longitude { get; }

    /// <summary>Gets the camera make from EXIF IFD0, when present.</summary>
    public string? CameraMake { get; }

    /// <summary>Gets the camera model from EXIF IFD0, when present.</summary>
    public string? CameraModel { get; }
}
