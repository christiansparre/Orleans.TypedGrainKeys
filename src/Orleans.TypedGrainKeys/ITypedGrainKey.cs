namespace Orleans;

/// <summary>
/// A strongly typed grain key that encodes to and decodes from an Orleans string grain key.
/// Implement it with <see cref="GrainKeyAttribute"/>, or by hand with <see cref="TypedGrainKeyWriter"/> and <see cref="TypedGrainKeyReader"/>.
/// </summary>
public interface ITypedGrainKey<TSelf> where TSelf : ITypedGrainKey<TSelf>
{
    /// <summary>
    /// The name of the key, written as the first part of every encoded key, e.g. <c>user</c> in <c>user|&lt;tenant&gt;|&lt;id&gt;</c>.
    /// Decoding rejects keys with a different name, so two key types with the same parts can't be mixed up.
    /// </summary>
    static abstract string GrainKeyName { get; }

    /// <summary>Encodes <paramref name="key"/> as an Orleans grain key string.</summary>
    static abstract string Format(TSelf key);

    /// <summary>Decodes an Orleans grain key string.</summary>
    /// <exception cref="GrainKeyFormatException">The key is not a valid encoded key of this type.</exception>
    static abstract TSelf Parse(string key);
}
