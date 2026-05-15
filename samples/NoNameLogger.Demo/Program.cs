using Microsoft.Extensions.Logging;
using NoNameLogger.Demo.Samples;
using Serilog;
using Serilog.Extensions.Logging;

global::Serilog.Log.Logger = new global::Serilog.LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

using ILoggerFactory loggerFactory = new SerilogLoggerFactory(global::Serilog.Log.Logger, dispose: true);

Microsoft.Extensions.Logging.ILogger logger =
    loggerFactory.CreateLogger("NoNameLogger.Demo");

logger.LogInformation("Demo started.");

RunThreeCoreSamples(logger);

await RunControllerSampleAsync(loggerFactory);

logger.LogInformation("Demo finished.");

void RunThreeCoreSamples(Microsoft.Extensions.Logging.ILogger loggerParameter)
{
    LoggingSamples.BasicUsage(loggerParameter);
    LoggingSamples.TimedOperationSample(loggerParameter);
    LoggingSamples.AmbientContextExample(loggerParameter);
}

async Task RunControllerSampleAsync(ILoggerFactory loggerFactoryParameter)
{
    var controllerLogger = loggerFactoryParameter.CreateLogger<SampleController>();
    var controller = new SampleController(controllerLogger);

    var request = new SampleRequest
    {
        PropertyId = "P-1001",
        ProviderCode = "PRV-01",
        PageNumber = 1
    };

    _ = await controller.ProcessRequestAsync(request);
}