using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Identity.Auth.ChangeEmail;
using AspireBoot.Core.Identity.Auth.ConfirmEmail;
using AspireBoot.Core.Identity.Auth.PasswordChanged;
using AspireBoot.Core.Identity.Auth.RequestRecovery;
using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Core.Identity.Sessions.CreateSession;
using AspireBoot.Core.Identity.Users.CleanupAvatars;
using AspireBoot.Core.Identity.Users.DeleteUser;
using AspireBoot.Core.Integrations.Smtp;
using AspireBoot.Infrastructure.Integrations.Rabbit;
using AspireBoot.Worker.Identity.Auth.ChangeEmail;
using AspireBoot.Worker.Identity.Auth.ConfirmEmail;
using AspireBoot.Worker.Identity.Auth.PasswordChanged;
using AspireBoot.Worker.Identity.Auth.RequestRecovery;
using AspireBoot.Worker.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Worker.Identity.Sessions.CreateSession;
using AspireBoot.Worker.Identity.Users.Audit;
using AspireBoot.Worker.Identity.Users.CleanupAvatars;
using AspireBoot.Worker.Identity.Users.DeleteUser;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolverine.ErrorHandling;

namespace AspireBoot.Worker;

public static class WorkerDependencyInjection
{
    public static void AddWorker(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.Services.TryAddSingleton(TimeProvider.System);
        hostApplicationBuilder.Services.AddEmailPreparation();
        hostApplicationBuilder.Services.AddCleanupAvatars(hostApplicationBuilder.Configuration);
        hostApplicationBuilder.AddRabbit(typeof(UserAuditEventConsumer).Assembly, true, wolverineOptions =>
        {
            wolverineOptions.CodeGeneration.AlwaysUseServiceLocationFor<ICommandHandler<SendRequestedEmailCommand>>();
            wolverineOptions.Policies.OnException<InvalidEmailRequestException>().MoveToErrorQueue();
            wolverineOptions.Policies.OnException<EmailRequestRateLimitedException>()
                .RetryWithCooldown(TimeSpan.FromMinutes(hostApplicationBuilder.Configuration.GetValue<int>(
                    "AspireBoot:Core:EmailRequests:WindowInMinutes")))
                .Then.MoveToErrorQueue();
            wolverineOptions.Discovery.IncludeType<ChangeEmailEventConsumer>();
            wolverineOptions.Discovery.IncludeType<ConfirmEmailEventConsumer>();
            wolverineOptions.Discovery.IncludeType<CreateSessionEventConsumer>();
            wolverineOptions.Discovery.IncludeType<DeleteUserEventConsumer>();
            wolverineOptions.Discovery.IncludeType<PasswordChangedEventConsumer>();
            wolverineOptions.Discovery.IncludeType<RequestRecoveryEventConsumer>();
            wolverineOptions.Discovery.IncludeType<SendEmailConfirmationEventConsumer>();
            wolverineOptions.Discovery.IncludeType<UserAuditEventConsumer>();
        });
    }

    private static void AddCleanupAvatars(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        CleanupAvatarsOptions.Add(serviceCollection, configuration);

        serviceCollection.AddScoped<CleanupAvatarsCommandHandler>();
        serviceCollection.AddScoped<ICommandValidator<CleanupAvatarsCommand>, CleanupAvatarsCommandValidator>();
        serviceCollection.AddScoped<ICommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>>(serviceProvider =>
            new ValidatorCommandHandler<CleanupAvatarsCommand, CleanupAvatarsResult>(
                serviceProvider.GetRequiredService<CleanupAvatarsCommandHandler>(),
                serviceProvider.GetServices<ICommandValidator<CleanupAvatarsCommand>>()));
        serviceCollection.AddHostedService<CleanupAvatarsService>();
    }

    private static void AddEmailPreparation(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICommandHandler<SendRequestedEmailCommand>, SendRequestedEmailCommandHandler>();
        serviceCollection.AddScoped<IEmailPreparation, ChangeEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, RequestRecoveryEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, SendEmailConfirmationEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, PasswordChangedEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, ConfirmEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, CreateSessionEmailPreparation>();
        serviceCollection.AddScoped<IEmailPreparation, DeleteUserEmailPreparation>();
    }
}
