using System;

namespace jaytwo.Ergonomics.Images;

/// <summary>
/// Raised when an image cannot be read, transformed, or encoded.
/// </summary>
public sealed class ImageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageException"/> class.
    /// </summary>
    public ImageException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageException"/> class.
    /// </summary>
    public ImageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
