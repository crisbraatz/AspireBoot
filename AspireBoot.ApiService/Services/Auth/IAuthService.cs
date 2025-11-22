using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.ApiService.Services.Auth;

public interface IAuthService
{
    Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    Task<BaseResponseDto> SignInAsync(SignInUserRequestDto request, CancellationToken cancellationToken = default);
    Task<BaseResponseDto> SignOutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<BaseResponseDto> SignUpAsync(SignUpUserRequestDto request, CancellationToken cancellationToken = default);
    Task<ValidatorResponse> ValidateJwtAsync(string? request, CancellationToken cancellationToken = default);
}
