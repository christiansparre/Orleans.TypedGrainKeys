namespace Orleans.TypedGrainKeys.Tests.Keys;

/// <summary>A hand-written key whose name contains the delimiter, which the writer and reader must reject.</summary>
public record InvalidNameGrainKey(int Id) : ITypedGrainKey<InvalidNameGrainKey>
{
    public static string GrainKeyName => "bad|name";

    public static string Format(InvalidNameGrainKey key) =>
        TypedGrainKeyWriter.Create<InvalidNameGrainKey>().Write(key.Id).Build();

    public static InvalidNameGrainKey Parse(string key)
    {
        using var reader = TypedGrainKeyReader.Create<InvalidNameGrainKey>(key);
        return new InvalidNameGrainKey(reader.ReadInt32());
    }
}
