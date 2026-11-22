using Orleans.Runtime;

namespace Orleans;

public static class AddressableExtensions
{
    /// <summary>Decodes the grain's primary key string as a <typeparamref name="TKey"/>.</summary>
    /// <exception cref="GrainKeyFormatException">The primary key is not a valid encoded <typeparamref name="TKey"/>.</exception>
    public static TKey GetTypedKey<TKey>(this IAddressable addressable) where TKey : ITypedGrainKey<TKey>
    {
        return TKey.Parse(addressable.GetPrimaryKeyString());
    }
}
