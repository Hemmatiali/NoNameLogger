namespace NoNameLogger.Application.Logging.Conventions;

/// <summary>
/// Generic, reusable message templates grouped by category.
/// You can extend this class via partials or ignore it and define your own.
/// </summary>
public static class CommonLogMessages
{
    public static class System
    {
        public const string Startup = "Application starting";
        public const string StartupCompleted = "Application started";
        public const string Shutdown = "Application shutting down";
        public const string ConfigurationLoaded = "Configuration loaded";
    }

    public static class Http
    {
        public const string RequestStarted = "HTTP {Method} {Path} started";
        public const string RequestCompleted = "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs} ms";
        public const string RequestFailed = "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMs} ms";
    }

    public static class Database
    {
        public const string CommandExecuting = "Executing database command {CommandText}";
        public const string CommandExecuted = "Executed database command {CommandText} in {ElapsedMs} ms";
        public const string CommandFailed = "Database command {CommandText} failed in {ElapsedMs} ms";
    }

    public static class Operation
    {
        public const string Started = "Operation {OperationName} started";
        public const string Completed = "Operation {OperationName} completed in {ElapsedMs} ms";
        public const string Failed = "Operation {OperationName} failed in {ElapsedMs} ms. Error: {ErrorMessage}";
    }

    public static class Cache
    {
        public const string Hit = "Cache hit for {CacheKey}";
        public const string Miss = "Cache miss for {CacheKey}";
        public const string Set = "Cache set for {CacheKey}";
        public const string Remove = "Cache remove for {CacheKey}";
    }

    public static class Security
    {
        public const string UnauthorizedAccess = "Unauthorized access to {Resource}";
        public const string Forbidden = "Forbidden access to {Resource}";
        public const string LoginSucceeded = "User {UserName} logged in";
        public const string LoginFailed = "Failed login attempt for {UserName}";
        public const string SuspiciousActivity = "Suspicious activity detected: {Details}";
    }

    public static class Business
    {
        public const string ValidationFailed = "Validation failed for {Entity}: {Reason}";
        public const string RuleViolated = "Business rule violated: {RuleName}";
        public const string StateChanged = "State changed for {Entity} from {OldState} to {NewState}";
    }
}