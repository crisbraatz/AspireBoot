using AspireBoot.Api.Contracts;
using AspireBoot.Api.Contracts.Auth;
using AspireBoot.Api.Services.Auth;
using AspireBoot.Domain.DTOs.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api.Controllers;

public class AuthController(IAuthService authService) : BaseController(authService)
{
    private readonly IAuthService _authService = authService;

    [HttpPost("dummy-call")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DummyCall(CancellationToken token = default)
    {
        var validationResponse = await ValidateJwtAsync(token);
        if (validationResponse.IsFailure)
            return BuildResponse(BaseResponse<bool>.Failure(
                validationResponse.ErrorCode, validationResponse.ErrorMessage));

        var serviceResponse = await _authService.DummyCallAsync(token);
        
        return BuildResponse(new BaseResponse<string> { Data = serviceResponse.Data });
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshTokenAsync(CancellationToken token = default)
    {
        var validationResponse = await ValidateJwtAsync(token);
        if (validationResponse.IsFailure)
            return BuildResponse(BaseResponse<bool>.Failure(
                validationResponse.ErrorCode, validationResponse.ErrorMessage));

        Request.Cookies.TryGetValue("authRefreshToken", out var refreshToken);

        var serviceResponse = await _authService.RefreshTokenAsync(
            new RefreshTokenRequestDto(refreshToken, GetClaimFromAuthorization()), token);

        if (!serviceResponse.IsFailure)
            AppendRefreshToken(serviceResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(serviceResponse));
    }

    [AllowAnonymous]
    [HttpPost("signin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignInAsync(
        [FromBody] SignInRequest request, CancellationToken token = default)
    {
        var signInResponse =
            await _authService.SignInAsync(request.ConvertToDto(GetClaimFromAuthorization()), token);
        if (signInResponse.IsFailure)
            return BuildResponse(BaseResponse<bool>.Failure(
                signInResponse.ErrorCode, signInResponse.ErrorMessage));

        var refreshTokenResponse = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto(
            null, request.Email.ToLowerInvariant(), false), token);

        AppendRefreshToken(refreshTokenResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(refreshTokenResponse));
    }

    [HttpPost("signout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignOutAsync(CancellationToken token = default)
    {
        var validationResponse = await ValidateJwtAsync(token);
        if (validationResponse.IsFailure)
            return BuildResponse(BaseResponse<bool>.Failure(
                validationResponse.ErrorCode, validationResponse.ErrorMessage));

        Request.Cookies.TryGetValue("authRefreshToken", out var refreshToken);

        var serviceResponse = await _authService.SignOutAsync(
            new RefreshTokenRequestDto(refreshToken, GetClaimFromAuthorization()), token);

        return BuildResponse(serviceResponse.IsFailure
            ? BaseResponse<bool>.Failure(serviceResponse.ErrorCode, serviceResponse.ErrorMessage)
            : BaseResponse<bool>.Success());
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUpAsync(
        [FromBody] SignUpRequest request, CancellationToken token = default)
    {
        var signUpResponse =
            await _authService.SignUpAsync(request.ConvertToDto(GetClaimFromAuthorization()), token);
        if (signUpResponse.IsFailure)
            return BuildResponse(BaseResponse<bool>.Failure(
                signUpResponse.ErrorCode, signUpResponse.ErrorMessage));

        var refreshTokenResponse = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto(
            null, request.Email.ToLowerInvariant(), false), token);

        AppendRefreshToken(refreshTokenResponse.Data?.RefreshToken!);

        return BuildResponse(RefreshTokenResponse.ConvertFromDto(refreshTokenResponse));
    }
}