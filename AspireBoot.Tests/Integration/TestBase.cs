namespace AspireBoot.Tests.Integration;

[Collection(TestCollectionFixture.Name)]
public abstract class TestBase(TestFixture testFixture) : IAsyncLifetime
{
    protected HttpClient HttpClient { get; private set; } = null!;
    protected TestFixture TestFixture { get; } = testFixture;

    public async ValueTask InitializeAsync()
    {
        await TestFixture.ResetAsync().ConfigureAwait(false);

        HttpClient = TestFixture.ApiWebApplicationFactory.CreateHttpClient(TestFixture.CreateIpAddress());
    }

    public ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}
