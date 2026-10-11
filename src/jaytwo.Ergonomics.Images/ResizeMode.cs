namespace jaytwo.Ergonomics.Images;

/// <summary>
/// How the source meets the frame requested by <see cref="ImagePipeline.Resize(ImageSize, ResizeMode, System.Action{ImagePipelineResize})"/> or <see cref="ImageResize"/>.
/// </summary>
/// <remarks>
/// Names follow the Magick-style pad / crop / distort trio.
/// The mode is geometry only. An <see cref="ImageCanvas"/> is a separate plate choice:
/// it applies with <see cref="Pad"/>, and on <see cref="Crop"/> it is the remainder when
/// <see cref="ImageFocus"/> does not fit in the cover window.
/// <see cref="Crop"/> honors the enlarge flag.
/// <see cref="Distort"/> always writes the requested size (enlarge does not apply).
/// <see cref="Crop"/> the resize mode is not <see cref="ImagePipeline.Crop"/>: that stage
/// extracts an explicit relative rectangle.
/// </remarks>
public enum ResizeMode
{
    /// <summary>
    /// Preserves the aspect ratio and scales the whole image inside the frame (Magick pad / contain).
    /// One side of the result can be shorter than the frame.
    /// Pair with <see cref="ImageCanvas"/> when the output must match the frame exactly.
    /// </summary>
    Pad,

    /// <summary>
    /// Preserves the aspect ratio and scales until the frame is filled, then discards the overflow
    /// (Magick crop / cover). The center is kept unless <see cref="ImageResize.Focus"/> moves the window.
    /// When that rectangle does not fit, cropping stops at the rectangle and the rest of the frame is canvas.
    /// With enlarge on, the output matches the frame.
    /// With enlarge off (the default), a window that still has the frame's aspect ratio can be smaller than the frame.
    /// A window that stopped early still paints the frame.
    /// </summary>
    Crop,

    /// <summary>
    /// Exactly the requested size. Aspect ratio is ignored (Magick distort / stretch).
    /// Enlarge does not apply.
    /// </summary>
    Distort,
}
