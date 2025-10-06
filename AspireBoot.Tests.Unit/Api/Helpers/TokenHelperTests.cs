using AspireBoot.Api.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Primitives;

namespace AspireBoot.Tests.Unit.Api.Helpers;

public class TokenHelperTests
{
    [Fact]
    public void Should_generate_jwt_for_email()
    {
        var jwt = TokenHelper.GenerateJwtFor("example@email.com");

        jwt.Should().HaveLength(263);
    }

    [Fact]
    public void Should_get_claim_from_authorization()
    {
        StringValues authorization =
            "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImV4YW1wbGVAZW1haWwuY29tIiwibmJmIjoxNzU3MjAwODY2LCJleHAiOjE3NTcyMDQ0NjYsImlhdCI6MTc1NzIwMDg2NiwiaXNzIjoiREVGQVVMVF9KV1RfSVNTVUVSIiwiYXVkIjoiREVGQVVMVF9KV1RfQVVESUVOQ0UifQ.bt9VBAvaBKZo0MbJ6FUHPaIdbLxj4AZCcs6G-o9Op1s";

        var claim = TokenHelper.GetClaimFrom(authorization);

        claim.Should().Be("example@email.com");
    }

    [Fact]
    public void Should_get_security_key()
    {
        Environment.SetEnvironmentVariable("JWT_SECURITY_KEY", "NOT_DEFAULT_256_BITS_JWT_SECURITY_KEY");

        var securityKey = TokenHelper.GetSecurityKey();

        securityKey.KeySize.Should().Be(264);
    }
}