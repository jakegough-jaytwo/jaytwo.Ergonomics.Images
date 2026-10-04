using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Cover window shifted so a relative focus rectangle stays inside it.
/// </summary>
internal static class FocusMath
{
    internal static (int Width, int Height) Cover(int sourceWidth, int sourceHeight, double widthOverHeight)
    {
        int cropWidth;
        int cropHeight;
        if (sourceWidth / (double)sourceHeight > widthOverHeight)
        {
            cropHeight = sourceHeight;
            cropWidth = (int)Math.Round(sourceHeight * widthOverHeight);
        }
        else
        {
            cropWidth = sourceWidth;
            cropHeight = (int)Math.Round(sourceWidth / widthOverHeight);
        }

        if (cropWidth < 1)
        {
            cropWidth = 1;
        }
        else if (cropWidth > sourceWidth)
        {
            cropWidth = sourceWidth;
        }

        if (cropHeight < 1)
        {
            cropHeight = 1;
        }
        else if (cropHeight > sourceHeight)
        {
            cropHeight = sourceHeight;
        }

        return (cropWidth, cropHeight);
    }

    internal static FocusWindow Window(int sourceWidth, int sourceHeight, double widthOverHeight, ImageFocus focus)
    {
        var (cropWidth, cropHeight) = Cover(sourceWidth, sourceHeight, widthOverHeight);
        var focusLeft = focus.X * sourceWidth;
        var focusTop = focus.Y * sourceHeight;
        var focusRight = (focus.X + focus.Width) * sourceWidth;
        var focusBottom = (focus.Y + focus.Height) * sourceHeight;
        var (spanLeft, spanRight) = Span(focusLeft, focusRight, sourceWidth);
        var (spanTop, spanBottom) = Span(focusTop, focusBottom, sourceHeight);
        var spanWidth = spanRight - spanLeft;
        var spanHeight = spanBottom - spanTop;
        var pad = spanWidth > cropWidth || spanHeight > cropHeight;
        if (pad)
        {
            (cropWidth, cropHeight) = BackOff(sourceWidth, sourceHeight, spanWidth, spanHeight, widthOverHeight);
        }

        var (left, top) = Place(
            sourceWidth,
            sourceHeight,
            cropWidth,
            cropHeight,
            focusLeft,
            focusTop,
            focusRight,
            focusBottom,
            spanLeft,
            spanTop,
            spanRight,
            spanBottom);
        return new FocusWindow(left, top, cropWidth, cropHeight, pad);
    }

    private static (int Width, int Height) BackOff(
        int sourceWidth,
        int sourceHeight,
        int spanWidth,
        int spanHeight,
        double widthOverHeight)
    {
        int width;
        int height;
        if (spanWidth / (double)spanHeight > widthOverHeight)
        {
            width = spanWidth;
            height = (int)Math.Round(spanWidth / widthOverHeight);
            if (height < spanHeight)
            {
                height = spanHeight;
            }

            if (height > sourceHeight)
            {
                height = sourceHeight;
            }
        }
        else
        {
            height = spanHeight;
            width = (int)Math.Round(spanHeight * widthOverHeight);
            if (width < spanWidth)
            {
                width = spanWidth;
            }

            if (width > sourceWidth)
            {
                width = sourceWidth;
            }
        }

        if (width < spanWidth)
        {
            width = spanWidth;
        }

        if (height < spanHeight)
        {
            height = spanHeight;
        }

        if (width > sourceWidth)
        {
            width = sourceWidth;
        }

        if (height > sourceHeight)
        {
            height = sourceHeight;
        }

        return (width, height);
    }

    private static (int Left, int Top) Place(
        int sourceWidth,
        int sourceHeight,
        int cropWidth,
        int cropHeight,
        double focusLeft,
        double focusTop,
        double focusRight,
        double focusBottom,
        int spanLeft,
        int spanTop,
        int spanRight,
        int spanBottom)
    {
        var left = (int)Math.Round(((focusLeft + focusRight) / 2d) - (cropWidth / 2d));
        var top = (int)Math.Round(((focusTop + focusBottom) / 2d) - (cropHeight / 2d));
        left = Math.Clamp(left, 0, Math.Max(0, sourceWidth - cropWidth));
        top = Math.Clamp(top, 0, Math.Max(0, sourceHeight - cropHeight));

        if (left > spanLeft)
        {
            left = spanLeft;
        }

        if (left + cropWidth < spanRight)
        {
            left = spanRight - cropWidth;
        }

        if (top > spanTop)
        {
            top = spanTop;
        }

        if (top + cropHeight < spanBottom)
        {
            top = spanBottom - cropHeight;
        }

        left = Math.Clamp(left, 0, Math.Max(0, sourceWidth - cropWidth));
        top = Math.Clamp(top, 0, Math.Max(0, sourceHeight - cropHeight));
        return (left, top);
    }

    private static (int Start, int End) Span(double start, double end, int size)
    {
        if (size < 1)
        {
            return (0, 1);
        }

        if (end - start <= 1e-9)
        {
            var pixel = start >= size ? size - 1 : (int)Math.Floor(start);
            if (pixel < 0)
            {
                pixel = 0;
            }

            if (pixel > size - 1)
            {
                pixel = size - 1;
            }

            return (pixel, pixel + 1);
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

    internal readonly record struct FocusWindow(int Left, int Top, int Width, int Height, bool Pad);
}
