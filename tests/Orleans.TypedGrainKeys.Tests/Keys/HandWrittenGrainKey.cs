namespace Orleans.TypedGrainKeys.Tests.Keys;

/// <summary>A key implemented by hand with the writer and reader, without the generator.</summary>
public record HandWrittenGrainKey(Guid TenantId, string Name) : ITypedGrainKey<HandWrittenGrainKey>
{
    public static string GrainKeyName => "hand-written";

    public static string Format(HandWrittenGrainKey key)
    {
        return TypedGrainKeyWriter.Create<HandWrittenGrainKey>()
            .Write(key.TenantId)
            .Write(key.Name)
            .Build();
    }

    public static HandWrittenGrainKey Parse(string key)
    {
        using var reader = TypedGrainKeyReader.Create<HandWrittenGrainKey>(key);

        return new HandWrittenGrainKey(reader.ReadGuid(), reader.ReadString());
    }
}
