using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ImageSizeTests
{
    [Fact]
    public void Named_sizes_are_square_and_the_aliases_match()
    {
        Assert.Equal(new ImageSize(180, 180), ImageSize.XS);
        Assert.Equal(new ImageSize(360, 360), ImageSize.S);
        Assert.Equal(new ImageSize(720, 720), ImageSize.M);
        Assert.Equal(new ImageSize(1440, 1440), ImageSize.L);
        Assert.Equal(new ImageSize(2880, 2880), ImageSize.XL);
        Assert.Equal(ImageSize.S, ImageSize.Thumbnail);
        Assert.Equal(ImageSize.M, ImageSize.Preview);
    }

    [Fact]
    public void Square_size_fits_the_long_side()
    {
        var output = Transform.Png(SampleImages.SolidPng(800, 400, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, ImageSize.Thumbnail);
            Assert.Equal(new ImageSize(360, 180), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(360, image.Width);
        Assert.Equal(180, image.Height);
    }

    [Fact]
    public void Square_size_fits_a_tall_image_by_its_height()
    {
        var output = Transform.Png(SampleImages.SolidPng(100, 400, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png, ImageSize.S));
            Assert.Equal(new ImageSize(90, 360), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(90, image.Width);
        Assert.Equal(360, image.Height);
    }

    [Fact]
    public void Custom_size_is_the_frame()
    {
        var output = Transform.Png(SampleImages.SolidPng(400, 200, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, new ImageSize(100, 100));
            Assert.Equal(new ImageSize(100, 50), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
    }

    [Fact]
    public void Size_without_enlarge_leaves_a_smaller_source()
    {
        var output = Transform.Png(SampleImages.SolidPng(40, 20, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, ImageSize.Preview, enlarge: false);
            Assert.Equal(new ImageSize(40, 20), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }
}
