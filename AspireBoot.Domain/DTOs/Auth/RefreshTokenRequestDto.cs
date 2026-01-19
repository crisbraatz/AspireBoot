namespace AspireBoot.Domain.DTOs.Auth;

public class RefreshTokenRequestDto(string? token, string? requestedBy, bool isRefresh = true)
    : BaseRequestDto(requestedBy)
{
    public string? Token { get; } = token;
    public bool IsRefresh { get; } = isRefresh;
}
