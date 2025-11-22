using AspireBoot.ApiService.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
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
    public void ShouldGetClaimFromAuthorization()
    {
        StringValues authorization =
            "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImV4YW1wbGVAZW1haWwuY29tIiwibmJmIjoxNzYzMzM1Njc4LCJleHAiOjE3NjMzMzkyNzgsImlhdCI6MTc2MzMzNTY3OCwiaXNzIjoiREVGQVVMVF9KV1RfSVNTVUVSIiwiYXVkIjoiREVGQVVMVF9KV1RfQVVESUVOQ0UifQ.wRcDBkVrTQhaO-wbK6WQIMpIzmTwTCI81ASgzjMgUMU";

        string? claim = TokenHelper.GetClaimFrom(authorization);

        claim.Should().Be("example@email.com");
    }

    [Fact]
    public void ShouldGetSecurityKey()
    {
        Environment.SetEnvironmentVariable("JWT_SECURITY_KEY", "NOT_DEFAULT_256_BITS_JWT_SECURITY_KEY");

        SymmetricSecurityKey securityKey = TokenHelper.GetSecurityKey();

        securityKey.KeySize.Should().Be(264);
    }
}
