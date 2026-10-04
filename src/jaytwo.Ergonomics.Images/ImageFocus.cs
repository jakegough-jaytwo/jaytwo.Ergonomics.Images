using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A rectangle on the upright image that a <see cref="ImageFit.Zoom"/> window must keep.
/// </summary>
/// <remarks>
/// <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> are fractions of the image, from 0 to 1.
/// The origin is the top left. The rectangle constrains where the cover window sits.
/// It does not set the window's size. <see cref="Point"/> is a rectangle with no area, and that point stays visible.
/// When the rectangle does not fit in the cover window, cropping stops at the rectangle.
/// </remarks>
public readonly record struct ImageFocus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageFocus"/> struct.
    /// </summary>
    /// <param name="x">Left edge, as a fraction of the width.</param>
    /// <param name="y">Top edge, as a fraction of the height.</param>
    /// <param name="width">Width, as a fraction of the image width.</param>
    /// <param name="height">Height, as a fraction of the image height.</param>
    public ImageFocus(double x, double y, double width, double height)
    {
        if (!IsUnit(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be from 0 to 1.");
        }

        if (!IsUnit(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be from 0 to 1.");
        }

        if (!IsUnit(width))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be from 0 to 1.");
        }

        if (!IsUnit(height))
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be from 0 to 1.");
        }

        if (x + width > 1d)
        {
            if (x + width > 1d + 1e-9)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "The box must stay inside the image.");
            }

            width = 1d - x;
        }

        if (y + height > 1d)
        {
            if (y + height > 1d + 1e-9)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The box must stay inside the image.");
            }

            height = 1d - y;
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Gets the left edge, as a fraction of the width.</summary>
    public double X { get; }

    /// <summary>Gets the top edge, as a fraction of the height.</summary>
    public double Y { get; }

    /// <summary>Gets the width, as a fraction of the image width.</summary>
    public double Width { get; }

    /// <summary>Gets the height, as a fraction of the image height.</summary>
    public double Height { get; }

    /// <summary>A point. The cover window moves so this location stays visible.</summary>
    /// <param name="x">Horizontal position, as a fraction of the width.</param>
    /// <param name="y">Vertical position, as a fraction of the height.</param>
    /// <returns>A focus rectangle with no area.</returns>
    public static ImageFocus Point(double x, double y) => new(x, y, 0d, 0d);

    private static bool IsUnit(double value) => double.IsFinite(value) && value >= 0d && value <= 1d;
}
