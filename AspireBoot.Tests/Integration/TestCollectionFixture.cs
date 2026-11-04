namespace AspireBoot.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public class TestCollectionFixture : ICollectionFixture<TestFixture>
{
    public const string Name = "Integration";
}
