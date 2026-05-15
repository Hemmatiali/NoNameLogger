using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Abstractions;
using NoNameLogger.Application.Logging.Context;

namespace NoNameLogger.Application.Logging.Core;

/// <summary>
/// Fluent builder for constructing structured log entries.
/// </summary>
public sealed class LogEntryBuilder
{
    private readonly ILogger _logger;

    private LogLevel _level;
    private EventId _eventId;
    private string _messageTemplate;
    private object[] _messageArgs;
    private Exception _exception;
    private ILoggingContext _context;
    private readonly Dictionary<string, object> _properties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntryBuilder"/> class.
    /// </summary>
    /// <param name="logger">The underlying logger.</param>
    public LogEntryBuilder(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _level = LogLevel.Information;
    }

    /// <summary>
    /// Sets the log level.
    /// </summary>
    public LogEntryBuilder Level(LogLevel level)
    {
        _level = level;
        return this;
    }

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Trace"/>.
    /// </summary>
    public LogEntryBuilder Trace() => Level(LogLevel.Trace);

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Debug"/>.
    /// </summary>
    public LogEntryBuilder Debug() => Level(LogLevel.Debug);

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Information"/>.
    /// </summary>
    public LogEntryBuilder Information() => Level(LogLevel.Information);

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Warning"/>.
    /// </summary>
    public LogEntryBuilder Warning() => Level(LogLevel.Warning);

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Error"/>.
    /// </summary>
    public LogEntryBuilder Error() => Level(LogLevel.Error);

    /// <summary>
    /// Sets the log level to <see cref="LogLevel.Critical"/>.
    /// </summary>
    public LogEntryBuilder Critical() => Level(LogLevel.Critical);

    /// <summary>
    /// Sets the event id for the log entry.
    /// </summary>
    public LogEntryBuilder Event(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    /// <summary>
    /// Sets the event id for the log entry.
    /// </summary>
    public LogEntryBuilder Event(int id, string name = null)
    {
        _eventId = new EventId(id, name ?? string.Empty);
        return this;
    }

    /// <summary>
    /// Sets the log message template and arguments.
    /// </summary>
    public LogEntryBuilder Message(string template, params object[] args)
    {
        _messageTemplate = template ?? throw new ArgumentNullException(nameof(template));
        _messageArgs = args;
        return this;
    }

    /// <summary>
    /// Sets the log exception.
    /// </summary>
    public LogEntryBuilder Exception(Exception exception)
    {
        _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        return this;
    }

    /// <summary>
    /// Sets or merges the logging context.
    /// If a context was already set (e.g., via WithAmbientContext), the new context is merged with it
    /// (new properties override existing ones). If context is null, clears the stored context
    /// (BuildState will then fall back to ambient context).
    /// </summary>
    public LogEntryBuilder WithContext(ILoggingContext? context)
    {
        // If null is passed, clear the stored context (allows fallback to ambient in BuildState)
        if (context == null)
        {
            _context = null;
            return this;
        }

        // If we already have a context (e.g., from WithAmbientContext), merge with it
        // New context properties override existing ones
        if (_context != null)
        {
            _context = LoggingContextBuilder
                .From(_context)
                .WithContext(context)
                .Build();
        }
        else
        {
            _context = context;
        }

        return this;
    }

    /// <summary>
    /// Uses the current ambient logging context from <see cref="LoggingScope.Current"/>.
    /// </summary>
    public LogEntryBuilder WithAmbientContext()
    {
        _context = LoggingScope.Current;
        return this;
    }

    /// <summary>
    /// Adds properties to the current context without replacing it.
    /// If an explicit context was set, merges the new properties with it.
    /// If only ambient context exists, merges with that.
    /// </summary>
    public LogEntryBuilder WithExtraContext(params (string Key, object Value)[] properties)
    {
        foreach (var (key, value) in properties)
        {
            _properties[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Adds or replaces a property.
    /// </summary>
    public LogEntryBuilder WithProperty(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or replaces multiple properties.
    /// </summary>
    public LogEntryBuilder WithProperties(IEnumerable<KeyValuePair<string, object>> properties)
    {
        foreach (var pair in properties)
        {
            WithProperty(pair.Key, pair.Value);
        }

        return this;
    }

    /// <summary>
    /// Writes the log entry to the underlying logger.
    /// </summary>
    public void Write()
    {
        var isDefaultEventId = _eventId.Equals(default);
        var hasCustomMessage = !string.IsNullOrEmpty(_messageTemplate);

        // Only use default template if no custom message was provided
        if (!hasCustomMessage)
        {
            _messageTemplate = isDefaultEventId
                ? "Log entry with no message."
                : "Log entry for event '{EventId}' ({EventName}).";
        }

        _properties.TryAdd("EventId", _eventId.Id);
        _properties.TryAdd("EventName", _eventId.Name ?? string.Empty);

        var state = BuildState();

        _logger.Log(_level, _eventId, state, _exception, static (s, ex) =>
        {
            // The provider will use {OriginalFormat} from the state.
            if (s.TryGetValue("{OriginalFormat}", out var formatObj) && formatObj is string fmt)
            {
                return fmt;
            }

            return string.Empty;
        });
    }

    private IDictionary<string, object> BuildState()
    {
        var state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Get effective context: the explicitly set context (which may already include ambient)
        // or fall back to ambient context if none was set
        var effectiveContext = _context ?? LoggingScope.Current;

        if (effectiveContext != null)
        {
            foreach (var pair in effectiveContext.Properties)
            {
                state[pair.Key] = pair.Value;
            }
        }

        // Properties added via WithProperty/WithExtraContext override context properties
        foreach (var pair in _properties)
        {
            state[pair.Key] = pair.Value;
        }

        if (!string.IsNullOrEmpty(_messageTemplate))
        {
            state["{OriginalFormat}"] = _messageTemplate!;
        }

        // Optionally, attach the message arguments array for providers that care.
        if (_messageArgs is { Length: > 0 })
        {
            state["Arguments"] = _messageArgs;
        }

        return state;
    }
}
