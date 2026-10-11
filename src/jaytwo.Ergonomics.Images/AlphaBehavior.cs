namespace jaytwo.Ergonomics.Images;

/// <summary>
/// What happens to transparency that already belongs to the source pixels.
/// </summary>
/// <remarks>
/// <para>
/// Source admission policy — the first of three alpha knobs. Set on <see cref="ImagePipeline.Create"/>
/// via <see cref="ImagePipelineSource.SourceAlpha"/>, or on option bags such as
/// <see cref="ImageResize.SourceAlpha"/>. Applied after auto-orient and before geometry.
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="Preserve"/> (default) leaves source holes as alpha into later stages.
/// </item>
/// <item>
/// <see cref="Flatten"/> composites the source onto a color so geometry sees an opaque raster.
/// </item>
/// </list>
/// <para>
/// This is not <see cref="ImageCanvas"/> (the plate under pad gaps, crop-focus remainders, and
/// expand corners — and, for solid/checkerboard plates, under preserved content alpha).
/// This is not <see cref="ImageEncoding.JpegEncoding.AlphaFallbackColor"/> (last-resort flatten
/// when the output format cannot store remaining alpha).
/// </para>
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

    internal sealed record Keep : AlphaBehavior;

    internal sealed record Flat(ImageColor Color) : AlphaBehavior;
}
