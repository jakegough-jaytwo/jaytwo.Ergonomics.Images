namespace jaytwo.Ergonomics.Images;

/// <summary>
/// The gap around an image fitted with <see cref="ImageFit.Fit"/>,
/// and the corners an expanded rotation adds.
/// </summary>
/// <remarks>
/// <see cref="SolidBlack"/> and <see cref="SolidWhite"/> paint that gap.
/// <see cref="Solid"/> paints it with any other <see cref="ImageColor"/>.
/// <see cref="Transparent"/> leaves the gap empty.
/// </remarks>
public abstract record ImageCanvas
{
    private ImageCanvas()
    {
    }

    /// <summary>Gets a black gap.</summary>
    public static ImageCanvas SolidBlack { get; } = new Paint(ImageColor.Black);

    /// <summary>Gets a white gap.</summary>
    public static ImageCanvas SolidWhite { get; } = new Paint(ImageColor.White);

    /// <summary>Gets an empty gap. The frame is kept, and the bars are transparent.</summary>
    public static ImageCanvas Transparent { get; } = new Clear();

    /// <summary>Paints the gap with <paramref name="color"/>.</summary>
    /// <returns>A canvas of <paramref name="color"/>.</returns>
    public static ImageCanvas Solid(ImageColor color) => new Paint(color);

    internal sealed record Paint(ImageColor Color) : ImageCanvas;

    internal sealed record Clear : ImageCanvas;
}
