using System.Security.Claims;
using System.Security.Principal;
using AspireBoot.Api.Identity.Users;
using AspireBoot.Api.Primitives;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.GetCurrentUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Api.Identity.Users;

public class UsersEndpointFilterTests
{
    [Fact]
    public async Task ShouldReturnTheEndpointResultWithTheResolvedUserId()
    {
        Guid userId = Guid.NewGuid();
        IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult> queryHandler = MockQueryHandler(Result<GetCurrentUserResult>.Success(new GetCurrentUserResult(userId)));
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(queryHandler).BuildServiceProvider();
        ClaimsIdentity claimsIdentity = new([new Claim("sub", "account")], "tests");
        DefaultHttpContext defaultHttpContext = new() { User = new ClaimsPrincipal(claimsIdentity), RequestServices = serviceProvider };
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(defaultHttpContext);
        object expectedResult = new();
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(expectedResult);

        object? result = await new UsersEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate);

        Assert.Same(expectedResult, result);
        Assert.Equal(userId.ToString(), claimsIdentity.FindFirst(ApiConstants.UserIdClaimType)!.Value);
        await queryHandler.Received(1).HandleAsync(Arg.Is<GetCurrentUserQuery>(query => query.IdentityUserId == "account"), defaultHttpContext.RequestAborted);
        await endpointFilterDelegate.Received(1).Invoke(defaultEndpointFilterInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnTheEndpointResultWithExistingUserIdClaimsReplaced()
    {
        Guid userId = Guid.NewGuid();
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(MockQueryHandler(Result<GetCurrentUserResult>.Success(new GetCurrentUserResult(userId)))).BuildServiceProvider();
        ClaimsIdentity claimsIdentity = new([new Claim("sub", "account"), new Claim(ApiConstants.UserIdClaimType, Guid.NewGuid().ToString()), new Claim(ApiConstants.UserIdClaimType, Guid.NewGuid().ToString())], "tests");

        await new UsersEndpointFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(new DefaultHttpContext { User = new ClaimsPrincipal(claimsIdentity), RequestServices = serviceProvider }), MockEndpointFilterDelegate(new object()));

        Assert.Equal(userId.ToString(), Assert.Single(claimsIdentity.FindAll(ApiConstants.UserIdClaimType)).Value);
        Assert.Equal("account", claimsIdentity.FindFirst("sub")!.Value);
    }

    [Fact]
    public async Task ShouldReturnTheEndpointResultWhenThePrincipalHasNoClaimsIdentity()
    {
        ClaimsPrincipal claimsPrincipal = MockClaimsPrincipal();
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(MockQueryHandler(Result<GetCurrentUserResult>.Success(new GetCurrentUserResult(Guid.NewGuid())))).BuildServiceProvider();
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(new DefaultHttpContext { User = claimsPrincipal, RequestServices = serviceProvider });
        object expectedResult = new();
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(expectedResult);

        object? result = await new UsersEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate);

        Assert.Same(expectedResult, result);
        await endpointFilterDelegate.Received(1).Invoke(defaultEndpointFilterInvocationContext);
    }

    [Theory]
    [InlineData(FailureKind.NotFound, 404)]
    [InlineData(FailureKind.ServiceUnavailable, 503)]
    public async Task ShouldReturnTheQueryFailureWithoutInvokingTheEndpoint(FailureKind failureKind, int statusCode)
    {
        ClaimsIdentity claimsIdentity = new([new Claim("sub", "account")], "tests");
        Failure failure = new(failureKind, "identity.users.unavailable", "User unavailable.");
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(MockQueryHandler(Result<GetCurrentUserResult>.Failure([failure]))).BuildServiceProvider();
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(new object());

        object? result = await new UsersEndpointFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(new DefaultHttpContext { User = new ClaimsPrincipal(claimsIdentity), RequestServices = serviceProvider }), endpointFilterDelegate);

        ProblemHttpResult problemHttpResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(statusCode, problemHttpResult.StatusCode);
        Assert.Equal(failure.Code, problemHttpResult.ProblemDetails.Extensions["code"]);
        Assert.Empty(claimsIdentity.FindAll(ApiConstants.UserIdClaimType));
        await endpointFilterDelegate.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    private static ClaimsPrincipal MockClaimsPrincipal()
    {
        ClaimsPrincipal claimsPrincipal = Substitute.For<ClaimsPrincipal>();
        claimsPrincipal.Identity.Returns(Substitute.For<IIdentity>());
        claimsPrincipal.FindFirst("sub").Returns(new Claim("sub", "account"));
        return claimsPrincipal;
    }

    private static EndpointFilterDelegate MockEndpointFilterDelegate(object result)
    {
        EndpointFilterDelegate endpointFilterDelegate = Substitute.For<EndpointFilterDelegate>();
        endpointFilterDelegate(Arg.Any<EndpointFilterInvocationContext>()).Returns(result);
        return endpointFilterDelegate;
    }

    private static IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult> MockQueryHandler(Result<GetCurrentUserResult> result)
    {
        IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult> queryHandler = Substitute.For<IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult>>();
        queryHandler.HandleAsync(Arg.Any<GetCurrentUserQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        return queryHandler;
    }
}
