namespace Orleans.TypedGrainKeys.Tests.Keys;

[GrainKey("user")]
public partial record UserGrainKey(Guid TenantId, int UserId);
