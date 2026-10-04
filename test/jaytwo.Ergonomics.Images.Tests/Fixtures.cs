using System;
using System.IO;

namespace jaytwo.Ergonomics.Images.Tests;

internal static class Fixtures
{
    public static string Path(string name)
    {
        return System.IO.Path.Combine(AppContext.BaseDirectory, "fixtures", name);
    }
}
