using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class FitScaleTests
{
    [Fact]
    public void Stretch_forces_exact_dimensions()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 50,
            Height = 80,
            Fit = ImageFit.Stretch,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(50, image.Width);
        Assert.Equal(80, image.Height);
    }

    [Fact]
    public void Stretch_upscales_to_the_box()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(20, 10, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 40,
            Height = 30,
            Fit = ImageFit.Stretch,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(30, image.Height);
    }
}
