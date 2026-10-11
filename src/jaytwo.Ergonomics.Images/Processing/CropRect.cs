using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Relative crop rectangle validation and pixel conversion.
/// </summary>
internal static class CropRect
{
    internal static (double X, double Y, double Width, double Height) Normalize(
        double x,
        double y,
        double width,
        double height)
    {
        if (!IsUnit(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be from 0 to 1.");
        }

        if (!IsUnit(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be from 0 to 1.");
        }

        if (!IsUnit(width) || width <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0 and at most 1.");
        }

        if (!IsUnit(height) || height <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0 and at most 1.");
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

        if (width <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0 and at most 1.");
        }

        if (height <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0 and at most 1.");
        }

        return (x, y, width, height);
    }

    internal static (int Left, int Top, int Width, int Height) ToPixels(
        int sourceWidth,
        int sourceHeight,
        double x,
        double y,
        double width,
        double height)
    {
        var (left, right) = Span(x * sourceWidth, (x + width) * sourceWidth, sourceWidth);
        var (top, bottom) = Span(y * sourceHeight, (y + height) * sourceHeight, sourceHeight);
        return (left, top, right - left, bottom - top);
    }

    private static (int Start, int End) Span(double start, double end, int size)
    {
        if (size < 1)
        {
            return (0, 1);
        }

        var left = (int)Math.Floor(start);
        var right = (int)Math.Ceiling(end - 1e-9);
        if (left < 0)
        {
            left = 0;
        }

        if (right > size)
        {
            right = size;
        }

        if (right <= left)
        {
            right = Math.Min(size, left + 1);
        }

        return (left, right);
    }

    private static bool IsUnit(double value) => double.IsFinite(value) && value >= 0d && value <= 1d;
}
