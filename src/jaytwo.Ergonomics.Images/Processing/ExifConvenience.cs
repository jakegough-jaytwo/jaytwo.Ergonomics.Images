using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using jaytwo.Ergonomics.Images.Metadata;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Resolves common EXIF convenience values from IFD bags.
/// </summary>
internal static class ExifConvenience
{
    private static readonly Regex ExifDateTime = new(
        @"^\s*([0-9]{4})[:\-.]([0-9]{2})[:\-.]([0-9]{2})(?:[ T]([0-9]{2}):([0-9]{2})(?::([0-9]{2}))?)?",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] DateTakenKeys =
    {
        "DateTimeOriginal",
        "DateTimeDigitized",
    };

    public static string? ReadField(IReadOnlyDictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.Length > 0 ? value : null;
    }

    public static DateTime? ReadDateTaken(IReadOnlyDictionary<string, string> ifd2)
    {
        foreach (var key in DateTakenKeys)
        {
            if (!ifd2.TryGetValue(key, out var raw) || raw.Length == 0)
            {
                continue;
            }

            if (TryParseExifDateTime(raw, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    public static ImageGps? ReadGps(IReadOnlyDictionary<string, string> ifd3)
    {
        if (!TryReadGpsCoordinate(
                ifd3,
                "GPSLatitude",
                "GPSLatitudeRef",
                "N",
                "S",
                out var latitude))
        {
            return null;
        }

        if (!TryReadGpsCoordinate(
                ifd3,
                "GPSLongitude",
                "GPSLongitudeRef",
                "E",
                "W",
                out var longitude))
        {
            return null;
        }

        return new ImageGps(latitude, longitude);
    }

    private static bool TryParseExifDateTime(string value, out DateTime result)
    {
        result = default;
        var match = ExifDateTime.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var year = ParseGroup(match, 1);
        var month = ParseGroup(match, 2);
        var day = ParseGroup(match, 3);
        var hasTime = match.Groups[4].Success;
        var hour = hasTime ? ParseGroup(match, 4) : 0;
        var minute = hasTime ? ParseGroup(match, 5) : 0;
        var second = match.Groups[6].Success ? ParseGroup(match, 6) : 0;

        if (year < 1
            || month is < 1 or > 12
            || day < 1
            || day > DateTime.DaysInMonth(year, month)
            || hour > 23
            || minute > 59
            || second > 59)
        {
            return false;
        }

        result = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        return true;
    }

    private static int ParseGroup(Match match, int group)
    {
        return int.Parse(match.Groups[group].Value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static bool TryReadGpsCoordinate(
        IReadOnlyDictionary<string, string> fields,
        string valueKey,
        string refKey,
        string positiveRef,
        string negativeRef,
        out double degrees)
    {
        degrees = 0;
        if (!fields.TryGetValue(valueKey, out var raw) || !fields.TryGetValue(refKey, out var reference))
        {
            return false;
        }

        if (!TryParseGpsRationalTriplet(raw, out var absolute))
        {
            return false;
        }

        var hemi = reference.Trim();
        if (hemi.Length == 0)
        {
            return false;
        }

        var signChar = char.ToUpperInvariant(hemi[0]);
        if (signChar == positiveRef[0])
        {
            degrees = absolute;
            return true;
        }

        if (signChar == negativeRef[0])
        {
            degrees = -absolute;
            return true;
        }

        return false;
    }

    private static bool TryParseGpsRationalTriplet(string value, out double degrees)
    {
        degrees = 0;
        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return false;
        }

        if (!TryParseRational(parts[0], out var d)
            || !TryParseRational(parts[1], out var m)
            || !TryParseRational(parts[2], out var s))
        {
            return false;
        }

        degrees = d + (m / 60d) + (s / 3600d);
        return double.IsFinite(degrees);
    }

    private static bool TryParseRational(string text, out double value)
    {
        value = 0;
        var slash = text.IndexOf('/');
        if (slash < 0)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        if (!double.TryParse(text.AsSpan(0, slash), NumberStyles.Float, CultureInfo.InvariantCulture, out var num)
            || !double.TryParse(text.AsSpan(slash + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var den)
            || den == 0d)
        {
            return false;
        }

        value = num / den;
        return true;
    }
}
