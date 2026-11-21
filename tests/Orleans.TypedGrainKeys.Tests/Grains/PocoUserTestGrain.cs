using Orleans.Runtime;

namespace Orleans.TypedGrainKeys.Tests.Grains;

/// <summary>A POCO grain (no Grain base class) that still gets a generated Key property.</summary>
public partial class PocoUserTestGrain(IGrainContext grainContext) : IGrainBase, IPocoUserTestGrain
{
    public IGrainContext GrainContext { get; } = grainContext;

    public Task<Response> Describe()
    {
        return Task.FromResult(new Response($"POCO grain for user {Key.UserId} in tenant {Key.TenantId}"));
    }
}
