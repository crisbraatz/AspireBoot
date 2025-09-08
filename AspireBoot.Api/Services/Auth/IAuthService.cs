using AspireBoot.Api.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.Api.Services.Auth;

public interface IAuthService
{
    Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken token = default);

    Task<BaseResponseDto> SignInAsync(SignInUserRequestDto request, CancellationToken token = default);
    Task<BaseResponseDto> SignOutAsync(RefreshTokenRequestDto request, CancellationToken token = default);
    Task<BaseResponseDto> SignUpAsync(SignUpUserRequestDto request, CancellationToken token = default);
    Task<BaseResponseDto<string>> TestAsync(CancellationToken token = default);
    Task<ValidatorResponse> ValidateJwtAsync(string? request, CancellationToken token = default);
}