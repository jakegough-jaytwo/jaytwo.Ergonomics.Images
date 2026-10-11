using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class CropTests
{
    [Fact]
    public void Crop_takes_the_left_half()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) =>
            ImageTransforms.Crop(source, destination, new ImageCrop(ImageEncoding.Png(), 0, 0, 0.5, 1)));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(190, 100, 220, 20, 20);
    }

    [Fact]
    public void Crop_takes_the_right_half()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) =>
            ImageTransforms.Crop(source, destination, new ImageCrop(ImageEncoding.Png(), 0.5, 0, 0.5, 1)));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 20, 20, 220);
        image.Near(190, 100, 20, 20, 220);
    }

    [Fact]
    public void Pipeline_crop_then_resize_uses_the_cropped_source()
    {
        var pipeline = ImagePipeline.Create()
            .Crop(0.5, 0, 0.5, 1)
            .Resize(100, 100)
            .EncodingPng();

        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) =>
            pipeline.Run(source, destination));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(10, 50, 20, 20, 220);
        image.Near(90, 50, 20, 20, 220);
    }

    [Fact]
    public void Crop_uses_upright_coordinates_after_auto_orient()
    {
        var output = Transform.Png(
            SampleImages.HorizontalHalves(80, 40, orientation: 6),
            (source, destination) =>
                ImageTransforms.Crop(source, destination, new ImageCrop(ImageEncoding.Png(), 0, 0, 1, 0.5)));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(20, 10, 220, 20, 20);
        image.Near(20, 30, 220, 20, 20);
    }

    [Fact]
    public void Crop_rejects_a_null_request()
    {
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ImageTransforms.Crop(Stream.Null, destination, null!));
    }

    [Fact]
    public void Crop_rejects_a_box_outside_the_image()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCrop(ImageEncoding.Png(), -0.1, 0.2, 0.2, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCrop(ImageEncoding.Png(), 0.8, 0.2, 0.3, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCrop(ImageEncoding.Png(), 0.2, 0.8, 0.2, 0.3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCrop(ImageEncoding.Png(), 0.2, 0.2, 0, 0.2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCrop(ImageEncoding.Png(), 0.2, 0.2, 0.2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImagePipeline.Create().Crop(0.2, 0.2, 0, 0.2));
    }
}
