using System;
using System.IO;
using NetVips;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class CapabilityTests
{
    [Fact]
    public void Bundled_and_system_runtimes_decode_the_ordinary_still_formats()
    {
        Assert.True(ImageTransforms.Capabilities.Jpeg);
        Assert.True(ImageTransforms.Capabilities.Png);
        Assert.True(ImageTransforms.Capabilities.WebP);
        Assert.True(ImageTransforms.Capabilities.Gif);
        Assert.True(ImageTransforms.Capabilities.Tiff);
    }

    [Fact]
    public void Bundled_and_system_runtimes_encode_the_ordinary_still_formats()
    {
        Assert.True(ImageTransforms.Capabilities.Encodes(ImageOutputFormat.Jpeg));
        Assert.True(ImageTransforms.Capabilities.Encodes(ImageOutputFormat.Png));
        Assert.True(ImageTransforms.Capabilities.Encodes(ImageOutputFormat.WebP));
        Assert.True(ImageTransforms.Capabilities.Encodes(ImageOutputFormat.Gif));
        Assert.True(ImageTransforms.Capabilities.Encodes(ImageOutputFormat.Tiff));
    }

    [Fact]
    public void Encode_flag_rejects_an_unknown_format()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageTransforms.Capabilities.Encodes((ImageOutputFormat)999));
    }

    [Theory]
    [InlineData(ImageOutputFormat.Avif, "avif-probe.avif")]
    [InlineData(ImageOutputFormat.Heic, "heic-probe.heic")]
    public void Decode_flag_predicts_the_probe(ImageOutputFormat format, string file)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "probes", file));
        if (!Decodes(format))
        {
            Assert.Throws<VipsException>(() => DecodePixels(bytes));
            return;
        }

        DecodePixels(bytes);
    }

    [Theory]
    [InlineData(ImageOutputFormat.Avif)]
    [InlineData(ImageOutputFormat.Heic)]
    public void Encode_flag_predicts_render(ImageOutputFormat format)
    {
        using var source = new MemoryStream(SampleImages.SolidJpeg(32, 16, 30, 140, 60));
        using var destination = new MemoryStream();
        if (!ImageTransforms.Capabilities.Encodes(format))
        {
            Assert.Throws<ImageException>(() =>
                ImageTransforms.Resize(source, destination, format, 32, 16, enlarge: false));
            return;
        }

        var size = ImageTransforms.Resize(source, destination, format, 32, 16, enlarge: false);
        Assert.Equal(new ImageSize(32, 16), size);
        if (!Decodes(format))
        {
            return;
        }

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(32, image.Width);
        Assert.Equal(16, image.Height);
    }

    private static bool Decodes(ImageOutputFormat format)
    {
        switch (format)
        {
            case ImageOutputFormat.Avif:
                return ImageTransforms.Capabilities.Avif;
            case ImageOutputFormat.Heic:
                return ImageTransforms.Capabilities.Heic;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void DecodePixels(byte[] bytes)
    {
        using var image = Image.NewFromBuffer(bytes);
        _ = image.Avg();
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
    }
}
