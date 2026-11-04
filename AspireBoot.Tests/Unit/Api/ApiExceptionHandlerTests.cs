using AspireBoot.Api;
using AspireBoot.Infrastructure.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AspireBoot.Tests.Unit.Api;

public class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ShouldReturnTrueAndWriteASafeInternalServerErrorForAnUnexpectedException()
    {
        DefaultHttpContext defaultHttpContext = new() { Request = { Path = "/api/users" } };

        bool handled = await new ApiExceptionHandler(MockProblemDetailsService(true, out List<ProblemDetailsContext> problemDetailsContexts)).TryHandleAsync(defaultHttpContext, new InvalidOperationException("private database details"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(500, defaultHttpContext.Response.StatusCode);
        ProblemDetails problemDetails = Assert.Single(problemDetailsContexts).ProblemDetails;
        Assert.Equal(500, problemDetails.Status);
        Assert.Equal("Internal server error", problemDetails.Title);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);
        Assert.Equal("/api/users", problemDetails.Instance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldReturnTrueAndNotTreatAnUncancelledRequestAsAClientDisconnect(bool ioFailure)
    {
        DefaultHttpContext defaultHttpContext = new();

        bool handled = await new ApiExceptionHandler(MockProblemDetailsService(true, out List<ProblemDetailsContext> problemDetailsContexts)).TryHandleAsync(defaultHttpContext, ioFailure ? new IOException("private IO details") : new OperationCanceledException(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(500, defaultHttpContext.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", Assert.Single(problemDetailsContexts).ProblemDetails.Detail);
    }

    [Theory]
    [InlineData(400, "Invalid request")]
    [InlineData(413, "Payload too large")]
    [InlineData(415, "Unsupported media type")]
    public async Task ShouldReturnTrueWhenPreservingBadRequestStatusWithoutExposingItsMessage(int statusCode, string title)
    {
        DefaultHttpContext defaultHttpContext = new();

        bool handled = await new ApiExceptionHandler(MockProblemDetailsService(true, out List<ProblemDetailsContext> problemDetailsContexts)).TryHandleAsync(defaultHttpContext, new BadHttpRequestException("private request details", statusCode), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(statusCode, defaultHttpContext.Response.StatusCode);
        ProblemDetails problemDetails = Assert.Single(problemDetailsContexts).ProblemDetails;
        Assert.Equal(statusCode, problemDetails.Status);
        Assert.Equal(title, problemDetails.Title);
        Assert.Equal("The request could not be read. Check its content type, format and size and try again.", problemDetails.Detail);
    }

    [Fact]
    public async Task ShouldReturnTrueWhenMappingARequiredDependencyFailureToServiceUnavailable()
    {
        DefaultHttpContext defaultHttpContext = new();

        bool handled = await new ApiExceptionHandler(MockProblemDetailsService(true, out List<ProblemDetailsContext> problemDetailsContexts)).TryHandleAsync(defaultHttpContext, new DependencyUnavailableException(DependencyKind.ObjectStorage, new IOException("private storage details")), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(503, defaultHttpContext.Response.StatusCode);
        ProblemDetails problemDetails = Assert.Single(problemDetailsContexts).ProblemDetails;
        Assert.Equal("Service unavailable", problemDetails.Title);
        Assert.Equal("A required service is temporarily unavailable. Try again later.", problemDetails.Detail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldReturn499WithoutWritingABodyWhenTheClientDisconnected(bool ioFailure)
    {
        DefaultHttpContext defaultHttpContext = new() { RequestAborted = new CancellationToken(true) };
        IProblemDetailsService problemDetailsService = MockProblemDetailsService(true, out List<ProblemDetailsContext> problemDetailsContexts);

        bool handled = await new ApiExceptionHandler(problemDetailsService).TryHandleAsync(defaultHttpContext, ioFailure ? new IOException("Disconnected.") : new OperationCanceledException(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(499, defaultHttpContext.Response.StatusCode);
        Assert.Empty(problemDetailsContexts);
        await problemDetailsService.DidNotReceive().TryWriteAsync(Arg.Any<ProblemDetailsContext>());
    }

    [Fact]
    public async Task ShouldReturnFalseWhenTheProblemDetailsWriterCannotHandleTheResponse()
    {
        DefaultHttpContext defaultHttpContext = new();

        bool handled = await new ApiExceptionHandler(MockProblemDetailsService(false, out List<ProblemDetailsContext> problemDetailsContexts)).TryHandleAsync(defaultHttpContext, new InvalidOperationException("private error details"), TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(500, defaultHttpContext.Response.StatusCode);
        Assert.Single(problemDetailsContexts);
    }

    private static IProblemDetailsService MockProblemDetailsService(bool written, out List<ProblemDetailsContext> problemDetailsContexts)
    {
        List<ProblemDetailsContext> capturedContexts = [];
        IProblemDetailsService problemDetailsService = Substitute.For<IProblemDetailsService>();
        problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(capturedContexts.Add)).Returns(written);
        problemDetailsContexts = capturedContexts;
        return problemDetailsService;
    }
}
