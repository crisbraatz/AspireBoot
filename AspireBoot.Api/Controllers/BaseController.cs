using AspireBoot.Api.Contracts;
using AspireBoot.Api.Helpers;
using AspireBoot.Api.Services.Auth;
using AspireBoot.Api.Validators;
using AspireBoot.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace AspireBoot.Api.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[Route("api/[controller]")]
public abstract class BaseController(IAuthService authService) : ControllerBase
{
    protected void AppendRefreshToken(string refreshToken) =>
        Response.Cookies.Append("authRefreshToken", refreshToken, new CookieOptions
        {
            Domain = AppSettings.CookieDomain,
            Expires = DateTime.UtcNow.AddDays(7),
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
        });

    protected static IActionResult BuildResponse(BaseResponse response) =>
        new ObjectResult(response) { StatusCode = response.ErrorCode };

    protected static IActionResult BuildResponse<T>(BaseResponse<T> response) =>
        new ObjectResult(response) { StatusCode = response.ErrorCode };

    protected string? GetClaimFromAuthorization() =>
        string.IsNullOrWhiteSpace(Request.Headers[HeaderNames.Authorization])
            ? null
            : TokenHelper.GetClaimFrom(Request.Headers[HeaderNames.Authorization]);

    protected async Task<ValidatorResponse> ValidateJwtAsync(CancellationToken token = default) =>
        await authService.ValidateJwtAsync(GetClaimFromAuthorization(), token);
}