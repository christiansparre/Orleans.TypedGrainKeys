namespace Orleans.TypedGrainKeys.Tests.Keys;

[GrainKey]
public partial record UserGrainKey(Guid TenantId, int UserId);
