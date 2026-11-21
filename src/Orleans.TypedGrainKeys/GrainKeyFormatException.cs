namespace Orleans;

/// <summary>Thrown when an encoded grain key cannot be decoded into its typed key.</summary>
public sealed class GrainKeyFormatException : FormatException
{
    public GrainKeyFormatException(string reason, string key, Type? keyType = null, int? partIndex = null, Exception? innerException = null)
        : base(BuildMessage(reason, key, keyType), innerException)
    {
        Reason = reason;
        Key = key;
        KeyType = keyType;
        PartIndex = partIndex;
    }

    /// <summary>Why the key could not be decoded, without the surrounding context.</summary>
    public string Reason { get; }

    /// <summary>The encoded key that failed to decode.</summary>
    public string Key { get; }

    /// <summary>The typed key that was being decoded, when known.</summary>
    public Type? KeyType { get; }

    /// <summary>The zero-based index of the part that failed, when the failure is tied to a part.</summary>
    public int? PartIndex { get; }

    private static string BuildMessage(string reason, string key, Type? keyType) =>
        keyType is null
            ? $"Invalid grain key '{key}': {reason}."
            : $"Invalid grain key '{key}' for {keyType.Name}: {reason}.";
}
