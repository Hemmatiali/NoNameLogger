# NoNameLogger

Lightweight, framework-agnostic logging helpers on top of `Microsoft.Extensions.Logging`.

NoNameLogger does **not** replace your logging provider (Serilog, Console, Seq, Application Insights, etc.).
Instead, it gives you:

* A **fluent API** for building structured log entries.
* A reusable, **immutable logging context** abstraction.
* Centralized, strongly-typed **log events** and **message templates** (generic, not domain-specific).
* Simple helpers for **timed operations** and common patterns.

This makes your logging:

* Easier to read and write.
* More consistent across projects.
* Safer to refactor (events/messages live in one place).

---

## 1. Project Structure

The library is organized into a few small namespaces and files:

```text
NoNameLogger
├─ Abstractions
│  └─ ILoggingContext.cs
│
├─ Context
│  ├─ LoggingContext.cs
│  ├─ LoggingContextBuilder.cs
│  ├─ LoggingContextKeys.cs
│  ├─ LoggingScope.cs
│  └─ LoggingScopeExtensions.cs
│
├─ Core
│  ├─ LogEntryBuilder.cs
│  ├─ LoggerExtensions.cs
│  └─ TimedLogOperation.cs
│
├─ Conventions
│  ├─ CommonLogEvents.cs
│  └─ CommonLogMessages.cs
│
├─ Enricher
│  ├─ ConsolePropertyFilterEnricher.cs
│  └─ LoggingScopeEnricher.cs
│
└─ Samples (optional, debug only)
   ├─ AppLoggingContext.cs
   └─ LoggingSamples.cs
```

### 1.1 Abstractions

* **`ILoggingContext`**

  * Immutable key/value bag that can be attached to any log entry.
  * Exposes `IReadOnlyDictionary<string, object?> Properties` and `TryGet`.

### 1.2 Context

* **`LoggingContext`**

  * Concrete implementation of `ILoggingContext`.
  * Stores data internally in a `FrozenDictionary<string, object?>` for performance and thread-safety.

* **`LoggingContextBuilder`**

  * Fluent builder for constructing `LoggingContext` instances.
  * You can add arbitrary keys via `With(key, value)` / `WithMany(...)`.
  * Includes convenience methods for common keys:

    * `WithEnvironment`, `WithApplication`, `WithService`, `WithMachineName`
    * `WithUserId`, `WithUserName`, `WithTenant`
    * `WithCorrelationId`, `WithRequestId`, `WithSessionId`
    * `WithOperation`, `WithOperationId`
    * `WithHttpMethod`, `WithPath`, `WithRoute`, `WithStatusCode`, `WithClientIp`

* **`LoggingContextKeys`**

  * String constants for the well-known property names above.
  * Helps to keep your keys consistent across the app.

* **`LoggingScope`**

  * Provides ambient (async-local) storage for `ILoggingContext`.
  * Allows you to establish a logging context at the entry point of a request or job so all logging calls in that async flow automatically use the context.
  * Key members:
    * `Current` – Gets the current ambient context (or `null` if none).
    * `HasCurrent` – Returns `true` if an ambient context is active.
    * `Begin(ILoggingContext context)` – Starts a new scope; returns `IDisposable` that restores the previous context when disposed.
    * `BeginWithProperties(...)` – Creates a new scope by adding properties to the current context.
    * `GetEffectiveContext(ILoggingContext? explicitContext)` – Merges ambient and explicit contexts.

* **`LoggingScopeExtensions`**

  * Helper extension methods for working with ambient contexts:
    * `WithProperties(...)` – Creates a new context by cloning and adding properties.
    * `MergeWith(...)` – Merges two contexts.
    * `BuilderFromCurrent()` – Gets a builder initialized from the current ambient context.
    * `PushProperties(...)` – Starts a nested scope with additional properties.

### 1.3 Core

* **`LogEntryBuilder`**

  * Fluent builder for constructing a structured log entry and writing it to `ILogger`.
  * Lets you set:

    * Level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.
    * `EventId` via `.Event(EventId)` or `.Event(int id, string? name = null)`.
    * Message template and arguments via `.Message("...", args...)`.
    * Exception via `.Exception(ex)`.
    * Context via `.WithContext(ILoggingContext)`.
    * Additional properties via `.WithProperty(key, value)` / `.WithProperties(...)`.
  * Call `.Write()` at the end to actually log.
  * Internally builds a `Dictionary<string, object?>` state that contains:

    * All context properties.
    * All custom properties.
    * The message template under `{OriginalFormat}` so providers (like Serilog) can do structured logging.

