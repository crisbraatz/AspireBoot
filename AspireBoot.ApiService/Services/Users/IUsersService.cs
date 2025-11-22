using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.ApiService.Services.Users;

public interface IUsersService
{
    Task<BaseListResponseDto<ListUserResponseDto>> ListByAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default);
}
