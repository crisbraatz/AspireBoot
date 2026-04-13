namespace AspireBoot.ApiService.Contracts.Sessions;

public sealed class RefreshSessionResponse(string token)
{
    public string Token { get; } = token;
}
