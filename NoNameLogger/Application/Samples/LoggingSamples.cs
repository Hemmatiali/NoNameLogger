#if DEBUG
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Application.Samples;

public static class LoggingSamples
{
    public static void BasicUsage(ILogger logger)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithEnvironment("Development")
            .WithApplication("SampleApp")
            .WithUserId("user-123")
            .WithCorrelationId("corr-xyz")
            .Build();

        logger.Log(context)
            .Information()
            .Event(CommonLogEvents.Http.RequestReceived)
            .Message(CommonLogMessages.Http.RequestStarted)
            .WithProperty("Path", "/api/values")
            .WithProperty("Method", "GET")
            .Write();

        logger.LogInformation(
            CommonLogEvents.System.StartupCompleted,
            CommonLogMessages.System.StartupCompleted,
            context: context);
    }

    public static void TimedOperationSample(ILogger logger)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithOperation("SampleOperation")
            .Build();

        using var op = TimedLogOperation.Start(logger, "SampleOperation", context: context);

        // Do some work...
        Thread.Sleep(100);

        op.Complete();
    }
}
#endif