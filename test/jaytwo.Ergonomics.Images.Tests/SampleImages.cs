using System;
using System.IO;
using NetVips;

namespace jaytwo.Ergonomics.Images.Tests;

internal static class SampleImages
{
    public static byte[] SrgbProfile()
    {
        return File.ReadAllBytes(Fixtures.Path("sRGB.icm"));
    }

    public static byte[] HorizontalHalves(
        int width,
        int height,
        int orientation = 1,
        string? make = null,
        byte[]? icc = null,
        string? model = null,
        string? dateTimeOriginal = null,
        double? latitude = null,
        double? longitude = null)
    {
        using var left = Solid(width / 2, height, 220, 20, 20);
        using var right = Solid(width / 2, height, 20, 20, 220);
        using var joined = left.Join(right, Enums.Direction.Horizontal);
        return Jpeg(joined, orientation, make, icc, model, dateTimeOriginal, latitude, longitude);
    }

    public static byte[] VerticalHalves(int width, int height)
    {
        using var top = Solid(width, height / 2, 220, 20, 20);
        using var bottom = Solid(width, height / 2, 20, 20, 220);
        using var joined = top.Join(bottom, Enums.Direction.Vertical);
        return Jpeg(joined, orientation: 1, make: null, icc: null);
    }

    public static byte[] SolidJpeg(int width, int height, byte red, byte green, byte blue)
    {
        using var image = Solid(width, height, red, green, blue);
        return Jpeg(image, orientation: 1, make: null, icc: null);
    }

    public static byte[] SolidPng(int width, int height, byte red, byte green, byte blue)
    {
        using var image = Solid(width, height, red, green, blue);
        var stream = new MemoryStream();
        image.PngsaveStream(stream);
        return stream.ToArray();
    }

    public static byte[] GreyPng(int width, int height, byte value)
    {
        using var image = Constant(width, height, value);
        var stream = new MemoryStream();
        image.PngsaveStream(stream);
        return stream.ToArray();
    }

    public static byte[] AlphaHalvesPng(int width, int height)
    {
        using var color = Solid(width, height, 20, 180, 40);
        using var opaque = Constant(width / 2, height, 255);
        using var clear = Constant(width / 2, height, 0);
        using var alpha = opaque.Join(clear, Enums.Direction.Horizontal);
        using var rgba = color.Bandjoin(alpha);
        var stream = new MemoryStream();
        rgba.PngsaveStream(stream);
        return stream.ToArray();
    }

    public static byte[] TwoFrameGif(int width, int height)
    {
        using var first = Solid(width, height, 220, 20, 20);
        using var second = Solid(width, height, 20, 20, 220);
        using var stacked = first.Join(second, Enums.Direction.Vertical);
        using var prepared = stacked.Mutate(mutable => mutable.Set(GValue.GIntType, "page-height", height));
        var stream = new MemoryStream();
        prepared.GifsaveStream(stream);
        return stream.ToArray();
    }

    public static byte[] TwoFrameWebP(int width, int height)
    {
        using var first = Solid(width, height, 220, 20, 20);
        using var second = Solid(width, height, 20, 20, 220);
        using var stacked = first.Join(second, Enums.Direction.Vertical);
        using var prepared = stacked.Mutate(mutable => mutable.Set(GValue.GIntType, "page-height", height));
        var stream = new MemoryStream();
        prepared.WebpsaveStream(stream, q: 95);
        return stream.ToArray();
    }

    private static Image Solid(int width, int height, byte red, byte green, byte blue)
    {
        using var canvas = Image.Black(width, height);
        return canvas.NewFromImage(new[] { (double)red, green, blue });
    }

    private static Image Constant(int width, int height, byte value)
    {
        using var canvas = Image.Black(width, height);
        return canvas.NewFromImage(new[] { (double)value });
    }

    private static byte[] Jpeg(
        Image image,
        int orientation,
        string? make,
        byte[]? icc,
        string? model = null,
        string? dateTimeOriginal = null,
        double? latitude = null,
        double? longitude = null)
    {
        using var tagged = image.Mutate(mutable =>
        {
            mutable.Set(GValue.GIntType, "orientation", orientation);
            if (make != null)
            {
                mutable.Set(GValue.GStrType, "exif-ifd0-Make", make);
            }

            if (model != null)
            {
                mutable.Set(GValue.GStrType, "exif-ifd0-Model", model);
            }

            if (dateTimeOriginal != null)
            {
                mutable.Set(GValue.GStrType, "exif-ifd2-DateTimeOriginal", dateTimeOriginal);
            }

            if (latitude is double lat && longitude is double lon)
            {
                SetGps(mutable, lat, lon);
            }

            if (icc != null)
            {
                mutable.Set(GValue.BlobType, "icc-profile-data", icc);
            }
        });

        var stream = new MemoryStream();
        tagged.JpegsaveStream(stream, q: 95, keep: Enums.ForeignKeep.All);
        return stream.ToArray();
    }

    private static void SetGps(MutableImage mutable, double latitude, double longitude)
    {
        mutable.Set(GValue.GStrType, "exif-ifd3-GPSLatitude", ToGpsRational(Math.Abs(latitude)));
        mutable.Set(GValue.GStrType, "exif-ifd3-GPSLatitudeRef", latitude >= 0 ? "N" : "S");
        mutable.Set(GValue.GStrType, "exif-ifd3-GPSLongitude", ToGpsRational(Math.Abs(longitude)));
        mutable.Set(GValue.GStrType, "exif-ifd3-GPSLongitudeRef", longitude >= 0 ? "E" : "W");
    }

    private static string ToGpsRational(double absoluteDegrees)
    {
        var degrees = Math.Floor(absoluteDegrees);
        var minutesFull = (absoluteDegrees - degrees) * 60d;
        var minutes = Math.Floor(minutesFull);
        var seconds = (minutesFull - minutes) * 60d;
        var secondsHundredths = (int)Math.Round(seconds * 100d);
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{(int)degrees}/1 {(int)minutes}/1 {secondsHundredths}/100");
    }
}
