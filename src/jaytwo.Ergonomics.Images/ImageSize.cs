using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Width and height, in pixels.
/// </summary>
/// <remarks>
/// Resize returns the size it wrote.
/// <see cref="XS"/>, <see cref="S"/>, <see cref="M"/>, <see cref="L"/>, <see cref="XL"/>, and <see cref="XXL"/> are square frames.
/// <see cref="Thumbnail"/> is <see cref="S"/>. <see cref="Preview"/> is <see cref="L"/>.
/// </remarks>
public readonly record struct ImageSize
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageSize"/> struct.
    /// </summary>
    public ImageSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        Width = width;
        Height = height;
    }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>144 by 144.</summary>
    public static ImageSize XS => new(144, 144);

    /// <summary>288 by 288.</summary>
    public static ImageSize S => new(288, 288);

    /// <summary>576 by 576.</summary>
    public static ImageSize M => new(576, 576);

    /// <summary>1008 by 1008.</summary>
    public static ImageSize L => new(1008, 1008);

    /// <summary>2016 by 2016.</summary>
    public static ImageSize XL => new(2016, 2016);

    /// <summary>4032 by 4032.</summary>
    public static ImageSize XXL => new(4032, 4032);

    /// <summary>288 by 288. The same size as <see cref="S"/>.</summary>
    public static ImageSize Thumbnail => S;

    /// <summary>1008 by 1008. The same size as <see cref="L"/>.</summary>
    public static ImageSize Preview => L;
}
