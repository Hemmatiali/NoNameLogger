using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Abstractions;
using NoNameLogger.Application.Logging.Context;

namespace NoNameLogger.Application.Logging.Core;


/// <summary>
/// Extension methods for <see cref="ILogger"/> to integrate with the logging framework.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Starts a fluent logging entry builder.
    /// Uses the ambient context from <see cref="LoggingScope.Current"/> if available.
    /// </summary>
    public static LogEntryBuilder Log(this ILogger logger)
        => new LogEntryBuilder(logger).WithAmbientContext();

    /// <summary>
    /// Starts a fluent logging entry builder with an explicit context.
    /// The explicit context is merged with the ambient context (if any),
    /// with explicit properties overriding ambient ones.
    /// </summary>
    public static LogEntryBuilder Log(this ILogger logger, ILoggingContext context)
        => new LogEntryBuilder(logger).WithContext(LoggingScope.GetEffectiveContext(context));

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Trace"/>.
    /// </summary>
    public static void LogTrace(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Trace, eventId, messageTemplate, context, exception, properties, args);
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Debug"/>.
    /// </summary>
    public static void LogDebug(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Debug, eventId, messageTemplate, context, exception, properties, args);
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Information"/>.
    /// </summary>
    public static void LogInformation(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Information, eventId, messageTemplate, context, exception, properties, args);
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Warning"/>.
    /// </summary>
    public static void LogWarning(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Warning, eventId, messageTemplate, context, exception, properties, args);
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Error"/>.
    /// </summary>
    public static void LogError(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Error, eventId, messageTemplate, context, exception, properties, args);
    }

    /// <summary>
    /// Logs a message with <see cref="LogLevel.Critical"/>.
    /// </summary>
    public static void LogCritical(
        this ILogger logger,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context = null,
        Exception? exception = null,
        IEnumerable<KeyValuePair<string, object>>? properties = null,
        params object[] args)
    {
        LogCore(logger, LogLevel.Critical, eventId, messageTemplate, context, exception, properties, args);
    }

    private static void LogCore(
        ILogger logger,
        LogLevel level,
        EventId eventId,
        string messageTemplate,
        ILoggingContext? context,
        Exception? exception,
        IEnumerable<KeyValuePair<string, object>>? properties,
        params object[] args)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

        // Get effective context: merge ambient context with explicit context
        var effectiveContext = LoggingScope.GetEffectiveContext(context);

        var state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["{OriginalFormat}"] = messageTemplate
        };

        if (effectiveContext != null)
        {
            foreach (var pair in effectiveContext.Properties)
            {
                state[pair.Key] = pair.Value;
            }
        }

        if (properties != null)
        {
            foreach (var pair in properties)
            {
                state[pair.Key] = pair.Value;
            }
        }

        if (args is { Length: > 0 })
        {
            state["Arguments"] = args;
        }

        logger.Log(level, eventId, state, exception, static (s, ex) =>
        {
            if (s.TryGetValue("{OriginalFormat}", out var fmtObj) && fmtObj is string fmt)
            {
                return fmt;
            }

            return string.Empty;
        });
    }
}