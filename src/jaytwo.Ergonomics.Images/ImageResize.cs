using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Fit, frame, and canvas for one <see cref="ImageTransforms"/> resize shortcut (compiles to <see cref="ImagePipeline"/>).
/// </summary>
/// <remarks>
/// <see cref="Fit"/> defaults to <see cref="ImageFit.Fit"/>.
/// Width and height are a pixel box. <see cref="Aspect"/> is the output ratio.
/// One side plus an aspect ratio calculates the other side.
/// Both sides plus an aspect ratio treat the sides as a maximum box: the frame is the
/// largest rectangle of that ratio inside the box.
/// An aspect ratio alone zooms with <see cref="ImageFit.Zoom"/>, or pads with
/// <see cref="ImageFit.Fit"/> and a <see cref="Canvas"/>, at the source size.
/// <see cref="Canvas"/> is separate from the mode. It applies with <see cref="ImageFit.Fit"/>,
/// and on <see cref="ImageFit.Zoom"/> it is the gap left when <see cref="Focus"/> does not fit.
/// <see cref="ImageFit.Zoom"/> keeps the center when <see cref="Focus"/> is omitted.
/// <see cref="SourceAlpha"/> resolves transparency inside the source before the canvas is applied.
/// </remarks>
public sealed record ImageResize
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageResize"/> class.
    /// </summary>
    /// <param name="format">Encoded format written to the destination stream.</param>
    public ImageResize(ImageOutputFormat format)
    {
        if (!Enum.IsDefined(typeof(ImageOutputFormat), format))
        {
            throw new ArgumentOutOfRangeException(nameof(format));
        }

        Format = format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageResize"/> class for a pixel frame.
    /// </summary>
    /// <param name="format">Encoded format written to the destination stream.</param>
    /// <param name="size">Width and height of the frame.</param>
    public ImageResize(ImageOutputFormat format, ImageSize size)
        : this(format)
    {
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>Gets the encoded format written to the destination.</summary>
    public ImageOutputFormat Format { get; }

    /// <summary>Gets the requested width, in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>Gets the requested height, in pixels.</summary>
    public int? Height { get; init; }

    /// <summary>Gets the output ratio. Omit it to use the pixel box and the source aspect.</summary>
    public ImageAspectRatio? Aspect { get; init; }

    /// <summary>Gets how the source meets the frame. Defaults to <see cref="ImageFit.Fit"/>.</summary>
    public ImageFit Fit { get; init; } = ImageFit.Fit;

    /// <summary>
    /// Gets the relative rectangle a <see cref="ImageFit.Zoom"/> window must keep.
    /// </summary>
    /// <remarks>Omit it and the window stays centered. Applies with <see cref="ImageFit.Zoom"/>.</remarks>
    public ImageFocus? Focus { get; init; }

    /// <summary>
    /// Gets the gap around a contained image.
    /// </summary>
    /// <remarks>
    /// Applies with <see cref="ImageFit.Fit"/>. The output then matches the frame.
    /// On <see cref="ImageFit.Zoom"/> it paints the gap when <see cref="Focus"/> does not fit in the cover window.
    /// Omit it there and the gap is transparent.
    /// </remarks>
    public ImageCanvas? Canvas { get; init; }

    /// <summary>
    /// Gets how transparency inside the source is resolved.
    /// </summary>
    /// <remarks>Defaults to <see cref="AlphaBehavior.Preserve"/>.</remarks>
    public AlphaBehavior SourceAlpha { get; init; } = AlphaBehavior.Preserve;

    /// <summary>
    /// Gets a value indicating whether the source may grow to meet the frame.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. <see cref="ImageFit.Stretch"/> always writes the requested size.
    /// </remarks>
    public bool Enlarge { get; init; } = true;

    /// <summary>Gets orientation and encode overrides. Omit it to keep the defaults.</summary>
    public ImageEncode? Encode { get; init; }
}
