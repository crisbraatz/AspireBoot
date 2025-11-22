using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AspireBoot.Domain;
using Microsoft.Extensions.Primitives;
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
            Subject = new ClaimsIdentity([new Claim(nameof(email), email)])
        }));
    }

    public static string GenerateRefreshJwtFor() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string? GetClaimFrom(StringValues stringValues) =>
        (new JwtSecurityTokenHandler().ReadToken(stringValues.ToString().Trim()[7..]) as JwtSecurityToken)?.Claims
        .FirstOrDefault(x => x.Type.Equals("email", StringComparison.OrdinalIgnoreCase))?.Value;

    public static SymmetricSecurityKey GetSecurityKey() => new(Encoding.ASCII.GetBytes(AppSettings.JwtSecurityKey));
}
