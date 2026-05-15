#if DEBUG
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Application.Samples;

/// <summary>
/// Sample request model for demonstration
/// </summary>
public class SampleRequest
{
    [Required]
    public string? PropertyId { get; set; }

    [Required]
    public string? ProviderCode { get; set; }

    public int? PageNumber { get; set; }
}

/// <summary>
/// Sample response model for demonstration
/// </summary>
public class SampleResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Sample controller for demonstration
/// </summary>
public class SampleController
{
    private readonly ILogger<SampleController> _logger;

    public SampleController(ILogger<SampleController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Complete example showing request validation, ambient context, and error handling
    /// </summary>
    public async Task<IEnumerable<SampleResponse>?> ProcessRequestAsync(
        SampleRequest? request,
        CancellationToken cancellationToken = default)
    {
        // Serialize request for logging (sanitize sensitive data in production)
        var requestBody = request != null
            ? JsonSerializer.Serialize(new { request.PropertyId, request.ProviderCode, request.PageNumber })
            : "null";

        // Create logging context with operation and request details
        var context = AppLoggingContext.For<SampleController>(
            nameof(ProcessRequestAsync),
            ("RequestBody", requestBody));

        // Establish ambient logging context for the entire request flow
        using (LoggingScope.Begin(context))
        {
            try
            {
                // Validate request is not null
                if (request == null)
                {
                    _logger.LogWarning(
                        CommonLogEvents.Business.ValidationFailed,
                        "Request is null.",
                        context);
                    return null;
                }

                // Perform validation
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    _logger.LogWarning(
                        CommonLogEvents.Business.ValidationFailed,
                        "Request validation failed.",
                        context,
                        properties: validationResults.Select(vr =>
                            new KeyValuePair<string, object>("ValidationError", vr.ErrorMessage ?? string.Empty)));

                    return null;
                }

                // Log successful validation
                _logger.LogInformation(
                    CommonLogEvents.Operation.Started,
                    "Processing request for PropertyId: {PropertyId}, ProviderCode: {ProviderCode}",
                    context,
                    properties: new[]
                    {
                        new KeyValuePair<string, object>("PropertyId", request.PropertyId ?? string.Empty),
                        new KeyValuePair<string, object>("ProviderCode", request.ProviderCode ?? string.Empty)
                    });

                // Simulate async work
                await Task.Delay(100, cancellationToken);

                // Simulate successful response
                var response = new List<SampleResponse>
                {
                    new() { Id = "1", Name = "Sample Item 1" },
                    new() { Id = "2", Name = "Sample Item 2" }
                };

                _logger.LogInformation(
                    CommonLogEvents.Operation.Completed,
                    "Request processed successfully. Returned {ItemCount} items.",
                    context,
                    properties: new[]
                    {
                        new KeyValuePair<string, object>("ItemCount", response.Count)
                    });

                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    CommonLogEvents.Operation.Canceled,
                    "Operation was canceled.",
                    context);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    CommonLogEvents.Operation.Failed,
                    "Failed to process request.",
                    context,
                    exception: ex);
                throw;
            }
        }
    }
}

/// <summary>
/// Sample service demonstrating ambient context usage
/// </summary>
public class SampleService
{
    private readonly ILogger<SampleService> _logger;

    public SampleService(ILogger<SampleService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Example showing how ambient context is automatically used without passing it explicitly
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

public static class LoggingSamples
{
    /// <summary>
    /// Basic usage example with explicit context
    /// </summary>
    public static void BasicUsage(ILogger logger)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithEnvironment("Development")
            .WithApplication("SampleApp")
            .WithUserId("user-123")
            .WithCorrelationId("corr-xyz")
            .Build();

        // Fluent API example
        logger.Log(context)
            .Information()
            .Event(CommonLogEvents.Http.RequestReceived)
            .Message(CommonLogMessages.Http.RequestStarted)
            .WithProperty("Path", "/api/values")
            .WithProperty("Method", "GET")
            .Write();

        // Extension method example
        logger.LogInformation(
            CommonLogEvents.System.StartupCompleted,
            CommonLogMessages.System.StartupCompleted,
            context: context);
    }

    /// <summary>
    /// Timed operation example with success and failure scenarios
    /// </summary>
    public static void TimedOperationSample(ILogger logger)
    {
        var context = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithOperation("SampleOperation")
            .Build();

        // Success scenario
        using (var op = TimedLogOperation.Start(logger, "SampleOperation", context: context))
        {
            // Simulate work
            Thread.Sleep(100);
            op.Complete(); // Logs completion with elapsed time
        }

        // Failure scenario
        using (var op = TimedLogOperation.Start(logger, "FailingOperation", context: context))
        {
            try
            {
                // Simulate work that fails
                Thread.Sleep(50);
                throw new InvalidOperationException("Operation failed");
            }
            catch (Exception ex)
            {
                op.Fail(ex); // Logs failure with exception and elapsed time
                throw;
            }
        }
    }

    /// <summary>
    /// Example showing ambient context with nested scopes
    /// </summary>
    public static void AmbientContextExample(ILogger logger)
    {
        // Establish root context
        var rootContext = LoggingContextBuilder
            .Create()
            .WithApplication("SampleApp")
            .WithCorrelationId(Guid.NewGuid().ToString())
            .WithUserId("user-123")
            .Build();

        using (LoggingScope.Begin(rootContext))
        {
            // All logging in this scope uses rootContext
            logger.LogInformation(
                CommonLogEvents.Operation.Started,
                "Starting main operation",
                context: null); // Ambient context used

            // Nested scope with additional properties
            using (LoggingScope.BeginWithProperties(("OperationId", "op-456"), ("Step", "Step1")))
            {
                // Logs include rootContext + OperationId + Step
                logger.LogInformation(
                    CommonLogEvents.Operation.Started,
                    "Processing step 1",
                    context: null);

                // Another nested scope
                using (LoggingScope.BeginWithProperties(("SubStep", "SubStep1")))
                {
                    // Logs include rootContext + OperationId + Step + SubStep
                    logger.LogDebug(
                        CommonLogEvents.Operation.Started,
                        "Processing sub-step",
                        context: null);
                }
            }

            // Back to root context only
            logger.LogInformation(
                CommonLogEvents.Operation.Completed,
                "Main operation completed",
                context: null);
        }
    }

    /// <summary>
    /// Example showing error handling with different log levels
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

    /// <summary>
    /// Example showing HTTP request/response logging pattern
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
            // Log request received
            logger.LogInformation(
                CommonLogEvents.Http.RequestReceived,
                CommonLogMessages.Http.RequestStarted,
                context: null,
                properties: new[]
                {
                    new KeyValuePair<string, object>("Method", method),
                    new KeyValuePair<string, object>("Path", path)
                });

            // Simulate processing
            Thread.Sleep(150);

            // Log request completed
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
    /// Example showing database operation logging
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

            // Simulate database operation
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
#endif