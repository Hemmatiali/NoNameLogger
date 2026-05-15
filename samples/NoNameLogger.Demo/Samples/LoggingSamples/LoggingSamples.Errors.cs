using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples;

/// <summary>
/// Error/exception handling demos.
/// </summary>
public static partial class LoggingSamples
{
    /// <summary>
    /// Example showing error handling with different log levels.
    /// </summary>
    public static void ErrorHandlingExample(ILogger logger)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithOperation("ErrorHandlingDemo")
            .Build();

        using (LoggingScope.Begin(context))
        {
            try
            {
                // Simulate an operation that might fail
                throw new InvalidOperationException("Something went wrong");
            }
            catch (InvalidOperationException ex)
            {
                // Log as error with exception details
                logger.LogError(
                    CommonLogEvents.Operation.Failed,
                    "Operation failed with InvalidOperationException",
                    context: null,
                    exception: ex,
                    properties: new[]
                    {
                        new KeyValuePair<string, object>("ErrorCode", "INVALID_OP"),
                        new KeyValuePair<string, object>("Retryable", false)
                    });
            }
            catch (Exception ex)
            {
                // Log as critical for unexpected errors
                logger.LogCritical(
                    CommonLogEvents.Operation.Failed,
                    "Unexpected error occurred",
                    context: null,
                    exception: ex);
            }
        }
    }
}