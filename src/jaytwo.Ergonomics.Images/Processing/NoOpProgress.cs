using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Progress sink used only so NetVips arms <see cref="System.Threading.CancellationToken"/> kill.
/// </summary>
internal sealed class NoOpProgress : IProgress<int>
{
    private NoOpProgress()
    {
    }

    public static NoOpProgress Instance { get; } = new();

    public void Report(int value)
    {
    }
}
