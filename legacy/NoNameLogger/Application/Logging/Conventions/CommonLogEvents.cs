using Microsoft.Extensions.Logging;

namespace NoNameLogger.Application.Logging.Conventions;

/// <summary>
/// Generic, reusable log events grouped by category.
/// You can extend this class via partials or ignore it and define your own events.
/// </summary>
public static class CommonLogEvents
{
    public static class System
    {
        public static readonly EventId Startup = new(1000, nameof(Startup));
        public static readonly EventId StartupCompleted = new(1001, nameof(StartupCompleted));
        public static readonly EventId Shutdown = new(1002, nameof(Shutdown));
        public static readonly EventId ConfigurationLoaded = new(1003, nameof(ConfigurationLoaded));
    }

    public static class Http
    {
        public static readonly EventId RequestReceived = new(2000, nameof(RequestReceived));
        public static readonly EventId RequestCompleted = new(2001, nameof(RequestCompleted));
        public static readonly EventId RequestFailed = new(2002, nameof(RequestFailed));
        public static readonly EventId Timeout = new(2003, nameof(Timeout));
        public static readonly EventId Retry = new(2004, nameof(Retry));

        // Response-focused events
        public static readonly EventId ResponseReceived = new(2030, nameof(ResponseReceived));
        public static readonly EventId ResponseFailed = new(2031, nameof(ResponseFailed));
        public static readonly EventId ResponseEmpty = new(2032, nameof(ResponseEmpty));
        public static readonly EventId ResponseIncomplete = new(2033, nameof(ResponseIncomplete));
        public static readonly EventId ResponseDeserializationFailed = new(2034, nameof(ResponseDeserializationFailed));
        public static readonly EventId ResponseValidationFailed = new(2035, nameof(ResponseValidationFailed));
        public static readonly EventId UnexpectedStatusCode = new(2036, nameof(UnexpectedStatusCode));
        public static readonly EventId ResponseTooLarge = new(2037, nameof(ResponseTooLarge));
    }

    public static class Database
    {
        public static readonly EventId CommandExecuting = new(3000, nameof(CommandExecuting));
        public static readonly EventId CommandExecuted = new(3001, nameof(CommandExecuted));
        public static readonly EventId CommandFailed = new(3002, nameof(CommandFailed));
        public static readonly EventId TransactionStarted = new(3003, nameof(TransactionStarted));
        public static readonly EventId TransactionCommitted = new(3004, nameof(TransactionCommitted));
        public static readonly EventId TransactionRolledBack = new(3005, nameof(TransactionRolledBack));
    }

    public static class Operation
    {
        public static readonly EventId Started = new(4000, nameof(Started));
        public static readonly EventId Completed = new(4001, nameof(Completed));
        public static readonly EventId Failed = new(4002, nameof(Failed));
        public static readonly EventId Canceled = new(4003, nameof(Canceled));
        public static readonly EventId Retried = new(4004, nameof(Retried));
    }

    public static class Cache
    {
        public static readonly EventId Hit = new(5000, nameof(Hit));
        public static readonly EventId Miss = new(5001, nameof(Miss));
        public static readonly EventId Set = new(5002, nameof(Set));
        public static readonly EventId Remove = new(5003, nameof(Remove));
        public static readonly EventId Clear = new(5004, nameof(Clear));
    }

    public static class Security
    {
        public static readonly EventId UnauthorizedAccess = new(6000, nameof(UnauthorizedAccess));
        public static readonly EventId Forbidden = new(6001, nameof(Forbidden));
        public static readonly EventId LoginSucceeded = new(6002, nameof(LoginSucceeded));
        public static readonly EventId LoginFailed = new(6003, nameof(LoginFailed));
        public static readonly EventId SuspiciousActivity = new(6004, nameof(SuspiciousActivity));
        public static readonly EventId TokenExpired = new(6005, nameof(TokenExpired));
    }

