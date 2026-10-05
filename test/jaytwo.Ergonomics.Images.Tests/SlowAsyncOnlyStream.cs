using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Ergonomics.Images.Tests;

/// <summary>
/// Destination that delays each write so cancellation can land mid-encode.
/// </summary>
internal sealed class SlowAsyncOnlyStream : Stream
{
    private readonly Stream _inner;
    private readonly int _delayMs;

    public SlowAsyncOnlyStream(Stream inner, int delayMs)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _delayMs = delayMs;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Sync Write is not supported.");

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await Task.Delay(_delayMs, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delayMs, cancellationToken).ConfigureAwait(false);
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
