namespace NoNameLogger.Application.Logging.Abstractions;

/// <summary>
/// Represents an immutable bag of logging-related properties
/// that can be attached to log events.
/// </summary>
public interface ILoggingContext : IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>
    /// Gets all properties in this context.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }


    /// <summary>
    /// Attempts to get a property value by key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value, if found.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    bool TryGet(string key, out object value);
}