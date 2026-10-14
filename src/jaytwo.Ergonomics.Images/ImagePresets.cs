namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Common pipelines built from the same fluent primitives.
/// </summary>
public static class ImagePresets
{
    /// <summary>
    /// Fits inside a 256×256 frame (<see cref="ResizeMode.Fit"/>) and encodes WebP with the library defaults.
    /// </summary>
    public static ImagePipeline ThumbnailWebP { get; } = ImagePipeline.Create()
        .Resize(ImageSize.Thumbnail)
        .EncodingWebP();

    /// <summary>
    /// Fits inside a 1024×1024 frame (<see cref="ResizeMode.Fit"/>) and encodes WebP with the library defaults.
    /// </summary>
    public static ImagePipeline PreviewWebP { get; } = ImagePipeline.Create()
        .Resize(ImageSize.Preview)
        .EncodingWebP();
}
