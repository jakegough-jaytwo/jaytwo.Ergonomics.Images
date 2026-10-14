using System;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class SourceAlphaTests
{
    [Fact]
    public void Create_configure_flattens_source_alpha_before_geometry()
    {
        var pipeline = ImagePipeline.Create(s => s.SourceAlpha(AlphaBehavior.Flatten(ImageColor.White)))
            .Resize(80, 80, ResizeMode.Pad, r => r.Canvas(ImageCanvas.Transparent))
            .EncodingPng();

        var output = Transform.Png(SampleImages.AlphaHalvesPng(80, 40), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        image.AlphaNear(40, 4, 0);
        image.Near(16, 40, 20, 180, 40, tolerance: 2);
        image.AlphaNear(16, 40, 255);
        image.Near(60, 40, 255, 255, 255, tolerance: 2);
        image.AlphaNear(60, 40, 255);
    }

    [Fact]
    public void Create_source_alpha_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ImagePipeline.Create(s => s.SourceAlpha(null!)));
    }

    [Fact]
    public void Preserve_keeps_source_alpha_and_a_transparent_canvas()
    {
        var output = Transform.Png(SampleImages.AlphaHalvesPng(80, 40), (source, destination) => ImageTransforms.Resize(
            source,
            destination,
            new ImageResize(ImageEncoding.Png())
            {
                Width = 80,
                Height = 80,
                Mode = ResizeMode.Pad,
                Canvas = ImageCanvas.Transparent,
                SourceAlpha = AlphaBehavior.Preserve,
            }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(80, image.Height);
        image.AlphaNear(40, 4, 0);
        image.Near(16, 40, 20, 180, 40, tolerance: 2);
        image.AlphaNear(16, 40, 255);
        image.AlphaNear(60, 40, 0);
    }

    [Fact]
    public void Flatten_whitens_the_source_and_leaves_the_canvas_transparent()
    {
        var output = Transform.Png(SampleImages.AlphaHalvesPng(80, 40), (source, destination) => ImageTransforms.Resize(
            source,
            destination,
            new ImageResize(ImageEncoding.Png())
            {
                Width = 80,
                Height = 80,
                Mode = ResizeMode.Pad,
                Canvas = ImageCanvas.Transparent,
                SourceAlpha = AlphaBehavior.Flatten(ImageColor.White),
            }));

        using var image = DecodedImage.Open(output);
        image.AlphaNear(40, 4, 0);
        image.Near(16, 40, 20, 180, 40, tolerance: 2);
        image.AlphaNear(16, 40, 255);
        image.Near(60, 40, 255, 255, 255, tolerance: 2);
        image.AlphaNear(60, 40, 255);
    }

    [Fact]
    public void Checkerboard_canvas_fills_gaps_and_content_alpha()
    {
        var output = Transform.Png(SampleImages.AlphaHalvesPng(80, 40), (source, destination) => ImageTransforms.Resize(
            source,
            destination,
            new ImageResize(ImageEncoding.Png())
            {
                Width = 80,
                Height = 80,
                Mode = ResizeMode.Pad,
                Canvas = ImageCanvas.Checkerboard,
                SourceAlpha = AlphaBehavior.Preserve,
            }));

        using var image = DecodedImage.Open(output);
        image.Near(40, 4, 204, 204, 204, tolerance: 2);
        image.AlphaNear(40, 4, 255);
        image.Near(16, 40, 20, 180, 40, tolerance: 2);
        image.Near(60, 40, 255, 255, 255, tolerance: 2);
        image.AlphaNear(60, 40, 255);
    }

    [Fact]
    public void Jpeg_flattens_remaining_alpha_onto_the_fallback()
    {
        var output = Transform.Run(
            SampleImages.AlphaHalvesPng(80, 40),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                new ImageResize(ImageEncoding.Jpeg(95, ImageColor.White))
                {
                    Width = 80,
                    Height = 80,
                    Mode = ResizeMode.Pad,
                    Canvas = ImageCanvas.Transparent,
                    SourceAlpha = AlphaBehavior.Preserve,
                }));

        using var image = DecodedImage.Open(output);
        Assert.False(image.HasAlpha);
        image.Near(40, 4, 255, 255, 255);
        image.Near(16, 40, 20, 180, 40);
        image.Near(60, 40, 255, 255, 255);
    }

    [Fact]
    public void Opaque_canvas_resolves_source_alpha_before_the_jpeg_fallback()
    {
        var output = Transform.Run(
            SampleImages.AlphaHalvesPng(80, 40),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                new ImageResize(ImageEncoding.Jpeg(95, ImageColor.White))
                {
                    Width = 80,
                    Height = 80,
                    Mode = ResizeMode.Pad,
                    Canvas = ImageCanvas.SolidBlack,
                    SourceAlpha = AlphaBehavior.Preserve,
                }));

        using var image = DecodedImage.Open(output);
        Assert.False(image.HasAlpha);
        image.Near(40, 4, 0, 0, 0);
        image.Near(16, 40, 20, 180, 40);
        image.Near(60, 40, 0, 0, 0);
    }
}
