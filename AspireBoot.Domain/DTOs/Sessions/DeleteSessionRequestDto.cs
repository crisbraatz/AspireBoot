namespace AspireBoot.Domain.DTOs.Sessions;

public class DeleteSessionRequestDto(string? token, string? requestedBy) : BaseRequestDto(requestedBy)
{
    public string? Token { get; } = token;
}
