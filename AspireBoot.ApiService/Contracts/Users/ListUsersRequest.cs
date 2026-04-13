using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.ApiService.Contracts.Users;

public sealed class ListUsersRequest : BaseListRequest
{
    public string? Email { get; init; }

    public ListUsersRequestDto ConvertToDto() => new()
    {
        Email = Email,
        CurrentPage = CurrentPage ?? 1,
        Size = Size ?? 10,
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "email" : SortBy,
        SortDescending = SortDescending ?? false
    };
}
