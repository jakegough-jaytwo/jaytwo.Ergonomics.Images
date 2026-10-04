using System;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class AspectRatioTests
{
    [Fact]
    public void Named_ratios_match_the_pair_and_the_quotient()
    {
        Assert.Equal(new ImageAspectRatio(1, 1), ImageAspectRatio.Square);
        Assert.Equal(new ImageAspectRatio(1d), ImageAspectRatio.Square);
        Assert.Equal(new ImageAspectRatio(4, 3), ImageAspectRatio.FourByThree);
        Assert.Equal(new ImageAspectRatio(3, 4), ImageAspectRatio.ThreeByFour);
        Assert.Equal(new ImageAspectRatio(16, 9), ImageAspectRatio.SixteenByNine);
        Assert.Equal(new ImageAspectRatio(16d / 9d), ImageAspectRatio.SixteenByNine);
        Assert.Equal(new ImageAspectRatio(9, 16), ImageAspectRatio.NineBySixteen);
        Assert.Equal(16d / 9d, ImageAspectRatio.SixteenByNine.WidthOverHeight);
    }

    [Fact]
    public void Unreduced_pair_matches_the_quotient()
    {
        Assert.Equal(new ImageAspectRatio(16, 9), new ImageAspectRatio(32, 18));
        Assert.Equal(ImageAspectRatio.Square, new ImageAspectRatio(2, 2));
    }

    [Fact]
    public void Ratio_rejects_a_non_positive_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(-2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(1, -2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(0d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(-1.5d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageAspectRatio(double.PositiveInfinity));
    }

    [Fact]
    public void Zoom_to_square_keeps_the_center_at_the_source_size()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Aspect = ImageAspectRatio.Square,
            Fit = ImageFit.Zoom,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(190, 100, 20, 20, 220);
    }

    [Fact]
    public void Fit_pads_to_square_at_the_source_size()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Aspect = ImageAspectRatio.Square,
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(400, image.Width);
        Assert.Equal(400, image.Height);
        image.Near(200, 2, 255, 255, 255);
        image.Near(200, 200, 30, 140, 60);
    }

    [Fact]
    public void One_side_and_an_aspect_ratio_calculate_the_other_side()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(400, 400, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 160,
            Aspect = ImageAspectRatio.SixteenByNine,
            Fit = ImageFit.Zoom,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(160, image.Width);
        Assert.Equal(90, image.Height);
    }

    [Fact]
    public void Both_sides_and_an_aspect_ratio_fit_the_ratio_inside_the_box()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(800, 200, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 400,
            Height = 300,
            Aspect = ImageAspectRatio.Square,
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(300, image.Width);
        Assert.Equal(300, image.Height);
        image.Near(150, 2, 255, 255, 255);
        image.Near(150, 150, 30, 140, 60);
    }

    [Fact]
    public void Zoom_to_sixteen_by_nine_discards_overflow_at_the_source_size()
    {
        var output = Transform.Png(SampleImages.SolidJpeg(300, 300, 30, 140, 60), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Aspect = ImageAspectRatio.SixteenByNine,
            Fit = ImageFit.Zoom,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(300, image.Width);
        Assert.Equal(169, image.Height);
    }
}
