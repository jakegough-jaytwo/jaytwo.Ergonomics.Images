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

    public static byte[] HorizontalHalves(int width, int height, int orientation = 1, string? make = null, byte[]? icc = null)
    {
        using var left = Solid(width / 2, height, 220, 20, 20);
        using var right = Solid(width / 2, height, 20, 20, 220);
        using var joined = left.Join(right, Enums.Direction.Horizontal);
        return Jpeg(joined, orientation, make, icc);
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

    private static byte[] Jpeg(Image image, int orientation, string? make, byte[]? icc)
    {
        using var tagged = image.Mutate(mutable =>
        {
            mutable.Set(GValue.GIntType, "orientation", orientation);
            if (make != null)
            {
                mutable.Set(GValue.GStrType, "exif-ifd0-Make", make);
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
}
