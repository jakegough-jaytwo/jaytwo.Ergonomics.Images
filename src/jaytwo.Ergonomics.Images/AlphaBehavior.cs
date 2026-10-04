namespace jaytwo.Ergonomics.Images;

/// <summary>
/// What happens to transparency that belongs to the source image.
/// </summary>
/// <remarks>
/// Applied after the resize, in output pixels, and before the image is placed on a canvas.
/// <see cref="Checkerboard"/> paints an 8-pixel grid of white and gray under the source.
/// </remarks>
public abstract record AlphaBehavior
{
    private AlphaBehavior()
    {
    }

    /// <summary>Gets a behavior that leaves source transparency unchanged.</summary>
    public static AlphaBehavior Preserve { get; } = new Keep();

    /// <summary>Removes source transparency by compositing onto <paramref name="color"/>.</summary>
    /// <returns>A flatten onto <paramref name="color"/>.</returns>
    public static AlphaBehavior Flatten(ImageColor color) => new Flat(color);

    /// <summary>Paints a white and gray checkerboard under source transparency.</summary>
    /// <returns>The checkerboard behavior.</returns>
    public static AlphaBehavior Checkerboard() => Grid.Instance;

    internal sealed record Keep : AlphaBehavior;

    internal sealed record Flat(ImageColor Color) : AlphaBehavior;

    internal sealed record Grid : AlphaBehavior
    {
        internal static readonly Grid Instance = new();
    }
}
