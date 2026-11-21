namespace Orleans;

/// <summary>
/// Generates the typed grain key implementation for a <c>partial record</c> or <c>partial record struct</c>.
/// The positional parameters of the record are the parts of the key, written in declaration order.
/// </summary>
/// <example>
/// <code>
/// [GrainKey]
/// public partial record UserGrainKey(Guid TenantId, int UserId);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class GrainKeyAttribute : Attribute;
