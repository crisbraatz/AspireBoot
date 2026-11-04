using AspireBoot.Api.Identity;
using AspireBoot.Api.Identity.Auth.ChangeEmail;
using AspireBoot.Api.Identity.Auth.ChangePassword;
using AspireBoot.Api.Identity.Auth.ConfirmEmail;
using AspireBoot.Api.Identity.Auth.ConfirmEmailChange;
using AspireBoot.Api.Identity.Auth.GetAntiforgery;
using AspireBoot.Api.Identity.Auth.Register;
using AspireBoot.Api.Identity.Auth.RequestRecovery;
using AspireBoot.Api.Identity.Auth.ResendEmailConfirmation;
using AspireBoot.Api.Identity.Auth.ResetPassword;
using AspireBoot.Api.Identity.Sessions.CreateSession;
using AspireBoot.Api.Identity.Sessions.DeleteCurrentSession;
using AspireBoot.Api.Identity.Sessions.DeleteOtherSessions;
using AspireBoot.Api.Identity.Sessions.DeleteSession;
using AspireBoot.Api.Identity.Sessions.GetCurrentSession;
using AspireBoot.Api.Identity.Users;
using AspireBoot.Api.Identity.Users.DeleteUser;
using AspireBoot.Api.Identity.Users.GetUser;
using AspireBoot.Api.Identity.Users.ListUsers;
using AspireBoot.Api.Identity.Users.ListUserSessions;
using AspireBoot.Api.Identity.Users.UpdateUser;
using AspireBoot.Api.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace AspireBoot.Api;

public static class ApiEndpointMapper
{
    public static void MapApiEndpoints(this WebApplication webApplication)
    {
        RouteGroupBuilder authRouteGroupBuilder = webApplication
            .MapGroup("/api/auth")
            .AddEndpointFilter<IdentityEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting(ApiConstants.RateLimiterAuthPolicyName)
            .WithMetadata(new RequestSizeLimitAttribute(32 * 1024))
            .WithTags("Auth");
        authRouteGroupBuilder.MapChangeEmail();
        authRouteGroupBuilder.MapChangePassword();
        authRouteGroupBuilder.MapConfirmEmail();
        authRouteGroupBuilder.MapConfirmEmailChange();
        authRouteGroupBuilder.MapGetAntiforgery();
        authRouteGroupBuilder.MapRegister();
        authRouteGroupBuilder.MapRequestRecovery();
        authRouteGroupBuilder.MapResendEmailConfirmation();
        authRouteGroupBuilder.MapResetPassword();

        RouteGroupBuilder sessionsRouteGroupBuilder = webApplication
            .MapGroup("/api/sessions")
            .AddEndpointFilter<IdentityEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting(ApiConstants.RateLimiterReadsPolicyName)
            .WithMetadata(new RequestSizeLimitAttribute(32 * 1024))
            .WithTags("Sessions");
        sessionsRouteGroupBuilder.MapCreateSession();
        sessionsRouteGroupBuilder.MapDeleteCurrentSession();
        sessionsRouteGroupBuilder.MapDeleteOtherSessions();
        sessionsRouteGroupBuilder.MapDeleteSession();
        sessionsRouteGroupBuilder.MapGetCurrentSession();

        RouteGroupBuilder usersRouteGroupBuilder = webApplication
            .MapGroup("/api/users")
            .AddEndpointFilter<IdentityEndpointFilter>()
            .AddEndpointFilter<UsersEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting(ApiConstants.RateLimiterWritesPolicyName)
            .WithTags("Users");
        usersRouteGroupBuilder.MapDeleteUser();
        usersRouteGroupBuilder.MapGetUser();
        usersRouteGroupBuilder.MapListUsers();
        usersRouteGroupBuilder.MapListUserSessions();
        usersRouteGroupBuilder.MapUpdateUser();
    }
}
