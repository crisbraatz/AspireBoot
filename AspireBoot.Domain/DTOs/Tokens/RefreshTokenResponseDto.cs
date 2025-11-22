namespace AspireBoot.Domain.DTOs.Tokens;

public class RefreshTokenResponseDto(string refreshToken, string token)
{
    public string RefreshToken { get; } = refreshToken;
    public string Token { get; } = token;
}
