using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NetVips;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

internal sealed class DecodedImage : IDisposable
{
    private readonly Image _image;

    private DecodedImage(Image image)
    {
        _image = image;
    }

    public int Width => _image.Width;

    public int Height => _image.Height;

    public int Bands => _image.Bands;

    public bool HasAlpha => _image.HasAlpha();

    public static DecodedImage Open(byte[] data)
    {
        return new DecodedImage(Image.NewFromBuffer(data));
    }

    public void Dispose()
    {
        _image.Dispose();
    }

    public int? Orientation()
    {
        foreach (var field in _image.GetFields())
        {
            if (string.Equals(field, "orientation", StringComparison.Ordinal))
            {
                return Convert.ToInt32(_image.Get(field), CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    public string? ExifMake()
    {
        foreach (var field in _image.GetFields())
        {
            if (!string.Equals(field, "exif-ifd0-Make", StringComparison.Ordinal))
            {
                continue;
            }

            var value = _image.Get(field);
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    return UnwrapExif(text);
                case byte[] bytes:
                    return System.Text.Encoding.ASCII.GetString(bytes).Trim().Trim('\0');
                default:
                    return value.GetType().FullName + " :: " + value;
            }
        }

        return null;
    }

    public bool HasIccProfile()
    {
        foreach (var field in _image.GetFields())
        {
            if (field.IndexOf("icc-profile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var value = _image.Get(field);
                if (value is byte[] bytes)
                {
                    return bytes.Length > 0;
                }

                return value != null;
            }
        }

        return false;
    }

    public void Near(int x, int y, byte red, byte green, byte blue, int tolerance = 16)
    {
        var pixel = _image.Getpoint(x, y);
        Assert.InRange(pixel[0], red - tolerance, red + tolerance);
        Assert.InRange(pixel[1], green - tolerance, green + tolerance);
        Assert.InRange(pixel[2], blue - tolerance, blue + tolerance);
    }

    public void AlphaNear(int x, int y, byte alpha, int tolerance = 8)
    {
        Assert.True(_image.HasAlpha());
        var pixel = _image.Getpoint(x, y);
        Assert.InRange(pixel[pixel.Length - 1], alpha - tolerance, alpha + tolerance);
    }

    private static string UnwrapExif(string value)
    {
        // libvips appends " (value, ASCII, N components, N bytes)" so EXIF strings can round-trip.
        var match = Regex.Match(value, @"^(.*) \(\1, .+\)$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return value.Trim().Trim('\0');
    }
}
