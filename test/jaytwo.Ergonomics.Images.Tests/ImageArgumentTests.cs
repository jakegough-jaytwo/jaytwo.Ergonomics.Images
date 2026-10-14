using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ImageArgumentTests
{
    [Fact]
    public void Resize_rejects_a_null_source()
    {
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() =>
            ImageTransforms.Resize(null!, destination, ImageEncoding.Jpeg(), 10, 10));
    }

    [Fact]
    public void Resize_rejects_a_non_positive_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageTransforms.Resize(null!, null!, ImageEncoding.Jpeg(), 0, 10));
    }

    [Fact]
    public void Resize_rejects_quality_outside_1_to_100()
    {
        using var source = new MemoryStream(SampleImages.SolidJpeg(8, 8, 10, 10, 10));
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageTransforms.Resize(source, destination, ImageEncoding.Jpeg(0), 4, 4));
    }

    [Fact]
    public void Fit_rejects_a_non_positive_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageTransforms.Resize(null!, null!, new ImageResize(ImageEncoding.Jpeg())
        {
            Width = 4,
            Height = 0,
            Mode = ResizeMode.Crop,
        }));
    }

    [Fact]
    public void Resize_without_dimensions_keeps_the_visual_size()
    {
        using var source = new MemoryStream(SampleImages.HorizontalHalves(80, 40, orientation: 6));
        using var destination = new MemoryStream();
        var size = ImageTransforms.Resize(source, destination, ImageEncoding.Jpeg(95));

        Assert.Equal(new ImageSize(40, 80), size);
        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
    }

    [Fact]
    public void Resize_without_dimensions_can_keep_the_stored_raster()
    {
        using var source = new MemoryStream(SampleImages.HorizontalHalves(80, 40, orientation: 6));
        using var destination = new MemoryStream();
        var size = ImageTransforms.Resize(
            source,
            destination,
            new ImageResize(ImageEncoding.Jpeg(95)) { AutoOrient = false });

        Assert.Equal(new ImageSize(80, 40), size);
        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
    }

    [Fact]
    public void Resize_returns_the_size_written_when_the_result_is_smaller_than_the_box()
    {
        using var source = new MemoryStream(SampleImages.HorizontalHalves(400, 200));
        using var destination = new MemoryStream();
        var size = ImageTransforms.Resize(source, destination, ImageEncoding.Png(), 100, 100);

        Assert.Equal(new ImageSize(100, 50), size);
        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(size.Width, image.Width);
        Assert.Equal(size.Height, image.Height);
    }

    [Fact]
    public void Stretch_ignores_enlarge_false()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(20, 10, 30, 140, 60), (source, destination) =>
            ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Png())
            {
                Width = 40,
                Height = 30,
                Mode = ResizeMode.Stretch,
                Enlarge = false,
            }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(30, image.Height);
    }

    [Fact]
    public void Stretch_rejects_both_sides_plus_aspect()
    {
        var fluent = Assert.Throws<ArgumentException>(() =>
            ImagePipeline.Create()
                .Resize(400, 300, ResizeMode.Stretch, r => r.Aspect(ImageAspectRatio.Square)));
        Assert.Contains("exact size", fluent.Message, StringComparison.Ordinal);

        var bag = Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageEncoding.Jpeg())
            {
                Width = 400,
                Height = 300,
                Aspect = ImageAspectRatio.Square,
                Mode = ResizeMode.Stretch,
            }));
        Assert.Contains("exact size", bag.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_rejects_a_canvas()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageEncoding.Jpeg())
            {
                Width = 4,
                Height = 4,
                Mode = ResizeMode.Fit,
                Canvas = ImageCanvas.SolidBlack,
            }));

        Assert.Contains("Pad", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Canvas_after_trim_does_not_convert_to_expand()
    {
        Assert.Throws<ArgumentException>(() =>
            ImagePipeline.Create()
                .Rotate(ImageRotation.Clockwise(12), r => r.Trim().Canvas(ImageCanvas.SolidBlack)));
    }

    [Fact]
    public void Resize_canvas_stays_on_that_resize_when_a_later_rotate_trims()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(100, 100, ResizeMode.Pad, r => r.Canvas(ImageCanvas.SolidWhite))
            .Rotate(ImageRotation.Clockwise(90), r => r.Trim())
            .EncodingPng();

        var output = Transform.Png(SampleImages.SolidJpeg(200, 100, 30, 140, 60), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public void Aspect_after_expand_does_not_convert_to_trim()
    {
        Assert.Throws<ArgumentException>(() =>
            ImagePipeline.Create()
                .Rotate(ImageRotation.Clockwise(12), r => r.Expand().Aspect(ImageAspectRatio.Square)));
    }

    [Fact]
    public void Focus_rejects_fit_immediately()
    {
        Assert.Throws<ArgumentException>(() =>
            ImagePipeline.Create()
                .Resize(10, 10, ResizeMode.Fit, r => r.Focus(ImageFocus.Point(0.5, 0.5))));
    }

    [Fact]
    public void Focus_requires_an_explicit_crop_mode()
    {
        // No Resize(width, height, Action) / Resize(ImageSize, Action) that silently forces Fit.
        Assert.Throws<ArgumentException>(() =>
            ImagePipeline.Create()
                .Resize(ImageSize.S, ResizeMode.Fit, r => r.Focus(ImageFocus.Point(0.5, 0.5))));

        var pipeline = ImagePipeline.Create()
            .Resize(ImageSize.S, ResizeMode.Crop, r => r.Focus(ImageFocus.Point(0.5, 0.5)))
            .EncodingPng();
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Fit_rejects_an_aspect_ratio_without_a_pixel_size()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageEncoding.Jpeg())
            {
                Aspect = ImageAspectRatio.Square,
                Mode = ResizeMode.Fit,
            }));
    }

    [Fact]
    public void Resize_rejects_a_null_request()
    {
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ImageTransforms.Resize(Stream.Null, destination, (ImageResize)null!));
    }

    [Fact]
    public void Size_rejects_a_non_positive_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageSize(0, 360));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageSize(360, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageSize(-360, -360));
    }

    [Fact]
    public void Focus_rejects_a_box_outside_the_image()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFocus(-0.1, 0.2, 0.2, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFocus(0.2, 1.1, 0.2, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFocus(0.8, 0.2, 0.3, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFocus(0.2, 0.8, 0.2, 0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageFocus(double.NaN, 0.2, 0.2, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageFocus.Point(0.2, double.PositiveInfinity));
    }

    [Fact]
    public void Fit_rejects_a_focus_box()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageEncoding.Jpeg())
            {
                Width = 4,
                Height = 4,
                Focus = ImageFocus.Point(0.5, 0.5),
            }));
    }
}
