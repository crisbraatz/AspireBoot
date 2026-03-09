using AspireBoot.ApiService.Helpers;
using AspireBoot.Domain.DTOs.Users;
using AspireBoot.Domain.Entities.Users;

namespace AspireBoot.ApiService.Contracts.Users;

public sealed class ListUsersRequest : BaseListRequest
{
    public string? Email { get; init; }

    public ListUsersRequestDto ConvertToDto() => new()
    {
        Email = Email,
        Id = Id,
        CreatedAt = CreatedAt,
        CreatedBy = CreatedBy,
        UpdatedAt = UpdatedAt,
        UpdatedBy = UpdatedBy,
        Active = Active,
        OrderBy = OrderByHelper.ToDictionary<User>(OrderBy),
        CurrentPage = CurrentPage,
        Size = Size
    };
}
