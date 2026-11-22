namespace Orleans;

/// <summary>
/// Generates the typed grain key implementation for a <c>partial record</c> or <c>partial record struct</c>.
/// The positional parameters of the record are the parts of the key, written in declaration order after the key's name.
/// </summary>
/// <example>
/// <code>
/// [GrainKey("user")]
/// public partial record UserGrainKey(Guid TenantId, int UserId); // user|&lt;tenant&gt;|&lt;id&gt;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class GrainKeyAttribute(string name) : Attribute
{
    /// <summary>
    /// The name written as the first part of the encoded key. It must be unique among your keys and may only contain
    /// letters, digits, <c>-</c>, <c>_</c> and <c>.</c>. Changing it changes every encoded key of this type.
    /// </summary>
    public string Name { get; } = name;
}
