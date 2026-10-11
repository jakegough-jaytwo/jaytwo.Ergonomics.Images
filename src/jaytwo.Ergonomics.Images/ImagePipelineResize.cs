using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Options for one <see cref="ImagePipeline.Resize(int?, int?, ResizeMode, System.Action{ImagePipelineResize})"/> stage,
/// configured like <see cref="ImagePipelineSource"/> on <see cref="ImagePipeline.Create"/>.
/// </summary>
/// <remarks>
/// Not a pipeline stage. Set aspect, focus, enlarge, and canvas here; they attach to that resize only.
/// </remarks>
public sealed class ImagePipelineResize
{
    private ImageAspectRatio? _aspect;
    private ImageFocus? _focus;
    private ImageCanvas? _canvas;
    private bool _enlarge;

    internal ImageAspectRatio? AspectRatio => _aspect;

    internal ImageFocus? FocusBox => _focus;

    internal ImageCanvas? CanvasPlate => _canvas;

    internal bool EnlargeEnabled => _enlarge;

    /// <summary>
    /// Sets the output ratio. Both sides plus an aspect become the largest rectangle of that ratio inside the box.
    /// </summary>
    /// <param name="aspect">Width over height.</param>
    /// <returns>This resize configuration.</returns>
    public ImagePipelineResize Aspect(ImageAspectRatio aspect)
    {
        if (!aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect ratio must be finite and positive.");
        }

        _aspect = aspect;
        return this;
    }

    /// <summary>
    /// Sets the relative focus rectangle. Applies with <see cref="ResizeMode.Crop"/> only.
    /// </summary>
    /// <param name="focus">Rectangle on the upright image, edges from 0 to 1.</param>
    /// <returns>This resize configuration.</returns>
    public ImagePipelineResize Focus(ImageFocus focus)
    {
        _focus = focus;
        return this;
    }

    /// <summary>
    /// Sets whether the source may grow to meet the frame. Defaults to false.
    /// Ignored for <see cref="ResizeMode.Distort"/>, which always writes the requested size.
    /// </summary>
    /// <param name="enlarge">True to allow upscaling.</param>
    /// <returns>This resize configuration.</returns>
    public ImagePipelineResize Enlarge(bool enlarge)
    {
        _enlarge = enlarge;
        return this;
    }

    /// <summary>
    /// Sets the canvas plate for pad gaps or a crop-focus remainder.
    /// </summary>
    /// <param name="canvas">The plate under geometry gaps and, for solid/checkerboard plates, under preserved content alpha.</param>
    /// <returns>This resize configuration.</returns>
    /// <remarks>See <see cref="ImageCanvas"/> for how plates interact with source alpha and JPEG alpha fallback.</remarks>
    public ImagePipelineResize Canvas(ImageCanvas canvas)
    {
        if (canvas is null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        _canvas = canvas;
        return this;
    }
}
