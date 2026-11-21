namespace Orleans.TypedGrainKeys.Tests.Keys;

/// <summary>A key implemented by hand with the writer and reader, without the generator.</summary>
public record HandWrittenGrainKey(Guid TenantId, string Name) : ITypedGrainKey<HandWrittenGrainKey>
{
    public static implicit operator string(HandWrittenGrainKey self)
    {
        return TypedGrainKeyWriter.Create()
            .Write(self.TenantId)
            .Write(self.Name);
    }

    public static implicit operator HandWrittenGrainKey(string self)
    {
        using var reader = TypedGrainKeyReader.Create<HandWrittenGrainKey>(self);

        return new HandWrittenGrainKey(reader.ReadGuid(), reader.ReadString());
    }
}
