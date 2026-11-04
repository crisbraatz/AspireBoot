using System.Security.Claims;
using AspireBoot.Api.Primitives;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Api.Primitives;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetEmailShouldReturnTheEmailClaim()
    {
        string email = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "user@example.com")])).GetEmail();

        Assert.Equal("user@example.com", email);
    }

    [Fact]
    public void GetEmailShouldThrowInvalidOperationExceptionWhenTheEmailClaimIsMissing()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ClaimsPrincipal().GetEmail());

        Assert.Equal("Unable to get email.", exception.Message);
    }

    [Fact]
    public void GetIdentityUserIdShouldReturnTheIdentityUserIdClaim()
    {
        string identityUserId = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "account")])).GetIdentityUserId();

        Assert.Equal("account", identityUserId);
    }

    [Fact]
    public void GetIdentityUserIdShouldThrowInvalidOperationExceptionWhenTheIdentityUserIdClaimIsMissing()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ClaimsPrincipal().GetIdentityUserId());

        Assert.Equal("Unable to get identity user ID.", exception.Message);
    }

    [Fact]
    public void GetUserIdShouldReturnTheParsedUserIdClaim()
    {
        Guid expectedUserId = Guid.NewGuid();

        Guid userId = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ApiConstants.UserIdClaimType, expectedUserId.ToString())])).GetUserId();

        Assert.Equal(expectedUserId, userId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void GetUserIdShouldThrowInvalidOperationExceptionWhenTheUserIdClaimIsMissingOrInvalid(string? value = null)
    {
        ClaimsIdentity claimsIdentity = new();
        if (value is not null)
            claimsIdentity.AddClaim(new Claim(ApiConstants.UserIdClaimType, value));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ClaimsPrincipal(claimsIdentity).GetUserId());

        Assert.Equal("Unable to get user ID.", exception.Message);
    }

    [Fact]
    public async Task IsAdministratorAsyncShouldReturnTrueWhenTheAdministratorPolicySucceeds()
    {
        ClaimsPrincipal claimsPrincipal = new();
        IAuthorizationService authorizationService = MockAuthorizationService(AuthorizationResult.Success());

        bool isAdministrator = await claimsPrincipal.IsAdministratorAsync(authorizationService);

        Assert.True(isAdministrator);
        await authorizationService.Received(1).AuthorizeAsync(claimsPrincipal, null, ApiConstants.AuthorizationPolicyName);
    }

    [Fact]
    public async Task IsAdministratorAsyncShouldReturnFalseWhenTheAdministratorPolicyFails()
    {
        ClaimsPrincipal claimsPrincipal = new(new ClaimsIdentity([new Claim("roles", "Administrator")], "tests"));
        IAuthorizationService authorizationService = MockAuthorizationService(AuthorizationResult.Failed());

        bool isAdministrator = await claimsPrincipal.IsAdministratorAsync(authorizationService);

        Assert.False(isAdministrator);
        await authorizationService.Received(1).AuthorizeAsync(claimsPrincipal, null, ApiConstants.AuthorizationPolicyName);
    }

    private static IAuthorizationService MockAuthorizationService(AuthorizationResult authorizationResult)
    {
        IAuthorizationService authorizationService = Substitute.For<IAuthorizationService>();
        authorizationService.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), ApiConstants.AuthorizationPolicyName).Returns(Task.FromResult(authorizationResult));
        return authorizationService;
    }
}
