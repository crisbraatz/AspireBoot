using AspireBoot.Core.Identity;

namespace AspireBoot.Tests.Unit.Core.Identity;

public class IdentityConstantsTests
{
    [Fact]
    public void GetAuthSessionsKeyShouldReturnTheAccountScopedSessionIndexKey()
    {
        string key = IdentityConstants.GetAuthSessionsKey("account");

        Assert.Equal("identity:auth:account:sessions", key);
    }

    [Theory]
    [InlineData("USER@DOMAIN.COM", "user@domain.com")]
    [InlineData("user+tag@domain.com", "user+tag@domain.com")]
    public void NormalizeEmailShouldReturnTheNormalizedEmail(string input, string expected)
    {
        string normalized = IdentityConstants.NormalizeEmail(input);

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("  jOÃO\t dA   sILVA ", "João Da Silva")]
    [InlineData("mARIA", "Maria")]
    public void NormalizeNameShouldReturnTheNormalizedName(string input, string expected)
    {
        string normalized = IdentityConstants.NormalizeName(input);

        Assert.Equal(expected, normalized);
    }
}