* **`LoggerExtensions`**

  * Adds extensions on top of `ILogger`:

    * `Log(this ILogger logger)` → starts a fluent `LogEntryBuilder`.
    * `Log(this ILogger logger, ILoggingContext context)` → same but with a context attached.
    * Typed helpers:

      * `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical`.
      * Each takes:

        * `EventId eventId`
        * `string messageTemplate`
        * optional `ILoggingContext context`
        * optional `Exception exception`
        * optional extra `properties` (key/value pairs)
        * params `args` used for the message template.
    * All of these helpers delegate to a single `LogCore` method to avoid duplication.

* **`TimedLogOperation`**

  * Small helper that measures an operation duration and logs start/completion/failure events.

  * Usage:

    ```csharp
    using var op = TimedLogOperation.Start(
        logger,
        operationName: "ProcessPayment",
        startEvent: CommonLogEvents.Operation.Started,
        completedEvent: CommonLogEvents.Operation.Completed,
        failedEvent: CommonLogEvents.Operation.Failed,
        context: context);

    // your code here

    op.Complete();
    ```

  * If `Complete()` is called, a `Completed` event is logged with elapsed milliseconds.

  * If `Fail(ex)` is called, a `Failed` event is logged with the exception.

  * If only `MarkFailed()` is called and then disposed, it logs `Failed` without exception.

### 1.4 Conventions

* **`CommonLogEvents`**

  * Generic `EventId` sets grouped by category:

    * `System` (startup, shutdown, configuration, ...)
    * `Http` (request received, completed, failed, ...)
    * `Database` (command executing, executed, failed, ...)
    * `Operation` (started, completed, failed)
    * `Cache` (hit, miss, set, remove)
    * `Security` (unauthorized, forbidden, login, suspicious activity)
    * `Business` (validation failed, rule violated, state changed)
  * You can:

    * Use them as-is.
    * Extend via partial classes in your own project.
    * Ignore them and define your own events elsewhere.

* **`CommonLogMessages`**

  * Generic message templates matching the events above.
  * Example:

    ```csharp
    public static class Operation
    {
        public const string Started = "Operation {OperationName} started";
        public const string Completed = "Operation {OperationName} completed in {ElapsedMs} ms";
        public const string Failed = "Operation {OperationName} failed in {ElapsedMs} ms. Error: {ErrorMessage}";
    }
    ```

### 1.5 Enricher

* **`LoggingScopeEnricher`**

  * Serilog enricher that automatically adds properties from `LoggingScope.Current` to log events.
  * Bridges the ambient logging context with Serilog's structured logging.
  * Excludes internal keys (`{OriginalFormat}`, `EventId`, `EventName`) to avoid duplication.

* **`ConsolePropertyFilterEnricher`**

  * Serilog enricher that filters out specified properties from console output.
  * Removes common properties (`Application`, `Environment`, `SourceContext`) for cleaner console logs.

### 1.6 Samples

* **`AppLoggingContext`**

  * Helper class for creating logging contexts with application, service, and operation pre-populated.
  * Provides convenience methods `For<TService>()` and `ForBuilder<TService>()` for common patterns.

* **`LoggingSamples`** (under `#if DEBUG`)

  * Contains runnable examples for basic usage and timed operations.
  * Demonstrates common patterns and integration approaches.

---

## 2. Requirements

* **.NET**: .NET 6.0 or later
* **Dependencies**: `Microsoft.Extensions.Logging.Abstractions`
* **Providers**: Compatible with any logging provider that integrates with `ILogger` (Serilog, NLog, Application Insights, Seq, etc.)

NoNameLogger extends `ILogger` without replacing your existing logging infrastructure.

---

## 3. Getting Started

### 3.1 Installation

Add a project reference or install the NuGet package into your application.

Required namespaces:

```csharp
using Microsoft.Extensions.Logging;
using NoNameLogger.Application.Logging.Abstractions;
using NoNameLogger.Application.Logging.Context;
using NoNameLogger.Application.Logging.Core;
using NoNameLogger.Application.Logging.Conventions;
```

### 3.2 Basic context and fluent logging

```csharp
public class SampleService
{
    private readonly ILogger<SampleService> _logger;

    public SampleService(ILogger<SampleService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        var context = LoggingContextBuilder
            .Create()
            .WithEnvironment("Production")
            .WithApplication("OrderApi")
            .WithUserId("user-123")
            .WithCorrelationId("corr-456")
            .Build();

        _logger.Log(context)
               .Information()
               .Event(CommonLogEvents.Http.RequestReceived)
               .Message(CommonLogMessages.Http.RequestStarted)
               .WithProperty("Path", "/api/orders")
               .WithProperty("Method", "GET")
               .Write();
    }
}
```

