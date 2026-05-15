using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Demo.Samples.Services;

/// <summary>
/// Demo service showcasing ambient context usage and nested scoped properties.
/// </summary>
public class SampleService
{
    private readonly ILogger<SampleService> _logger;

    public SampleService(ILogger<SampleService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates logging with ambient context (no explicit context parameter) and nested properties.
    /// </summary>
    public async Task<string> ProcessDataAsync(string dataId)
    {
        // No need to accept ILoggingContext parameter - ambient context is used automatically
        _logger.Log()
            .Information()
            .Event(CommonLogEvents.Operation.Started)
            .Message("Processing data with ID: {DataId}", dataId)
            .WithProperty("DataId", dataId)
            .Write();

        // Add nested context properties
        using (LoggingScope.BeginWithProperties(("DataId", dataId), ("ProcessingStage", "DataTransformation")))
        {
            // All logs in this scope automatically include DataId and ProcessingStage
            await Task.Delay(50);

            _logger.LogInformation(
                CommonLogEvents.Operation.Completed,
                "Data processing completed for {DataId}",
                context: null); // Ambient context is used automatically
        }

        return $"Processed-{dataId}";
    }
}