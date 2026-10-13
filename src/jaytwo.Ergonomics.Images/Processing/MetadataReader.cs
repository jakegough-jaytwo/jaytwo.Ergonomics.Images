using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using jaytwo.Ergonomics.Images.Metadata;
using NetVips;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Builds <see cref="ImageMetadata"/> from an open libvips image header.
/// </summary>
internal static class MetadataReader
{
    private static readonly Regex ExifValueSuffix = new(
        @", (Byte|ASCII|Short|Long|Rational|SByte|Undefined|SShort|SLong|SRational|Float|Double), (\d+) components, (\d+) bytes\)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ExifUnwrapAscii = new(
        @"^(.*) \(\1, ASCII, \d+ components, \d+ bytes\)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ExifFieldName = new(
        @"^exif-ifd(\d+)-(.+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ImageMetadata FromImage(Image image, ReadOnlySpan<byte> headerHint)
    {
        var names = image.GetFields() ?? Array.Empty<string>();
        var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
        var ifds = CollectExifIfds(image, names);
        var exifPresent = HasBlob(image, nameSet, "exif-data")
            || ifds.Ifd0.Count > 0
            || ifds.Ifd1.Count > 0
            || ifds.Ifd2.Count > 0
            || ifds.Ifd3.Count > 0
            || ifds.Ifd4.Count > 0;
        var exif = new ExifMetadata(
            exifPresent,
            ifds.Ifd0,
            ifds.Ifd1,
            ifds.Ifd2,
            ifds.Ifd3,
            ifds.Ifd4);

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

        return new ImageMetadata(structure, exif, xmp, iptc, colorProfile);
    }

    private static (
        ExifIfd0 Ifd0,
        ExifIfd1 Ifd1,
        ExifIfd2 Ifd2,
        ExifIfd3 Ifd3,
        ExifIfd4 Ifd4)
        CollectExifIfds(Image image, string[] names)
    {
        Dictionary<string, string>? ifd0 = null;
        Dictionary<string, string>? ifd1 = null;
        Dictionary<string, string>? ifd2 = null;
        Dictionary<string, string>? ifd3 = null;
        Dictionary<string, string>? ifd4 = null;

        foreach (var name in names)
        {
            if (name is null || !TryParseExifFieldName(name, out var ifd, out var tag) || ifd is < 0 or > 4)
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

            var bag = ifd switch
            {
                0 => ifd0 ??= new Dictionary<string, string>(StringComparer.Ordinal),
                1 => ifd1 ??= new Dictionary<string, string>(StringComparer.Ordinal),
                2 => ifd2 ??= new Dictionary<string, string>(StringComparer.Ordinal),
                3 => ifd3 ??= new Dictionary<string, string>(StringComparer.Ordinal),
                _ => ifd4 ??= new Dictionary<string, string>(StringComparer.Ordinal),
            };

            if (!bag.ContainsKey(tag))
            {
                bag[tag] = UnwrapExif(text);
            }
        }

        return (
            FreezeIfd0(ifd0),
            FreezeIfd1(ifd1),
            FreezeIfd2(ifd2),
            FreezeIfd3(ifd3),
            FreezeIfd4(ifd4));
    }

    private static IReadOnlyDictionary<string, string> FreezeFields(Dictionary<string, string>? bag)
    {
        if (bag is null || bag.Count == 0)
        {
            return ExifIfd.EmptyFields;
        }

        return new ReadOnlyDictionary<string, string>(bag);
    }

    private static ExifIfd0 FreezeIfd0(Dictionary<string, string>? bag)
        => bag is null || bag.Count == 0 ? ExifIfd0.Empty : new ExifIfd0(FreezeFields(bag));

    private static ExifIfd1 FreezeIfd1(Dictionary<string, string>? bag)
        => bag is null || bag.Count == 0 ? ExifIfd1.Empty : new ExifIfd1(FreezeFields(bag));

    private static ExifIfd2 FreezeIfd2(Dictionary<string, string>? bag)
        => bag is null || bag.Count == 0 ? ExifIfd2.Empty : new ExifIfd2(FreezeFields(bag));

    private static ExifIfd3 FreezeIfd3(Dictionary<string, string>? bag)
        => bag is null || bag.Count == 0 ? ExifIfd3.Empty : new ExifIfd3(FreezeFields(bag));

    private static ExifIfd4 FreezeIfd4(Dictionary<string, string>? bag)
        => bag is null || bag.Count == 0 ? ExifIfd4.Empty : new ExifIfd4(FreezeFields(bag));

    private static bool TryParseExifFieldName(string name, out int ifd, out string tag)
    {
        ifd = 0;
        tag = string.Empty;
        var match = ExifFieldName.Match(name);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out ifd))
        {
            return false;
        }

        tag = match.Groups[2].Value;
        return tag.Length > 0;
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
        var trimmed = value.Trim().Trim('\0');
        var suffix = ExifValueSuffix.Match(trimmed);
        if (!suffix.Success)
        {
            return trimmed;
        }

        var format = suffix.Groups[1].Value;
        if (string.Equals(format, "ASCII", StringComparison.Ordinal))
        {
            var ascii = ExifUnwrapAscii.Match(trimmed);
            return ascii.Success ? ascii.Groups[1].Value : trimmed;
        }

        var openParen = trimmed.IndexOf(" (", StringComparison.Ordinal);
        return openParen < 0 ? trimmed : trimmed.Substring(0, openParen);
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
}
