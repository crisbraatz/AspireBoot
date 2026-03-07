namespace AspireBoot.Tests.Integration;

#pragma warning disable CA1515
#pragma warning disable CA1711
[CollectionDefinition("IntegrationTestsCollection")]
public abstract class IntegrationTestsCollection : ICollectionFixture<IntegrationTestsFixture>;
#pragma warning restore CA1711
#pragma warning restore CA1515
