namespace AspireBoot.Domain.DTOs.Sessions;

public class RefreshSessionRequestDto(string? token, string? requestedBy) : BaseRequestDto(requestedBy)
{
    public string? Token { get; } = token;
}
