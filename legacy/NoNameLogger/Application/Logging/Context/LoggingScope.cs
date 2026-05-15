using NoNameLogger.Application.Logging.Abstractions;

namespace NoNameLogger.Application.Logging.Context;

/// <summary>
/// Provides ambient (async-local) storage for <see cref="ILoggingContext"/>.
/// Use this to establish a logging context at the entry point of a request or job,
/// so that all logging calls within that async flow automatically use the context.
/// </summary>
public static class LoggingScope
{
    private static readonly AsyncLocal<ILoggingContext?> _current = new();

    /// <summary>
    /// Gets the current ambient logging context, or <c>null</c> if no scope is active.
    /// </summary>
    public static ILoggingContext? Current => _current.Value;

    /// <summary>
    /// Gets whether a logging scope is currently active.
    /// </summary>
    public static bool HasCurrent => _current.Value != null;

    /// <summary>
    /// Begins a new logging scope with the specified context.
    /// When disposed, the previous context (if any) is restored.
    /// </summary>
    /// <param name="context">The logging context for this scope.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    public static IDisposable Begin(ILoggingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return new LoggingScopeHandle(context);
    }

    /// <summary>
    /// Begins a new logging scope by adding properties to the current context.
    /// If no current context exists, creates a new context with only the specified properties.
    /// When disposed, the previous context is restored.
    /// </summary>
    /// <param name="properties">The properties to add to the current context.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    public static IDisposable BeginWithProperties(params (string Key, object Value)[] properties)
    {
        var builder = Current != null
            ? LoggingContextBuilder.From(Current)
            : LoggingContextBuilder.Create();

        foreach (var (key, value) in properties)
        {
            builder.With(key, value);
        }

        return Begin(builder.Build());
    }

    /// <summary>
    /// Begins a new logging scope by adding properties to the current context.
    /// If no current context exists, creates a new context with only the specified properties.
    /// When disposed, the previous context is restored.
    /// </summary>
    /// <param name="properties">The properties to add to the current context.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    public static IDisposable BeginWithProperties(IEnumerable<KeyValuePair<string, object>> properties)
    {
        var builder = Current != null
            ? LoggingContextBuilder.From(Current)
            : LoggingContextBuilder.Create();

        builder.WithMany(properties);

        return Begin(builder.Build());
    }

    /// <summary>
    /// Gets the effective context by merging the ambient context with an explicitly provided context.
    /// The explicit context properties override the ambient context properties.
    /// </summary>
    /// <param name="explicitContext">The explicitly provided context, or null to use only ambient context.</param>
    /// <returns>The merged context, or null if both ambient and explicit contexts are null.</returns>
    public static ILoggingContext? GetEffectiveContext(ILoggingContext? explicitContext)
    {
        var ambient = Current;

        // Both null -> null
        if (ambient == null && explicitContext == null)
            return null;

        // Only ambient -> ambient
        if (explicitContext == null)
            return ambient;

        // Only explicit -> explicit
        if (ambient == null)
            return explicitContext;

        // Both present -> merge (explicit overrides ambient)
        return LoggingContextBuilder
            .From(ambient)
            .WithContext(explicitContext)
            .Build();
    }

    /// <summary>
    /// Internal handle that manages the scope lifetime.
    /// </summary>
    private sealed class LoggingScopeHandle : IDisposable
    {
        private readonly ILoggingContext? _previousContext;
        private int _disposed; // Changed from bool

        public LoggingScopeHandle(ILoggingContext newContext)
        {
            _previousContext = _current.Value;
            _current.Value = newContext;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            _current.Value = _previousContext;
        }
    }
}
