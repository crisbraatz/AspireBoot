using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Users.ListUsers;

namespace AspireBoot.Tests.Unit.Core.Identity.Users.ListUsers;

public class ListUsersQueryValidatorTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(int.MaxValue, 1)]
    public void ShouldReturnNoFailuresWhenAcceptingValidPagination(int page, int size)
    {
        IReadOnlyList<Failure> failures = new ListUsersQueryValidator().Validate(new ListUsersQuery(page, size, false));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(0, 20, "identity.users.page.minimum", "Page must be at least 1.", "page")]
    [InlineData(-1, 20, "identity.users.page.minimum", "Page must be at least 1.", "page")]
    [InlineData(1, 0, "identity.users.size.range", "Page size must be between 1 and 100.", "size")]
    [InlineData(1, 101, "identity.users.size.range", "Page size must be between 1 and 100.", "size")]
    [InlineData(int.MaxValue, 100, "identity.users.page.offset", "Page offset is too large.", "page")]
    public void ShouldReturnValidationFailuresWhenThePaginationIsInvalid(int page, int size, string code, string message, string field)
    {
        IReadOnlyList<Failure> failures = new ListUsersQueryValidator().Validate(new ListUsersQuery(page, size, false));

        Failure failure = Assert.Single(failures);
        Assert.Equal(FailureKind.Validation, failure.FailureKind);
        Assert.Equal(code, failure.Code);
        Assert.Equal(message, failure.Message);
        Assert.Equal(field, failure.Field);
    }

    [Fact]
    public void ShouldReturnValidationFailuresWhenReportingEveryInvalidPaginationField()
    {
        IReadOnlyList<Failure> failures = new ListUsersQueryValidator().Validate(new ListUsersQuery(0, 0, false));

        Assert.Equal(2, failures.Count);
        Assert.Equal("identity.users.page.minimum", failures[0].Code);
        Assert.Equal("identity.users.size.range", failures[1].Code);
    }
}
