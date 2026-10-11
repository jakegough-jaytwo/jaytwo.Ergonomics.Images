namespace jaytwo.Ergonomics.Images;

/// <summary>
/// An ordered native stage inside <see cref="ImagePipeline"/>.
/// </summary>
internal abstract record PipelineOp
{
    private PipelineOp()
    {
    }

    internal sealed record RotateOp(
        ImageRotation Rotation,
        RotationBounds Bounds,
        ImageAspectRatio? Aspect,
        ImageCanvas? Canvas) : PipelineOp;

    internal sealed record ResizeOp(
        int? Width,
        int? Height,
        ImageAspectRatio? Aspect,
        ResizeMode Mode,
        ImageFocus? Focus,
        ImageCanvas? Canvas,
        bool Enlarge) : PipelineOp;

    internal sealed record CropOp(
        double X,
        double Y,
        double Width,
        double Height) : PipelineOp;
}
