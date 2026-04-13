using AspireBoot.ApiService.Contracts.Sessions;
using AspireBoot.ApiService.Services.Sessions;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.ApiService.Controllers;

[Route("api/sessions")]
public sealed class SessionsController(ISessionsService sessionsService) : BaseController
{
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        BaseResponseDto<RefreshSessionResponseDto> response = await sessionsService
            .CreateAsync(request.ConvertToDto(GetRequestedBy()), cancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
            return BuildProblem(response.ErrorCode, response.ErrorMessage);

        string? refreshToken = response.Data?.RefreshToken;
        string? token = response.Data?.Token;
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AppendSessionRefreshToken(refreshToken);

        return Ok(new RefreshSessionResponse(token));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = EnsureRequestedBy();
        if (validatorResponse.IsFailure)
            return BuildProblem(validatorResponse.ErrorCode, validatorResponse.ErrorMessage);

        Request.Cookies.TryGetValue("refreshToken", out string? refreshToken);

        BaseResponseDto response = await sessionsService
            .DeleteAsync(new DeleteSessionRequestDto(refreshToken, GetRequestedBy()), cancellationToken)
            .ConfigureAwait(false);
        if (response.IsFailure)
            return BuildProblem(response.ErrorCode, response.ErrorMessage);

        DeleteSessionRefreshToken();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        Request.Cookies.TryGetValue("refreshToken", out string? refreshToken);

        BaseResponseDto<RefreshSessionResponseDto> response = await sessionsService
            .RefreshAsync(new RefreshSessionRequestDto(refreshToken, GetRequestedBy()), cancellationToken)
            .ConfigureAwait(false);
        if (response.IsFailure)
            return BuildProblem(response.ErrorCode, response.ErrorMessage);

        string? newRefreshToken = response.Data?.RefreshToken;
        string? token = response.Data?.Token;
        ArgumentException.ThrowIfNullOrWhiteSpace(newRefreshToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AppendSessionRefreshToken(newRefreshToken);

        return Ok(new RefreshSessionResponse(token));
    }
}
