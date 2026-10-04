using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A width-to-height ratio used as a resize frame.
/// </summary>
/// <remarks>
/// The stored value is width divided by height. <see cref="Square"/> is 1.
/// <c>new ImageAspectRatio(2, 2)</c> and <c>new ImageAspectRatio(1d)</c> are the same value.
/// </remarks>
public readonly record struct ImageAspectRatio
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageAspectRatio"/> struct.
    /// </summary>
    /// <param name="widthOverHeight">Width divided by height. Must be finite and positive.</param>
    public ImageAspectRatio(double widthOverHeight)
    {
        if (!IsInRange(widthOverHeight))
        {
            throw new ArgumentOutOfRangeException(nameof(widthOverHeight), "Width over height must be finite and positive.");
        }

        WidthOverHeight = widthOverHeight;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageAspectRatio"/> struct from ratio terms.
    /// </summary>
    /// <param name="width">Horizontal term. Must be positive.</param>
    /// <param name="height">Vertical term. Must be positive.</param>
    /// <remarks>Stores <paramref name="width"/> divided by <paramref name="height"/>.</remarks>
    public ImageAspectRatio(int width, int height)
        : this(Quotient(width, height))
    {
    }

    /// <summary>Gets the width divided by the height.</summary>
    public double WidthOverHeight { get; }

    /// <summary>1:1.</summary>
    public static ImageAspectRatio Square => new(1, 1);

    /// <summary>4:3.</summary>
    public static ImageAspectRatio FourByThree => new(4, 3);

    /// <summary>3:4.</summary>
    public static ImageAspectRatio ThreeByFour => new(3, 4);

    /// <summary>16:9.</summary>
    public static ImageAspectRatio SixteenByNine => new(16, 9);

    /// <summary>9:16.</summary>
    public static ImageAspectRatio NineBySixteen => new(9, 16);

    internal bool IsInRange() => IsInRange(WidthOverHeight);

    private static bool IsInRange(double widthOverHeight) => double.IsFinite(widthOverHeight) && widthOverHeight > 0d;

    private static double Quotient(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        return (double)width / height;
    }
}
