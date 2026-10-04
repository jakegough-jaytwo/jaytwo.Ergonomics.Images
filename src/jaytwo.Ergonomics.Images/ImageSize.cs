using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Width and height, in pixels.
/// </summary>
/// <remarks>
/// Resize returns the size it wrote.
/// <see cref="XS"/>, <see cref="S"/>, <see cref="M"/>, <see cref="L"/>, and <see cref="XL"/> are square frames, and each doubles the last.
/// <see cref="Thumbnail"/> is <see cref="S"/>. <see cref="Preview"/> is <see cref="M"/>.
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

    /// <summary>180 by 180.</summary>
    public static ImageSize XS => new(180, 180);

    /// <summary>360 by 360.</summary>
    public static ImageSize S => new(360, 360);

    /// <summary>720 by 720.</summary>
    public static ImageSize M => new(720, 720);

    /// <summary>1440 by 1440.</summary>
    public static ImageSize L => new(1440, 1440);

    /// <summary>2880 by 2880.</summary>
    public static ImageSize XL => new(2880, 2880);

    /// <summary>360 by 360. The same size as <see cref="S"/>.</summary>
    public static ImageSize Thumbnail => S;

    /// <summary>720 by 720. The same size as <see cref="M"/>.</summary>
    public static ImageSize Preview => M;
}
