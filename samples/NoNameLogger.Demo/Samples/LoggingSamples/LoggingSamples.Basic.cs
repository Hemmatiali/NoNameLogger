using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples;

/// <summary>
/// Basic (explicit) context usage demos.
/// </summary>
public static partial class LoggingSamples
{
    /// <summary>
    /// Demonstrates explicit context creation + structured logging.
    /// </summary>
    public static void BasicUsage(ILogger logger)
    {
        var context = AppLoggingContext.For<LoggingSamplesMarker>(
            nameof(BasicUsage),
            ("Environment", "Development"),
            ("UserId", "user-123"),
            ("Path", "/api/values"),
            ("Method", "GET"));

        using (LoggingScope.Begin(context))
        {
            // Fluent API example (explicit context)
            logger.Log(context)
                .Information()
                .Event(CommonLogEvents.Http.RequestReceived)
                .Message("HTTP {Method} {Path} started", "GET", "/api/values")
                .Write();

            // Extension method example (explicit context)
            logger.LogInformation(
                CommonLogEvents.System.StartupCompleted,
                "Application started",
                context: context);
        }
    }
}