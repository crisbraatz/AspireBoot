using System.Security.Claims;
using AspireBoot.ApiService.Validators;
using AspireBoot.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace AspireBoot.ApiService.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class BaseController : ControllerBase
{
    protected void AppendSessionRefreshToken(string refreshToken) =>
        Response.Cookies.Append("refreshToken", refreshToken, BuildRefreshTokenCookieOptions());

    protected ObjectResult BuildProblem(int? statusCode, string? detail) =>
        Problem(
            statusCode: statusCode ?? StatusCodes.Status400BadRequest,
            title: "Request failed",
            detail: detail);

    protected void DeleteSessionRefreshToken() =>
        Response.Cookies.Delete("refreshToken", BuildRefreshTokenCookieOptions());

    protected ValidatorResponse EnsureRequestedBy()
        => string.IsNullOrWhiteSpace(GetRequestedBy())
            ? ValidatorResponse.Failure(StatusCodes.Status401Unauthorized, "Unauthorized access.")
            : ValidatorResponse.Success();

    protected string? GetRequestedBy() =>
        User.FindFirstValue("email") ??
        User.FindFirstValue(ClaimTypes.Email);

    private CookieOptions BuildRefreshTokenCookieOptions()
    {
        CookieOptions cookieOptions = new()
        {
            Expires = DateTime.UtcNow.AddDays(AppSettings.CookieExpiresAfter),
            HttpOnly = true,
            Path = "/",
            SameSite = SameSiteMode.None,
            Secure = true
        };

        string? cookieDomain = AppSettings.GetCookieDomainFor(Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(cookieDomain))
            cookieOptions.Domain = cookieDomain;

        return cookieOptions;
    }
}
