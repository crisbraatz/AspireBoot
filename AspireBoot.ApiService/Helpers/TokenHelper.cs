using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AspireBoot.Domain;
using Microsoft.IdentityModel.Tokens;

namespace AspireBoot.ApiService.Helpers;

public static class TokenHelper
{
    public static string GenerateJwtFor(string email)
    {
        JwtSecurityTokenHandler jwtSecurityTokenHandler = new();

        return jwtSecurityTokenHandler.WriteToken(jwtSecurityTokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = AppSettings.JwtAudience,
            Expires = DateTime.UtcNow.AddMinutes(AppSettings.JwtExpiresAfter),
            Issuer = AppSettings.JwtIssuer,
            SigningCredentials = new SigningCredentials(GetSecurityKey(), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity([new Claim("email", email)])
        }));
    }

    public static string GenerateRefreshJwtFor() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static SymmetricSecurityKey GetSecurityKey() => new(Encoding.ASCII.GetBytes(AppSettings.JwtSecurityKey));
}
