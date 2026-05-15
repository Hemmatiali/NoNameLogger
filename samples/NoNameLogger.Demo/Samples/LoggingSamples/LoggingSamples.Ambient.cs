using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples;

/// <summary>
/// Ambient context and nested scope demos.
/// </summary>
public static partial class LoggingSamples
{
    /// <summary>
    /// Example showing ambient context with nested scopes.
    /// </summary>
    public static void AmbientContextExample(ILogger logger)
    {
        var correlationId = Guid.NewGuid().ToString();

        var rootContext = AppLoggingContext.For<LoggingSamplesMarker>(
            nameof(AmbientContextExample),
            ("CorrelationId", correlationId),
            ("UserId", "user-123"));

        using (LoggingScope.Begin(rootContext))
        {
            // Ambient context used (context: null)
            logger.LogInformation(
                CommonLogEvents.Operation.Started,
                "Starting main operation",
                context: null);

            using (LoggingScope.BeginWithProperties(("OperationId", "op-456"), ("Step", "Step1")))
            {
                logger.LogInformation(
                    CommonLogEvents.Operation.Started,
                    "Processing step 1",
                    context: null);

                using (LoggingScope.BeginWithProperties(("SubStep", "SubStep1")))
                {
                    logger.LogDebug(
                        CommonLogEvents.Operation.Started,
                        "Processing sub-step",
                        context: null);
                }
            }

            logger.LogInformation(
                CommonLogEvents.Operation.Completed,
                "Main operation completed",
                context: null);
        }
    }
}