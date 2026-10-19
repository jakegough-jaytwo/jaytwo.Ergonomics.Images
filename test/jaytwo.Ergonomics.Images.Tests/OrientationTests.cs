using System.IO;
using System.Threading.Tasks;
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

    // Photo-sized, so the upside-down autorot reads rows the sequential JPEG decoder has already passed.
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void Auto_orient_flips_a_large_jpeg_from_a_non_seekable_source(int orientation)
    {
        var bytes = SampleImages.HorizontalHalves(4032, 3024, orientation);
        using var source = new NonSeekableStream(new MemoryStream(bytes));
        using var destination = new MemoryStream();
        var size = ImagePipeline.Create()
            .Resize(ImageSize.L)
            .EncodingWebP()
            .Run(source, destination);

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(1008, size.Width);
        Assert.Equal(size.Width, image.Width);
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

    [Theory]
    [InlineData(5, 40, 80, false, 220, 20, 20, true, 20, 20, 220)]
    [InlineData(6, 40, 80, false, 220, 20, 20, true, 20, 20, 220)]
    [InlineData(7, 40, 80, false, 20, 20, 220, true, 220, 20, 20)]
    [InlineData(8, 40, 80, false, 20, 20, 220, true, 220, 20, 20)]
    public void Auto_orient_matches_on_a_non_seekable_stream(
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
        var bytes = SampleImages.HorizontalHalves(80, 40, orientation);
        using var source = new NonSeekableStream(new MemoryStream(bytes));
        using var destination = new MemoryStream();
        ImageTransforms.Resize(
            source,
            destination,
            ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve },
            200,
            200,
            enlarge: false);

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.True(image.Orientation() is null or 1);

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
    public void Crop_uses_upright_coordinates_on_a_non_seekable_orientation_6()
    {
        var bytes = SampleImages.HorizontalHalves(80, 40, orientation: 6);
        using var source = new NonSeekableStream(new MemoryStream(bytes));
        using var destination = new MemoryStream();
        ImageTransforms.Crop(source, destination, new ImageCrop(ImageEncoding.Png(), 0, 0, 1, 0.5));

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(40, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(20, 10, 220, 20, 20);
        image.Near(20, 30, 220, 20, 20);
    }

    [Theory]
    [InlineData(6, 220, 20, 20, 20, 20, 220)]
    [InlineData(8, 20, 20, 220, 220, 20, 20)]
    public async Task Auto_orient_matches_on_RunAsync_with_async_only_source(
        int orientation,
        byte topRed,
        byte topGreen,
        byte topBlue,
        byte bottomRed,
        byte bottomGreen,
        byte bottomBlue)
    {
        var bytes = SampleImages.HorizontalHalves(80, 40, orientation);
        var pipeline = ImagePipeline.Create()
            .Resize(200, 200)
            .Encoding(ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve });

        await using var source = new AsyncOnlyStream(new MemoryStream(bytes));
        await using var destination = new MemoryStream();
        var size = await pipeline.RunAsync(source, destination);

        Assert.Equal(new ImageSize(40, 80), size);
        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        Assert.True(image.Orientation() is null or 1);
        image.Near(20, 8, topRed, topGreen, topBlue);
        image.Near(20, 72, bottomRed, bottomGreen, bottomBlue);
    }
}
