using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Width and height, in pixels.
/// </summary>
/// <remarks>
/// Resize returns the size it wrote.
/// <see cref="XS"/>, <see cref="S"/>, <see cref="M"/>, <see cref="L"/>, <see cref="XL"/>, and <see cref="XXL"/> are square frames, and each doubles the last.
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

    /// <summary>128 by 128.</summary>
    public static ImageSize XS => new(128, 128);

    /// <summary>256 by 256.</summary>
    public static ImageSize S => new(256, 256);

    /// <summary>512 by 512.</summary>
    public static ImageSize M => new(512, 512);

    /// <summary>1024 by 1024.</summary>
    public static ImageSize L => new(1024, 1024);

    /// <summary>2048 by 2048.</summary>
    public static ImageSize XL => new(2048, 2048);

    /// <summary>4096 by 4096.</summary>
    public static ImageSize XXL => new(4096, 4096);

    /// <summary>256 by 256. The same size as <see cref="S"/>.</summary>
    public static ImageSize Thumbnail => S;

    /// <summary>1024 by 1024. The same size as <see cref="L"/>.</summary>
    public static ImageSize Preview => L;
}
