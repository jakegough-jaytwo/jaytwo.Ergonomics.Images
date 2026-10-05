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
            .Fit(ImageFit.Fit)
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
            .Fit(ImageFit.Fit)
            .EncodingPng();

        using var source1 = new MemoryStream(SampleImages.SolidJpeg(400, 200, 30, 140, 60));
        using var destination1 = new MemoryStream();
        using var source2 = new MemoryStream(SampleImages.SolidJpeg(400, 200, 30, 140, 60));
        using var destination2 = new MemoryStream();

        var size1 = pipeline.Run(source1, destination1);
        var size2 = pipeline.Run(source2, destination2);

        Assert.Equal(size1, size2);
        Assert.Equal(new ImageSize(256, 128), size1);
    }

    [Fact]
    public void Thumbnail_preset_fits_inside_256()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) =>
            ImagePipeline.Preset(ImagePresets.Thumbnail)
                .EncodingPng()
                .Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(256, image.Width);
        Assert.Equal(128, image.Height);
    }

    [Fact]
    public void Fit_mode_is_set_with_Fit_not_Resize()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(100, 100)
            .Fit(ImageFit.Zoom)
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
    public void Encoding_defaults_are_jpeg_80_and_webp_60()
    {
        Assert.Equal(80, ImageEncoding.Jpeg().Quality);
        Assert.Equal(60, ImageEncoding.WebP().Quality);
        Assert.Equal(ImageColor.White, ImageEncoding.Jpeg().AlphaFallbackColor);
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
        var encoding = ImageEncoding.Png() with { AutoOrient = false };
        Assert.Equal(ImageOutputFormat.Png, encoding.Format);
        Assert.False(encoding.AutoOrient);
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
    public void Trim_needs_a_preceding_rotate()
    {
        Assert.Throws<InvalidOperationException>(() => ImagePipeline.Create().Trim());
    }
}
