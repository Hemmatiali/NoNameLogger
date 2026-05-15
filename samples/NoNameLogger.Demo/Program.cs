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

RunAllSamples(logger);

await RunServiceSampleAsync(loggerFactory);

await RunControllerSampleAsync(loggerFactory);

logger.LogInformation("Demo finished.");

void RunAllSamples(Microsoft.Extensions.Logging.ILogger loggerParameter)
{
    LoggingSamples.BasicUsage(loggerParameter);
    LoggingSamples.TimedOperationSample(loggerParameter);
    LoggingSamples.AmbientContextExample(loggerParameter);
    LoggingSamples.ErrorHandlingExample(loggerParameter);
    LoggingSamples.HttpRequestExample(loggerParameter, method: "GET", path: "/api/values");
    LoggingSamples.DatabaseOperationExample(loggerParameter, commandText: "SELECT TOP (1) * FROM SampleTable");
}

async Task RunServiceSampleAsync(ILoggerFactory loggerFactoryParameter)
{
    var serviceLogger = loggerFactoryParameter.CreateLogger<SampleService>();
    var service = new SampleService(serviceLogger);

    await service.ProcessDataAsync("data-001");
}

async Task RunControllerSampleAsync(ILoggerFactory loggerFactoryParameter)
{
    var controllerLogger = loggerFactoryParameter.CreateLogger<SampleController>();
    var controller = new SampleController(controllerLogger);

    // 1) Valid request
    var request = new SampleRequest
    {
        PropertyId = "P-1001",
        ProviderCode = "PRV-01",
        PageNumber = 1
    };

    _ = await controller.ProcessRequestAsync(request);

    // 2) Null request (shows validation + warning path)
    _ = await controller.ProcessRequestAsync(null);
}