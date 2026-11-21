using System.Diagnostics.CodeAnalysis;

namespace Orleans;

/// <summary>Helpers for decoding typed grain keys.</summary>
public static class TypedGrainKey
{
    /// <summary>Decodes <paramref name="key"/>, throwing <see cref="GrainKeyFormatException"/> if it is not a valid <typeparamref name="TKey"/>.</summary>
    public static TKey Parse<TKey>(string key) where TKey : ITypedGrainKey<TKey>
    {
        ArgumentNullException.ThrowIfNull(key);
        return key;
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
            result = key;
            return true;
        }
        catch (GrainKeyFormatException)
        {
            result = default;
            return false;
        }
    }
}
