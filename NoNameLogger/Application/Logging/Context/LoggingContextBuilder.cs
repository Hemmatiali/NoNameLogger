using NoNameLogger.Application.Logging.Abstractions;

namespace NoNameLogger.Application.Logging.Context;

/// <summary>
/// Builder for <see cref="LoggingContext"/> instances.
/// </summary>
public sealed class LoggingContextBuilder
{
    private readonly Dictionary<string, object> _properties;

    private LoggingContextBuilder(Dictionary<string, object> properties)
    {
        _properties = properties;
    }

    /// <summary>
    /// Creates a new <see cref="LoggingContextBuilder"/> instance.
    /// </summary>
    public static LoggingContextBuilder Create()
    {
        return new LoggingContextBuilder(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a new builder initialized with an existing context.
    /// </summary>
    public static LoggingContextBuilder From(ILoggingContext context)
    {
        var dict = new Dictionary<string, object>(context.Properties, StringComparer.OrdinalIgnoreCase);
        return new LoggingContextBuilder(dict);
    }

    /// <summary>
    /// Adds or replaces a property in the context.
    /// </summary>
    public LoggingContextBuilder With(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or replaces multiple properties in the context.
    /// </summary>
    public LoggingContextBuilder WithMany(IEnumerable<KeyValuePair<string, object>> properties)
    {
        foreach (var pair in properties)
        {
            With(pair.Key, pair.Value);
        }

        return this;
    }

    /// <summary>
    /// Merges all properties from another context into this builder.
    /// Properties from the other context will override existing properties with the same key.
    /// </summary>
    /// <param name="context">The context whose properties should be merged in.</param>
    /// <returns>This builder for chaining.</returns>
    public LoggingContextBuilder WithContext(ILoggingContext context)
    {
        if (context == null)
            return this;

        return WithMany(context.Properties);
    }

    // ************* Helpers for common keys *************

    public LoggingContextBuilder WithEnvironment(string environment)
        => With(LoggingContextKeys.Environment, environment ?? string.Empty);

    public LoggingContextBuilder WithApplication(string application)
        => With(LoggingContextKeys.Application, application ?? string.Empty);

    public LoggingContextBuilder WithService(string service)
        => With(LoggingContextKeys.Service, service ?? string.Empty);

    public LoggingContextBuilder WithMachineName(string machineName)
        => With(LoggingContextKeys.MachineName, machineName ?? Environment.MachineName);

    public LoggingContextBuilder WithUserId(string userId)
        => With(LoggingContextKeys.UserId, userId ?? string.Empty);

    public LoggingContextBuilder WithUserName(string userName)
        => With(LoggingContextKeys.UserName, userName ?? string.Empty);

    public LoggingContextBuilder WithTenant(string tenant)
        => With(LoggingContextKeys.Tenant, tenant ?? string.Empty);

    public LoggingContextBuilder WithCorrelationId(string correlationId)
        => With(LoggingContextKeys.CorrelationId, correlationId ?? string.Empty);

    public LoggingContextBuilder WithRequestId(string requestId)
        => With(LoggingContextKeys.RequestId, requestId ?? string.Empty);

    public LoggingContextBuilder WithSessionId(string sessionId)
        => With(LoggingContextKeys.SessionId, sessionId ?? string.Empty);

    public LoggingContextBuilder WithOperation(string operation)
        => With(LoggingContextKeys.Operation, operation ?? string.Empty);

    public LoggingContextBuilder WithOperationId(string operationId)
        => With(LoggingContextKeys.OperationId, operationId ?? string.Empty);

    public LoggingContextBuilder WithHttpMethod(string httpMethod)
        => With(LoggingContextKeys.HttpMethod, httpMethod ?? string.Empty);

    public LoggingContextBuilder WithPath(string path)
        => With(LoggingContextKeys.Path, path ?? string.Empty);

    public LoggingContextBuilder WithRoute(string route)
        => With(LoggingContextKeys.Route, route ?? string.Empty);

    public LoggingContextBuilder WithStatusCode(int statusCode)
        => With(LoggingContextKeys.StatusCode, statusCode);

    public LoggingContextBuilder WithClientIp(string clientIp)
        => With(LoggingContextKeys.ClientIp, clientIp ?? string.Empty);

    /// <summary>
    /// Builds the immutable <see cref="LoggingContext"/> instance.
    /// </summary>
    public ILoggingContext Build()
    {
        return new LoggingContext(_properties);
    }
}
