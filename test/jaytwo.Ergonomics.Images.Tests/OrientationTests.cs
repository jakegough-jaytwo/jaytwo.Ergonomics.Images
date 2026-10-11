using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class OrientationTests
{
    [Theory]
    [InlineData(1, 80, 40, true, 220, 20, 20, false, 20, 20, 220)]
    [InlineData(2, 80, 40, true, 20, 20, 220, false, 220, 20, 20)]
    [InlineData(3, 80, 40, true, 20, 20, 220, false, 220, 20, 20)]
    [InlineData(4, 80, 40, true, 220, 20, 20, false, 20, 20, 220)]
    [InlineData(5, 40, 80, false, 220, 20, 20, true, 20, 20, 220)]
    [InlineData(6, 40, 80, false, 220, 20, 20, true, 20, 20, 220)]
    [InlineData(7, 40, 80, false, 20, 20, 220, true, 220, 20, 20)]
    [InlineData(8, 40, 80, false, 20, 20, 220, true, 220, 20, 20)]
    public void Auto_orient_uses_visual_dimensions(
        int orientation,
        int width,
        int height,
        bool sampleLeft,
        byte firstRed,
        byte firstGreen,
        byte firstBlue,
        bool sampleTop,
        byte secondRed,
        byte secondGreen,
        byte secondBlue)
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve },
                200,
                200,
                enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);

        var orientationTag = image.Orientation();
        Assert.True(orientationTag is null or 1);

        if (sampleLeft)
        {
            image.Near(8, height / 2, firstRed, firstGreen, firstBlue);
            image.Near(width - 8, height / 2, secondRed, secondGreen, secondBlue);
        }
        else if (sampleTop)
        {
            image.Near(width / 2, 8, firstRed, firstGreen, firstBlue);
            image.Near(width / 2, height - 8, secondRed, secondGreen, secondBlue);
        }
    }

    [Fact]
    public void Disabling_auto_orient_keeps_the_stored_raster()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                new ImageResize(ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve })
                {
                    Width = 200,
                    Height = 200,
                    AutoOrient = false,
                }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
        Assert.Equal(6, image.Orientation());
    }
}
