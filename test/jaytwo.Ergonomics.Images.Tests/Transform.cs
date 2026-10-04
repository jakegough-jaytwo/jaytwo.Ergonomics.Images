using System.IO;

namespace jaytwo.Ergonomics.Images.Tests;

internal static class Transform
{
    public static byte[] Run(byte[] source, System.Action<Stream, Stream> act)
    {
        using var input = new MemoryStream(source);
        using var output = new MemoryStream();
        act(input, output);
        return output.ToArray();
    }

    public static byte[] Png(byte[] source, System.Action<Stream, Stream> render)
    {
        return Run(source, render);
    }
}
