using NoNameLogger.Application.Logging.Abstractions;
using NoNameLogger.Application.Logging.Context;

namespace NoNameLogger.Application.Samples;

public static class AppLoggingContext
{
    private const string ApplicationName = "NoNameLogger.Samples"; // Name of the application

    /// <summary>
    /// Creates an ILoggingContext with application, service and operation.
    /// Optional extra properties can be passed as key/value tuples.
    /// </summary>
    public static ILoggingContext For<TService>(
        string operation,
        params (string Key, object? Value)[] properties)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation name cannot be null or empty.", nameof(operation));
        }

        var builder = LoggingContextBuilder
            .Create()
            .WithApplication(ApplicationName)
            .WithService(typeof(TService).Name)
            .WithOperation(operation);

        if (properties is { Length: > 0 })
        {
            foreach (var (key, value) in properties)
            {
                builder.With(key, value);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a LoggingContextBuilder pre-populated with application, service and operation.
    /// Use this when you want to keep a builder and add more properties fluently before building.
    /// </summary>
    public static LoggingContextBuilder ForBuilder<TService>(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation name cannot be null or empty.", nameof(operation));
        }

        return LoggingContextBuilder
            .Create()
            .WithApplication(ApplicationName)
            .WithService(typeof(TService).Name)
            .WithOperation(operation);
    }
}