using Microsoft.Extensions.Logging;

namespace AspireBoot.Infrastructure.Extensions;

public static class LoggerMessageExtension
{
    public static void LogExceptionMiddlewareError(ILogger logger, Exception exception) =>
        LoggerMessage.Define(
                LogLevel.Error, new EventId(1000, "ExceptionMiddlewareError"), "Unexpected exception occurred")
            (logger, exception);

    public static void LogBasePublisherError(ILogger logger, string exchange, Exception exception) =>
        LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(1001, "BasePublisherError"),
                "Error publishing message to exchange {Exchange}")
            (logger, exchange, exception);

    public static void LogBaseConsumerError(ILogger logger, string queue, string exchange, Exception exception) =>
        LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(1002, "BaseConsumerError"),
                "Error consuming message from queue {Queue} and exchange {Exchange}")
            (logger, queue, exchange, exception);

    public static void LogConsumerInformation(ILogger logger, string message) =>
        LoggerMessage.Define<string>(
                LogLevel.Information, new EventId(1003, "ConsumerInformation"), "Message {Message} consumed.")
            (logger, message, null);

    public static void LogServiceError(ILogger logger, string errorMessage) =>
#pragma warning disable CA2254
        LoggerMessage.Define(LogLevel.Information, new EventId(1004, "ServiceError"), errorMessage)(logger, null);
#pragma warning restore CA2254
}
