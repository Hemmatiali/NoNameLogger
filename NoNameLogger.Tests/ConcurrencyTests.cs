using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Tests;

/// <summary>
/// Tests for thread-safety and concurrency in the logging framework.
/// </summary>
public class ConcurrencyTests
{
    #region LoggingScope Tests

    [Fact]
    public async Task LoggingScope_ParallelScopes_DoNotInterfere()
    {
        // Arrange
        var results = new ConcurrentBag<(int Index, string? CorrelationId)>();
        const int iterations = 100;

        // Act
        await Parallel.ForEachAsync(
            Enumerable.Range(0, iterations),
            new ParallelOptions { MaxDegreeOfParallelism = 10 },
            async (i, ct) =>
            {
                var context = LoggingContextBuilder.Create()
                    .WithCorrelationId($"corr-{i}")
                    .Build();

                using (LoggingScope.Begin(context))
                {
                    // Simulate async work
                    await Task.Delay(Random.Shared.Next(1, 5), ct);

                    var current = LoggingScope.Current;
                    current!.TryGet("CorrelationId", out var corrId);
                    results.Add((i, corrId as string));
                }
            });

        // Assert - each async flow should have its own isolated context
        results.Should().HaveCount(iterations);
        foreach (var (index, correlationId) in results)
        {
            correlationId.Should().Be($"corr-{index}",
                because: $"async flow {index} should have its own context");
        }
    }

    [Fact]
    public void LoggingScope_NestedScopes_RestoreCorrectly()
    {
        // Arrange & Act & Assert
        LoggingScope.Current.Should().BeNull("no scope should be active initially");

        var outer = LoggingContextBuilder.Create().WithUserId("outer-user").Build();
        var inner = LoggingContextBuilder.Create().WithUserId("inner-user").Build();

        using (LoggingScope.Begin(outer))
        {
            GetUserId().Should().Be("outer-user");

            using (LoggingScope.Begin(inner))
            {
                GetUserId().Should().Be("inner-user");
            }

            // After inner scope disposed, outer should be restored
            GetUserId().Should().Be("outer-user");
        }

        LoggingScope.Current.Should().BeNull("scope should be cleared after all scopes disposed");
    }

    [Fact]
    public void LoggingScope_BeginWithProperties_MergesWithParent()
    {
        // Arrange
        var parent = LoggingContextBuilder.Create()
            .WithCorrelationId("corr-123")
            .WithUserId("user-456")
            .Build();

        // Act & Assert
        using (LoggingScope.Begin(parent))
        {
            using (LoggingScope.BeginWithProperties(("OrderId", "order-789")))
            {
                var current = LoggingScope.Current!;

                // Should have all three properties
                current.TryGet("CorrelationId", out var corrId);
                current.TryGet("UserId", out var userId);
                current.TryGet("OrderId", out var orderId);

                corrId.Should().Be("corr-123");
                userId.Should().Be("user-456");
                orderId.Should().Be("order-789");
            }

            // After inner scope, parent properties should still be there
            var restored = LoggingScope.Current!;
            restored.TryGet("CorrelationId", out var restoredCorrId);
            restored.TryGet("OrderId", out var restoredOrderId);

            restoredCorrId.Should().Be("corr-123");
            restoredOrderId.Should().BeNull("OrderId was only in inner scope");
        }
    }

    [Fact]
    public void LoggingScope_DoubleDispose_IsIdempotent()
    {
        // Arrange
        var context = LoggingContextBuilder.Create().WithUserId("test").Build();
        var outer = LoggingContextBuilder.Create().WithUserId("outer").Build();

        // Act
        using (LoggingScope.Begin(outer))
        {
            var scope = LoggingScope.Begin(context);

            GetUserId().Should().Be("test");

            // Double dispose
            scope.Dispose();
            scope.Dispose();

            // Should restore to outer, not to null
            GetUserId().Should().Be("outer");
        }
    }

    [Fact]
    public void LoggingScope_MultipleDisposeCallsFromSameThread_OnlyRestoresOnce()
    {
        // Note: AsyncLocal values are per-execution-context, so cross-thread dispose
        // only affects the thread doing the dispose. This test verifies that multiple
        // dispose calls from the same thread are idempotent.

        // Arrange
        var parent = LoggingContextBuilder.Create().WithUserId("parent").Build();
        var child = LoggingContextBuilder.Create().WithUserId("child").Build();

        using (LoggingScope.Begin(parent))
        {
            var scope = LoggingScope.Begin(child);

            GetUserId().Should().Be("child");

            // Act - dispose multiple times from the same thread
            scope.Dispose();
            GetUserId().Should().Be("parent", because: "first dispose should restore to parent");

            scope.Dispose(); // Second dispose should be idempotent
            GetUserId().Should().Be("parent", because: "second dispose should not change anything");

            scope.Dispose(); // Third dispose
            GetUserId().Should().Be("parent", because: "third dispose should not change anything");
        }

        LoggingScope.Current.Should().BeNull();
    }

