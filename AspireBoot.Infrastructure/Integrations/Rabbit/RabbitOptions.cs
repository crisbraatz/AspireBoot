using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AspireBoot.Core.Integrations.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireBoot.Infrastructure.Integrations.Rabbit;

public class RabbitOptions
{
    [Required] public required bool AutoProvision { get; init; }
    [Required] public required IReadOnlyList<MessagingBindingOptions> Bindings { get; init; }
    [Required] [Range(1, 100)] public required ushort ConsumerDispatchConcurrency { get; init; }
    [Required] public required IReadOnlyDictionary<string, MessagingEventRouteOptions> EventRoutes { get; init; }
    [Required] public required IReadOnlyDictionary<string, MessagingExchangeOptions> Exchanges { get; init; }
    [Required] [Range(1, 365)] public required int InboxRetentionDays { get; init; }
    [Required] [Range(1, 1000)] public required ushort PrefetchCount { get; init; }
    [Required] public required IReadOnlyDictionary<string, MessagingQueueOptions> Queues { get; init; }

    public static RabbitOptions Get(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        RabbitOptions? rabbitOptions =
            configuration.GetSection("AspireBoot:Infrastructure:Messaging").Get<RabbitOptions>();
        ArgumentNullException.ThrowIfNull(rabbitOptions);

        Validate(rabbitOptions);

        serviceCollection.AddSingleton(rabbitOptions);

        return rabbitOptions;
    }

    private static bool MatchesTopicRoutingKey(string bindingKey, string routingKey)
    {
        string[] words = routingKey.Split('.');
        bool[] matches = new bool[words.Length + 1];
        matches[0] = true;

        foreach (string pattern in bindingKey.Split('.'))
        {
            bool[] nextMatches = new bool[words.Length + 1];

            for (int index = 0; index <= words.Length; index++)
                nextMatches[index] = pattern is "#"
                    ? matches[index] || index > 0 && nextMatches[index - 1]
                    : index > 0 && matches[index - 1] && (pattern is "*" || pattern == words[index - 1]);

            matches = nextMatches;
        }

        return matches[^1];
    }

    private static void Validate(RabbitOptions rabbitOptions)
    {
        Validator.ValidateObject(rabbitOptions, new ValidationContext(rabbitOptions), true);

        foreach ((string name, MessagingExchangeOptions exchange) in rabbitOptions.Exchanges)
        {
            Validator.ValidateObject(exchange, new ValidationContext(exchange), true);

            if (!exchange.IsDurable || exchange.AutoDelete)
                throw new InvalidOperationException($"Exchange {name} must be durable and must not auto-delete.");
        }

        foreach ((string name, MessagingQueueOptions queue) in rabbitOptions.Queues)
        {
            Validator.ValidateObject(queue, new ValidationContext(queue), true);

            if (!queue.IsDurable || queue.IsExclusive || queue.AutoDelete)
                throw new InvalidOperationException(
                    $"Queue {name} must be durable, non-exclusive and must not auto-delete.");
        }

        foreach (MessagingBindingOptions binding in rabbitOptions.Bindings)
        {
            Validator.ValidateObject(binding, new ValidationContext(binding), true);

            if (!rabbitOptions.Exchanges.ContainsKey(binding.Exchange) ||
                !rabbitOptions.Queues.ContainsKey(binding.Queue))
                throw new InvalidOperationException(
                    $"Binding {binding.Exchange} -> {binding.Queue} references an undeclared resource.");
        }

        foreach ((string name, MessagingEventRouteOptions eventRoute) in rabbitOptions.EventRoutes)
        {
            Validator.ValidateObject(eventRoute, new ValidationContext(eventRoute), true);

            if (!rabbitOptions.Exchanges.ContainsKey(eventRoute.Exchange))
                throw new InvalidOperationException(
                    $"Event route {name} references undeclared exchange {eventRoute.Exchange}.");

            if (!rabbitOptions.Bindings.Any(binding =>
                    binding.Exchange == eventRoute.Exchange &&
                    rabbitOptions.Exchanges[eventRoute.Exchange].Type switch
                    {
                        "fanout" => true,
                        "direct" => binding.RoutingKey == eventRoute.RoutingKey,
                        "topic" => MatchesTopicRoutingKey(binding.RoutingKey, eventRoute.RoutingKey),
                        _ => false
                    }))
                throw new InvalidOperationException($"Event route {name} has no matching queue binding.");
        }

        HashSet<string> eventTypes = typeof(IEvent).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IEvent).IsAssignableFrom(type))
            .Select(type =>
                type.GetCustomAttribute<EventTypeAttribute>()?.EventType ??
                throw new InvalidOperationException($"Event {type.Name} must declare {nameof(EventTypeAttribute)}"))
            .ToHashSet(StringComparer.Ordinal);

        string? undeclaredEvent =
            eventTypes.FirstOrDefault(eventType => !rabbitOptions.EventRoutes.ContainsKey(eventType));

        if (undeclaredEvent is not null)
            throw new InvalidOperationException($"Event {undeclaredEvent} has no declared event route.");

        string? undeclaredEventRoute =
            rabbitOptions.EventRoutes.Keys.FirstOrDefault(eventType => !eventTypes.Contains(eventType));

        if (undeclaredEventRoute is not null)
            throw new InvalidOperationException($"Event route {undeclaredEventRoute} has no declared event.");
    }
}

public class MessagingBindingOptions
{
    [Required] public required string Exchange { get; init; }
    [Required] public required string Queue { get; init; }
    [Required] public required string RoutingKey { get; init; }
}

public class MessagingEventRouteOptions
{
    [Required] public required string Exchange { get; init; }
    [Required] public required string RoutingKey { get; init; }
}

public class MessagingExchangeOptions
{
    [Required] public required bool AutoDelete { get; init; }
    [Required] public required bool IsDurable { get; init; }

    [Required]
    [RegularExpression("^(direct|fanout|topic)$")]
    public required string Type { get; init; }
}

public class MessagingQueueOptions
{
    [Required] public required bool AutoDelete { get; init; }
    [Required] public required bool ConsumerEnabled { get; init; }
    [Required] public required bool IsDurable { get; init; }
    [Required] public required bool IsExclusive { get; init; }

    [Required]
    [RegularExpression("^(classic|quorum)$")]
    public required string QueueType { get; init; }
}
