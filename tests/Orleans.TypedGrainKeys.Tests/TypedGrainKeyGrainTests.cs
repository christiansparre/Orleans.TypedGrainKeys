using Orleans.TestingHost;
using Orleans.TypedGrainKeys.Tests.Grains;
using Orleans.TypedGrainKeys.Tests.Keys;
using Xunit.Abstractions;

namespace Orleans.TypedGrainKeys.Tests;

[Collection(TestClusterCollectionFixture.Name)]
public class TypedGrainKeyGrainTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestCluster _cluster;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly int _userId = Random.Shared.Next(1_000_000, 99_999_999);

    public TypedGrainKeyGrainTests(TestClusterFixture fixture, ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _cluster = fixture.Cluster;
    }

    [Fact]
    public void Can_Get_Grain_Using_ClusterClient()
    {
        var userGrainKey = new UserGrainKey(_tenantId, _userId);

        var userTestGrain = _cluster.Client.GetGrain<IUserTestGrain>(userGrainKey);

        Assert.Equal(userGrainKey, (UserGrainKey)userTestGrain.GetPrimaryKeyString());
    }

    [Fact]
    public void Can_Get_Grain_Using_GrainFactory()
    {
        var userGrainKey = new UserGrainKey(_tenantId, _userId);
        
        var userTestGrain = _cluster.GrainFactory.GetGrain<IUserTestGrain>(userGrainKey);
        
        Assert.Equal(userGrainKey, (UserGrainKey)userTestGrain.GetPrimaryKeyString());
    }

    [Fact]
    public async Task Can_Call_Grain()
    {
        var userTestGrain = _cluster.GrainFactory.GetGrain<IUserTestGrain>(new UserGrainKey(_tenantId, _userId));

        var response = await userTestGrain.Lock();

        _outputHelper.WriteLine(response.Message);

        Assert.Equal($"The user {_userId} in tenant {_tenantId} was successfully locked", response.Message);
    }
}