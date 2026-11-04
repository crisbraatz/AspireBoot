using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.ListUserSessions;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.ListUserSessions;

public class ListUserSessionsQueryValidatorTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(int.MaxValue, 1)]
    public void ShouldReturnNoFailuresWhenAcceptingOwnerAndValidPagination(int page, int size)
    {
        Guid userId = Guid.CreateVersion7();

        IReadOnlyList<Failure> failures = new ListUserSessionsQueryValidator().Validate(new ListUserSessionsQuery(userId, userId, "session", page, size));

        Assert.Empty(failures);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenAnotherUsersSessionsBeforePaginationValidationIsProvided()
    {
        IReadOnlyList<Failure> failures = new ListUserSessionsQueryValidator().Validate(new ListUserSessionsQuery(Guid.CreateVersion7(), Guid.CreateVersion7(), "session", 0, 0));

        Assert.Equal(FailureKind.Forbidden, Assert.Single(failures).FailureKind);
    }

    [Theory]
    [InlineData(0, 10, "identity.users.page.minimum")]
    [InlineData(1, 0, "identity.users.size.range")]
    [InlineData(1, 101, "identity.users.size.range")]
    [InlineData(int.MaxValue, 100, "identity.users.page.offset")]
    public void ShouldReturnValidationFailuresWhenThePaginationIsInvalid(int page, int size, string code)
    {
        Guid userId = Guid.CreateVersion7();

        IReadOnlyList<Failure> failures = new ListUserSessionsQueryValidator().Validate(new ListUserSessionsQuery(userId, userId, "session", page, size));

        Failure failure = Assert.Single(failures);
        Assert.Equal(FailureKind.Validation, failure.FailureKind);
        Assert.Equal(code, failure.Code);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenReportingEveryInvalidPaginationField()
    {
        Guid userId = Guid.NewGuid();

        IReadOnlyList<Failure> failures = new ListUserSessionsQueryValidator().Validate(new ListUserSessionsQuery(userId, userId, "session", 0, 0));

        Assert.Equal(2, failures.Count);
        Assert.Equal("identity.users.page.minimum", failures[0].Code);
        Assert.Equal("identity.users.size.range", failures[1].Code);
    }
}
