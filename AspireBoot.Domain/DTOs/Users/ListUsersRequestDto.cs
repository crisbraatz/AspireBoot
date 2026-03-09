using AspireBoot.Domain.Entities.Users;

namespace AspireBoot.Domain.DTOs.Users;

public class ListUsersRequestDto : BaseListRequestDto<User>
{
    public string? Email { get; init; }
}
