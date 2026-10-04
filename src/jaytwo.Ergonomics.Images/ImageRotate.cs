using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Angle, bounds, and canvas for one <see cref="ImageTransforms.Rotate"/>.
/// </summary>
/// <remarks>
/// <see cref="Bounds"/> defaults to <see cref="RotationBounds.Expand"/>.
/// <see cref="Canvas"/> paints the corners Expand adds and defaults to transparent.
/// <see cref="Aspect"/> applies with <see cref="RotationBounds.Trim"/> and is part of that crop:
/// the largest centered rectangle of that ratio inside the rotated source.
/// <see cref="SourceAlpha"/> is resolved on the upright source, before the turn, so the canvas stays independent of it.
/// </remarks>
public sealed record ImageRotate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRotate"/> class.
    /// </summary>
    /// <param name="format">Encoded format written to the destination stream.</param>
    public ImageRotate(ImageOutputFormat format)
    {
        if (!Enum.IsDefined(typeof(ImageOutputFormat), format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        Format = format;
    }

    /// <summary>Gets the encoded format written to the destination.</summary>
    public ImageOutputFormat Format { get; }

    /// <summary>Gets the caller rotation. The default is no turn, which still auto-orients and re-encodes.</summary>
    public ImageRotation Rotation { get; init; }

    /// <summary>Gets which rectangle the rotation keeps. Defaults to <see cref="RotationBounds.Expand"/>.</summary>
    public RotationBounds Bounds { get; init; } = RotationBounds.Expand;

    /// <summary>
    /// Gets the ratio of a trim.
    /// </summary>
    /// <remarks>
    /// Applies with <see cref="RotationBounds.Trim"/>. The crop stays centered.
    /// Omit it to keep the largest rectangle of any ratio.
    /// A resampled angle keeps the crop one pixel inside that rectangle.
    /// </remarks>
    public ImageAspectRatio? Aspect { get; init; }

    /// <summary>
    /// Gets the color of the corners <see cref="RotationBounds.Expand"/> adds.
    /// </summary>
    /// <remarks>
    /// Defaults to transparent. A multiple of 90 degrees adds no corners, so the canvas has nothing to paint.
    /// A format that cannot store alpha flattens those corners with <see cref="ImageEncode.AlphaFallbackColor"/>.
    /// </remarks>
    public ImageCanvas? Canvas { get; init; }

    /// <summary>
    /// Gets how transparency inside the source is resolved.
    /// </summary>
    /// <remarks>Defaults to <see cref="AlphaBehavior.Preserve"/>. Applied before the rotation.</remarks>
    public AlphaBehavior SourceAlpha { get; init; } = AlphaBehavior.Preserve;

    /// <summary>Gets orientation and encode overrides. Omit it to keep the defaults.</summary>
    public ImageEncode? Encode { get; init; }
}
