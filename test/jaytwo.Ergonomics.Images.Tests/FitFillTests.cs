using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class FitFillTests
{
    [Fact]
    public void Color_letterboxes_a_landscape_image()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(50, 2, 255, 255, 255);
        image.Near(50, 50, 30, 140, 60);
        image.Near(50, 97, 255, 255, 255);
    }

    [Fact]
    public void Color_pillarboxes_a_portrait_image()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(200, 400, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(2, 50, 255, 255, 255);
        image.Near(50, 50, 30, 140, 60);
        image.Near(97, 50, 255, 255, 255);
    }

    [Fact]
    public void Color_argument_paints_the_bars()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.Solid(new ImageColor(10, 20, 30)),
            Encode = new ImageEncode
            {
                AlphaFallbackColor = ImageColor.Black,
            },
        }));

        using var image = DecodedImage.Open(output);
        image.Near(50, 2, 10, 20, 30);
    }

    [Fact]
    public void Color_can_pad_with_transparency()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.Transparent,
        }));

        using var image = DecodedImage.Open(output);
        Assert.True(image.HasAlpha);
        image.AlphaNear(50, 2, 0);
        image.AlphaNear(50, 50, 255);
        image.Near(50, 50, 30, 140, 60);
    }

    [Fact]
    public void Color_upscales_before_padding()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(20, 10, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(50, 50, 30, 140, 60);
        image.Near(50, 2, 255, 255, 255);
    }

    [Fact]
    public void Color_without_enlarge_pads_a_smaller_image()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(20, 10, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 100,
            Canvas = ImageCanvas.SolidWhite,
            Enlarge = false,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(50, 50, 30, 140, 60);
        image.Near(5, 5, 255, 255, 255);
    }

    [Fact]
    public void Jpeg_output_flattens_source_alpha_onto_the_background()
    {
        var output = Transform.Run(
            SampleImages.AlphaHalvesPng(80, 40),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                ImageOutputFormat.Jpeg,
                80,
                40,
                encode: new ImageEncode
                {
                    AlphaFallbackColor = ImageColor.White,
                    Quality = 95,
                }));

        using var image = DecodedImage.Open(output);
        Assert.False(image.HasAlpha);
        image.Near(10, 20, 20, 180, 40);
        image.Near(70, 20, 255, 255, 255);
    }
}
