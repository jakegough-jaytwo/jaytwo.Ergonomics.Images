using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Angle, bounds, and canvas for one <see cref="ImageTransforms.Rotate"/> shortcut (compiles to <see cref="ImagePipeline"/>).
/// </summary>
/// <remarks>
/// <see cref="Bounds"/> defaults to <see cref="RotationBounds.Expand"/>.
/// <see cref="Canvas"/> is the plate for corners Expand adds (defaults to transparent); see
/// <see cref="ImageCanvas"/> for how solid/checkerboard plates interact with preserved content alpha.
/// <see cref="Aspect"/> applies with <see cref="RotationBounds.Trim"/> and is part of that crop:
/// the largest centered rectangle of that ratio inside the rotated source.
/// <see cref="AutoOrient"/> and <see cref="SourceAlpha"/> are source admission (same as
/// <see cref="ImagePipeline.Create"/>). Source alpha runs after auto-orient and before the turn,
/// so expand corners stay independent of flatten-or-preserve.
/// </remarks>
public sealed record ImageRotate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageRotate"/> class.
    /// </summary>
    /// <param name="encoding">Output encoding written to the destination stream.</param>
    public ImageRotate(ImageEncoding encoding)
    {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    /// <summary>Gets the output encoding written to the destination.</summary>
    public ImageEncoding Encoding { get; }

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
    /// Gets the plate for corners <see cref="RotationBounds.Expand"/> adds.
    /// </summary>
    /// <remarks>
    /// Defaults to transparent. Solid and checkerboard plates also show through preserved content
    /// alpha on the expanded rectangle — see <see cref="ImageCanvas"/>.
    /// A format that cannot store alpha still flattens remaining alpha with
    /// <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/> at encode time.
    /// </remarks>
    public ImageCanvas? Canvas { get; init; }

    /// <summary>
    /// Gets a value indicating whether EXIF/XMP orientation is applied before the turn.
    /// </summary>
    /// <remarks>Defaults to <c>true</c>. Same admission as <see cref="ImagePipelineSource.AutoOrient"/>.</remarks>
    public bool AutoOrient { get; init; } = true;

    /// <summary>
    /// Gets how transparency inside the source is resolved after auto-orient and before the turn.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="AlphaBehavior.Preserve"/>. Preserve or flatten only.
    /// Separate from <see cref="Canvas"/> and from JPEG <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>.
    /// </remarks>
    public AlphaBehavior SourceAlpha { get; init; } = AlphaBehavior.Preserve;
}
