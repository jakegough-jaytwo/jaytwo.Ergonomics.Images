namespace jaytwo.Ergonomics.Images;

/// <summary>
/// An opaque 8-bit color used when padding a canvas or flattening alpha.
/// </summary>
public readonly record struct ImageColor
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

    /// <summary>White.</summary>
    public static ImageColor White => new(255, 255, 255);

    /// <summary>Black.</summary>
    public static ImageColor Black => new(0, 0, 0);
}
