namespace Orleans;

public sealed class TypedGrainKeyReader : IDisposable
{
    private readonly string[] _parts;

    private int _position;

    public TypedGrainKeyReader(string key)
    {
        _parts = key.Split(TypedGrainKeyWriter.Delimiter, StringSplitOptions.RemoveEmptyEntries);
    }

    public static TypedGrainKeyReader Create(string key) => new TypedGrainKeyReader(key);

    public string ReadString()
    {
        var value = _parts[_position];
        _position++;
        return value;
    }

    public Guid ReadGuid()
    {
        return Guid.Parse(ReadString());
    }

    public int ReadInt32()
    {
        return int.Parse(ReadString());
    }

    public long ReadInt64()
    {
        return long.Parse(ReadString());
    }

    public DateTime ReadDateTime()
    {
        return DateTime.ParseExact(ReadString(), "O", null);
    }

    public DateTimeOffset ReadDateTimeOffset()
    {
        return DateTimeOffset.ParseExact(ReadString(), "O", null);
    }

    public TEnum ReadEnum<TEnum>() where TEnum : struct
    {
        return Enum.Parse<TEnum>(ReadString());
    }

    public void Dispose()
    {
        if (_position != _parts.Length)
        {
            throw new GrainKeyReadException($"GrainKey has {_parts.Length} parts, but reader recorded {_position} reads");
        }
    }
}