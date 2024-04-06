using Orleans.Runtime;

namespace Orleans;

public static class AddressableExtensions
{
    public static TKey GetTypedKey<TKey>(this IAddressable addressable) where TKey: ITypedGrainKey<TKey>
    {
        return addressable.GetPrimaryKeyString();
    }
}