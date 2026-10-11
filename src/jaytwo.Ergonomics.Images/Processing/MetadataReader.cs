using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NetVips;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Builds <see cref="ImageMetadata"/> from an open libvips image header.
/// </summary>
internal static class MetadataReader
{
    private static readonly Regex ExifUnwrap = new(
        @"^(.*) \(\1, .+\)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] CapturedAtKeys =
    {
        "exif-ifd2-DateTimeOriginal",
        "exif-ifd2-DateTimeDigitized",
        "exif-ifd0-DateTime",
        "exif-ifd2-DateTime",
    };

    public static ImageMetadata FromImage(Image image, ReadOnlySpan<byte> headerHint)
    {
        var names = image.GetFields() ?? Array.Empty<string>();
        var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
        var fields = CollectExifFields(image, names);
        var exifPresent = HasBlob(image, nameSet, "exif-data") || fields.Count > 0;
        var exif = new ExifMetadata(exifPresent, fields);

        string? xmpXml = null;
        var xmpPresent = TryReadBlobAsUtf8(image, nameSet, "xmp-data", out xmpXml);
        var xmp = new XmpMetadata(xmpPresent, xmpXml);

        var iptc = new IptcMetadata(HasBlob(image, nameSet, "iptc-data"));
        var colorProfile = new ColorProfileMetadata(HasBlob(image, nameSet, "icc-profile-data"));

        var orientation = ReadOrientation(image, nameSet);
        var pageCount = ReadPageCount(image, nameSet);
        var format = ResolveFormat(image, nameSet, headerHint);
        var structure = new ImageStructure(
            new ImageSize(image.Width, image.Height),
            format,
            image.HasAlpha(),
            image.Bands,
            orientation,
            pageCount);

        TryReadGps(fields, out var latitude, out var longitude);

        return new ImageMetadata(
            structure,
            exif,
            xmp,
            iptc,
            colorProfile,
            ReadCapturedAt(fields),
            latitude,
            longitude,
            ReadField(fields, "exif-ifd0-Make"),
            ReadField(fields, "exif-ifd0-Model"));
    }

    private static Dictionary<string, string> CollectExifFields(Image image, string[] names)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (name is null
                || !name.StartsWith("exif-", StringComparison.Ordinal)
                || string.Equals(name, "exif-data", StringComparison.Ordinal))
            {
                continue;
            }

            object? raw;
            try
            {
                raw = image.Get(name);
            }
            catch (VipsException)
            {
                continue;
            }

            var text = ToPlainString(raw);
            if (text is null)
            {
                continue;
            }

            result[name] = UnwrapExif(text);
        }

