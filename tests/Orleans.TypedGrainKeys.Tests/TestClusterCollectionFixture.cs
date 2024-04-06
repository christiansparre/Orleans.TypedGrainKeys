namespace Orleans.TypedGrainKeys.Tests;

[CollectionDefinition(Name)]
public class TestClusterCollectionFixture : ICollectionFixture<TestClusterFixture>
{
    public const string Name = "TestClusterCollection";
}