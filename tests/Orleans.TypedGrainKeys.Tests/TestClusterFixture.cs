using Orleans.TestingHost;

namespace Orleans.TypedGrainKeys.Tests;

public class TestClusterFixture : IAsyncLifetime
{
    public TestClusterFixture()
    {
        var builder = new TestClusterBuilder
        {
            Options =
                {
                    ClusterId = "test-cluster",
                    ServiceId = "test-service"
                }
        };
        builder
            .AddSiloBuilderConfigurator<TestSiloConfigurator>();

        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; set; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
    }
}