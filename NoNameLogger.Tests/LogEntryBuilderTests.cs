using FluentAssertions;
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Core;

namespace NoNameLogger.Tests;

//todo: xml comment
public class LogEntryBuilderTests
{
    [Fact]
    public void Write_WithDefaultEventIdAndCustomMessageTemplate_ShouldUseCustomTemplate()
    {
        // Arrange
        var logger = new TestLogger();
        var builder = new LogEntryBuilder(logger)
            .Information()
            .Message("Custom message {Value}", "test-value");

        // Act
        builder.Write();

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        var logCall = logger.LogCalls[0];
        logCall.State.Should().ContainKey("{OriginalFormat}");
        logCall.State["{OriginalFormat}"].Should().Be("Custom message {Value}");
    }

    [Fact]
    public void Write_WithNonDefaultEventIdAndNullMessageTemplate_ShouldUseDefaultTemplate()
    {
        // Arrange
        var logger = new TestLogger();
        var eventId = new EventId(1000, "TestEvent");
        var builder = new LogEntryBuilder(logger)
            .Information()
            .Event(eventId);

        // Act
        builder.Write();

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        var logCall = logger.LogCalls[0];
        logCall.State.Should().ContainKey("{OriginalFormat}");
        logCall.State["{OriginalFormat}"].Should().Be("Log entry for event '{EventId}' ({EventName}).");
        logCall.EventId.Should().Be(eventId);
    }

    [Fact]
    public void Write_WithNonDefaultEventIdAndEmptyMessageTemplate_ShouldUseDefaultTemplate()
    {
        // Arrange
        var logger = new TestLogger();
        var eventId = new EventId(1000, "TestEvent");
        var builder = new LogEntryBuilder(logger)
            .Information()
            .Event(eventId)
            .Message("");

        // Act
        builder.Write();

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        var logCall = logger.LogCalls[0];
        logCall.State.Should().ContainKey("{OriginalFormat}");
        logCall.State["{OriginalFormat}"].Should().Be("Log entry for event '{EventId}' ({EventName}).");
        logCall.EventId.Should().Be(eventId);
    }

    [Fact]
    public void Write_WithNonDefaultEventIdAndCustomMessageTemplate_ShouldUseCustomTemplate()
    {
        // Arrange
        var logger = new TestLogger();
        var eventId = new EventId(1000, "TestEvent");
        var builder = new LogEntryBuilder(logger)
            .Information()
            .Event(eventId)
            .Message("Custom message {Value}", "test-value");

        // Act
        builder.Write();

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        var logCall = logger.LogCalls[0];
        logCall.State.Should().ContainKey("{OriginalFormat}");
        logCall.State["{OriginalFormat}"].Should().Be("Custom message {Value}");
        logCall.EventId.Should().Be(eventId);
    }

    [Fact]
    public void Write_WithDefaultEventIdAndNoMessageTemplate_ShouldUseDefaultNoMessageTemplate()
    {
        // Arrange
        var logger = new TestLogger();
        var builder = new LogEntryBuilder(logger)
            .Information();

        // Act
        builder.Write();

        // Assert
        logger.LogCalls.Should().HaveCount(1);
        var logCall = logger.LogCalls[0];
        logCall.State.Should().ContainKey("{OriginalFormat}");
        logCall.State["{OriginalFormat}"].Should().Be("Log entry with no message.");
    }

    private class TestLogger : ILogger
    {
        public List<LogCall> LogCalls { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotImplementedException();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var stateDict = state as IDictionary<string, object>;
            LogCalls.Add(new LogCall
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = stateDict ?? new Dictionary<string, object>(),
                Exception = exception
            });
        }
    }

    private class LogCall
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public IDictionary<string, object> State { get; set; } = new Dictionary<string, object>();
        public Exception? Exception { get; set; }
    }
}

