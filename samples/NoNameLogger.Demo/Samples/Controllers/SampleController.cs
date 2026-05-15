using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;
using NoNameLogger.Application.Logging.Core;
using NoNameLogger.Demo.Samples.Models.cs;

namespace NoNameLogger.Demo.Samples.Controllers;

/// <summary>
/// Demo controller showcasing end-to-end logging: validation, ambient context, and error handling.
/// </summary>
public class SampleController
{
    private readonly ILogger<SampleController> _logger;

    public SampleController(ILogger<SampleController> logger)
    {
        _logger = logger;
    }

    // Where to use ForBuilder:
    // Use it when you DON'T have all context properties at the beginning,
    // and you want to add properties gradually during the flow (validation, DB calls, branching).
    // Good places: controllers/handlers, long workflows, multi-step operations.
    public async Task<IEnumerable<SampleResponse>?> ProcessRequestAsync(
        SampleRequest? request,
        CancellationToken cancellationToken = default)
    {
        // 1) Start with a pre-populated builder (Application + Service + Operation are set)
        var contextBuilder = AppLoggingContext.ForBuilder<SampleController>(nameof(ProcessRequestAsync));

        // 2) Add properties as they become available (step-by-step)
        contextBuilder.With("RequestId", Guid.NewGuid().ToString());
        contextBuilder.With("Stage", "RequestReceived");

        // If request exists, enrich context with request data
        if (request != null)
        {
            contextBuilder.With("PropertyId", request.PropertyId);
            contextBuilder.With("ProviderCode", request.ProviderCode);
            contextBuilder.With("PageNumber", request.PageNumber);
        }

        // 3) Build the final context once you have enough information
        var context = contextBuilder.Build();

        // 4) Begin the scope so ALL logs in this method (and deeper calls) share the same context
        using (LoggingScope.Begin(context))
        {
            try
            {
                // If request is null, log with the same context
                if (request == null)
                {
                    // Ambient context is active, so context can be null here
                    _logger.LogWarning(
                        CommonLogEvents.Business.ValidationFailed,
                        "Request is null.",
                        context: null);

                    return null;
                }

                // Update stage (you can add nested properties using BeginWithProperties)
                using (LoggingScope.BeginWithProperties(("Stage", "ValidatingRequest")))
                {
                    var validationResults = new List<ValidationResult>();
                    var validationContext = new ValidationContext(request);

                    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                    {
                        _logger.LogWarning(
                            CommonLogEvents.Business.ValidationFailed,
                            "Request validation failed.",
                            context: null,
                            properties: validationResults.Select(vr =>
                                new KeyValuePair<string, object>("ValidationError", vr.ErrorMessage ?? string.Empty)));

                        return null;
                    }
                }

                // Log success path
                _logger.LogInformation(
                    CommonLogEvents.Operation.Started,
                    "Processing request...",
                    context: null);

                await Task.Delay(100, cancellationToken);

                var response = new List<SampleResponse>
            {
                new() { Id = "1", Name = "Sample Item 1" },
                new() { Id = "2", Name = "Sample Item 2" }
            };

                _logger.LogInformation(
                    CommonLogEvents.Operation.Completed,
                    "Request processed successfully. Returned {ItemCount} items.",
                    context: null,
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
                    context: null);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    CommonLogEvents.Operation.Failed,
                    "Failed to process request.",
                    context: null,
                    exception: ex);

                throw;
            }
        }
    }
}