### 3.3 Using shortcut extension methods

```csharp
_logger.LogInformation(
    CommonLogEvents.System.StartupCompleted,
    CommonLogMessages.System.StartupCompleted,
    context: context);
```

All levels (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`) have the same signature.

You can also attach extra properties directly:

```csharp
_logger.LogError(
    CommonLogEvents.Business.ValidationFailed,
    CommonLogMessages.Business.ValidationFailed,
    context: context,
    properties: new []
    {
        new KeyValuePair<string, object?>("Entity", "Order"),
        new KeyValuePair<string, object?>("Reason", "Missing customer id"),
    });
```

### 3.4 Timed operations

```csharp
public async Task ProcessPaymentAsync(ILoggingContext context)
{
    using var op = TimedLogOperation.Start(
        _logger,
        operationName: "ProcessPayment",
        startEvent: CommonLogEvents.Operation.Started,
        completedEvent: CommonLogEvents.Operation.Completed,
        failedEvent: CommonLogEvents.Operation.Failed,
        context: context);

    try
    {
        // your logic
        await Task.Delay(250);

        op.Complete();
    }
    catch (Exception ex)
    {
        op.Fail(ex);
        throw;
    }
}
```

This will produce:

* A "started" log when the operation begins.
* A "completed" log with elapsed milliseconds on success.
* A "failed" log with elapsed time and error message on failure.

### 3.5 Ambient logging context (LoggingScope)

Instead of passing `ILoggingContext` through every method, you can establish an **ambient context** at the entry point of a request or job. All logging calls within that async flow will automatically use the context.

#### Starting a scope at the entry point

```csharp
// In a controller action, middleware, or background job entry point:
public async Task<IActionResult> GetOrderAsync(string orderId)
{
    var context = LoggingContextBuilder
        .Create()
        .WithCorrelationId(Guid.NewGuid().ToString())
        .WithUserId(User.Identity?.Name ?? "anonymous")
        .WithOperation("GetOrder")
        .With("OrderId", orderId)
        .Build();

    using (LoggingScope.Begin(context))
    {
        // All logging in this block (and any async calls) will automatically
        // include the context properties without passing context explicitly.
        
        return await _orderService.GetOrderAsync(orderId);
    }
}
```

#### Logging without passing context

Once a scope is active, logging methods automatically pick up the ambient context:

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task<Order> GetOrderAsync(string orderId)
    {
        // No need to accept ILoggingContext as a parameter!
        // The ambient context is used automatically.
        
        _logger.Log()
            .Information()
            .Event(CommonLogEvents.Operation.Started)
            .Message("Fetching order {OrderId}", orderId)
            .Write();

        // ... fetch order ...

        return order;
    }
}
```

#### Adding extra properties in nested calls

Lower-level services can add extra properties to the current context without rebuilding everything:

```csharp
public async Task<InventoryStatus> CheckInventoryAsync(string productId)
{
    // Option 1: Use BeginWithProperties to start a nested scope with extra properties
    using (LoggingScope.BeginWithProperties(("ProductId", productId)))
    {
        _logger.LogInformation(
            CommonLogEvents.Operation.Started,
            "Checking inventory for product {ProductId}");
        
        // All logs in this block include ProductId in addition to the parent context
    }

    // Option 2: Use PushProperties helper (same effect)
    using (LoggingScopeExtensions.PushProperties(("ProductId", productId)))
    {
        // ...
    }
}
```

#### Merging explicit and ambient contexts

If you pass an explicit context to a logging method while an ambient context is active, the two are merged (explicit overrides ambient):

```csharp
var extraContext = LoggingContextBuilder
    .Create()
    .With("PaymentMethod", "CreditCard")
    .Build();

// This merges the ambient context with extraContext
_logger.Log(extraContext)
    .Information()
    .Message("Processing payment")
    .Write();
```

#### Backward compatibility

If no scope is active, logging works exactly as before. The ambient context feature is fully opt-in and backward compatible.

---

## 4. Architecture

### 4.1 State Object and Structured Logging

Both `LogEntryBuilder` and `LoggerExtensions` build a `Dictionary<string, object?>` as the logging state containing:

* Context properties
* Additional properties
* Message template under `{OriginalFormat}` key
* Optional message arguments array

All logging goes through the standard `ILogger.Log` method. Providers like Serilog recognize `{OriginalFormat}` and the state dictionary to produce structured logs.

### 4.2 Performance Considerations

* `LoggingContext` is immutable and uses `FrozenDictionary` for thread-safety and performance
* Builders are short-lived and stack-allocated
* Minimal runtime overhead while providing a rich API

---

## 5. Extending the Framework

NoNameLogger is intentionally domain-agnostic. Common extension patterns:

### 5.1 Project-Specific Context Helpers

Create static helpers that wrap `LoggingContextBuilder` for your domain:

```csharp
public static class BookingContextBuilder
{
    public static ILoggingContext CreateForBooking(string bookingId, string userId)
    {
        return LoggingContextBuilder
            .Create()
            .WithApplication("BookingApi")
            .WithUserId(userId)
            .With("BookingId", bookingId)
            .Build();
    }
}
```

### 5.2 Project-Specific Events and Messages

Define custom event and message sets:

```csharp
public static class BookingLogEvents
{
    public static readonly EventId BookingCreated = new(8000, nameof(BookingCreated));
    public static readonly EventId BookingCancelled = new(8001, nameof(BookingCancelled));
}

public static class BookingLogMessages
{
    public const string BookingCreated = "Booking {BookingId} created for user {UserId}";
    public const string BookingCancelled = "Booking {BookingId} cancelled";
}
```

Use them with the standard APIs:

```csharp
_logger.LogInformation(
    BookingLogEvents.BookingCreated,
    BookingLogMessages.BookingCreated,
    context: context);
```

### 5.3 Custom Wrappers

Create wrapper methods for repeated patterns (e.g., API endpoints, background jobs) that prefill context, events, or messages.

---

## 6. Integration with Serilog

When using Serilog as the provider for `ILogger`, the state dictionary and `{OriginalFormat}` are automatically recognized. All context and extra properties become structured properties in Serilog.

### 6.1 Basic Setup

No special integration is required. Register Serilog as usual and use NoNameLogger helpers:

```csharp
_logger.Log(context)
       .Information()
       .Event(CommonLogEvents.Http.RequestCompleted)
       .Message(CommonLogMessages.Http.RequestCompleted)
       .WithProperty("Path", path)
       .WithProperty("Method", method)
       .WithProperty("ElapsedMs", elapsedMs)
       .Write();
```

This produces a structured Serilog event with properties from the context and additional properties.

### 6.2 Using Enrichers

To automatically include ambient context properties in all log events, register `LoggingScopeEnricher`:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.With<LoggingScopeEnricher>()
    .Enrich.With<ConsolePropertyFilterEnricher>() // Optional: filter console output
    .WriteTo.Console()
    .CreateLogger();
```

The `LoggingScopeEnricher` automatically enriches all log events with properties from the current `LoggingScope`, eliminating the need to pass context explicitly to every logging call.

---

## 7. Best Practices

* **Consistent Context Usage**: Define a minimal set of standard properties (environment, application, user, correlation ID) and include them consistently across all logging calls.

* **Centralized Events and Messages**: Use `CommonLogEvents` and `CommonLogMessages` or define your own equivalents. Avoid hardcoding event IDs and templates throughout the codebase.

* **Selective Property Addition**: Add only properties that provide value for debugging or analytics. Avoid cluttering logs with unnecessary data.

* **Timed Operations**: Wrap business-critical flows in `TimedLogOperation` to automatically capture timing metrics.

* **Domain-Specific Extensions**: Keep the framework domain-agnostic. Add domain-specific helpers in your application projects, not in the core library.

---

## 8. Troubleshooting

* **Context properties not appearing in logs**

  * Ensure `ILoggingContext` is passed to logging methods: `logger.Log(context)...` or `logger.LogInformation(..., context: context, ...)`
  * If using ambient context, verify `LoggingScope.Begin(context)` is called and the scope is active
  * When using Serilog, register `LoggingScopeEnricher` to automatically include ambient context

* **Empty message text**

  * Verify `.Message(...)` is called before `.Write()` in the fluent API
  * If `.Message(...)` is omitted, `LogEntryBuilder` generates a default message based on `EventId`

* **Compiler errors**

  * Ensure your project targets .NET 6.0 or later
  * Verify all required NuGet packages are installed

---

## 9. Summary

NoNameLogger enhances your existing logging infrastructure by providing:

* A fluent API for building structured log entries
* Immutable logging context abstraction for consistent property management
* Centralized event IDs and message templates
* Ambient context support via `LoggingScope`
* Integration with Serilog through custom enrichers

Drop it into any .NET application using `Microsoft.Extensions.Logging` to improve logging structure, consistency, and maintainability.
