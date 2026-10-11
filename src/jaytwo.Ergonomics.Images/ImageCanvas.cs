namespace jaytwo.Ergonomics.Images;

/// <summary>
/// The plate the image is placed on after geometry: pad letterboxes, crop-focus remainders,
/// and expand-rotation corners.
/// </summary>
/// <remarks>
/// <para>
/// Canvas is one of three alpha knobs. It is not <see cref="AlphaBehavior"/> (source admission)
/// and not <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/> (encode-time flatten for
/// formats that cannot store alpha).
/// </para>
/// <para>
/// Plates are RGB: colors come from <see cref="ImageColor"/> (three 8-bit channels). The image
/// placed on the plate must already be RGB or RGBA (three color bands, optional alpha). Greyscale
/// and other band layouts are rejected when a canvas is applied — this library does not silently
/// convert colorspaces. Omit the canvas, or convert the source to RGB first.
/// </para>
/// <para>
/// When the finished raster is placed on a plate, content is composited <c>Over</c> that plate:
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="Transparent"/> invents empty (fully transparent) gaps. It does not paint a color
/// under preserved content alpha — source holes stay true transparency.
/// </item>
/// <item>
/// <see cref="SolidBlack"/>, <see cref="SolidWhite"/>, and <see cref="Solid"/> paint an opaque
/// plate. Gaps use that color. With <see cref="AlphaBehavior.Preserve"/>, transparent pixels
/// inside the content also show that color (the plate is under the whole placed image, not only
/// the letterbox bars).
/// </item>
/// <item>
/// <see cref="Checkerboard"/> is the same compositing model as a solid plate, but always builds
/// an 8-pixel white/gray full-frame plate — including when the image already fills the frame —
/// so preserved content alpha shows the grid. Use it to preview transparency; it is not a
/// source-alpha policy.
/// </item>
/// </list>
/// <para>
/// If <see cref="AlphaBehavior.Flatten"/> already removed source alpha, solid and checkerboard
/// plates only affect invented gaps (there is no content alpha left to show through).
/// </para>
/// </remarks>
public abstract record ImageCanvas
{
    private ImageCanvas()
    {
    }

    /// <summary>Gets a black plate.</summary>
    public static ImageCanvas SolidBlack { get; } = new Paint(ImageColor.Black);

    /// <summary>Gets a white plate.</summary>
    public static ImageCanvas SolidWhite { get; } = new Paint(ImageColor.White);

    /// <summary>
    /// Gets an empty plate: invented gaps stay fully transparent, and preserved content alpha
    /// is left alone (no color painted under content holes).
    /// </summary>
    public static ImageCanvas Transparent { get; } = new Clear();

    /// <summary>
    /// Gets an 8-pixel white and gray checkerboard plate the size of the frame.
    /// </summary>
    /// <remarks>
    /// Always composites the image over the grid, even when the image already fills the frame,
    /// so preserved content alpha shows the checkerboard. Geometry gaps use the same plate.
    /// </remarks>
    public static ImageCanvas Checkerboard { get; } = new Checkered();

    /// <summary>Paints the plate with <paramref name="color"/>.</summary>
    /// <returns>A canvas of <paramref name="color"/>.</returns>
    public static ImageCanvas Solid(ImageColor color) => new Paint(color);

    internal sealed record Paint(ImageColor Color) : ImageCanvas;

    internal sealed record Clear : ImageCanvas;

    internal sealed record Checkered : ImageCanvas;
}
