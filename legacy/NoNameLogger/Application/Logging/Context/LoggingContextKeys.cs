namespace NoNameLogger.Application.Logging.Context;

/// <summary>
/// Well-known context keys.
/// </summary>
public static class LoggingContextKeys
{
    public const string Environment = "Environment";
    public const string Application = "Application";
    public const string Service = "Service";
    public const string MachineName = "MachineName";
    public const string UserId = "UserId";
    public const string UserName = "UserName";
    public const string Tenant = "Tenant";
    public const string CorrelationId = "CorrelationId";
    public const string RequestId = "RequestId";
    public const string SessionId = "SessionId";
    public const string Operation = "Operation";
    public const string OperationId = "OperationId";
    public const string HttpMethod = "HttpMethod";
    public const string Path = "Path";
    public const string Route = "Route";
    public const string StatusCode = "StatusCode";
    public const string ClientIp = "ClientIp";
}