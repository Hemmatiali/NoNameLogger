using NoNameLogger.Application.Logging.Context;
using Serilog.Core;
using Serilog.Events;

namespace NoNameLogger.Application.Enricher;

/// <summary>
/// A Serilog enricher that adds properties from <see cref="LoggingScope.Current"/> to each log event.
/// This bridges the gap between the custom LoggingScope ambient context and Serilog's log events.
/// </summary>
public sealed class LoggingScopeEnricher : ILogEventEnricher
{
    /// <summary>
    /// Keys to exclude from enrichment (they may already be added via other means or are internal).
    /// </summary>
    private static readonly HashSet<string> ExcludedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "{OriginalFormat}",
        "EventId",
        "EventName"
    };

    /// <summary>
    /// Enriches the log event with properties from the current <see cref="LoggingScope"/>.
    /// </summary>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        var context = LoggingScope.Current;
        if (context == null) return;

        foreach (var pair in context.Properties)
        {
            // Skip excluded keys and keys that already exist in the log event
            if (ExcludedKeys.Contains(pair.Key))
                continue;

            if (logEvent.Properties.ContainsKey(pair.Key))
                continue;

            var value = ConvertToLogEventPropertyValue(pair.Value, propertyFactory);
            logEvent.AddPropertyIfAbsent(new LogEventProperty(pair.Key, value));
        }
    }

    /// <summary>
    /// Converts an object value to a Serilog <see cref="LogEventPropertyValue"/>.
    /// </summary>
    private static LogEventPropertyValue ConvertToLogEventPropertyValue(object value, ILogEventPropertyFactory propertyFactory)
    {
        if (value == null)
            return new ScalarValue(null);

        // Use the property factory which handles destructuring properly
        var property = propertyFactory.CreateProperty("_temp_", value, destructureObjects: false);
        return property.Value;
    }
}

