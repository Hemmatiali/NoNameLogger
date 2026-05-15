using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Abstractions;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Conventions;

namespace NoNameLogger.Application.Logging.Core;


/// <summary>
/// Represents a timed operation that logs start, completion, and optionally failure.
/// </summary>
public sealed class TimedLogOperation : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly EventId _startEvent;
    private readonly EventId _completedEvent;
    private readonly EventId _failedEvent;
    private readonly ILoggingContext? _context;
    private readonly Stopwatch _stopwatch;

    private int _completionState; // 0=pending, 1=completed, 2=markedFailed, 3=loggedFailed

    private TimedLogOperation(
        ILogger logger,
        string operationName,
        EventId startEvent,
        EventId completedEvent,
        EventId failedEvent,
        ILoggingContext? context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _startEvent = startEvent;
        _completedEvent = completedEvent;
        _failedEvent = failedEvent;
        _context = context;

        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            _startEvent,
            CommonLogMessages.Operation.Started,
            context: _context,
            properties: null,
            args: _operationName);
    }

    /// <summary>
    /// Starts a new timed operation.
    /// Uses the ambient context from <see cref="LoggingScope.Current"/> if no explicit context is provided.
    /// If both ambient and explicit contexts exist, they are merged (explicit overrides ambient).
    /// </summary>
    public static TimedLogOperation Start(
        ILogger logger,
        string operationName,
        EventId? startEvent = null,
        EventId? completedEvent = null,
        EventId? failedEvent = null,
        ILoggingContext? context = null)
    {
        // Get effective context: merge ambient with explicit
        var effectiveContext = LoggingScope.GetEffectiveContext(context);

        return new TimedLogOperation(
            logger,
            operationName,
            startEvent ?? CommonLogEvents.Operation.Started,
            completedEvent ?? CommonLogEvents.Operation.Completed,
            failedEvent ?? CommonLogEvents.Operation.Failed,
            effectiveContext);
    }

    /// <summary>
    /// Marks the operation as failed. The failure will be logged on dispose.
    /// State: 0 (pending) → 2 (markedFailed)
    /// </summary>
    public void MarkFailed()
    {
        Interlocked.CompareExchange(ref _completionState, 2, 0);
    }

    /// <summary>
    /// Marks the operation as completed successfully and logs immediately.
    /// State: 0 (pending) → 1 (completed)
    /// </summary>
    public void Complete()
    {
        if (Interlocked.CompareExchange(ref _completionState, 1, 0) != 0)
            return;

        _stopwatch.Stop();
        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

        _logger.LogInformation(
            _completedEvent,
            CommonLogMessages.Operation.Completed,
            context: _context,
            properties: null,
            args: new object[] { _operationName, elapsedMs });
    }

    /// <summary>
    /// Marks the operation as failed and logs immediately with an exception.
    /// State: 0 (pending) → 3 (loggedFailed) or 2 (markedFailed) → 3 (loggedFailed)
    /// </summary>
    public void Fail(Exception exception)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));

        // Try to transition from pending (0) to loggedFailed (3)
        var prev = Interlocked.CompareExchange(ref _completionState, 3, 0);
        if (prev != 0)
        {
            // If already marked failed (2), transition to loggedFailed (3)
            if (prev == 2)
            {
                prev = Interlocked.CompareExchange(ref _completionState, 3, 2);
                if (prev != 2)
                    return; // Already transitioned by another thread
            }
            else
            {
                return; // Already completed (1) or loggedFailed (3)
            }
        }

        _stopwatch.Stop();
        var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

        _logger.LogError(
            _failedEvent,
            CommonLogMessages.Operation.Failed,
            context: _context,
            exception: exception,
            properties: null,
            args: new object[] { _operationName, elapsedMs, exception.Message });
    }

    /// <inheritdoc />
    /// <summary>
    /// Disposes the operation and logs completion or failure.
    /// State transitions:
    /// - 0 (pending) → 1 (completed): logs success
    /// - 2 (markedFailed) → 3 (loggedFailed): logs failure
    /// - 1 or 3: already logged, do nothing
    /// </summary>
    public void Dispose()
    {
        // Try to transition from pending (0) to completed (1)
        var previousState = Interlocked.CompareExchange(ref _completionState, 1, 0);

        if (previousState == 0)
        {
            // Was pending, now completed - log success
            _stopwatch.Stop();
            var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

            _logger.LogInformation(
                _completedEvent,
                CommonLogMessages.Operation.Completed,
                context: _context,
                properties: null,
                args: new object[] { _operationName, elapsedMs });
            return;
        }

        if (previousState == 2)
        {
            // Was markedFailed, try to transition to loggedFailed (3)
            previousState = Interlocked.CompareExchange(ref _completionState, 3, 2);
            if (previousState != 2)
                return; // Another thread already handled it

            // Log the failure
            _stopwatch.Stop();
            var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

            _logger.LogError(
                _failedEvent,
                CommonLogMessages.Operation.Failed,
                context: _context,
                exception: null,
                properties: null,
                args: new object[] { _operationName, elapsedMs, string.Empty });
            return;
        }

        // State was 1 (completed) or 3 (loggedFailed) - already handled
    }
}
