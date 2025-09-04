using AspireBoot.Api.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;
using AspireBoot.Domain.DTOs.Users;

namespace AspireBoot.Api.Services.Auth;

public interface IAuthService
{
    Task<BaseResponseDto<string>> DummyCallAsync(CancellationToken token = default);
    
    Task<BaseResponseDto<RefreshTokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request, CancellationToken token = default);

    Task<BaseResponseDto<bool>> SignInAsync(SignInUserRequestDto request, CancellationToken token = default);
    Task<BaseResponseDto<bool>> SignOutAsync(RefreshTokenRequestDto request, CancellationToken token = default);
    Task<BaseResponseDto<bool>> SignUpAsync(SignUpUserRequestDto request, CancellationToken token = default);
    Task<ValidatorResponse> ValidateJwtAsync(string? request, CancellationToken token = default);
}