using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Mode, frame, and canvas for one <see cref="ImageTransforms"/> resize shortcut (compiles to <see cref="ImagePipeline"/>).
/// </summary>
/// <remarks>
/// <see cref="Mode"/> defaults to <see cref="ResizeMode.Pad"/>.
/// Width and height are a pixel box. <see cref="Aspect"/> is the output ratio.
/// One side plus an aspect ratio calculates the other side.
/// Both sides plus an aspect ratio treat the sides as a maximum box: the frame is the
/// largest rectangle of that ratio inside the box.
/// An aspect ratio alone crops with <see cref="ResizeMode.Crop"/>, or pads with
/// <see cref="ResizeMode.Pad"/> and a <see cref="Canvas"/>, at the source size.
/// <see cref="Canvas"/> is separate from the mode. It applies with <see cref="ResizeMode.Pad"/>,
/// and on <see cref="ResizeMode.Crop"/> it is the remainder when <see cref="Focus"/> does not fit.
/// <see cref="ResizeMode.Crop"/> keeps the center when <see cref="Focus"/> is omitted.
/// <see cref="AutoOrient"/> and <see cref="SourceAlpha"/> are source admission (same as
/// <see cref="ImagePipeline.Create"/>). Metadata and format options live on <see cref="Encoding"/>.
/// </remarks>
public sealed record ImageResize
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageResize"/> class.
    /// </summary>
    /// <param name="encoding">Output encoding written to the destination stream.</param>
    public ImageResize(ImageEncoding encoding)
    {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageResize"/> class for a pixel frame.
    /// </summary>
    /// <param name="encoding">Output encoding written to the destination stream.</param>
    /// <param name="size">Width and height of the frame.</param>
    public ImageResize(ImageEncoding encoding, ImageSize size)
        : this(encoding)
    {
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>Gets the output encoding written to the destination.</summary>
    public ImageEncoding Encoding { get; }

    /// <summary>Gets the requested width, in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>Gets the requested height, in pixels.</summary>
    public int? Height { get; init; }

    /// <summary>Gets the output ratio. Omit it to use the pixel box and the source aspect.</summary>
    public ImageAspectRatio? Aspect { get; init; }

    /// <summary>Gets how the source meets the frame. Defaults to <see cref="ResizeMode.Pad"/>.</summary>
    public ResizeMode Mode { get; init; } = ResizeMode.Pad;

    /// <summary>
    /// Gets the relative rectangle a <see cref="ResizeMode.Crop"/> window must keep.
    /// </summary>
    /// <remarks>Omit it and the window stays centered. Applies with <see cref="ResizeMode.Crop"/>.</remarks>
    public ImageFocus? Focus { get; init; }

    /// <summary>
    /// Gets the plate the padded or focus-remainder image is placed on.
    /// </summary>
    /// <remarks>
    /// Applies with <see cref="ResizeMode.Pad"/> so the output matches the frame.
    /// On <see cref="ResizeMode.Crop"/> it paints the remainder when <see cref="Focus"/> does not
    /// fit in the cover window; omit it there and the remainder is transparent.
    /// Solid and checkerboard plates also show through preserved content alpha — see
    /// <see cref="ImageCanvas"/>. Not <see cref="AlphaBehavior"/> and not
    /// <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>.
    /// </remarks>
    public ImageCanvas? Canvas { get; init; }

    /// <summary>
    /// Gets a value indicating whether EXIF/XMP orientation is applied before measuring and geometry.
    /// </summary>
    /// <remarks>Defaults to <c>true</c>. Same admission as <see cref="ImagePipelineSource.AutoOrient"/>.</remarks>
    public bool AutoOrient { get; init; } = true;

    /// <summary>
    /// Gets how transparency inside the source is resolved after auto-orient and before geometry.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="AlphaBehavior.Preserve"/>. Preserve or flatten only.
    /// Separate from <see cref="Canvas"/> and from JPEG <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/>.
    /// </remarks>
    public AlphaBehavior SourceAlpha { get; init; } = AlphaBehavior.Preserve;

    /// <summary>
    /// Gets a value indicating whether the source may grow to meet the frame.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>. <see cref="ResizeMode.Distort"/> always writes the requested size
    /// and ignores this flag.
    /// </remarks>
    public bool Enlarge { get; init; }
}
