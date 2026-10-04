using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class FitClipTests
{
    [Fact]
    public void Resize_scales_a_landscape_image_inside_the_box()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 100, 100));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
        image.Near(10, 25, 220, 20, 20);
        image.Near(90, 25, 20, 20, 220);
    }

    [Fact]
    public void Resize_scales_a_portrait_image_inside_the_box()
    {
        var output = Transform.Png(SampleImages.VerticalHalves(200, 400), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 100, 100));

        using var image = DecodedImage.Open(output);
        Assert.Equal(50, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public void Resize_from_width_alone_keeps_aspect()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, width: 100));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
    }

    [Fact]
    public void Resize_from_height_alone_keeps_aspect()
    {
        var output = Transform.Png(SampleImages.VerticalHalves(200, 400), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, height: 100));

        using var image = DecodedImage.Open(output);
        Assert.Equal(50, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public void Resize_upscales_to_the_limiting_side()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(40, 20, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 200, 200));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(100, image.Height);
    }

    [Fact]
    public void Resize_without_enlarge_keeps_a_smaller_source()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(40, 20, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 200, 200, enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }
}
