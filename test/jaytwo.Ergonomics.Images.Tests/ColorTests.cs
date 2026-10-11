using System;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class ColorTests
{
    [Theory]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("FFFFFF", 255, 255, 255)]
    [InlineData("#cfcfcf", 0xCF, 0xCF, 0xCF)]
    [InlineData(" #0A14FF ", 10, 20, 255)]
    [InlineData("#ABC", 0xAA, 0xBB, 0xCC)]
    [InlineData("f0f", 0xFF, 0x00, 0xFF)]
    [InlineData("#ABCF", 0xAA, 0xBB, 0xCC)]
    [InlineData("#AABBCCFF", 0xAA, 0xBB, 0xCC)]
    public void FromHex_parses_opaque_css_hex(string hex, int r, int g, int b)
    {
        Assert.Equal(new ImageColor((byte)r, (byte)g, (byte)b), ImageColor.FromHex(hex));
    }

    [Fact]
    public void FromHex_matches_named_colors()
    {
        Assert.Equal(ImageColor.Black, ImageColor.FromHex("#000"));
        Assert.Equal(ImageColor.White, ImageColor.FromHex("#FFFFFF"));
        Assert.Equal(ImageColor.Gray, ImageColor.FromHex("#808080"));
    }

    [Fact]
    public void FromHex_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => ImageColor.FromHex(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#1234")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#12345678")]
    [InlineData("#GG0000")]
    [InlineData("red")]
    public void FromHex_rejects_invalid_input(string hex)
    {
        Assert.Throws<ArgumentException>(() => ImageColor.FromHex(hex));
    }

    [Theory]
    [InlineData("red", 255, 0, 0)]
    [InlineData("RED", 255, 0, 0)]
    [InlineData(" rebeccapurple ", 102, 51, 153)]
    [InlineData("grey", 128, 128, 128)]
    [InlineData("Gray", 128, 128, 128)]
    [InlineData("aqua", 0, 255, 255)]
    [InlineData("cyan", 0, 255, 255)]
    [InlineData("#0f0", 0, 255, 0)]
    [InlineData("00FF00", 0, 255, 0)]
    [InlineData("#00FF00FF", 0, 255, 0)]
    public void Parse_accepts_names_and_hex(string value, int r, int g, int b)
    {
        Assert.Equal(new ImageColor((byte)r, (byte)g, (byte)b), ImageColor.Parse(value));
    }

    [Fact]
    public void Parse_matches_static_named_colors()
    {
        Assert.Equal(ImageColor.RebeccaPurple, ImageColor.Parse("rebeccapurple"));
        Assert.Equal(ImageColor.AliceBlue, ImageColor.Parse("aliceblue"));
        Assert.Equal(ImageColor.Grey, ImageColor.Gray);
        Assert.Equal(ImageColor.Cyan, ImageColor.Aqua);
        Assert.Equal(ImageColor.Magenta, ImageColor.Fuchsia);
    }

    [Fact]
    public void Named_colors_match_css_hex()
    {
        Assert.Equal(ImageColor.FromHex("#F0F8FF"), ImageColor.AliceBlue);
        Assert.Equal(ImageColor.FromHex("#663399"), ImageColor.RebeccaPurple);
        Assert.Equal(ImageColor.FromHex("#808080"), ImageColor.Gray);
        Assert.Equal(ImageColor.FromHex("#FF00FF"), ImageColor.Fuchsia);
        Assert.Equal(ImageColor.FromHex("#FF00FF"), ImageColor.Magenta);
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => ImageColor.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    [InlineData("transparent")]
    [InlineData("#1234")]
    public void Parse_rejects_invalid_input(string value)
    {
        Assert.Throws<ArgumentException>(() => ImageColor.Parse(value));
    }
}
