namespace Orleans.TypedGrainKeys.Tests.Keys;

public record UserGrainKey(Guid TenantId, int UserId) : ITypedGrainKey<UserGrainKey>
{
    public static implicit operator string(UserGrainKey self)
    {
        return TypedGrainKeyWriter.Create()
            .Write(self.TenantId)
            .Write(self.UserId);
    }

    public static implicit operator UserGrainKey(string self)
    {
        using var reader = TypedGrainKeyReader.Create(self);

        return new UserGrainKey(reader.ReadGuid(), reader.ReadInt32());
    }
}