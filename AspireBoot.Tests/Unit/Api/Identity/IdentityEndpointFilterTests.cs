using AspireBoot.Api.Identity;
using AspireBoot.Api.Identity.Auth.Register;
using AspireBoot.Api.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Api.Identity;

public class IdentityEndpointFilterTests
{
    [Fact]
    public async Task ShouldReturnTheEndpointResultWhenTheIdentityRequestIsValid()
    {
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(new DefaultHttpContext(), new RegisterRequest("user@example.com", "Test-Password123!", "User Name"));
        object expectedResult = new();
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(expectedResult);

        object? result = await new IdentityEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate);

        Assert.Same(expectedResult, result);
        await endpointFilterDelegate.Received(1).Invoke(defaultEndpointFilterInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnTheEndpointResultWhenThePrimitivesRequestIsValid()
    {
        object expectedResult = new();

        object? result = await new IdentityEndpointFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new ListRequest(1, 100)), MockEndpointFilterDelegate(expectedResult));

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task ShouldReturnTheEndpointResultWhenArgumentsAreNullOrNotRequests()
    {
        DefaultHttpContext defaultHttpContext = new();
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(defaultHttpContext, null, defaultHttpContext, 42, new object());
        object expectedResult = new();
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(expectedResult);

        object? result = await new IdentityEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate);

        Assert.Same(expectedResult, result);
        await endpointFilterDelegate.Received(1).Invoke(defaultEndpointFilterInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnAllInvalidIdentityFieldsUsingCamelCase()
    {
        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(new object());

        object? result = await new IdentityEndpointFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), new RegisterRequest("invalid", "short", "User123")), endpointFilterDelegate);

        ProblemHttpResult problemHttpResult = Assert.IsType<ProblemHttpResult>(result);
        HttpValidationProblemDetails httpValidationProblemDetails = Assert.IsAssignableFrom<HttpValidationProblemDetails>(problemHttpResult.ProblemDetails);
        Assert.Equal(400, problemHttpResult.StatusCode);
        Assert.Equal(3, httpValidationProblemDetails.Errors.Count);
        Assert.Contains("email", httpValidationProblemDetails.Errors.Keys);
        Assert.Contains("password", httpValidationProblemDetails.Errors.Keys);
        Assert.Contains("name", httpValidationProblemDetails.Errors.Keys);
        await endpointFilterDelegate.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    [Fact]
    public async Task ShouldReturnValidationProblemDetailsForEveryInvalidPaginationField()
    {
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(new DefaultHttpContext(), new ListRequest(0, 101));

        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(new object());

        HttpValidationProblemDetails httpValidationProblemDetails = Assert.IsAssignableFrom<HttpValidationProblemDetails>(Assert.IsType<ProblemHttpResult>(await new IdentityEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate)).ProblemDetails);
        Assert.Equal(2, httpValidationProblemDetails.Errors.Count);
        Assert.Contains("page", httpValidationProblemDetails.Errors.Keys);
        Assert.Contains("size", httpValidationProblemDetails.Errors.Keys);
        await endpointFilterDelegate.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    [Fact]
    public async Task ShouldReturnValidationProblemDetailsForTheFirstInvalidRequestArgument()
    {
        DefaultEndpointFilterInvocationContext defaultEndpointFilterInvocationContext = new(new DefaultHttpContext(), new object(), new RegisterRequest("invalid", "Test-Password123!", "User Name"), new ListRequest(0, 0));

        EndpointFilterDelegate endpointFilterDelegate = MockEndpointFilterDelegate(new object());

        Assert.Equal("email", Assert.Single(Assert.IsAssignableFrom<HttpValidationProblemDetails>(Assert.IsType<ProblemHttpResult>(await new IdentityEndpointFilter().InvokeAsync(defaultEndpointFilterInvocationContext, endpointFilterDelegate)).ProblemDetails).Errors).Key);
        await endpointFilterDelegate.DidNotReceive().Invoke(Arg.Any<EndpointFilterInvocationContext>());
    }

    private static EndpointFilterDelegate MockEndpointFilterDelegate(object result)
    {
        EndpointFilterDelegate endpointFilterDelegate = Substitute.For<EndpointFilterDelegate>();
        endpointFilterDelegate(Arg.Any<EndpointFilterInvocationContext>()).Returns(result);
        return endpointFilterDelegate;
    }
}
