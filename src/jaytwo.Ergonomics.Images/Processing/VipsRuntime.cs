using System;
using System.Threading;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// One-time libvips setup for this process.
/// </summary>
internal static class VipsRuntime
{
    private static readonly object Gate = new();
    private static int _ready;

    public static void EnsureInitialized()
    {
        if (Volatile.Read(ref _ready) == 1)
        {
            return;
        }

        lock (Gate)
        {
            if (_ready == 1)
            {
                return;
            }

            try
            {
                // Module initialization may already have called this. A second call is a no-op.
                NetVips.NetVips.Init();

                // The operation cache retains input images. Unique backend transforms should not.
                NetVips.Cache.Max = 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException)
            {
                throw new ImageException(
                    "libvips could not be loaded. Add the NetVips.Native package for local development, or install libvips on the host and do not load both in the same process.",
                    ex);
            }

            _ready = 1;
        }
    }
}
