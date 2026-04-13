namespace AspireBoot.Domain.DTOs.Users;

public class CreateUserRequestDto(string email, string password, string? requestedBy) : BaseRequestDto(requestedBy)
{
    public string Email { get; } = email;
    public string Password { get; } = password;
}
