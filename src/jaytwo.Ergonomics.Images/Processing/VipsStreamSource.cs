using System;
using System.IO;
using NetVips;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// A libvips <see cref="Source"/> over a caller-owned stream.
/// The stream is not closed when the source is disposed.
/// </summary>
internal sealed class VipsStreamSource : SourceCustom
{
    private readonly Stream _stream;
    private readonly long _start;

    public VipsStreamSource(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _start = stream.CanSeek ? stream.Position : 0;
        OnRead += Read;
        if (stream.CanSeek)
        {
            OnSeek += Seek;
        }
    }

    private int Read(byte[] buffer, int length)
    {
        return _stream.Read(buffer, 0, length);
    }

    private long Seek(long offset, SeekOrigin origin)
    {
        try
        {
            var position = origin switch
            {
                SeekOrigin.Begin => _stream.Seek(_start + offset, SeekOrigin.Begin),
                SeekOrigin.Current => _stream.Seek(offset, SeekOrigin.Current),
                SeekOrigin.End => _stream.Seek(offset, SeekOrigin.End),
                _ => -1L,
            };

            if (position < 0)
            {
                return -1;
            }

            return position - _start;
        }
        catch (Exception)
        {
            return -1;
        }
    }
}