        return result;
    }

    private static string? ToPlainString(object? raw)
    {
        switch (raw)
        {
            case null:
                return null;
            case string text:
                return text;
            case byte[] bytes:
                return Encoding.ASCII.GetString(bytes).Trim().Trim('\0');
            default:
                return Convert.ToString(raw, CultureInfo.InvariantCulture);
        }
    }

    private static string UnwrapExif(string value)
    {
        var match = ExifUnwrap.Match(value);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return value.Trim().Trim('\0');
    }

    private static bool HasBlob(Image image, HashSet<string> names, string name)
    {
        if (!names.Contains(name))
        {
            return false;
        }

        try
        {
            return image.Get(name) is byte[] bytes && bytes.Length > 0;
        }
        catch (VipsException)
        {
            return false;
        }
    }

    private static bool TryReadBlobAsUtf8(Image image, HashSet<string> names, string name, out string? text)
    {
        text = null;
        if (!names.Contains(name))
        {
            return false;
        }

        try
        {
            if (image.Get(name) is not byte[] bytes || bytes.Length == 0)
            {
                return false;
            }

            text = Encoding.UTF8.GetString(bytes).Trim('\0');
            return text.Length > 0;
        }
        catch (VipsException)
        {
            return false;
        }
    }

    private static int? ReadOrientation(Image image, HashSet<string> names)
    {
        if (!names.Contains("orientation"))
        {
            return null;
        }

        try
        {
            var value = Convert.ToInt32(image.Get("orientation"), CultureInfo.InvariantCulture);
            return value is >= 1 and <= 8 ? value : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static int ReadPageCount(Image image, HashSet<string> names)
    {
        if (!names.Contains("n-pages"))
        {
            return 1;
        }

        try
        {
            var value = Convert.ToInt32(image.Get("n-pages"), CultureInfo.InvariantCulture);
            return value > 0 ? value : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    private static ImageFormat ResolveFormat(Image image, HashSet<string> names, ReadOnlySpan<byte> headerHint)
    {
        var loader = ReadStringField(image, names, "vips-loader");
        if (loader is null)
        {
            return ImageFormat.Unknown;
        }

        if (loader.Contains("jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ImageFormat.Jpeg;
        }

        if (loader.Contains("png", StringComparison.OrdinalIgnoreCase))
        {
            return ImageFormat.Png;
        }

        if (loader.Contains("webp", StringComparison.OrdinalIgnoreCase))
        {
            return ImageFormat.WebP;
        }

        if (loader.Contains("gif", StringComparison.OrdinalIgnoreCase))
        {
            return ImageFormat.Gif;
        }

        if (loader.Contains("tiff", StringComparison.OrdinalIgnoreCase))
        {
            return ImageFormat.Tiff;
        }

        if (loader.Contains("heif", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveHeifBrand(headerHint);
        }

        return ImageFormat.Unknown;
    }

    private static ImageFormat ResolveHeifBrand(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
        {
            return ImageFormat.Heic;
        }

        // ISO BMFF: size(4) + 'ftyp'(4) + major_brand(4)
        if (header[4] != (byte)'f'
            || header[5] != (byte)'t'
            || header[6] != (byte)'y'
            || header[7] != (byte)'p')
        {
            return ImageFormat.Heic;
        }

        var brand = Encoding.ASCII.GetString(header.Slice(8, 4));
        if (brand is "avif" or "avis" or "AVIF" or "AVIS")
        {
            return ImageFormat.Avif;
        }

        return ImageFormat.Heic;
    }

    private static string? ReadStringField(Image image, HashSet<string> names, string name)
    {
        if (!names.Contains(name))
        {
            return null;
        }

        try
        {
            return ToPlainString(image.Get(name));
        }
        catch (VipsException)
        {
            return null;
        }
    }

    private static string? ReadField(IReadOnlyDictionary<string, string> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.Length > 0 ? value : null;
    }

    private static DateTime? ReadCapturedAt(IReadOnlyDictionary<string, string> fields)
    {
        foreach (var key in CapturedAtKeys)
        {
            if (!fields.TryGetValue(key, out var raw) || raw.Length == 0)
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

    private static bool TryParseExifDateTime(string value, out DateTime result)
    {
        result = default;
        var text = UnwrapExif(value);
        if (DateTime.TryParseExact(
                text,
                "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
        {
            result = DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
            return true;
        }

        return false;
    }

    private static void TryReadGps(
        IReadOnlyDictionary<string, string> fields,
        out double? latitude,
        out double? longitude)
    {
        latitude = null;
        longitude = null;

        if (!TryReadGpsCoordinate(
                fields,
                "exif-ifd3-GPSLatitude",
                "exif-ifd3-GPSLatitudeRef",
                "N",
                "S",
                out var lat))
        {
            return;
        }

        if (!TryReadGpsCoordinate(
                fields,
                "exif-ifd3-GPSLongitude",
                "exif-ifd3-GPSLongitudeRef",
                "E",
                "W",
                out var lon))
        {
            return;
        }

        latitude = lat;
        longitude = lon;
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

        var hemi = UnwrapExif(reference).Trim();
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
        var text = UnwrapExif(value);
        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
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
