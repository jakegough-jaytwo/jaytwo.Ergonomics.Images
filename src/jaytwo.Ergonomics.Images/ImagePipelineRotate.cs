using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Options for one <see cref="ImagePipeline.Rotate(ImageRotation, System.Action{ImagePipelineRotate})"/> stage,
/// configured like <see cref="ImagePipelineSource"/> on <see cref="ImagePipeline.Create"/>.
/// </summary>
/// <remarks>
/// Not a pipeline stage. Bounds default to <see cref="RotationBounds.Expand"/>.
/// Canvas applies with Expand; aspect applies with Trim. Neither converts the other.
/// </remarks>
public sealed class ImagePipelineRotate
{
    private RotationBounds _bounds = RotationBounds.Expand;
    private ImageAspectRatio? _aspect;
    private ImageCanvas? _canvas;

    internal RotationBounds Bounds => _bounds;

    internal ImageAspectRatio? AspectRatio => _aspect;

    internal ImageCanvas? CanvasPlate => _canvas;

    /// <summary>Sets bounds to <see cref="RotationBounds.Expand"/> and clears any trim aspect.</summary>
    /// <returns>This rotation configuration.</returns>
    public ImagePipelineRotate Expand()
    {
        _bounds = RotationBounds.Expand;
        _aspect = null;
        return this;
    }

    /// <summary>Sets bounds to <see cref="RotationBounds.Trim"/> and clears any canvas.</summary>
    /// <param name="aspect">Optional ratio of the centered trim rectangle.</param>
    /// <returns>This rotation configuration.</returns>
    public ImagePipelineRotate Trim(ImageAspectRatio? aspect = null)
    {
        if (aspect is ImageAspectRatio ratio && !ratio.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect ratio must be finite and positive.");
        }

        _bounds = RotationBounds.Trim;
        _aspect = aspect;
        _canvas = null;
        return this;
    }

    /// <summary>
    /// Sets the trim ratio. Bounds must already be <see cref="RotationBounds.Trim"/>
    /// (this does not convert Expand to Trim).
    /// </summary>
    /// <param name="aspect">Width over height of the centered trim rectangle.</param>
    /// <returns>This rotation configuration.</returns>
    public ImagePipelineRotate Aspect(ImageAspectRatio aspect)
    {
        if (!aspect.IsInRange())
        {
            throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect ratio must be finite and positive.");
        }

        if (_bounds != RotationBounds.Trim)
        {
            throw new ArgumentException("An aspect ratio applies with Trim.", nameof(aspect));
        }

        _aspect = aspect;
        return this;
    }

    /// <summary>
    /// Sets the plate for corners Expand adds. Bounds must already be <see cref="RotationBounds.Expand"/>
    /// (this does not convert Trim to Expand).
    /// </summary>
    /// <param name="canvas">The plate under invented corners and, for solid/checkerboard plates, under preserved content alpha.</param>
    /// <returns>This rotation configuration.</returns>
    public ImagePipelineRotate Canvas(ImageCanvas canvas)
    {
        if (canvas is null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        if (_bounds != RotationBounds.Expand)
        {
            throw new ArgumentException("A canvas applies with Expand.", nameof(canvas));
        }

        _canvas = canvas;
        return this;
    }
}
