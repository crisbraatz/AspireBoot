using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.ApiService.Services.Users;

public interface IUsersService
{
    Task<BaseResponseDto<RefreshSessionResponseDto>> CreateAsync(
        CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<BaseListResponseDto<ListUserResponseDto>> ListAsync(
        ListUsersRequestDto request, CancellationToken cancellationToken = default);
}
