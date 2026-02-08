using System.Collections;
using System.Collections.Frozen;
using NoNameLogger.Application.Logging.Abstractions;

namespace NoNameLogger.Application.Logging.Context;

/// <summary>
/// Immutable implementation of <see cref="ILoggingContext"/>.
/// </summary>
public sealed class LoggingContext : ILoggingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingContext"/> class.
    /// </summary>
    /// <param name="properties">The properties to include in the context.</param>
    public LoggingContext(IEnumerable<KeyValuePair<string, object>> properties)
    {
        // Use FrozenDictionary for performance and thread-safety.
        Properties = properties.ToFrozenDictionary(
            p => p.Key,
            p => p.Value,
            StringComparer.OrdinalIgnoreCase);
    }


    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties { get; }


    /// <inheritdoc />
    public bool TryGet(string key, out object value)
    {
        return Properties.TryGetValue(key, out value);
    }


    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return Properties.GetEnumerator();
    }


    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}