using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class PipelineTests
{
    [Fact]
    public void Rotate_then_resize_keeps_quarter_turn_framing()
    {
        var pipeline = ImagePipeline.Create()
            .Rotate(ImageRotation.Clockwise(90))
            .Resize(40, 40)
            .EncodingPng();

        var output = Transform.Png(SampleImages.HorizontalHalves(80, 40), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(20, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(image.Width / 2, 4, 220, 20, 20);
        image.Near(image.Width / 2, image.Height - 4, 20, 20, 220);
    }

    [Fact]
    public void Pipeline_is_reusable_across_runs()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(ImageSize.S)
            .EncodingPng();

        using var source1 = new MemoryStream(SampleImages.SolidJpeg(400, 200, 30, 140, 60));
        using var destination1 = new MemoryStream();
        using var source2 = new MemoryStream(SampleImages.SolidJpeg(400, 200, 30, 140, 60));
        using var destination2 = new MemoryStream();

        var size1 = pipeline.Run(source1, destination1);
        var size2 = pipeline.Run(source2, destination2);

        Assert.Equal(size1, size2);
        Assert.Equal(new ImageSize(288, 144), size1);
    }

    [Fact]
    public void Thumbnail_preset_fits_inside_288()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) =>
            ImagePipeline.Preset(ImagePresets.ThumbnailWebP)
                .EncodingPng()
                .Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(288, image.Width);
        Assert.Equal(144, image.Height);
    }

    [Fact]
    public void Resize_accepts_mode()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(100, 100, ResizeMode.Crop)
            .EncodingPng();

        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public void Encoding_accepts_a_configured_output_object()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(40, 40)
            .Encoding(ImageEncoding.WebP(quality: 80));

        var output = Transform.Run(SampleImages.SolidJpeg(80, 40, 30, 140, 60), (source, destination) =>
            pipeline.Run(source, destination));

        Assert.True(output.Length > 0);
        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }

    [Fact]
    public void Encoding_defaults_use_format_quality_constants()
    {
        Assert.Equal(ImageEncoding.DefaultJpegQuality, ImageEncoding.Jpeg().Quality);
        Assert.Equal(WebPCompression.Lossy(), ImageEncoding.WebP().Compression);
        Assert.Equal(WebPCompression.Lossless, ImageEncoding.WebP(WebPCompression.Lossless).Compression);
        Assert.Equal(ImageEncoding.DefaultAvifQuality, ImageEncoding.Avif().Quality);
        Assert.Equal(ImageEncoding.DefaultHeicQuality, ImageEncoding.Heic().Quality);
        Assert.Equal(ImageColor.White, ImageEncoding.Jpeg().AlphaFallbackColor);
    }

    [Fact]
    public void EncodingWebP_lossless_encodes()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(40, 40)
            .EncodingWebP(WebPCompression.Lossless);

        var output = Transform.Run(SampleImages.SolidJpeg(80, 40, 30, 140, 60), (source, destination) =>
            pipeline.Run(source, destination));

        Assert.Equal((byte)'R', output[0]);
        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }

    [Fact]
    public void EncodingJpeg_accepts_alpha_fallback_color()
    {
        var encoding = ImageEncoding.Jpeg(quality: 80, alphaFallbackColor: ImageColor.Black);
        Assert.Equal(ImageColor.Black, encoding.AlphaFallbackColor);
    }

    [Fact]
    public void EncodingWebP_rejects_quality_outside_1_to_100()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePipeline.Create().EncodingWebP(0));
    }

    [Fact]
    public void EncodingPng_has_no_quality_argument()
    {
        var encoding = ImageEncoding.Png() with { Metadata = MetadataPolicy.Strip };
        Assert.Equal(ImageOutputFormat.Png, encoding.Format);
        Assert.Equal(MetadataPolicy.Strip, encoding.Metadata);
    }

    [Fact]
    public void Create_can_disable_auto_orient()
    {
        var pipeline = ImagePipeline.Create(s => s.AutoOrient(false))
            .Encoding(ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve });

        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6),
            (source, destination) => pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
        Assert.Equal(6, image.Orientation());
    }

    [Fact]
    public void Run_requires_encoding()
    {
        var pipeline = ImagePipeline.Create().Resize(10, 10);
        using var source = new MemoryStream(SampleImages.SolidJpeg(8, 8, 10, 10, 10));
        using var destination = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => pipeline.Run(source, destination));
    }

    [Fact]
    public void Rotate_trim_is_configured_on_the_stage()
    {
        var pipeline = ImagePipeline.Create()
            .Rotate(ImageRotation.Clockwise(12), r => r.Trim())
            .EncodingPng();

        var output = Transform.Png(SampleImages.SolidJpeg(200, 200, 30, 140, 60), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
    }
}
