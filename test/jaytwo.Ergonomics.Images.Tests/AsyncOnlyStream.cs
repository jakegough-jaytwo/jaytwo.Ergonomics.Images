using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Ergonomics.Images.Tests;

/// <summary>
/// Stream that rejects sync Read/Write but supports async I/O (ASP.NET-style).
/// </summary>
internal sealed class AsyncOnlyStream : Stream
{
    private readonly Stream _inner;

    public AsyncOnlyStream(Stream inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException("Sync Flush is not supported.");

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Sync Read is not supported.");

    public override int Read(Span<byte> buffer)
        => throw new NotSupportedException("Sync Read is not supported.");

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Sync Write is not supported.");

    public override void Write(ReadOnlySpan<byte> buffer)
        => throw new NotSupportedException("Sync Write is not supported.");

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(buffer, cancellationToken);

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
