using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class RotationTests
{
    [Fact]
    public void Clockwise_and_counter_clockwise_store_the_same_turn()
    {
        Assert.Equal(0d, ImageRotation.Clockwise(0).ClockwiseDegrees);
        Assert.Equal(0d, ImageRotation.Clockwise(360).ClockwiseDegrees);
        Assert.Equal(90d, ImageRotation.Clockwise(90).ClockwiseDegrees);
        Assert.Equal(90d, ImageRotation.Clockwise(450).ClockwiseDegrees);
        Assert.Equal(ImageRotation.Clockwise(270), ImageRotation.CounterClockwise(90));
        Assert.Equal(ImageRotation.Clockwise(0), ImageRotation.CounterClockwise(360));
        Assert.Equal(default, ImageRotation.Clockwise(0));
    }

    [Fact]
    public void Angle_must_be_finite_and_zero_or_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRotation.Clockwise(-1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRotation.CounterClockwise(-0.5d));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRotation.Clockwise(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageRotation.Clockwise(double.PositiveInfinity));
    }

    [Fact]
    public void Trim_aspect_is_not_a_second_crop_of_the_largest_rectangle()
    {
        var open = RotationMath.Inscribed(400, 200, 15d, null);
        var square = RotationMath.Inscribed(400, 200, 15d, 1d);

        Assert.True(square.Width > Math.Min(open.Width, open.Height));
        Assert.Equal(square.Width, square.Height);
        AssertInside(400, 200, 15d, square.Width, square.Height);
        AssertInside(400, 200, 15d, open.Width, open.Height);
    }

    [Theory]
    [InlineData(0d, 80, 40)]
    [InlineData(90d, 40, 80)]
    [InlineData(180d, 80, 40)]
    [InlineData(270d, 40, 80)]
    public void Quarter_turns_keep_the_whole_raster(double degrees, int width, int height)
    {
        var size = RotationMath.Inscribed(80, 40, degrees, null);

        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
    }

    [Fact]
    public void Clockwise_90_moves_the_left_half_to_the_top()
    {
        var output = Render(SampleImages.HorizontalHalves(80, 40), ImageRotation.Clockwise(90));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        image.Near(image.Width / 2, 8, 220, 20, 20);
        image.Near(image.Width / 2, image.Height - 8, 20, 20, 220);
    }

    [Fact]
    public void Clockwise_180_swaps_the_halves()
    {
        var output = Render(SampleImages.HorizontalHalves(80, 40), ImageRotation.Clockwise(180));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(8, image.Height / 2, 20, 20, 220);
        image.Near(image.Width - 8, image.Height / 2, 220, 20, 20);
    }

    [Fact]
    public void Counter_clockwise_90_moves_the_left_half_to_the_bottom()
    {
        var output = Render(SampleImages.HorizontalHalves(80, 40), ImageRotation.CounterClockwise(90));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        image.Near(image.Width / 2, 8, 20, 20, 220);
        image.Near(image.Width / 2, image.Height - 8, 220, 20, 20);
    }

    [Fact]
    public void Expand_grows_and_leaves_the_corners_transparent()
    {
        var output = Render(SampleImages.SolidPng(40, 20, 180, 30, 30), ImageRotation.Clockwise(12));

        using var image = DecodedImage.Open(output);
        Assert.True(image.Width > 40);
        Assert.True(image.Height > 20);
        image.Near(image.Width / 2, image.Height / 2, 180, 30, 30);
        image.AlphaNear(0, 0, 0);
        image.AlphaNear(image.Width - 1, image.Height - 1, 0);
    }

    [Fact]
    public void Expand_can_paint_the_corners()
    {
        var output = Transform.Png(SampleImages.SolidPng(40, 20, 180, 30, 30), (source, destination) =>
            ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = ImageRotation.Clockwise(12),
                Canvas = ImageCanvas.Solid(new ImageColor(0, 180, 0)),
            }));

        using var image = DecodedImage.Open(output);
        image.Near(0, 0, 0, 180, 0);
        image.Near(image.Width / 2, image.Height / 2, 180, 30, 30);
        Assert.False(image.HasAlpha);
    }

    [Fact]
    public void Jpeg_expand_flattens_transparent_corners_to_the_fallback()
    {
        var output = Transform.Run(SampleImages.SolidPng(40, 20, 180, 30, 30), (source, destination) =>
            ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Jpeg)
            {
                Rotation = ImageRotation.Clockwise(12),
                Encode = new ImageEncode { Quality = 95 },
            }));

        using var image = DecodedImage.Open(output);
        Assert.False(image.HasAlpha);
        image.Near(0, 0, 255, 255, 255);
        image.Near(image.Width / 2, image.Height / 2, 180, 30, 30);
    }

    [Fact]
    public void Trim_drops_the_generated_corners()
    {
        var source = SampleImages.SolidPng(40, 20, 180, 30, 30);
        var expanded = Render(source, ImageRotation.Clockwise(12));
        var trimmed = Transform.Png(source, (input, output) =>
            ImageTransforms.Rotate(input, output, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = ImageRotation.Clockwise(12),
                Bounds = RotationBounds.Trim,
            }));

        using var expand = DecodedImage.Open(expanded);
        using var trim = DecodedImage.Open(trimmed);
        var geometric = RotationMath.Inscribed(40, 20, 12d, null);
        var expected = RotationMath.ResampledCrop(geometric.Width, geometric.Height, null);
        Assert.Equal(expected.Width, trim.Width);
        Assert.Equal(expected.Height, trim.Height);
        Assert.True(trim.Width < expand.Width);
        Assert.True(trim.Height < expand.Height);
        trim.Near(0, 0, 180, 30, 30);
        trim.Near(trim.Width - 1, trim.Height - 1, 180, 30, 30);
        trim.Near(trim.Width / 2, trim.Height / 2, 180, 30, 30);
    }

    [Fact]
    public void Trim_aspect_stays_centered_inside_the_rotated_source()
    {
        var output = Transform.Png(SampleImages.SolidPng(80, 40, 180, 30, 30), (source, destination) =>
            ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = ImageRotation.Clockwise(15),
                Bounds = RotationBounds.Trim,
                Aspect = ImageAspectRatio.Square,
            }));

        using var image = DecodedImage.Open(output);
        var geometric = RotationMath.Inscribed(80, 40, 15d, 1d);
        var expected = RotationMath.ResampledCrop(geometric.Width, geometric.Height, 1d);
        Assert.Equal(expected.Width, image.Width);
        Assert.Equal(expected.Height, image.Height);
        image.Near(0, 0, 180, 30, 30);
        image.Near(image.Width - 1, image.Height - 1, 180, 30, 30);
    }

    [Fact]
    public void Zero_degree_trim_to_a_square_keeps_the_center()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(80, 40), (source, destination) =>
            ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
            {
                Bounds = RotationBounds.Trim,
                Aspect = ImageAspectRatio.Square,
            }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(8, image.Height / 2, 220, 20, 20);
        image.Near(image.Width - 8, image.Height / 2, 20, 20, 220);
    }

    [Fact]
    public void Manual_rotation_follows_auto_orient_and_clears_the_tag()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6, make: "jaytwo-cam"),
            (source, destination) => ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Jpeg)
            {
                Rotation = ImageRotation.Clockwise(90),
                Encode = new ImageEncode
                {
                    Quality = 95,
                    Metadata = MetadataPolicy.Preserve,
                },
            }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(40, image.Height);
        image.Near(8, image.Height / 2, 20, 20, 220);
        image.Near(image.Width - 8, image.Height / 2, 220, 20, 20);
        Assert.True(image.Orientation() is null or 1);
        Assert.Equal("jaytwo-cam", image.ExifMake());
    }

    [Fact]
    public void Disabling_auto_orient_rotates_the_stored_raster_and_still_clears_the_tag()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6, make: "jaytwo-cam"),
            (source, destination) => ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Jpeg)
            {
                Rotation = ImageRotation.Clockwise(90),
                Encode = new ImageEncode
                {
                    AutoOrient = false,
                    Quality = 95,
                    Metadata = MetadataPolicy.Preserve,
                },
            }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        image.Near(image.Width / 2, 8, 220, 20, 20);
        image.Near(image.Width / 2, image.Height - 8, 20, 20, 220);
        Assert.True(image.Orientation() is null or 1);
        Assert.Equal("jaytwo-cam", image.ExifMake());
    }

    [Fact]
    public void Flatten_does_not_paint_the_expanded_corners()
    {
        var output = Transform.Png(SampleImages.AlphaHalvesPng(40, 20), (source, destination) =>
            ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = ImageRotation.Clockwise(12),
                SourceAlpha = AlphaBehavior.Flatten(ImageColor.White),
            }));

        using var image = DecodedImage.Open(output);
        image.AlphaNear(0, 0, 0);
        image.AlphaNear(image.Width / 2, image.Height / 2, 255);
    }

    [Fact]
    public void Rotate_accepts_a_non_seekable_source()
    {
        var bytes = SampleImages.HorizontalHalves(80, 40);
        using var source = new NonSeekableStream(new MemoryStream(bytes));
        using var destination = new MemoryStream();
        var size = ImageTransforms.Rotate(source, destination, new ImageRotate(ImageOutputFormat.Png)
        {
            Rotation = ImageRotation.Clockwise(12),
        });

        using var image = DecodedImage.Open(destination.ToArray());
        Assert.Equal(size.Width, image.Width);
        Assert.True(image.Width > 80);
        Assert.True(image.Height > 40);
    }

    [Fact]
    public void Rotate_rejects_a_null_request()
    {
        using var destination = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => ImageTransforms.Rotate(Stream.Null, destination, null!));
    }

    [Fact]
    public void Expand_rejects_an_aspect_ratio()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Rotate(null!, null!, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = ImageRotation.Clockwise(12),
                Aspect = ImageAspectRatio.Square,
            }));
    }

    [Fact]
    public void Trim_rejects_a_canvas()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageTransforms.Rotate(null!, null!, new ImageRotate(ImageOutputFormat.Png)
            {
                Bounds = RotationBounds.Trim,
                Canvas = ImageCanvas.SolidWhite,
            }));
    }

    private static byte[] Render(byte[] source, ImageRotation rotation)
    {
        return Transform.Png(source, (input, output) =>
            ImageTransforms.Rotate(input, output, new ImageRotate(ImageOutputFormat.Png)
            {
                Rotation = rotation,
            }));
    }

    private static void AssertInside(int sourceWidth, int sourceHeight, double degrees, int width, int height)
    {
        var radians = degrees * Math.PI / 180d;
        var cosine = Math.Abs(Math.Cos(radians));
        var sine = Math.Abs(Math.Sin(radians));
        Assert.True((width * cosine) + (height * sine) <= sourceWidth + 1e-6);
        Assert.True((width * sine) + (height * cosine) <= sourceHeight + 1e-6);
    }
}
