using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using jaytwo.Ergonomics.Images.Metadata;
using NetVips;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ExifFieldsTests
{
    private static readonly Regex LibvipsSuffixTail = new(
        @", \d+ components, \d+ bytes\)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void Identify_exif_ifds_use_standard_tag_names()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(80, 40, make: "jaytwo-cam");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.DoesNotContain(AllKeys(meta.Exif), key => key.StartsWith("exif-", StringComparison.Ordinal));
    }

    [Fact]
    public void Identify_exif_ifd0_returns_raw_fraction()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(80, 40);
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal("25400/1000", meta.Exif.Ifd0["XResolution"]);
        Assert.Equal("25400/1000", meta.Exif.Ifd0.XResolution);
    }

    [Fact]
    public void Identify_exif_ifd0_returns_raw_short()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(80, 40, orientation: 1);
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal("1", meta.Exif.Ifd0["Orientation"]);
        Assert.Equal("1", meta.Exif.Ifd0.Orientation);
    }

    [Fact]
    public void Identify_exif_ifd3_returns_raw_gps_triplet()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(80, 40, latitude: 37.7749, longitude: -122.4194);
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal("37/1 46/1 2964/100", meta.Exif.Ifd3["GPSLatitude"]);
        Assert.Equal("37/1 46/1 2964/100", meta.Exif.Ifd3.GPSLatitude);
        Assert.NotNull(meta.Exif.Ifd3.Gps);
    }

    [Fact]
    public void Identify_exif_ifds_strip_libvips_suffix()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(
            80,
            40,
            make: "jaytwo-cam",
            latitude: 37.7749,
            longitude: -122.4194);
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.DoesNotContain(AllValues(meta.Exif), value => LibvipsSuffixTail.IsMatch(value));
    }

    [Fact]
    public void Identify_exif_keeps_image_and_thumbnail_xresolution_separate()
    {
        // arrange
        var bytes = File.ReadAllBytes(Fixtures.Path("still-tiny.heic"));
        var heicRequired = string.Equals(
            Environment.GetEnvironmentVariable("REQUIRE_HEIC"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!ImageTransforms.Capabilities.Heic)
        {
            Assert.False(heicRequired, "REQUIRE_HEIC is set, but this libvips build cannot decode a HEVC HEIC.");
            Assert.Throws<VipsException>(() =>
            {
                using var image = Image.NewFromBuffer(bytes);
                _ = image.Avg();
            });
            return;
        }

        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal("240/1", meta.Exif.Ifd0["XResolution"]);
        Assert.Equal("72/1", meta.Exif.Ifd1["XResolution"]);
    }

    private static IEnumerable<string> AllKeys(ExifMetadata exif)
    {
        foreach (var key in exif.Ifd0.Keys)
        {
            yield return key;
        }

        foreach (var key in exif.Ifd1.Keys)
        {
            yield return key;
        }

        foreach (var key in exif.Ifd2.Keys)
        {
            yield return key;
        }

        foreach (var key in exif.Ifd3.Keys)
        {
            yield return key;
        }

        foreach (var key in exif.Ifd4.Keys)
        {
            yield return key;
        }
    }

    private static IEnumerable<string> AllValues(ExifMetadata exif)
    {
        foreach (var value in exif.Ifd0.Values)
        {
            yield return value;
        }

        foreach (var value in exif.Ifd1.Values)
        {
            yield return value;
        }

        foreach (var value in exif.Ifd2.Values)
        {
            yield return value;
        }

        foreach (var value in exif.Ifd3.Values)
        {
            yield return value;
        }

        foreach (var value in exif.Ifd4.Values)
        {
            yield return value;
        }
    }
}
