using System;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// Layered read of a source image's structure and metadata bags.
/// </summary>
/// <remarks>
/// Read-only. This is not an EXIF editor. Convenience properties resolve common facts from
/// <see cref="Exif"/> IFD dictionaries on access (see each property). Missing or malformed tags are null.
/// </remarks>
public sealed class ImageMetadata
{
    internal ImageMetadata(
        ImageStructure structure,
        ExifMetadata exif,
        XmpMetadata xmp,
        IptcMetadata iptc,
        ColorProfileMetadata colorProfile)
    {
        Structure = structure;
        Exif = exif;
        Xmp = xmp;
        Iptc = iptc;
        ColorProfile = colorProfile;
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
    /// Gets the date the image was taken from EXIF, when parseable.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="ExifMetadata.Ifd2"/> <c>DateTimeOriginal</c>, then <c>DateTimeDigitized</c>, and takes
    /// the first that parses. The IFD0 <c>DateTime</c> tag is not used: it is the file's last-modified time,
    /// which editing software rewrites. Null when neither tag parses; the caller picks its own fallback. Parsing
    /// is lenient: the date may use <c>:</c>, <c>-</c>, or <c>.</c> separators, the time is optional (midnight
    /// when absent), seconds are optional, and text after the time (subseconds, an offset, padding) is ignored.
    /// An all-zero or impossible date counts as unparseable, so the next field is tried. Dates are not
    /// range-checked, so a reset camera clock (for example <c>1970:01:01</c>) is returned as written. A UTC
    /// offset in the text is ignored, so the value is the camera's local wall-clock time. Kind is
    /// <see cref="DateTimeKind.Unspecified"/> (EXIF has no timezone).
    /// </remarks>
    public DateTime? DateTaken => ExifConvenience.ReadDateTaken(Exif.Ifd2);

    /// <summary>
    /// Gets GPS coordinates in decimal degrees from <see cref="ExifMetadata.Ifd3"/>, when both
    /// latitude and longitude parse. Null when either is missing or malformed.
    /// </summary>
    /// <remarks>
    /// Requires value tags and hemisphere Refs (<c>GPSLatitude</c>/<c>GPSLatitudeRef</c> and
    /// <c>GPSLongitude</c>/<c>GPSLongitudeRef</c>). Values may be a DMS triplet, degrees+minutes,
    /// or a single rational already in degrees. Absolute values outside ±90 (lat) or ±180 (lon)
    /// are treated as malformed. Missing Ref → null (not assumed north/east).
    /// </remarks>
    public ImageGps? Gps => Exif.Ifd3.Gps;

    /// <summary>Gets the camera make from <see cref="ExifIfd0.Make"/>, when present.</summary>
    public string? CameraMake => Exif.Ifd0.Make;

    /// <summary>Gets the camera model from <see cref="ExifIfd0.Model"/>, when present.</summary>
    public string? CameraModel => Exif.Ifd0.Model;
}
