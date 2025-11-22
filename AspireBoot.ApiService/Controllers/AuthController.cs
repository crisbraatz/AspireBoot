using AspireBoot.ApiService.Contracts;
using AspireBoot.ApiService.Contracts.Tokens;
using AspireBoot.ApiService.Contracts.Users;
using AspireBoot.ApiService.Services.Auth;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.ApiService.Controllers;

public sealed class AuthController(IAuthService authService) : BaseController(authService)
{
    private readonly IAuthService _authService = authService;

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = await ValidateJwtAsync(cancellationToken).ConfigureAwait(false);
        if (validatorResponse.IsFailure)
            return BuildResponse(BaseResponse.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage));

        Request.Cookies.TryGetValue("refreshToken", out string? refreshToken);

        BaseResponseDto<RefreshTokenResponseDto> serviceResponse = await _authService
            .RefreshTokenAsync(
                new RefreshTokenRequestDto(refreshToken, GetClaimFromAuthorization()), cancellationToken)
            .ConfigureAwait(false);
        if (!serviceResponse.IsFailure)
            AppendAuthRefreshToken(serviceResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(serviceResponse));
    }

    [AllowAnonymous]
    [HttpPost("sign-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignInAsync(
        [FromBody] SignInRequest request, CancellationToken cancellationToken = default)
    {
        BaseResponseDto signInResponse = await _authService
            .SignInAsync(request.ConvertToDto(GetClaimFromAuthorization()), cancellationToken)
            .ConfigureAwait(false);
        if (signInResponse.IsFailure)
            return BuildResponse(BaseResponse.Failure(signInResponse.ErrorCode, signInResponse.ErrorMessage));

        BaseResponseDto<RefreshTokenResponseDto> refreshTokenResponse = await _authService
            .RefreshTokenAsync(
                new RefreshTokenRequestDto(null, request.Email, false), cancellationToken)
            .ConfigureAwait(false);

        AppendAuthRefreshToken(refreshTokenResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(refreshTokenResponse));
    }

    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = await ValidateJwtAsync(cancellationToken).ConfigureAwait(false);
        if (validatorResponse.IsFailure)
            return BuildResponse(BaseResponse.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage));

        Request.Cookies.TryGetValue("refreshToken", out string? refreshToken);

        BaseResponseDto serviceResponse = await _authService
            .SignOutAsync(new RefreshTokenRequestDto(refreshToken, GetClaimFromAuthorization()), cancellationToken)
            .ConfigureAwait(false);

        return BuildResponse(serviceResponse.IsFailure
            ? BaseResponse.Failure(serviceResponse.ErrorCode, serviceResponse.ErrorMessage)
            : BaseResponse.Success());
    }

    [AllowAnonymous]
    [HttpPost("sign-up")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUpAsync(
        [FromBody] SignUpRequest request, CancellationToken cancellationToken = default)
    {
        BaseResponseDto signUpResponse = await _authService
            .SignUpAsync(request.ConvertToDto(GetClaimFromAuthorization()), cancellationToken)
            .ConfigureAwait(false);
        if (signUpResponse.IsFailure)
            return BuildResponse(BaseResponse.Failure(signUpResponse.ErrorCode, signUpResponse.ErrorMessage));

        BaseResponseDto<RefreshTokenResponseDto> refreshTokenResponse = await _authService
            .RefreshTokenAsync(
                new RefreshTokenRequestDto(null, request.Email, false), cancellationToken)
            .ConfigureAwait(false);

        AppendAuthRefreshToken(refreshTokenResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(refreshTokenResponse));
    }
}
