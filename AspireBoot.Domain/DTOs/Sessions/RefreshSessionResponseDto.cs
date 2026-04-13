namespace AspireBoot.Domain.DTOs.Sessions;

public class RefreshSessionResponseDto(string refreshToken, string token)
{
    public string RefreshToken { get; } = refreshToken;
    public string Token { get; } = token;
}
