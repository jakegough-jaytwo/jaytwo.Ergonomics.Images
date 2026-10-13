using System;
using System.Collections;
using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images.Metadata;

/// <summary>
/// One EXIF IFD: standard tag names to raw string values, with keyed lookup for arbitrary tags.
/// </summary>
public abstract class ExifIfd : IReadOnlyDictionary<string, string>
{
    internal static readonly IReadOnlyDictionary<string, string> EmptyFields =
        new Dictionary<string, string>(0, StringComparer.Ordinal);

    private readonly IReadOnlyDictionary<string, string> _fields;

    private protected ExifIfd(IReadOnlyDictionary<string, string> fields)
    {
        _fields = fields;
    }

    /// <inheritdoc />
    public int Count => _fields.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys => _fields.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values => _fields.Values;

    /// <summary>Gets the raw value for <paramref name="key"/>, or throws if the tag is absent.</summary>
    public string this[string key] => _fields[key];

    /// <inheritdoc />
    public bool ContainsKey(string key) => _fields.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out string value) => _fields.TryGetValue(key, out value!);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _fields.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Gets a raw tag value when present and non-empty; otherwise null.</summary>
    private protected string? Get(string key) => ExifConvenience.ReadField(_fields, key);
}
