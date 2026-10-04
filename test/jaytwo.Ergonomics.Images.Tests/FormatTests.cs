using System;
using System.IO;
using NetVips;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class FormatTests
{
    [Fact]
    public void Gif_input_uses_the_first_frame()
    {
        var output = Transform.Run(
            SampleImages.TwoFrameGif(40, 20),
            (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 40, 80));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
        image.Near(20, 10, 220, 20, 20);
    }

    [Fact]
    public void WebP_round_trip_keeps_the_fitted_size()
    {
        var output = Transform.Run(
            SampleImages.SolidJpeg(80, 40, 30, 140, 60),
            (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.WebP, 40, 40, enlarge: false));

        Assert.Equal((byte)'R', output[0]);
        Assert.Equal((byte)'I', output[1]);
        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }

    [Fact]
    public void Png_output_keeps_source_alpha()
    {
        var output = Transform.Run(
            SampleImages.AlphaHalvesPng(80, 40),
            (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 80, 40, enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.True(image.HasAlpha);
        image.AlphaNear(10, 20, 255);
        image.AlphaNear(70, 20, 0);
    }

    [Fact]
    public void Heic_fixture_resizes_when_the_runtime_can_decode_it()
    {
        var bytes = File.ReadAllBytes(Fixtures.Path("still-tiny.heic"));
        var heicRequired = string.Equals(
            Environment.GetEnvironmentVariable("REQUIRE_HEIC"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!ImageTransforms.Capabilities.Heic)
        {
            Assert.False(heicRequired, "REQUIRE_HEIC is set, but this libvips build cannot decode a HEVC HEIC.");
            Assert.Throws<VipsException>(() =>
            {
                using var image = Image.NewFromBuffer(bytes);
                _ = image.Avg();
            });
            return;
        }

        using var source = new MemoryStream(bytes);
        using var destination = new MemoryStream();
        ImageTransforms.Resize(
            source,
            destination,
            ImageOutputFormat.Jpeg,
            160,
            160,
            enlarge: false,
            encode: new ImageEncode
            {
                Metadata = MetadataPolicy.Strip,
                Quality = 85,
            });

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.InRange(image.Width, 1, 160);
        Assert.InRange(image.Height, 1, 160);
        Assert.True(image.Width == 160 || image.Height == 160);
    }
}
