using AspireBoot.Core.Identity.Users;

namespace AspireBoot.Tests.Unit.Core.Identity.Users;

public class NamePolicyTests
{
    [Theory]
    [InlineData("João da Silva")]
    [InlineData("Jose\u0301")]
    [InlineData("  mARIA\t  sILVA  ")]
    public void ShouldReturnTrueWhenAcceptingLettersAndNormalizedSpaces(string name)
    {
        bool valid = NamePolicy.Validate(name);

        Assert.True(valid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("User123")]
    [InlineData("User-Name")]
    [InlineData("User_Name")]
    public void ShouldReturnFalseWhenNamesWithInvalidCharactersAreProvided(string name)
    {
        bool valid = NamePolicy.Validate(name);

        Assert.False(valid);
    }
}
