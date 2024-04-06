using Orleans.Runtime;
using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests.Grains;

public class UserTestGrain : GrainBase<UserGrainKey>, IUserTestGrain
{
    public UserTestGrain(IGrainContext grainContext) : base(grainContext)
    {
    }

    public Task<Response> Lock()
    {
        return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully locked"));
    }

    public Task<Response> Unlock()
    {
        return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully unlocked"));
    }

    public Task<Response> Delete()
    {
        return Task.FromResult(new Response($"The user {Key.UserId} in tenant {Key.TenantId} was successfully deleted"));
    }
}