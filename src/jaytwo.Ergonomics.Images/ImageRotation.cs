using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A caller-requested turn, measured clockwise from the upright image.
/// </summary>
/// <remarks>
/// <see cref="Clockwise"/> and <see cref="CounterClockwise"/> take a non-negative magnitude in degrees.
/// A multiple of 90 is an exact quarter turn. Any other angle resamples.
/// This is not EXIF orientation. Auto-orient runs first, then this turn.
/// </remarks>
public readonly record struct ImageRotation
{
    private ImageRotation(double clockwiseDegrees)
    {
        ClockwiseDegrees = clockwiseDegrees;
    }

    /// <summary>Gets the turn in degrees, clockwise, in the range 0 inclusive to 360 exclusive.</summary>
    public double ClockwiseDegrees { get; }

    /// <summary>Turns clockwise by <paramref name="degrees"/>.</summary>
    /// <param name="degrees">A finite magnitude of zero or more.</param>
    /// <returns>The clockwise turn.</returns>
    public static ImageRotation Clockwise(double degrees)
    {
        if (!double.IsFinite(degrees) || degrees < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), "Degrees must be finite and zero or positive.");
        }

        return new ImageRotation(degrees % 360d);
    }

    /// <summary>Turns counter-clockwise by <paramref name="degrees"/>.</summary>
    /// <param name="degrees">A finite magnitude of zero or more.</param>
    /// <returns>The same turn, stored clockwise.</returns>
    public static ImageRotation CounterClockwise(double degrees)
    {
        if (!double.IsFinite(degrees) || degrees < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), "Degrees must be finite and zero or positive.");
        }

        var clockwise = degrees % 360d;
        if (clockwise == 0d)
        {
            return new ImageRotation(0d);
        }

        return new ImageRotation(360d - clockwise);
    }
}
