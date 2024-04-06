namespace Orleans.TypedGrainKeys.Tests.Keys;

public record TestGrainKey(
    string One,
    int Two,
    long Three,
    DateTime Four,
    DateTimeOffset Five,
    Guid Six,
    TestEnum Seven,
    FullName Eight) : ITypedGrainKey<TestGrainKey>
{
    public static implicit operator string(TestGrainKey self)
    {
        return TypedGrainKeyWriter.Create()
            .Write(self.One)
            .Write(self.Two)
            .Write(self.Three)
            .Write(self.Four)
            .Write(self.Five)
            .Write(self.Six)
            .Write(self.Seven)
            .Write(self.Eight.First)
            .Write(self.Eight.Last);
    }

    public static implicit operator TestGrainKey(string self)
    {
        using var reader = TypedGrainKeyReader.Create(self);

        return new TestGrainKey(
            reader.ReadString(),
            reader.ReadInt32(),
            reader.ReadInt64(),
            reader.ReadDateTime(),
            reader.ReadDateTimeOffset(),
            reader.ReadGuid(),
            reader.ReadEnum<TestEnum>(),
            new FullName(
                reader.ReadString(),
                reader.ReadString()));
    }
}