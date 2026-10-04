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
            ImageTransforms.Resize(null!, destination, ImageOutputFormat.Jpeg, 10, 10));
    }

    [Fact]
    public void Resize_rejects_a_non_positive_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageTransforms.Resize(null!, null!, ImageOutputFormat.Jpeg, 0, 10));
    }

    [Fact]
    public void Resize_rejects_quality_outside_1_to_100()
    {
        using var source = new MemoryStream(SampleImages.SolidJpeg(8, 8, 10, 10, 10));
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageTransforms.Resize(source, destination, ImageOutputFormat.Jpeg, 4, 4, encode: new ImageEncode { Quality = 0 }));
    }

    [Fact]
    public void Fit_rejects_a_non_positive_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageTransforms.Resize(null!, null!, new ImageResize(ImageOutputFormat.Jpeg)
        {
            Width = 4,
            Height = 0,
            Fit = ImageFit.Zoom,
        }));
    }

    [Fact]
    public void Resize_without_dimensions_keeps_the_visual_size()
    {
        using var source = new MemoryStream(SampleImages.HorizontalHalves(80, 40, orientation: 6));
        using var destination = new MemoryStream();
        var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Jpeg, encode: new ImageEncode { Quality = 95 });

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
            ImageOutputFormat.Jpeg,
            encode: new ImageEncode { AutoOrient = false, Quality = 95 });

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
        var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 100, 100);

        Assert.Equal(new ImageSize(100, 50), size);
        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(size.Width, image.Width);
        Assert.Equal(size.Height, image.Height);
    }

    [Fact]
    public void Stretch_rejects_enlarge_false()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageOutputFormat.Jpeg)
            {
                Width = 4,
                Height = 4,
                Fit = ImageFit.Stretch,
                Enlarge = false,
            }));
    }

    [Fact]
    public void Zoom_rejects_a_canvas()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageOutputFormat.Jpeg)
            {
                Width = 4,
                Height = 4,
                Fit = ImageFit.Zoom,
                Canvas = ImageCanvas.SolidBlack,
            }));
    }

    [Fact]
    public void Fit_rejects_an_aspect_ratio_without_a_canvas_or_a_pixel_size()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageOutputFormat.Jpeg)
            {
                Aspect = ImageAspectRatio.Square,
            }));
    }

    [Fact]
    public void Resize_rejects_a_null_request()
    {
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ImageTransforms.Resize(Stream.Null, destination, null!));
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
            ImageTransforms.Resize(null!, null!, new ImageResize(ImageOutputFormat.Jpeg)
            {
                Width = 4,
                Height = 4,
                Focus = ImageFocus.Point(0.5, 0.5),
            }));
    }
}
