using System.Diagnostics.CodeAnalysis;

namespace Orleans;

/// <summary>Helpers for encoding and decoding typed grain keys.</summary>
public static class TypedGrainKey
{
    /// <summary>Encodes <paramref name="key"/> as an Orleans grain key string.</summary>
    public static string Format<TKey>(TKey key) where TKey : ITypedGrainKey<TKey> => TKey.Format(key);

    /// <summary>Decodes <paramref name="key"/>, throwing <see cref="GrainKeyFormatException"/> if it is not a valid <typeparamref name="TKey"/>.</summary>
    public static TKey Parse<TKey>(string key) where TKey : ITypedGrainKey<TKey>
    {
        ArgumentNullException.ThrowIfNull(key);
        return TKey.Parse(key);
    }

    /// <summary>Tries to decode <paramref name="key"/> into a <typeparamref name="TKey"/>.</summary>
    public static bool TryParse<TKey>([NotNullWhen(true)] string? key, [MaybeNullWhen(false)] out TKey result) where TKey : ITypedGrainKey<TKey>
    {
        if (key is null)
        {
            result = default;
            return false;
        }

        try
        {
            result = TKey.Parse(key);
            return true;
        }
        catch (GrainKeyFormatException)
        {
            result = default;
            return false;
        }
    }
}
