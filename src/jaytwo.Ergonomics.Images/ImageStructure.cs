namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Structural header of a source image (size, format, alpha, orientation).
/// </summary>
/// <remarks>
/// Values are taken from the file as stored. <see cref="VisualSize"/> applies the
/// EXIF orientation swap without rewriting pixels.
/// </remarks>
public sealed class ImageStructure
{
    internal ImageStructure(
        ImageSize size,
        ImageFormat format,
        bool hasAlpha,
        int bands,
        int? orientation,
        int pageCount)
    {
        Size = size;
        Format = format;
        HasAlpha = hasAlpha;
        Bands = bands;
        Orientation = orientation;
        PageCount = pageCount;
    }

    /// <summary>Gets the stored pixel width and height (before orientation).</summary>
    public ImageSize Size { get; }

    /// <summary>
    /// Gets the pixel size after applying EXIF orientation (tags 5–8 swap width and height).
    /// </summary>
    public ImageSize VisualSize
    {
        get
        {
            if (Orientation is >= 5 and <= 8)
            {
                return new ImageSize(Size.Height, Size.Width);
            }

            return Size;
        }
    }

    /// <summary>Gets the detected container format.</summary>
    public ImageFormat Format { get; }

    /// <summary>Gets a value indicating whether the image has an alpha band.</summary>
    public bool HasAlpha { get; }

    /// <summary>Gets the number of bands (channels).</summary>
    public int Bands { get; }

    /// <summary>Gets the EXIF/TIFF orientation tag (1–8), or null when absent.</summary>
    public int? Orientation { get; }

    /// <summary>Gets the page or frame count when known; otherwise 1.</summary>
    public int PageCount { get; }
}
