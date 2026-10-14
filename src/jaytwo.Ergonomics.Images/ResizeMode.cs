namespace jaytwo.Ergonomics.Images;

/// <summary>
/// How the source meets the frame requested by <see cref="ImagePipeline.Resize(ImageSize, ResizeMode, System.Action{ImagePipelineResize})"/> or <see cref="ImageResize"/>.
/// </summary>
/// <remarks>
/// Mode picks the size policy. An <see cref="ImageCanvas"/> is paint only: bars on
/// <see cref="Pad"/>, and uncovered gaps on <see cref="Crop"/>. It never switches Fit into Pad.
/// <see cref="Fit"/>, <see cref="Pad"/>, and <see cref="Crop"/> honor enlarge.
/// <see cref="Stretch"/> always writes the requested size (enlarge does not apply).
/// <see cref="Crop"/> the resize mode is not <see cref="ImagePipeline.Crop"/>: that stage
/// extracts an explicit relative rectangle.
/// </remarks>
public enum ResizeMode
{
    /// <summary>
    /// Preserves the aspect ratio and scales the whole image inside the frame.
    /// One side of the result can be shorter than the frame. Canvas is not used.
    /// </summary>
    Fit,

    /// <summary>
    /// Preserves the aspect ratio, scales the whole image inside the frame, then letterboxes
    /// so the output is always the frame. Gaps use <see cref="ImageCanvas"/> (transparent if omitted).
    /// </summary>
    Pad,

    /// <summary>
    /// Preserves the aspect ratio and scales until the frame is filled, then discards the overflow.
    /// The center is kept unless <see cref="ImageResize.Focus"/> moves the window.
    /// When both dimensions are set, the output is always the frame; underfill and focus remainders
    /// use <see cref="ImageCanvas"/> (transparent if omitted).
    /// </summary>
    Crop,

    /// <summary>
    /// Exactly the requested size. Aspect ratio is ignored.
    /// Enlarge does not apply.
    /// </summary>
    Stretch,
}