    public static class Business
    {
        public static readonly EventId ValidationFailed = new(7000, nameof(ValidationFailed));
        public static readonly EventId RuleViolated = new(7001, nameof(RuleViolated));
        public static readonly EventId StateChanged = new(7002, nameof(StateChanged));
        public static readonly EventId NotFound = new(7003, nameof(NotFound));
        public static readonly EventId Conflict = new(7004, nameof(Conflict));
    }

    public static class FileSystem
    {
        // 8000–8029: basic directory / file operations
        public static readonly EventId DirectoryCreated = new(8000, nameof(DirectoryCreated));
        public static readonly EventId DirectoryDeleted = new(8001, nameof(DirectoryDeleted));
        public static readonly EventId DirectoryNotFound = new(8002, nameof(DirectoryNotFound));
        public static readonly EventId FileCreated = new(8003, nameof(FileCreated));
        public static readonly EventId FileDeleted = new(8004, nameof(FileDeleted));
        public static readonly EventId FileNotFound = new(8005, nameof(FileNotFound));

        // 8031–8059: failures / IO errors (leave gap from 8000-block)
        public static readonly EventId DirectoryCreateFailed = new(8031, nameof(DirectoryCreateFailed));
        public static readonly EventId DirectoryDeleteFailed = new(8032, nameof(DirectoryDeleteFailed));
        public static readonly EventId FileDeleteFailed = new(8033, nameof(FileDeleteFailed));
        public static readonly EventId FileReadFailed = new(8034, nameof(FileReadFailed));
        public static readonly EventId FileWriteFailed = new(8035, nameof(FileWriteFailed));
        public static readonly EventId FileDecompressFailed = new(8036, nameof(FileDecompressFailed));
        public static readonly EventId DirectoryEnumerationFailed = new(8037, nameof(DirectoryEnumerationFailed));
    }

    public static class Integration
    {
        // 9000–9029: Request/response lifecycle with external systems
        public static readonly EventId RequestSent = new(9000, nameof(RequestSent));
        public static readonly EventId ResponseReceived = new(9001, nameof(ResponseReceived));
        public static readonly EventId ResponseFailed = new(9002, nameof(ResponseFailed));

        // 9031–9059: Circuit breaker states (leave space after 9000-block)
        public static readonly EventId CircuitOpen = new(9031, nameof(CircuitOpen));
        public static readonly EventId CircuitHalfOpen = new(9032, nameof(CircuitHalfOpen));
        public static readonly EventId CircuitClosed = new(9033, nameof(CircuitClosed));
    }

    public static class Payload
    {
        // Expected payload is missing (null, empty, no items, etc.)
        public static readonly EventId Missing = new(10000, nameof(Missing));

        // Payload is present but incomplete (required fields missing, partial data, etc.)
        public static readonly EventId Incomplete = new(10001, nameof(Incomplete));

        // Payload could not be deserialized / parsed to the expected model
        public static readonly EventId DeserializationFailed = new(10002, nameof(DeserializationFailed));

        // Payload is structurally fine but semantically invalid (e.g. codes out of range, inconsistent values, etc.)
        public static readonly EventId Invalid = new(10003, nameof(Invalid));
        // 10004–10029 reserved for future Payload events
    }

    public static class Jobs
    {
        public static readonly EventId JobStarted = new(11000, nameof(JobStarted));
        public static readonly EventId JobCompleted = new(11001, nameof(JobCompleted));
        public static readonly EventId JobCancelled = new(11002, nameof(JobCancelled));
        public static readonly EventId JobSkipped = new(11003, nameof(JobSkipped));
        public static readonly EventId JobFailed = new(11004, nameof(JobFailed));
        public static readonly EventId JobCompletionRetryFailed = new(11005, nameof(JobCompletionRetryFailed));
        public static readonly EventId JobCompletionFailed = new(11006, nameof(JobCompletionFailed));
    }

}
