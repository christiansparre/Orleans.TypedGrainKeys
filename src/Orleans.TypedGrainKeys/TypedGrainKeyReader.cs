namespace Orleans;

/// <summary>
/// Reads typed parts from an encoded grain key, in the order they were written.
/// Every read value must be in its canonical form, so a typed key maps to exactly one grain identity.
/// Disposing the reader verifies that every part was read.
/// </summary>
public sealed class TypedGrainKeyReader : IDisposable
{
    private readonly string _key;
    private readonly Type? _keyType;
    private readonly List<string> _parts;
    private int _position;
    private bool _faulted;

    private TypedGrainKeyReader(string key, Type? keyType)
    {
        ArgumentNullException.ThrowIfNull(key);

        _key = key;
        _keyType = keyType;
        _parts = GrainKeyFormat.Split(key, keyType);
    }

    /// <summary>
    /// Creates a reader for an encoded <typeparamref name="TKey"/>. It checks that the key starts with
    /// <typeparamref name="TKey"/>'s name and skips it, and its errors name <typeparamref name="TKey"/>.
    /// </summary>
    /// <exception cref="GrainKeyFormatException">The key is malformed or has a different name.</exception>
    public static TypedGrainKeyReader Create<TKey>(string key) where TKey : ITypedGrainKey<TKey>
    {
        var name = GrainKeyFormat.GetValidName<TKey>();
        var reader = new TypedGrainKeyReader(key, typeof(TKey));

        var actual = reader._parts[0];
        if (!string.Equals(actual, name, StringComparison.Ordinal))
        {
            throw reader.Fail($"it starts with '{actual}' instead of the key name '{name}'", 0);
        }

        reader._position = 1;
        return reader;
    }

    /// <summary>Creates a reader over the raw parts of a key, without a name. Used to test the part encoding.</summary>
    internal static TypedGrainKeyReader Create(string key) => new(key, null);

    /// <summary>The number of parts in the key, including its name.</summary>
    public int PartCount => _parts.Count;

    /// <summary>The number of parts read so far, including the name.</summary>
    public int Position => _position;

    public string ReadString()
    {
        if (_position >= _parts.Count)
        {
            throw Fail($"expected at least {_position + 1} parts but it has {_parts.Count}", _position);
        }

        return _parts[_position++];
    }

    public Guid ReadGuid() => Read(GrainKeyFormat.ParseGuid, GrainKeyFormat.Format);

    public int ReadInt32() => Read(GrainKeyFormat.ParseInt32, GrainKeyFormat.Format);

    public long ReadInt64() => Read(GrainKeyFormat.ParseInt64, GrainKeyFormat.Format);

    public bool ReadBoolean() => Read(GrainKeyFormat.ParseBoolean, GrainKeyFormat.Format);

    public DateTime ReadDateTime() => Read(GrainKeyFormat.ParseDateTime, GrainKeyFormat.Format);

    public DateTimeOffset ReadDateTimeOffset() => Read(GrainKeyFormat.ParseDateTimeOffset, GrainKeyFormat.Format);

    public DateOnly ReadDateOnly() => Read(GrainKeyFormat.ParseDateOnly, GrainKeyFormat.Format);

    public TimeOnly ReadTimeOnly() => Read(GrainKeyFormat.ParseTimeOnly, GrainKeyFormat.Format);

    public TimeSpan ReadTimeSpan() => Read(GrainKeyFormat.ParseTimeSpan, GrainKeyFormat.Format);

    public TEnum ReadEnum<TEnum>() where TEnum : struct, Enum => Read(GrainKeyFormat.ParseEnum<TEnum>, GrainKeyFormat.Format);

    public void Dispose()
    {
        // Don't mask the exception from a failed read with a part count mismatch.
        if (_faulted)
        {
            return;
        }

        if (_position != _parts.Count)
        {
            throw Fail($"it has {_parts.Count} parts but only {_position} were read");
        }
    }

    private T Read<T>(Func<string, T> parse, Func<T, string> format)
    {
        var index = _position;
        var raw = ReadString();

        T value;
        try
        {
            value = parse(raw);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            throw Fail($"part {index} '{raw}' is not a valid {typeof(T).Name}", index, ex);
        }

        var canonical = format(value);
        if (!string.Equals(canonical, raw, StringComparison.Ordinal))
        {
            throw Fail($"part {index} '{raw}' is not the canonical form of {typeof(T).Name} (expected '{canonical}')", index);
        }

        return value;
    }

    private GrainKeyFormatException Fail(string reason, int? partIndex = null, Exception? innerException = null)
    {
        _faulted = true;
        return new GrainKeyFormatException(reason, _key, _keyType, partIndex, innerException);
    }
}
