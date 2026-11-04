using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Users.ListUserSessions;

public class ListUserSessionsQueryValidator : IQueryValidator<ListUserSessionsQuery>
{
    public IReadOnlyList<Failure> Validate(ListUserSessionsQuery query)
    {
        if (query.TargetUserId != query.CurrentUserId)
            return
            [
                Failure.Forbidden(
                    "identity.users.forbidden",
                    "User is forbidden.")
            ];

        List<Failure> failures = [];

        if (query.Page < 1)
            failures.Add(Failure.Validation(
                "identity.users.page.minimum",
                "Page must be at least 1.",
                "page"));

        if (query.Size is < 1 or > 100)
            failures.Add(Failure.Validation(
                "identity.users.size.range",
                "Page size must be between 1 and 100.",
                "size"));

        if (query is { Page: >= 1, Size: >= 1 and <= 100 } && (long)(query.Page - 1) * query.Size > int.MaxValue)
            failures.Add(
                Failure.Validation(
                    "identity.users.page.offset",
                    "Page offset is too large.",
                    "page"));

        return failures;
    }
}
