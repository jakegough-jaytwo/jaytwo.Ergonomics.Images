using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ImageSizeTests
{
    [Fact]
    public void Named_sizes_are_square_and_the_aliases_match()
    {
        Assert.Equal(new ImageSize(144, 144), ImageSize.XS);
        Assert.Equal(new ImageSize(288, 288), ImageSize.S);
        Assert.Equal(new ImageSize(576, 576), ImageSize.M);
        Assert.Equal(new ImageSize(1008, 1008), ImageSize.L);
        Assert.Equal(new ImageSize(2016, 2016), ImageSize.XL);
        Assert.Equal(new ImageSize(4032, 4032), ImageSize.XXL);
        Assert.Equal(ImageSize.S, ImageSize.Thumbnail);
        Assert.Equal(ImageSize.L, ImageSize.Preview);
    }

    [Fact]
    public void Square_size_fits_the_long_side()
    {
        var output = Transform.Png(SampleImages.SolidPng(800, 400, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, ImageEncoding.Png(), ImageSize.Thumbnail);
            Assert.Equal(new ImageSize(288, 144), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(288, image.Width);
        Assert.Equal(144, image.Height);
    }

    [Fact]
    public void Square_size_fits_a_tall_image_by_its_height()
    {
        var output = Transform.Png(SampleImages.SolidPng(100, 400, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, new ImageResize(ImageEncoding.Png(), ImageSize.S));
            Assert.Equal(new ImageSize(72, 288), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(72, image.Width);
        Assert.Equal(288, image.Height);
    }

    [Fact]
    public void Custom_size_is_the_frame()
    {
        var output = Transform.Png(SampleImages.SolidPng(400, 200, 30, 140, 60), (source, destination) =>
        {
            var size = ImageTransforms.Resize(source, destination, ImageEncoding.Png(), new ImageSize(100, 100));
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
            var size = ImageTransforms.Resize(source, destination, ImageEncoding.Png(), ImageSize.Preview, enlarge: false);
            Assert.Equal(new ImageSize(40, 20), size);
        });

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }
}
