namespace Orleans.TypedGrainKeys.Tests.Keys;

/// <summary>A hand-written key whose Parse forgets to read its last part.</summary>
public record IncorrectReadTestGrainKey(
    string One,
    int Two,
    long Three) : ITypedGrainKey<IncorrectReadTestGrainKey>
{
    public static string GrainKeyName => "incorrect-read";

    public static string Format(IncorrectReadTestGrainKey key)
    {
        return TypedGrainKeyWriter.Create<IncorrectReadTestGrainKey>()
            .Write(key.One)
            .Write(key.Two)
            .Write(key.Three);
    }

    public static IncorrectReadTestGrainKey Parse(string key)
    {
        using var reader = TypedGrainKeyReader.Create<IncorrectReadTestGrainKey>(key);

        return new IncorrectReadTestGrainKey(
            reader.ReadString(),
            reader.ReadInt32(),
            10);
    }
}
