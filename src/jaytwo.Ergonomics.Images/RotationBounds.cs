namespace jaytwo.Ergonomics.Images;

/// <summary>
/// How much of an arbitrary rotation is kept.
/// </summary>
/// <remarks>
/// A multiple of 90 degrees does not create corners, so both values describe the same rectangle
/// until <see cref="ImageRotate.Aspect"/> narrows a trim.
/// </remarks>
public enum RotationBounds
{
    /// <summary>
    /// The smallest axis-aligned rectangle that contains every rotated source pixel.
    /// Corners that rectangle adds are the canvas.
    /// </summary>
    Expand,

    /// <summary>
    /// The largest centered axis-aligned rectangle that stays inside the rotated source.
    /// A resampled angle keeps the crop one pixel inside that rectangle.
    /// </summary>
    Trim,
}
