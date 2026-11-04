using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangeEmail;
using AspireBoot.Core.Identity.Auth.ChangePassword;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Auth.ConfirmEmailChange;
using AspireBoot.Core.Identity.Auth.Register;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using AspireBoot.Core.Identity.Auth.ResendEmailConfirmation;
using AspireBoot.Core.Identity.Auth.ResetPassword;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using AspireBoot.Core.Identity.Sessions.DeleteOtherSessions;
using AspireBoot.Core.Identity.Sessions.DeleteSession;
using AspireBoot.Core.Identity.Sessions.EndSession;
using AspireBoot.Core.Identity.Sessions.GetCurrentSession;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Core.Identity.Users.GetCurrentUser;
using AspireBoot.Core.Identity.Users.GetUser;
using AspireBoot.Core.Identity.Users.ListUsers;
using AspireBoot.Core.Identity.Users.ListUserSessions;
using AspireBoot.Core.Identity.Users.UpdateUser;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Core;

public static class CoreDependencyInjection
{
    public static void AddCore(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddIdentity();
    }

    private static void AddIdentity(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddIdentityAuthChangeEmail();
        serviceCollection.AddIdentityAuthChangePassword();
        serviceCollection.AddIdentityAuthConfirmEmail();
        serviceCollection.AddIdentityAuthConfirmEmailChange();
        serviceCollection.AddIdentityAuthRegister();
        serviceCollection.AddIdentityAuthRequestRecovery();
        serviceCollection.AddIdentityAuthResendEmailConfirmation();
        serviceCollection.AddIdentityAuthResetPassword();

        serviceCollection.AddIdentitySessionsCreateSession();
        serviceCollection.AddIdentitySessionsDeleteOtherSessions();
        serviceCollection.AddIdentitySessionsDeleteSession();
        serviceCollection.AddIdentitySessionsEndSession();
        serviceCollection.AddIdentitySessionsGetCurrentSession();

        serviceCollection.AddIdentityUsersDeleteUser();
        serviceCollection.AddIdentityUsersGetCurrentUser();
        serviceCollection.AddIdentityUsersGetUser();
        serviceCollection.AddIdentityUsersListUsers();
        serviceCollection.AddIdentityUsersListUserSessions();
        serviceCollection.AddIdentityUsersUpdateUser();
    }

    private static void AddIdentityAuthChangeEmail(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<ChangeEmailCommand>, ChangeEmailCommandHandler>();

    private static void AddIdentityAuthChangePassword(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ChangePasswordCommandHandler>();
        serviceCollection.AddScoped<ICommandValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();
        serviceCollection.AddScoped<ICommandHandler<ChangePasswordCommand>>(serviceProvider =>
            new ValidatorCommandHandler<ChangePasswordCommand>(
                serviceProvider.GetRequiredService<ChangePasswordCommandHandler>(),
                serviceProvider.GetServices<ICommandValidator<ChangePasswordCommand>>()));
    }

    private static void AddIdentityAuthConfirmEmail(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<ConfirmEmailCommand>, ConfirmEmailCommandHandler>();

    private static void AddIdentityAuthConfirmEmailChange(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<ConfirmEmailChangeCommand>, ConfirmEmailChangeCommandHandler>();

    private static void AddIdentityAuthRegister(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<RegisterCommandHandler>();
        serviceCollection.AddScoped<ICommandValidator<RegisterCommand>, RegisterCommandValidator>();
        serviceCollection.AddScoped<ICommandHandler<RegisterCommand, RegisterResult>>(serviceProvider =>
            new ValidatorCommandHandler<RegisterCommand, RegisterResult>(
                serviceProvider.GetRequiredService<RegisterCommandHandler>(),
                serviceProvider.GetServices<ICommandValidator<RegisterCommand>>()));
    }

    private static void AddIdentityAuthRequestRecovery(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<RequestRecoveryCommand>, RequestRecoveryCommandHandler>();

    private static void AddIdentityAuthResendEmailConfirmation(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<ICommandHandler<ResendEmailConfirmationCommand>, ResendEmailConfirmationCommandHandler>();

    private static void AddIdentityAuthResetPassword(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<ResetPasswordCommand>, ResetPasswordCommandHandler>();

    private static void AddIdentitySessionsCreateSession(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<CreateSessionCommand>, CreateSessionCommandHandler>();

    private static void AddIdentitySessionsDeleteOtherSessions(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<DeleteOtherSessionsCommand>, DeleteOtherSessionsCommandHandler>();

    private static void AddIdentitySessionsDeleteSession(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<DeleteSessionCommand>, DeleteSessionCommandHandler>();

    private static void AddIdentitySessionsEndSession(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<EndSessionCommand>, EndSessionCommandHandler>();

    private static void AddIdentitySessionsGetCurrentSession(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<IQueryHandler<GetCurrentSessionQuery, GetCurrentSessionResult>, GetCurrentSessionQueryHandler>();

    private static void AddIdentityUsersDeleteUser(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<ICommandHandler<DeleteUserCommand>, DeleteUserCommandHandler>();

    private static void AddIdentityUsersGetCurrentUser(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult>, GetCurrentUserQueryHandler>();

    private static void AddIdentityUsersGetUser(this IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped<IQueryHandler<GetUserQuery, GetUserResult>, GetUserQueryHandler>();

    private static void AddIdentityUsersListUsers(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ListUsersQueryHandler>();
        serviceCollection.AddScoped<IQueryValidator<ListUsersQuery>, ListUsersQueryValidator>();
        serviceCollection.AddScoped<IQueryHandler<ListUsersQuery, ListUsersResult>>(serviceProvider =>
            new ValidatorQueryHandler<ListUsersQuery, ListUsersResult>(
                serviceProvider.GetRequiredService<ListUsersQueryHandler>(),
                serviceProvider.GetServices<IQueryValidator<ListUsersQuery>>()));
    }

    private static void AddIdentityUsersListUserSessions(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ListUserSessionsQueryHandler>();
        serviceCollection.AddScoped<IQueryValidator<ListUserSessionsQuery>, ListUserSessionsQueryValidator>();
        serviceCollection.AddScoped<IQueryHandler<ListUserSessionsQuery, ListUserSessionsResult>>(serviceProvider =>
            new ValidatorQueryHandler<ListUserSessionsQuery, ListUserSessionsResult>(
                serviceProvider.GetRequiredService<ListUserSessionsQueryHandler>(),
                serviceProvider.GetServices<IQueryValidator<ListUserSessionsQuery>>()));
    }

    private static void AddIdentityUsersUpdateUser(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<UpdateUserCommandHandler>();
        serviceCollection.AddScoped<ICommandValidator<UpdateUserCommand>, UpdateUserCommandValidator>();
        serviceCollection.AddScoped<ICommandHandler<UpdateUserCommand>>(serviceProvider =>
            new ValidatorCommandHandler<UpdateUserCommand>(
                serviceProvider.GetRequiredService<UpdateUserCommandHandler>(),
                serviceProvider.GetServices<ICommandValidator<UpdateUserCommand>>()));
    }
}
