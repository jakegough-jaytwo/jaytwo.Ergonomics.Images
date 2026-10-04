using System;
using System.IO;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Replays a short prefix and then the remainder of a stream that cannot seek.
/// </summary>
internal sealed class PrefixStream : Stream
{
    private readonly byte[] _prefix;
    private readonly int _prefixLength;
    private readonly Stream _remainder;
    private int _prefixOffset;

    public PrefixStream(byte[] prefix, int prefixLength, Stream remainder)
    {
        _prefix = prefix;
        _prefixLength = prefixLength;
        _remainder = remainder ?? throw new ArgumentNullException(nameof(remainder));
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var copied = 0;
        if (_prefixOffset < _prefixLength)
        {
            var available = _prefixLength - _prefixOffset;
            var take = Math.Min(available, count);
            Buffer.BlockCopy(_prefix, _prefixOffset, buffer, offset, take);
            _prefixOffset += take;
            copied += take;
            offset += take;
            count -= take;
        }

        if (count > 0)
        {
            copied += _remainder.Read(buffer, offset, count);
        }

        return copied;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
