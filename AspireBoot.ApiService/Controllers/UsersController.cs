using AspireBoot.ApiService.Contracts.Sessions;
using AspireBoot.ApiService.Contracts.Users;
using AspireBoot.ApiService.Services.Users;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Sessions;
using AspireBoot.Domain.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.ApiService.Controllers;

[Route("api/users")]
public sealed class UsersController(IUsersService usersService) : BaseController
{
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        BaseResponseDto<RefreshSessionResponseDto> response = await usersService
            .CreateAsync(request.ConvertToDto(GetRequestedBy()), cancellationToken).ConfigureAwait(false);
        if (response.IsFailure)
            return BuildProblem(response.ErrorCode, response.ErrorMessage);

        string? refreshToken = response.Data?.RefreshToken;
        string? token = response.Data?.Token;
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AppendSessionRefreshToken(refreshToken);

        return Created(new Uri("/api/sessions", UriKind.Relative), new RefreshSessionResponse(token));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] ListUsersRequest request, CancellationToken cancellationToken = default)
    {
        BaseListResponseDto<ListUserResponseDto> response =
            await usersService.ListAsync(request.ConvertToDto(), cancellationToken).ConfigureAwait(false);

        return Ok(ListUserResponse.ConvertFromDto(response));
    }
}
