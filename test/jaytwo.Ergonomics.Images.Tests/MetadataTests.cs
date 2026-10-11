using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class MetadataTests
{
    [Fact]
    public void Preserve_keeps_exif_and_normalizes_orientation()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6, make: "jaytwo-cam", icc: SampleImages.SrgbProfile()),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Preserve },
                200,
                200,
                enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        Assert.Equal("jaytwo-cam", image.ExifMake());
        Assert.True(image.HasIccProfile());
        var orientation = image.Orientation();
        Assert.True(orientation is null or 1);
    }

    [Fact]
    public void Strip_removes_exif_and_the_color_profile()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 6, make: "jaytwo-cam", icc: SampleImages.SrgbProfile()),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.Strip },
                200,
                200,
                enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        Assert.Null(image.ExifMake());
        Assert.False(image.HasIccProfile());
    }

    [Fact]
    public void PreserveColorProfileOnly_drops_camera_metadata()
    {
        var output = Transform.Run(
            SampleImages.HorizontalHalves(80, 40, orientation: 1, make: "jaytwo-cam", icc: SampleImages.SrgbProfile()),
            (source, destination) => ImageTransforms.Resize(
                source,
                destination,
                ImageEncoding.Jpeg(95) with { Metadata = MetadataPolicy.PreserveColorProfileOnly },
                40,
                20,
                enlarge: false));

        using var image = DecodedImage.Open(output);
        Assert.Null(image.ExifMake());
        Assert.True(image.HasIccProfile());
    }
}
