using System.Reflection;
using AspireBoot.Core.Integrations.Messaging;
using AspireBoot.Infrastructure.Integrations.Postgres;
using JasperFx;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.HealthChecks;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;
using Wolverine.Util;

namespace AspireBoot.Infrastructure.Integrations.Rabbit;

public static class RabbitDependencyInjection
{
    public const string WolverineDatabaseSchema = "messaging";

    public static void AddRabbit(
        this IHostApplicationBuilder hostApplicationBuilder,
        Assembly assembly,
        bool consumeEvents = false,
        Action<WolverineOptions>? action = null)
    {
        string? postgresConnectionString = hostApplicationBuilder.Configuration.GetConnectionString("Postgres");
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        string? rabbitConnectionString = hostApplicationBuilder.Configuration.GetConnectionString("Rabbit");
        ArgumentException.ThrowIfNullOrWhiteSpace(rabbitConnectionString);

        RabbitOptions rabbitOptions =
            RabbitOptions.Get(hostApplicationBuilder.Services, hostApplicationBuilder.Configuration);

        if (consumeEvents && !rabbitOptions.Queues.Values.Any(queue => queue.ConsumerEnabled))
            throw new InvalidOperationException("The Worker must have at least one enabled messaging queue.");

        PostgresOptions? postgresOptions =
            hostApplicationBuilder.Configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>();
        ArgumentNullException.ThrowIfNull(postgresOptions);

        hostApplicationBuilder.Logging.AddFilter("JasperFx", LogLevel.Warning);
        hostApplicationBuilder.Logging.AddFilter("Wolverine", LogLevel.Warning);
        WolverineMessageNaming.InsertFirst<RabbitEventMessageTypeNaming>();
        hostApplicationBuilder.UseWolverine(wolverineOptions =>
        {
            wolverineOptions.ApplicationAssembly = assembly;
            wolverineOptions.ServiceName = "AspireBoot";
            wolverineOptions.CodeGeneration.GeneratedCodeOutputPath =
                Path.Combine(AppContext.BaseDirectory, "Generated");
            wolverineOptions.MetadataRules.Add(new RabbitEventEnvelopeRule());
            wolverineOptions.Discovery.DisableConventionalDiscovery();
            wolverineOptions.Policies.DisableConventionalLocalRouting();
            wolverineOptions.PersistMessagesWithPostgresql(postgresConnectionString, WolverineDatabaseSchema)
                .OverrideAutoCreateResources(postgresOptions.ApplyMigrationsOnStartup is true
                    ? AutoCreate.CreateOrUpdate
                    : AutoCreate.None);
            wolverineOptions.UseEntityFrameworkCoreTransactions();
            wolverineOptions.Durability.KeepAfterMessageHandling =
                TimeSpan.FromDays(rabbitOptions.InboxRetentionDays);
            wolverineOptions.Durability.MessageIdentity = MessageIdentity.IdAndDestination;
            RabbitMqTransportExpression rabbitMqTransportExpression =
                wolverineOptions.UseRabbitMq(new Uri(rabbitConnectionString, UriKind.Absolute))
                    .DisableSystemRequestReplyQueueDeclaration()
                    .DisableDeadLetterQueueing()
                    .ConfigureChannelCreation(wolverineRabbitMqChannelOptions =>
                    {
                        wolverineRabbitMqChannelOptions.PublisherConfirmationsEnabled = true;
                        wolverineRabbitMqChannelOptions.PublisherConfirmationTrackingEnabled = true;
                    });

            if (rabbitOptions.AutoProvision)
                rabbitMqTransportExpression.AutoProvision();

            if (!consumeEvents)
                rabbitMqTransportExpression.UseSenderConnectionOnly();

            foreach ((string name, MessagingExchangeOptions exchange) in rabbitOptions.Exchanges)
                rabbitMqTransportExpression.DeclareExchange(name, rabbitMqBindableExchange =>
                {
                    rabbitMqBindableExchange.AutoDelete = exchange.AutoDelete;
                    rabbitMqBindableExchange.ExchangeType = Enum.Parse<ExchangeType>(exchange.Type, true);
                    rabbitMqBindableExchange.IsDurable = exchange.IsDurable;
                });

            foreach ((string name, MessagingQueueOptions queue) in rabbitOptions.Queues)
            {
                rabbitMqTransportExpression.DeclareQueue(name, rabbitMqQueue =>
                {
                    rabbitMqQueue.AutoDelete = queue.AutoDelete;
                    rabbitMqQueue.IsDurable = queue.IsDurable;
                    rabbitMqQueue.IsExclusive = queue.IsExclusive;
                    rabbitMqQueue.QueueType = Enum.Parse<QueueType>(queue.QueueType, true);
                });

                if (consumeEvents && queue.ConsumerEnabled)
                    wolverineOptions
                        .ListenToRabbitQueue(name)
                        .PreFetchCount(rabbitOptions.PrefetchCount)
                        .MaximumParallelMessages(rabbitOptions.ConsumerDispatchConcurrency)
                        .UseDurableInbox();
            }

            foreach (MessagingBindingOptions binding in rabbitOptions.Bindings)
                rabbitMqTransportExpression.BindExchange(binding.Exchange).ToQueue(binding.Queue, binding.RoutingKey);

            foreach (Type eventType in typeof(IEvent).Assembly.GetTypes().Where(type =>
                         type is { IsAbstract: false, IsInterface: false } && typeof(IEvent).IsAssignableFrom(type)))
            {
                wolverineOptions.RegisterMessageType(eventType);
                MessagingEventRouteOptions eventRoute =
                    rabbitOptions.EventRoutes[eventType.GetCustomAttribute<EventTypeAttribute>()!.EventType];
                wolverineOptions
                    .PublishMessage(eventType)
                    .ToRabbitRoutingKey(eventRoute.Exchange, eventRoute.RoutingKey)
                    .UseDurableOutbox();
            }

            action?.Invoke(wolverineOptions);

            wolverineOptions.Policies.OnException<Exception>()
                .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30))
                .Then.MoveToErrorQueue();
        });
        hostApplicationBuilder.Services.AddHealthChecks().AddWolverine(tags: ["ready", "dependency", "messaging"]);

        if (consumeEvents)
            hostApplicationBuilder.Services.AddHealthChecks().AddWolverineListeners(
                filter: listeningAgent => listeningAgent.Uri.Scheme is "rabbitmq",
                tags: ["ready", "dependency", "rabbit"]);
    }
}
