using System;
using System.IO;
using jaytwo.Ergonomics.Images.Metadata;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class IdentifyTests
{
    [Fact]
    public void Identify_reads_structure_and_visual_size_for_orientation_6()
    {
        var bytes = SampleImages.HorizontalHalves(80, 40, orientation: 6);

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.Equal(ImageFormat.Jpeg, meta.Structure.Format);
        Assert.Equal(80, meta.Structure.Size.Width);
        Assert.Equal(40, meta.Structure.Size.Height);
        Assert.Equal(40, meta.Structure.VisualSize.Width);
        Assert.Equal(80, meta.Structure.VisualSize.Height);
        Assert.Equal(6, meta.Structure.Orientation);
        Assert.False(meta.Structure.HasAlpha);
        Assert.Equal(3, meta.Structure.Bands);
        Assert.Equal(1, meta.Structure.PageCount);
    }

    [Fact]
    public void Identify_exposes_exif_fields_and_resolved_camera_date_gps()
    {
        var bytes = SampleImages.HorizontalHalves(
            80,
            40,
            orientation: 1,
            make: "jaytwo-cam",
            model: "Unit-1",
            dateTimeOriginal: "2020:01:15 14:30:00",
            latitude: 37.7749,
            longitude: -122.4194,
            icc: SampleImages.SrgbProfile());

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.True(meta.Exif.Present);
        Assert.Equal("jaytwo-cam", meta.Exif.Ifd0["Make"]);
        Assert.Equal("jaytwo-cam", meta.Exif.Ifd0.Make);
        Assert.Equal("Unit-1", meta.Exif.Ifd0.Model);
        Assert.Equal("Unit-1", meta.CameraModel);
        Assert.Equal("jaytwo-cam", meta.CameraMake);
        Assert.Equal(new DateTime(2020, 1, 15, 14, 30, 0, DateTimeKind.Unspecified), meta.DateTaken);
        Assert.NotNull(meta.Gps);
        Assert.InRange(meta.Gps!.Latitude, 37.7748, 37.7750);
        Assert.InRange(meta.Gps!.Longitude, -122.4195, -122.4193);
        Assert.True(meta.ColorProfile.Present);
        Assert.False(meta.Xmp.Present);
        Assert.False(meta.Iptc.Present);
    }

    [Fact]
    public void Identify_png_with_alpha()
    {
        var bytes = SampleImages.AlphaHalvesPng(40, 20);

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.Equal(ImageFormat.Png, meta.Structure.Format);
        Assert.True(meta.Structure.HasAlpha);
        Assert.Equal(4, meta.Structure.Bands);
        Assert.Null(meta.CameraMake);
        Assert.Null(meta.DateTaken);
        Assert.Null(meta.Gps);
    }

    [Fact]
    public void Identify_resolves_southern_and_western_gps()
    {
        // Sydney Opera House area
        var bytes = SampleImages.HorizontalHalves(16, 8, latitude: -33.8568, longitude: 151.2153);

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.NotNull(meta.Gps);
        Assert.InRange(meta.Gps!.Latitude, -33.8570, -33.8566);
        Assert.InRange(meta.Gps!.Longitude, 151.2151, 151.2155);
    }

    [Fact]
    public void Identify_gps_accepts_a_single_rational_degree()
    {
        var bytes = SampleImages.JpegWithGpsTags(
            latitude: "377749/10000",
            latitudeRef: "N",
            longitude: "1224194/10000",
            longitudeRef: "W");

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.NotNull(meta.Gps);
        Assert.InRange(meta.Gps!.Latitude, 37.7748, 37.7750);
        Assert.InRange(meta.Gps!.Longitude, -122.4195, -122.4193);
    }

    [Fact]
    public void Identify_gps_is_null_when_hemisphere_ref_is_missing()
    {
        var bytes = SampleImages.JpegWithGpsTags(
            latitude: "37/1 46/1 2964/100",
            latitudeRef: "N",
            longitude: "122/1 25/1 984/100",
            longitudeRef: null);

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.Null(meta.Gps);
    }

    [Fact]
    public void Identify_gps_is_null_when_latitude_is_out_of_range()
    {
        var bytes = SampleImages.JpegWithGpsTags(
            latitude: "91/1 0/1 0/1",
            latitudeRef: "N",
            longitude: "10/1 0/1 0/1",
            longitudeRef: "E");

        using var source = new MemoryStream(bytes);
        var meta = ImageTransforms.Identify(source);

        Assert.Null(meta.Gps);
    }

    [Fact]
    public void Identify_rejects_non_image()
    {
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        Assert.Throws<ImageException>(() => ImageTransforms.Identify(source));
    }
}
