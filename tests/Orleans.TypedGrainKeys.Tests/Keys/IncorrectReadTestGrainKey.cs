namespace Orleans.TypedGrainKeys.Tests.Keys;

public record IncorrectReadTestGrainKey(
    string One,
    int Two,
    long Three) : ITypedGrainKey<IncorrectReadTestGrainKey>
{
    public static implicit operator string(IncorrectReadTestGrainKey self)
    {
        return TypedGrainKeyWriter.Create()
            .Write(self.One)
            .Write(self.Two)
            .Write(self.Three);
    }

    public static implicit operator IncorrectReadTestGrainKey(string self)
    {
        using var reader = TypedGrainKeyReader.Create(self);

        return new IncorrectReadTestGrainKey(
            reader.ReadString(),
            reader.ReadInt32(),
            10);
    }
}