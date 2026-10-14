using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class CropMinTests
{
    [Fact]
    public void Zoom_without_enlarge_discards_overflow_to_the_requested_aspect()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(300, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Png())
        {
            Width = 500,
            Height = 200,
            Mode = ResizeMode.Crop,
            Enlarge = false,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(500, image.Width);
        Assert.Equal(200, image.Height);
        Assert.True(image.HasAlpha);
        image.AlphaNear(2, 100, 0);
        image.Near(250, 100, 30, 140, 60);
    }

    [Fact]
    public void Zoom_without_enlarge_scales_down_when_the_aspect_window_is_larger_than_the_box()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Png())
        {
            Width = 100,
            Height = 100,
            Mode = ResizeMode.Crop,
            Enlarge = false,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }
}
