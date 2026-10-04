using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Centered crop that stays inside a rotated rectangle.
/// </summary>
internal static class RotationMath
{
    internal static (int Width, int Height) Inscribed(
        int sourceWidth,
        int sourceHeight,
        double clockwiseDegrees,
        double? widthOverHeight)
    {
        if (TryQuarter(clockwiseDegrees, sourceWidth, sourceHeight, out var turnedWidth, out var turnedHeight))
        {
            if (widthOverHeight is not double ratio)
            {
                return (turnedWidth, turnedHeight);
            }

            return FitRatio(turnedWidth, turnedHeight, ratio);
        }

        var radians = clockwiseDegrees * Math.PI / 180d;
        var cosine = Math.Abs(Math.Cos(radians));
        var sine = Math.Abs(Math.Sin(radians));
        if (widthOverHeight is double requested)
        {
            return FitConstraints(sourceWidth, sourceHeight, cosine, sine, requested);
        }

        return MaxArea(sourceWidth, sourceHeight, cosine, sine);
    }

    internal static (int Width, int Height) ResampledCrop(int width, int height, double? widthOverHeight)
    {
        var boxWidth = width > 2 ? width - 2 : width;
        var boxHeight = height > 2 ? height - 2 : height;
        if (widthOverHeight is not double ratio)
        {
            return (boxWidth, boxHeight);
        }

        return FitRatio(boxWidth, boxHeight, ratio);
    }

    private static bool TryQuarter(double degrees, int width, int height, out int turnedWidth, out int turnedHeight)
    {
        if (degrees == 0d || degrees == 180d)
        {
            turnedWidth = width;
            turnedHeight = height;
            return true;
        }

        if (degrees == 90d || degrees == 270d)
        {
            turnedWidth = height;
            turnedHeight = width;
            return true;
        }

        turnedWidth = 0;
        turnedHeight = 0;
        return false;
    }

    private static (int Width, int Height) FitRatio(int width, int height, double widthOverHeight)
    {
        int cropWidth;
        int cropHeight;
        if (width / (double)height > widthOverHeight)
        {
            cropHeight = height;
            cropWidth = (int)Math.Round(height * widthOverHeight);
        }
        else
        {
            cropWidth = width;
            cropHeight = (int)Math.Round(width / widthOverHeight);
        }

        if (cropWidth < 1)
        {
            cropWidth = 1;
        }
        else if (cropWidth > width)
        {
            cropWidth = width;
        }

        if (cropHeight < 1)
        {
            cropHeight = 1;
        }
        else if (cropHeight > height)
        {
            cropHeight = height;
        }

        return (cropWidth, cropHeight);
    }

    private static (int Width, int Height) FitConstraints(
        double sourceWidth,
        double sourceHeight,
        double cosine,
        double sine,
        double widthOverHeight)
    {
        var heightLimit = Math.Min(
            sourceWidth / ((widthOverHeight * cosine) + sine),
            sourceHeight / ((widthOverHeight * sine) + cosine));
        return (WholePixels(widthOverHeight * heightLimit), WholePixels(heightLimit));
    }

    private static (int Width, int Height) MaxArea(double width, double height, double cosine, double sine)
    {
        var widthIsLonger = width >= height;
        var sideLong = widthIsLonger ? width : height;
        var sideShort = widthIsLonger ? height : width;
        double rawWidth;
        double rawHeight;
        if (sideShort <= (2d * sine * cosine * sideLong) || Math.Abs(sine - cosine) < 1e-10)
        {
            var halfShort = 0.5d * sideShort;
            if (widthIsLonger)
            {
                rawWidth = halfShort / sine;
                rawHeight = halfShort / cosine;
            }
            else
            {
                rawWidth = halfShort / cosine;
                rawHeight = halfShort / sine;
            }
        }
        else
        {
            var cos2 = (cosine * cosine) - (sine * sine);
            rawWidth = ((width * cosine) - (height * sine)) / cos2;
            rawHeight = ((height * cosine) - (width * sine)) / cos2;
        }

        return (WholePixels(rawWidth), WholePixels(rawHeight));
    }

    private static int WholePixels(double value)
    {
        if (!double.IsFinite(value) || value < 1d)
        {
            return 1;
        }

        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)Math.Floor(value);
    }
}
