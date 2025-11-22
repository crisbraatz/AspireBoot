using System.Net;
using AspireBoot.ApiService.Contracts;
using AspireBoot.ApiService.Helpers;
using AspireBoot.ApiService.Services.Auth;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace AspireBoot.ApiService.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[Route("api/[controller]")]
public abstract class BaseController(IAuthService authService) : ControllerBase
{
    protected void AppendAuthRefreshToken(string refreshToken) =>
        Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                Domain = AppSettings.CookieDomain,
                Expires = DateTime.UtcNow.AddDays(AppSettings.CookieExpiresAfter),
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true
            });

    protected static IActionResult BuildResponse(BaseResponse baseResponse) =>
        baseResponse.IsFailure
            ? new ObjectResult(baseResponse) { StatusCode = baseResponse.ErrorCode }
            : new ObjectResult(baseResponse) { StatusCode = (int)HttpStatusCode.OK };

    protected static IActionResult BuildResponse<T>(BaseResponse<T> baseResponse) =>
        baseResponse.IsFailure
            ? new ObjectResult(baseResponse) { StatusCode = baseResponse.ErrorCode }
            : new ObjectResult(baseResponse) { StatusCode = (int)HttpStatusCode.OK };

    protected static IActionResult BuildResponse<T>(BaseListResponse<T> baseListResponse) =>
        new ObjectResult(baseListResponse) { StatusCode = (int)HttpStatusCode.OK };

    protected string? GetClaimFromAuthorization() =>
        string.IsNullOrWhiteSpace(Request.Headers[HeaderNames.Authorization])
            ? null
            : TokenHelper.GetClaimFrom(Request.Headers[HeaderNames.Authorization]);

    protected async Task<ValidatorResponse> ValidateJwtAsync(CancellationToken cancellationToken = default) =>
        await authService.ValidateJwtAsync(GetClaimFromAuthorization(), cancellationToken).ConfigureAwait(false);
}
