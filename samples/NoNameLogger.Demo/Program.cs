
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

using MSILogger = Microsoft.Extensions.Logging.ILogger;
using SerilogILogger = Serilog.ILogger;

// Minimal demo app to show how NoNameLogger can be wired in a typical .NET console app.
// Note: Replace any NoNameLogger-specific types below with your actual public APIs.

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
MSILogger logger = loggerFactory.CreateLogger("NoNameLogger.Demo");

logger.LogInformation("Demo started.");
logger.LogInformation("If you have NoNameLogger helpers, call them here to enrich context and standardize events.");
logger.LogInformation("Demo finished.");
