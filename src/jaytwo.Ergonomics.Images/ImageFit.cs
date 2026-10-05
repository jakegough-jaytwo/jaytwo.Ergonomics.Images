namespace jaytwo.Ergonomics.Images;

/// <summary>
/// How the source meets the frame requested by <see cref="ImagePipeline.Fit"/> or <see cref="ImageResize"/>.
/// </summary>
/// <remarks>
/// The mode is geometry only. The gap is an <see cref="ImageCanvas"/>,
/// and it applies with <see cref="Fit"/>.
/// On <see cref="Zoom"/> that canvas is the gap left when <see cref="ImageFocus"/> does not fit.
/// <see cref="Zoom"/> honors the enlarge flag.
/// <see cref="Stretch"/> always writes the requested size.
/// </remarks>
public enum ImageFit
{
    /// <summary>
    /// Preserves the aspect ratio and scales the whole image inside the frame.
    /// One side of the result can be shorter than the frame.
    /// </summary>
    Fit,

    /// <summary>
    /// Preserves the aspect ratio and scales until the frame is filled, then discards the overflow.
    /// The center is kept unless <see cref="ImageResize.Focus"/> moves the window.
    /// When that rectangle does not fit, cropping stops at the rectangle and the rest of the frame is canvas.
    /// With enlarge left on, the output matches the frame.
    /// With enlarge off, a window that still has the frame's aspect ratio can be smaller than the frame.
    /// A window that stopped early still paints the frame.
    /// </summary>
    Zoom,

    /// <summary>
    /// Exactly the requested size. Aspect ratio is ignored.
    /// </summary>
    Stretch,
}
