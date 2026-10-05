namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Common pipelines built from the same fluent primitives.
/// </summary>
public static class ImagePresets
{
    /// <summary>
    /// Fits inside a 256×256 frame and encodes WebP with the library defaults.
    /// </summary>
    public static ImagePipeline Thumbnail { get; } = ImagePipeline.Create()
        .Resize(ImageSize.Thumbnail)
        .Fit(ImageFit.Fit)
        .EncodingWebP();

    /// <summary>
    /// Fits inside a 1024×1024 frame and encodes WebP with the library defaults.
    /// </summary>
    public static ImagePipeline Preview { get; } = ImagePipeline.Create()
        .Resize(ImageSize.Preview)
        .Fit(ImageFit.Fit)
        .EncodingWebP();
}
