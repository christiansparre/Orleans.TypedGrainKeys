namespace Orleans;

public class TypedGrainKeyWriter
{
    internal const char Delimiter = '|';
    private readonly List<string> _parts = new();

    public TypedGrainKeyWriter Write(string part)
    {
        _parts.Add(part);
        return this;
    }

    public static TypedGrainKeyWriter Create() => new();

    public TypedGrainKeyWriter Write(Guid part) => Write(part.ToString());

    public TypedGrainKeyWriter Write(int part) => Write(part.ToString());

    public TypedGrainKeyWriter Write(long part) => Write(part.ToString());

    public TypedGrainKeyWriter Write(DateTime part) => Write(part.ToString("O"));

    public TypedGrainKeyWriter Write(DateTimeOffset part) => Write(part.ToString("O"));

    public TypedGrainKeyWriter Write<TEnum>(TEnum part) where TEnum : struct => Write(part.ToString()!);

    public override string ToString() => string.Join(Delimiter, _parts);

    public static implicit operator string(TypedGrainKeyWriter writer) => writer.ToString();
}