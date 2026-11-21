using Orleans.TestingHost;
using TUnit.Core.Interfaces;

namespace Orleans.TypedGrainKeys.Tests;

public class TestClusterFixture : IAsyncInitializer, IAsyncDisposable
{
    public TestCluster Cluster { get; } = new TestClusterBuilder
        {
            Options =
            {
                ClusterId = "test-cluster",
                ServiceId = "test-service"
            }
        }
        .AddSiloBuilderConfigurator<TestSiloConfigurator>()
        .Build();

    public Task InitializeAsync() => Cluster.DeployAsync();

    public async ValueTask DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await Cluster.DisposeAsync();
    }
}
