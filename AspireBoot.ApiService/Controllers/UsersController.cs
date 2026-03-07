using AspireBoot.ApiService.Contracts;
using AspireBoot.ApiService.Contracts.Users;
using AspireBoot.ApiService.Services.Auth;
using AspireBoot.ApiService.Services.Users;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.DTOs.Users;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.ApiService.Controllers;

public sealed class UsersController(IAuthService authService, IUsersService usersService) : BaseController(authService)
{
    [HttpGet("list-by")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByAsync(
        [FromQuery] ListUsersRequest request, CancellationToken cancellationToken = default)
    {
        ValidatorResponse validatorResponse = await ValidateJwtAsync(cancellationToken).ConfigureAwait(false);
        if (validatorResponse.IsFailure)
            return BuildResponse(BaseResponse.Failure(validatorResponse.ErrorCode, validatorResponse.ErrorMessage));

        BaseListResponseDto<ListUserResponseDto> serviceResponse =
            await usersService.ListByAsync(request.ConvertToDto(), cancellationToken).ConfigureAwait(false);

        return BuildResponse(ListUserResponse.ConvertFromDto(serviceResponse));
    }
}
