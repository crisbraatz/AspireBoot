namespace AspireBoot.Domain.DTOs.Auth;

public class SignUpUserRequestDto(string email, string password, string? requestedBy) : BaseRequestDto(requestedBy)
{
    public string Email { get; } = email;
    public string Password { get; } = password;
}
