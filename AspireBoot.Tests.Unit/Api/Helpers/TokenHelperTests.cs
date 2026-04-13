using AspireBoot.ApiService.Helpers;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace AspireBoot.Tests.Unit.Api.Helpers;

public class TokenHelperTests
{
    [Fact]
    public void ShouldGenerateJwtForEmail()
    {
        string jwt = TokenHelper.GenerateJwtFor("example@email.com");

        jwt.Should().HaveLength(263);
    }

    [Fact]
    public void ShouldGetSecurityKey()
    {
        Environment.SetEnvironmentVariable("JWT_SECURITY_KEY", "NOT_DEFAULT_256_BITS_JWT_SECURITY_KEY");

        SymmetricSecurityKey securityKey = TokenHelper.GetSecurityKey();

        securityKey.KeySize.Should().Be(264);
    }

    [Fact]
    public void ShouldEmbedEmailClaim()
    {
        JwtSecurityToken? token = new JwtSecurityTokenHandler().ReadToken(
            TokenHelper.GenerateJwtFor("example@email.com")) as JwtSecurityToken;

        token?.Claims.Should().ContainSingle(x => x.Type == "email" && x.Value == "example@email.com");
    }
}
