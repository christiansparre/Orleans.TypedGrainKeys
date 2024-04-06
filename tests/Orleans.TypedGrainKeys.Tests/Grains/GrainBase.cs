using Orleans.Runtime;

namespace Orleans.TypedGrainKeys.Tests.Grains;

public abstract class GrainBase<TKey> : IGrainBase, IGrainWithTypedKey<TKey> where TKey : ITypedGrainKey<TKey>
{
    protected GrainBase(IGrainContext grainContext)
    {
        GrainContext = grainContext;
    }

    public IGrainContext GrainContext { get; }

    public TKey Key => this.GetPrimaryKeyString();

    public virtual Task OnActivateAsync(CancellationToken token) => Task.CompletedTask;

    public virtual Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token) => Task.CompletedTask;
}