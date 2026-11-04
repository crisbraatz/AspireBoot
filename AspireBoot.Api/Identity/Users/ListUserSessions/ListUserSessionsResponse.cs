using AspireBoot.Api.Primitives;

namespace AspireBoot.Api.Identity.Users.ListUserSessions;

public record ListUserSessionsResponse(IReadOnlyList<ListUserSessionItemResponse> Items, int Page, int Size, int Count)
    : ListResponse<ListUserSessionItemResponse>(Items, Page, Size, Count);
