namespace Orleans;

/// <summary>
/// A grain whose primary key is a typed grain key. <see cref="Key"/> is decoded once, on first use, and then cached.
/// </summary>
/// <typeparam name="TKey">The typed key of this grain.</typeparam>
public abstract class TypedKeyGrain<TKey> : Grain where TKey : ITypedGrainKey<TKey>
{
    private TKey? _key;
    private bool _keyDecoded;

    /// <summary>The decoded primary key of this grain.</summary>
    public TKey Key
    {
        get
        {
            if (!_keyDecoded)
            {
                _key = this.GetTypedKey<TKey>();
                _keyDecoded = true;
            }

            return _key!;
        }
    }
}
