using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests.Grains;

public interface IPocoUserTestGrain : IGrainWithTypedKey<UserGrainKey>
{
    Task<Response> Describe();
}
