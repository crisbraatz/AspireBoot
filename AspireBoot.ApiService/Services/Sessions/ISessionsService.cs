using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;

namespace AspireBoot.ApiService.Services.Sessions;

public interface ISessionsService
{
    Task<BaseResponseDto<RefreshSessionResponseDto>> CreateAsync(
        CreateSessionRequestDto request, CancellationToken cancellationToken = default);

    Task<BaseResponseDto> DeleteAsync(DeleteSessionRequestDto request, CancellationToken cancellationToken = default);

    Task<BaseResponseDto<RefreshSessionResponseDto>> RefreshAsync(
        RefreshSessionRequestDto request, CancellationToken cancellationToken = default);
}
