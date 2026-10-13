using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class DateTakenTests
{
    [Fact]
    public void Identify_parses_dash_separated_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024-05-01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 33, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_parses_dot_separated_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024.05.01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 33, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_parses_date_taken_without_seconds()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024:05:01 08:12");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 0, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_parses_date_taken_without_time_as_midnight()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024:05:01");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_ignores_text_after_date_taken_seconds()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024:05:01 08:12:33.250+02:00");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 33, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_keeps_unspecified_kind_on_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTimeOriginal: "2024:05:01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(DateTimeKind.Unspecified, meta.DateTaken!.Value.Kind);
    }

    [Fact]
    public void Identify_keeps_old_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(
            16,
            8,
            dateTimeOriginal: "1975:07:04 12:00:00",
            dateTimeDigitized: "2024:05:01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(1975, 7, 4, 12, 0, 0, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_skips_impossible_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(
            16,
            8,
            dateTimeOriginal: "2024:02:30 08:12:33",
            dateTimeDigitized: "2024:05:01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 33, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_skips_all_zero_date_taken()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(
            16,
            8,
            dateTimeOriginal: "0000:00:00 00:00:00",
            dateTimeDigitized: "2024:05:01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal(new DateTime(2024, 5, 1, 8, 12, 33, DateTimeKind.Unspecified), meta.DateTaken);
    }

    [Fact]
    public void Identify_ignores_modified_date()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, dateTime: "2024:05:01 08:12:33");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Equal("2024:05:01 08:12:33", meta.Exif.Ifd0["DateTime"]);
        Assert.Equal("2024:05:01 08:12:33", meta.Exif.Ifd0.DateTime);
        Assert.Null(meta.DateTaken);
    }

    [Fact]
    public void Identify_returns_null_date_taken_when_no_date_parses()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(
            16,
            8,
            dateTimeOriginal: "0000:00:00 00:00:00",
            dateTimeDigitized: "    :  :     :  :  ");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Null(meta.DateTaken);
    }

    [Fact]
    public void Identify_returns_null_date_taken_when_dates_are_missing()
    {
        // arrange
        var bytes = SampleImages.HorizontalHalves(16, 8, make: "jaytwo-cam");
        using var source = new MemoryStream(bytes);

        // act
        var meta = ImageTransforms.Identify(source);

        // assert
        Assert.Null(meta.DateTaken);
    }
}
