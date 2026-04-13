namespace AspireBoot.Domain.DTOs.Sessions;

public class CreateSessionRequestDto(string email, string password, string? requestedBy) : BaseRequestDto(requestedBy)
{
    public string Email { get; } = email;
    public string Password { get; } = password;
}
