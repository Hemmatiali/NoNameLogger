using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples;

/// <summary>
/// Timed operation demos (success + failure handling).
/// </summary>
public static partial class LoggingSamples
{
    /// <summary>
    /// Demonstrates timed operations with success/failure logging (failure is handled to keep the demo running).
    /// </summary>
    public static void TimedOperationSample(ILogger logger)
    {
        var operationName = "SampleOperation";

        var context = AppLoggingContext.For<LoggingSamplesMarker>(
            nameof(TimedOperationSample),
            ("OperationName", operationName));

        using (LoggingScope.Begin(context))
        {
            // Success scenario
            using (var op = TimedLogOperation.Start(logger, operationName, context: context))
            {
                Thread.Sleep(100);
                op.Complete();
            }

            // Failure scenario (DO NOT crash demo)
            using (var op = TimedLogOperation.Start(logger, "FailingOperation", context: context))
            {
                try
                {
                    Thread.Sleep(50);
                    throw new InvalidOperationException("Operation failed");
                }
                catch (Exception ex)
                {
                    op.Fail(ex);

                    // Demo-friendly: log and continue (no rethrow)
                    logger.LogWarning(
                        CommonLogEvents.Operation.Failed,
                        "Failure scenario executed (expected in demo). Continuing...",
                        context: null);
                }
            }
        }
    }
}