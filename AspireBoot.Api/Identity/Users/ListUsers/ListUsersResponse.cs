using AspireBoot.Api.Primitives;

namespace AspireBoot.Api.Identity.Users.ListUsers;

public record ListUsersResponse(IReadOnlyList<ListUserItemResponse> Items, int Page, int Size, int Count)
    : ListResponse<ListUserItemResponse>(Items, Page, Size, Count);
