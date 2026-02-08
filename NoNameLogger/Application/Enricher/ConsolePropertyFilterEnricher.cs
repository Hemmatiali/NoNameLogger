using Serilog.Core;
using Serilog.Events;

namespace NoNameLogger.Application.Enricher;

public sealed class ConsolePropertyFilterEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> ExcludedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application",
        "Environment",
        "SourceContext"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        foreach (var key in ExcludedKeys)
        {
            logEvent.RemovePropertyIfPresent(key);
        }
    }
}