using NoNameLogger.Application.Logging.Abstractions;

namespace NoNameLogger.Application.Logging.Context;

/// <summary>
/// Extension methods for working with <see cref="LoggingScope"/> and <see cref="ILoggingContext"/>.
/// </summary>
public static class LoggingScopeExtensions
{
    /// <summary>
    /// Creates a new context by cloning the current context and adding extra properties.
    /// Does not affect the current scope - returns a new context that can be used or scoped separately.
    /// </summary>
    /// <param name="context">The base context to clone (can be null).</param>
    /// <param name="properties">The properties to add.</param>
    /// <returns>A new context with the merged properties.</returns>
    public static ILoggingContext WithProperties(
        this ILoggingContext? context,
        params (string Key, object Value)[] properties)
    {
        var builder = context != null
            ? LoggingContextBuilder.From(context)
            : LoggingContextBuilder.Create();

        foreach (var (key, value) in properties)
        {
            builder.With(key, value);
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a new context by cloning the current context and adding extra properties.
    /// Does not affect the current scope - returns a new context that can be used or scoped separately.
    /// </summary>
    /// <param name="context">The base context to clone (can be null).</param>
    /// <param name="properties">The properties to add.</param>
    /// <returns>A new context with the merged properties.</returns>
    public static ILoggingContext WithProperties(
        this ILoggingContext? context,
        IEnumerable<KeyValuePair<string, object>> properties)
    {
        var builder = context != null
            ? LoggingContextBuilder.From(context)
            : LoggingContextBuilder.Create();

        builder.WithMany(properties);

        return builder.Build();
    }

    /// <summary>
    /// Creates a new context by merging two contexts.
    /// Properties from the overlay context override properties from the base context.
    /// </summary>
    /// <param name="baseContext">The base context.</param>
    /// <param name="overlayContext">The context whose properties should override the base.</param>
    /// <returns>A new merged context.</returns>
    public static ILoggingContext MergeWith(
        this ILoggingContext? baseContext,
        ILoggingContext? overlayContext)
    {
        if (baseContext == null && overlayContext == null)
            return LoggingContextBuilder.Create().Build();

        if (baseContext == null)
            return overlayContext!;

        if (overlayContext == null)
            return baseContext;

        return LoggingContextBuilder
            .From(baseContext)
            .WithContext(overlayContext)
            .Build();
    }

    /// <summary>
    /// Creates a builder initialized from the current ambient context (or empty if none).
    /// Use this when you need fine-grained control over building a new context.
    /// </summary>
    /// <returns>A builder pre-populated with the current ambient context properties.</returns>
    public static LoggingContextBuilder BuilderFromCurrent()
    {
        var current = LoggingScope.Current;
        return current != null
            ? LoggingContextBuilder.From(current)
            : LoggingContextBuilder.Create();
    }

    /// <summary>
    /// Creates a new context from the current ambient context with additional properties,
    /// then starts a new scope with that context.
    /// </summary>
    /// <param name="properties">The properties to add to the current context.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    public static IDisposable PushProperties(params (string Key, object Value)[] properties)
    {
        return LoggingScope.BeginWithProperties(properties);
    }

    /// <summary>
    /// Creates a new context from the current ambient context with additional properties,
    /// then starts a new scope with that context.
    /// </summary>
    /// <param name="properties">The properties to add to the current context.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    public static IDisposable PushProperties(IEnumerable<KeyValuePair<string, object>> properties)
    {
        return LoggingScope.BeginWithProperties(properties);
    }
}