    #endregion

    #region TimedLogOperation Tests

    [Fact]
    public void TimedLogOperation_Complete_LogsSuccessOnce()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        using var op = TimedLogOperation.Start(logger, "TestOp");

        // Act
        op.Complete();

        // Assert - should have start + complete = 2 logs
        logger.LogCalls.Should().HaveCount(2);
        logger.LogCalls.Last().LogLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void TimedLogOperation_Fail_LogsErrorOnce()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        using var op = TimedLogOperation.Start(logger, "TestOp");
        var exception = new InvalidOperationException("Test error");

        // Act
        op.Fail(exception);

        // Assert - should have start + fail = 2 logs
        logger.LogCalls.Should().HaveCount(2);
        logger.LogCalls.Last().LogLevel.Should().Be(LogLevel.Error);
        logger.LogCalls.Last().Exception.Should().Be(exception);
    }

    [Fact]
    public void TimedLogOperation_MarkFailedThenDispose_LogsError()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();

        // Act
        using (var op = TimedLogOperation.Start(logger, "TestOp"))
        {
            op.MarkFailed();
        } // Dispose called here

        // Assert - should have start + fail = 2 logs
        logger.LogCalls.Should().HaveCount(2);
        logger.LogCalls.Last().LogLevel.Should().Be(LogLevel.Error,
            because: "MarkFailed + Dispose should log as error");
    }

    [Fact]
    public void TimedLogOperation_DisposeWithoutComplete_LogsSuccess()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();

        // Act
        using (var op = TimedLogOperation.Start(logger, "TestOp"))
        {
            // Don't call Complete() or MarkFailed()
        }

        // Assert - should have start + success = 2 logs
        logger.LogCalls.Should().HaveCount(2);
        logger.LogCalls.Last().LogLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void TimedLogOperation_CompleteCalledTwice_LogsOnlyOnce()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        using var op = TimedLogOperation.Start(logger, "TestOp");

        // Act
        op.Complete();
        op.Complete();

        // Assert - should have start + complete = 2 logs (not 3)
        logger.LogCalls.Should().HaveCount(2);
    }

    [Fact]
    public void TimedLogOperation_CompleteThenDispose_DoesNotDoubleLog()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();

        // Act
        using (var op = TimedLogOperation.Start(logger, "TestOp"))
        {
            op.Complete();
        }

        // Assert - should have start + complete = 2 logs (not 3)
        logger.LogCalls.Should().HaveCount(2);
    }

    [Fact]
    public void TimedLogOperation_FailThenDispose_DoesNotDoubleLog()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();

        // Act
        using (var op = TimedLogOperation.Start(logger, "TestOp"))
        {
            op.Fail(new Exception("Test"));
        }

        // Assert - should have start + fail = 2 logs (not 3)
        logger.LogCalls.Should().HaveCount(2);
    }

    [Fact]
    public async Task TimedLogOperation_ConcurrentCompleteAndDispose_LogsOnlyOnce()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        var op = TimedLogOperation.Start(logger, "TestOp");
        var startLogCount = logger.LogCalls.Count; // Should be 1 (start)

        // Act - race Complete and Dispose
        var tasks = new[]
        {
            Task.Run(() => op.Complete()),
            Task.Run(() => op.Dispose())
        };
        await Task.WhenAll(tasks);

        // Assert - should have exactly 1 completion log (not 2)
        var completionLogs = logger.LogCalls.Count - startLogCount;
        completionLogs.Should().Be(1, because: "only one completion should be logged");
    }

    [Fact]
    public async Task TimedLogOperation_ConcurrentFailAndDispose_LogsOnlyOnce()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        var op = TimedLogOperation.Start(logger, "TestOp");
        var startLogCount = logger.LogCalls.Count;

        // Act - race Fail and Dispose
        var tasks = new[]
        {
            Task.Run(() => op.Fail(new Exception("Test"))),
            Task.Run(() => op.Dispose())
        };
        await Task.WhenAll(tasks);

        // Assert - should have exactly 1 completion/failure log
        var completionLogs = logger.LogCalls.Count - startLogCount;
        completionLogs.Should().Be(1);
    }

    [Fact]
    public async Task TimedLogOperation_HighConcurrency_NoRaceConditions()
    {
        // Arrange
        const int iterations = 100;
        var errors = new ConcurrentBag<string>();

        // Act
        await Parallel.ForEachAsync(
            Enumerable.Range(0, iterations),
            new ParallelOptions { MaxDegreeOfParallelism = 20 },
            async (i, ct) =>
            {
                var logger = new ThreadSafeTestLogger();

                using (var op = TimedLogOperation.Start(logger, $"Op-{i}"))
                {
                    await Task.Delay(Random.Shared.Next(1, 3), ct);

                    // Randomly choose action
                    switch (i % 4)
                    {
                        case 0:
                            op.Complete();
                            break;
                        case 1:
                            op.Fail(new Exception("Test"));
                            break;
                        case 2:
                            op.MarkFailed();
                            break;
                            // case 3: just dispose
                    }
                }

                // Verify exactly 2 logs (start + end)
                if (logger.LogCalls.Count != 2)
                {
                    errors.Add($"Op-{i}: Expected 2 logs, got {logger.LogCalls.Count}");
                }
            });

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region LoggingContext Immutability Tests

    [Fact]
    public void LoggingContext_IsImmutable_ConcurrentReadsAreSafe()
    {
        // Arrange
        var context = LoggingContextBuilder.Create()
            .WithCorrelationId("test-corr")
            .WithUserId("test-user")
            .Build();

        var results = new ConcurrentBag<string>();

        // Act - concurrent reads
        Parallel.For(0, 1000, _ =>
        {
            context.TryGet("CorrelationId", out var corrId);
            context.TryGet("UserId", out var userId);
            results.Add($"{corrId}-{userId}");
        });

        // Assert - all reads should return same values
        results.Should().AllBe("test-corr-test-user");
    }

    [Fact]
    public void LoggingContextBuilder_From_CreatesIndependentCopy()
    {
        // Arrange
        var original = LoggingContextBuilder.Create()
            .WithCorrelationId("original")
            .Build();

        // Act
        var modified = LoggingContextBuilder.From(original)
            .WithCorrelationId("modified")
            .Build();

        // Assert - original should be unchanged
        original.TryGet("CorrelationId", out var origCorrId);
        modified.TryGet("CorrelationId", out var modCorrId);

        origCorrId.Should().Be("original");
        modCorrId.Should().Be("modified");
    }

    #endregion

    #region Ambient Context Integration Tests

    [Fact]
    public void LogEntryBuilder_WithAmbientContext_UsesCurrentScope()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        var context = LoggingContextBuilder.Create()
            .WithCorrelationId("ambient-corr")
            .Build();

        // Act
        using (LoggingScope.Begin(context))
        {
            logger.Log()
                .Information()
                .Message("Test message")
                .Write();
        }

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        logger.LogCalls[0].State.Should().ContainKey("CorrelationId");
        logger.LogCalls[0].State["CorrelationId"].Should().Be("ambient-corr");
    }

    [Fact]
    public void LogEntryBuilder_WithContext_MergesWithAmbient()
    {
        // Arrange
        var logger = new ThreadSafeTestLogger();
        var ambient = LoggingContextBuilder.Create()
            .WithCorrelationId("ambient-corr")
            .WithUserId("ambient-user")
            .Build();
        var @explicit = LoggingContextBuilder.Create()
            .WithOperation("explicit-op")
            .Build();

        // Act
        using (LoggingScope.Begin(ambient))
        {
            logger.Log()
                .WithContext(@explicit)
                .Information()
                .Message("Test message")
                .Write();
        }

        // Assert - should have both ambient and explicit properties
        var state = logger.LogCalls[0].State;
        state.Should().ContainKey("CorrelationId");
        state.Should().ContainKey("UserId");
        state.Should().ContainKey("Operation");
        state["CorrelationId"].Should().Be("ambient-corr");
        state["UserId"].Should().Be("ambient-user");
        state["Operation"].Should().Be("explicit-op");
    }

    #endregion

    #region Helper Methods and Classes

    private static string? GetUserId()
    {
        if (LoggingScope.Current?.TryGet("UserId", out var userId) == true)
            return userId as string;
        return null;
    }

    /// <summary>
    /// Thread-safe test logger for concurrent tests.
    /// Uses a lock to maintain insertion order (ConcurrentBag doesn't preserve order).
    /// </summary>
    private class ThreadSafeTestLogger : ILogger
    {
        private readonly List<LogCall> _logCalls = new();
        private readonly object _lock = new();

        public IReadOnlyList<LogCall> LogCalls
        {
            get
            {
                lock (_lock)
                {
                    return _logCalls.ToList();
                }
            }
        }

        public int ErrorCount
        {
            get
            {
                lock (_lock)
                {
                    return _logCalls.Count(l => l.LogLevel == LogLevel.Error);
                }
            }
        }

        public int InfoCount
        {
            get
            {
                lock (_lock)
                {
                    return _logCalls.Count(l => l.LogLevel == LogLevel.Information);
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var stateDict = state as IDictionary<string, object>
                ?? new Dictionary<string, object>();

            lock (_lock)
            {
                _logCalls.Add(new LogCall
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    State = new Dictionary<string, object>(stateDict),
                    Exception = exception
                });
            }
        }
    }

    private class LogCall
    {
        public LogLevel LogLevel { get; init; }
        public EventId EventId { get; init; }
        public IDictionary<string, object> State { get; init; } = new Dictionary<string, object>();
        public Exception? Exception { get; init; }
    }

    #endregion
}
