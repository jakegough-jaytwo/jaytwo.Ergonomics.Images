using System;
using System.Globalization;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// An opaque 8-bit color used when padding a canvas or flattening alpha.
/// </summary>
public readonly partial record struct ImageColor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageColor"/> struct.
    /// </summary>
    public ImageColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>Red, 0-255.</summary>
    public byte R { get; }

    /// <summary>Green, 0-255.</summary>
    public byte G { get; }

    /// <summary>Blue, 0-255.</summary>
    public byte B { get; }

    /// <summary>
    /// Parses a CSS / web color: a canonical named color, or a hex color.
    /// </summary>
    /// <param name="value">
    /// A named color such as <c>rebeccapurple</c>, or a CSS hex color
    /// (<c>#RGB</c>, <c>#RGBA</c>, <c>#RRGGBB</c>, <c>#RRGGBBAA</c>; leading <c>#</c> optional).
    /// Alpha forms are accepted only when fully opaque. Case-insensitive; surrounding whitespace is ignored.
    /// </param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a named color or opaque hex color.</exception>
    public static ImageColor Parse(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var trimmed = value.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            throw new ArgumentException("Color value is empty.", nameof(value));
        }

        if (NamedColors.TryGetValue(trimmed.ToString(), out var named))
        {
            return named;
        }

        if (TryParseHex(trimmed, out var color))
        {
            return color;
        }

        throw new ArgumentException(
            "Color must be a CSS named color or an opaque hex color (#RGB, #RGBA, #RRGGBB, or #RRGGBBAA).",
            nameof(value));
    }

    /// <summary>
    /// Creates an <see cref="ImageColor"/> from a CSS hex string.
    /// </summary>
    /// <param name="hex">
    /// <c>#RGB</c>, <c>#RGBA</c>, <c>#RRGGBB</c>, or <c>#RRGGBBAA</c> (leading <c>#</c> optional).
    /// Alpha forms are accepted only when fully opaque. Case-insensitive.
    /// </param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hex"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="hex"/> is not a valid opaque hex color.</exception>
    public static ImageColor FromHex(string hex)
    {
        if (hex is null)
        {
            throw new ArgumentNullException(nameof(hex));
        }

        var span = hex.AsSpan().Trim();
        if (TryParseHex(span, out var color))
        {
            return color;
        }

        throw new ArgumentException(
            "Hex color must be #RGB, #RGBA, #RRGGBB, or #RRGGBBAA (leading # optional; alpha must be opaque).",
            nameof(hex));
    }

    private static bool TryParseHex(ReadOnlySpan<char> span, out ImageColor color)
    {
        color = default;

        if (span.Length > 0 && span[0] == '#')
        {
            span = span[1..];
        }

        switch (span.Length)
        {
            case 3:
                if (TryExpandNibble(span[0], out var r3)
                    && TryExpandNibble(span[1], out var g3)
                    && TryExpandNibble(span[2], out var b3))
                {
                    color = new ImageColor(r3, g3, b3);
                    return true;
                }

                return false;

            case 4:
                if (TryExpandNibble(span[0], out var r4)
                    && TryExpandNibble(span[1], out var g4)
                    && TryExpandNibble(span[2], out var b4)
                    && TryExpandNibble(span[3], out var a4)
                    && a4 == 255)
                {
                    color = new ImageColor(r4, g4, b4);
                    return true;
                }

                return false;

            case 6:
                if (TryParseByte(span[..2], out var r6)
                    && TryParseByte(span[2..4], out var g6)
                    && TryParseByte(span[4..], out var b6))
                {
                    color = new ImageColor(r6, g6, b6);
                    return true;
                }

                return false;

            case 8:
                if (TryParseByte(span[..2], out var r8)
                    && TryParseByte(span[2..4], out var g8)
                    && TryParseByte(span[4..6], out var b8)
                    && TryParseByte(span[6..], out var a8)
                    && a8 == 255)
                {
                    color = new ImageColor(r8, g8, b8);
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    private static bool TryExpandNibble(char digit, out byte value)
    {
        if (!TryParseNibble(digit, out var nibble))
        {
            value = 0;
            return false;
        }

        value = (byte)((nibble << 4) | nibble);
        return true;
    }

    private static bool TryParseByte(ReadOnlySpan<char> hex, out byte value)
        => byte.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);

    private static bool TryParseNibble(char digit, out int nibble)
    {
        if (digit is >= '0' and <= '9')
        {
            nibble = digit - '0';
            return true;
        }

        if (digit is >= 'a' and <= 'f')
        {
            nibble = digit - 'a' + 10;
            return true;
        }

        if (digit is >= 'A' and <= 'F')
        {
            nibble = digit - 'A' + 10;
            return true;
        }

        nibble = 0;
        return false;
    }
}
