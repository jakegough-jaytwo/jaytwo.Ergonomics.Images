using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Relative rectangle and encode options for one <see cref="ImageTransforms.Crop"/> shortcut (compiles to <see cref="ImagePipeline"/>).
/// </summary>
/// <remarks>
/// <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> are fractions of the upright image, from 0 to 1.
/// The origin is the top left. The box must stay inside the image and have positive area.
/// <see cref="AutoOrient"/> and <see cref="SourceAlpha"/> are source admission (same as
/// <see cref="ImagePipeline.Create"/>).
/// </remarks>
public sealed record ImageCrop
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageCrop"/> class.
    /// </summary>
    /// <param name="encoding">Output encoding written to the destination stream.</param>
    /// <param name="x">Left edge, as a fraction of the width.</param>
    /// <param name="y">Top edge, as a fraction of the height.</param>
    /// <param name="width">Width, as a fraction of the image width.</param>
    /// <param name="height">Height, as a fraction of the image height.</param>
    public ImageCrop(ImageEncoding encoding, double x, double y, double width, double height)
    {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        (X, Y, Width, Height) = CropRect.Normalize(x, y, width, height);
    }

    /// <summary>Gets the output encoding written to the destination.</summary>
    public ImageEncoding Encoding { get; }

    /// <summary>Gets the left edge, as a fraction of the width.</summary>
    public double X { get; }

    /// <summary>Gets the top edge, as a fraction of the height.</summary>
    public double Y { get; }

    /// <summary>Gets the width, as a fraction of the image width.</summary>
    public double Width { get; }

    /// <summary>Gets the height, as a fraction of the image height.</summary>
    public double Height { get; }

    /// <summary>
    /// Gets a value indicating whether EXIF/XMP orientation is applied before the crop.
    /// </summary>
    /// <remarks>Defaults to <c>true</c>. Same admission as <see cref="ImagePipelineSource.AutoOrient"/>.</remarks>
    public bool AutoOrient { get; init; } = true;

    /// <summary>
    /// Gets how transparency inside the source is resolved after auto-orient and before the crop.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="AlphaBehavior.Preserve"/>. Preserve or flatten only.
    /// Separate from <see cref="ImageCanvas"/> and from JPEG <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>.
    /// </remarks>
    public AlphaBehavior SourceAlpha { get; init; } = AlphaBehavior.Preserve;
}
