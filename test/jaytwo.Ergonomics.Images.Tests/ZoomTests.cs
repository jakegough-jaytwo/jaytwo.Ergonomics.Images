using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ZoomTests
{
    [Fact]
    public void Zoom_keeps_the_middle_of_a_horizontal_image()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Zoom,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(90, 100, 20, 20, 220);
    }

    [Fact]
    public void Zoom_upscales_to_the_exact_box()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(40, 20, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 80,
            Height = 80,
            Fit = ImageFit.Zoom,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(80, image.Height);
    }

    [Fact]
    public void Zoom_without_enlarge_keeps_the_center_aspect_window()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 200,
            Height = 200,
            Fit = ImageFit.Zoom,
            Enlarge = false,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(190, 100, 20, 20, 220);
    }
}
