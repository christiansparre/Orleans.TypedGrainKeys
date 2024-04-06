using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests.Grains;

public interface IUserTestGrain : IGrainWithTypedKey<UserGrainKey>
{
    Task<Response> Lock();
    Task<Response> Unlock();
    Task<Response> Delete();
}