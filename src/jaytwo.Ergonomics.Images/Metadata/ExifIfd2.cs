using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// IFD2 — EXIF sub-IFD (capture time, exposure, …).
/// </summary>
public sealed class ExifIfd2 : ExifIfd
{
    internal static readonly ExifIfd2 Empty = new(EmptyFields);

    internal ExifIfd2(IReadOnlyDictionary<string, string> fields)
        : base(fields)
    {
    }

    /// <summary>Gets <c>DateTimeOriginal</c> as a raw string, when present.</summary>
    public string? DateTimeOriginal => Get("DateTimeOriginal");

    /// <summary>Gets <c>DateTimeDigitized</c> as a raw string, when present.</summary>
    public string? DateTimeDigitized => Get("DateTimeDigitized");

    /// <summary>Gets <c>ExposureTime</c> as a raw string, when present.</summary>
    public string? ExposureTime => Get("ExposureTime");

    /// <summary>Gets <c>FNumber</c> as a raw string, when present.</summary>
    public string? FNumber => Get("FNumber");

    /// <summary>Gets <c>ISOSpeedRatings</c> as a raw string, when present.</summary>
    public string? ISOSpeedRatings => Get("ISOSpeedRatings");

    /// <summary>Gets <c>PhotographicSensitivity</c> as a raw string, when present.</summary>
    public string? PhotographicSensitivity => Get("PhotographicSensitivity");

    /// <summary>Gets <c>FocalLength</c> as a raw string, when present.</summary>
    public string? FocalLength => Get("FocalLength");

    /// <summary>Gets <c>ApertureValue</c> as a raw string, when present.</summary>
    public string? ApertureValue => Get("ApertureValue");

    /// <summary>Gets <c>ShutterSpeedValue</c> as a raw string, when present.</summary>
    public string? ShutterSpeedValue => Get("ShutterSpeedValue");

    /// <summary>Gets <c>ExifVersion</c> as a raw string, when present.</summary>
    public string? ExifVersion => Get("ExifVersion");
}
