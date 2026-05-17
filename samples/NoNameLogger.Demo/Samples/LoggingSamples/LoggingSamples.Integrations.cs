using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples;

/// <summary>
/// Integration-like demos (HTTP / DB patterns).
/// </summary>
public static partial class LoggingSamples
{
    /// <summary>
    /// Example showing HTTP request/response logging pattern.
    /// </summary>
    public static void HttpRequestExample(ILogger logger, string method, string path)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithHttpMethod(method)
            .WithPath(path)
            .WithCorrelationId(Guid.NewGuid().ToString())
            .Build();

        using (LoggingScope.Begin(context))
        {
            logger.LogInformation(
                CommonLogEvents.Http.RequestReceived,
                CommonLogMessages.Http.RequestStarted,
                context: null,
                properties: new[]
                {
                    new KeyValuePair<string, object>("Method", method),
                    new KeyValuePair<string, object>("Path", path)
                });

            Thread.Sleep(150);

            logger.LogInformation(
                CommonLogEvents.Http.RequestCompleted,
                CommonLogMessages.Http.RequestCompleted,
                context: null,
                properties: new[]
                {
                    new KeyValuePair<string, object>("StatusCode", 200),
                    new KeyValuePair<string, object>("ElapsedMs", 150)
                });
        }
    }

    /// <summary>
    /// Example showing database operation logging.
    /// </summary>
    public static void DatabaseOperationExample(ILogger logger, string commandText)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithOperation("DatabaseQuery")
            .Build();

        using (LoggingScope.Begin(context))
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            logger.LogInformation(
                CommonLogEvents.Database.CommandExecuting,
                CommonLogMessages.Database.CommandExecuting,
                context: null,
                properties: new[]
                {
                    new KeyValuePair<string, object>("CommandText", commandText)
                });

            Thread.Sleep(200);
            stopwatch.Stop();

            logger.LogInformation(
                CommonLogEvents.Database.CommandExecuted,
                CommonLogMessages.Database.CommandExecuted,
                context: null,
                properties: new[]
                {
                    new KeyValuePair<string, object>("CommandText", commandText),
                    new KeyValuePair<string, object>("ElapsedMs", stopwatch.ElapsedMilliseconds)
                });
        }
    }
}