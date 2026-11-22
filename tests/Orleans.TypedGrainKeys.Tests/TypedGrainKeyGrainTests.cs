using Orleans.TestingHost;
using Orleans.TypedGrainKeys.Tests.Grains;
using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

[ClassDataSource<TestClusterFixture>(Shared = SharedType.PerTestSession)]
public class TypedGrainKeyGrainTests(TestClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly int _userId = Random.Shared.Next(1_000_000, 99_999_999);

    [Test]
    public async Task Can_Get_Grain_Using_ClusterClient()
    {
        var userGrainKey = new UserGrainKey(_tenantId, _userId);

        var userTestGrain = _cluster.Client.GetGrain<IUserTestGrain>(userGrainKey);

        await Assert.That(userTestGrain.GetTypedKey<UserGrainKey>()).IsEqualTo(userGrainKey);
    }

    [Test]
    public async Task Can_Get_Grain_Using_GrainFactory()
    {
        var userGrainKey = new UserGrainKey(_tenantId, _userId);

        var userTestGrain = _cluster.GrainFactory.GetGrain<IUserTestGrain>(userGrainKey);

        await Assert.That(userTestGrain.GetPrimaryKeyString()).IsEqualTo($"user|{_tenantId}|{_userId}");
    }

    [Test]
    public async Task Can_Call_Grain()
    {
        var userTestGrain = _cluster.GrainFactory.GetGrain<IUserTestGrain>(new UserGrainKey(_tenantId, _userId));

        var response = await userTestGrain.Lock();

        await Assert.That(response.Message).IsEqualTo($"The user {_userId} in tenant {_tenantId} was successfully locked");
    }

    [Test]
    public async Task Poco_Grain_Gets_A_Generated_Key()
    {
        var grain = _cluster.GrainFactory.GetGrain<IPocoUserTestGrain>(new UserGrainKey(_tenantId, _userId));

        var response = await grain.Describe();

        await Assert.That(response.Message).IsEqualTo($"POCO grain for user {_userId} in tenant {_tenantId}");
    }
}
