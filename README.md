<p align="center">
  <img src="https://raw.githubusercontent.com/Hemmatiali/NoNameLogger/main/assets/logo.png" alt="NoNameLogger" width="256" />
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/NoNameLogger/"><img src="https://img.shields.io/nuget/v/NoNameLogger.svg" alt="NuGet" /></a>
</p>

# NoNameLogger

Lightweight, framework-agnostic logging helpers on top of `Microsoft.Extensions.Logging`.

NoNameLogger does **not** replace your logging provider (Serilog, Console, Seq, Application Insights, etc.).
Instead, it provides:

- A **fluent API** for building structured log entries.
- A reusable, **immutable logging context** abstraction.
- Centralized, strongly-typed **log events** and **message templates** (generic, not domain-specific).
- Simple helpers for **timed operations** and common patterns.

This makes your logging:

- Easier to read and write.
- More consistent across projects.
- Safer to refactor (events/messages live in one place).

---

## Table of Contents

- [1. Project Structure](#1-project-structure)
  - [1.1 Abstractions](#11-abstractions)
  - [1.2 Context](#12-context)
  - [1.3 Core](#13-core)
  - [1.4 Conventions](#14-conventions)
  - [1.5 Enricher](#15-enricher)
  - [1.6 Samples](#16-samples)
- [2. Requirements](#2-requirements)
- [3. Getting Started](#3-getting-started)
  - [3.1 Installation](#31-installation)
  - [3.2 Basic context and fluent logging](#32-basic-context-and-fluent-logging)
  - [3.3 Using shortcut extension methods](#33-using-shortcut-extension-methods)
  - [3.4 Timed operations](#34-timed-operations)
  - [3.5 Ambient logging context (LoggingScope)](#35-ambient-logging-context-loggingscope)
- [4. Architecture](#4-architecture)
  - [4.1 State Object and Structured Logging](#41-state-object-and-structured-logging)
  - [4.2 Performance Considerations](#42-performance-considerations)
- [5. Extending the Framework](#5-extending-the-framework)
  - [5.1 Project-Specific Context Helpers](#51-project-specific-context-helpers)
  - [5.2 Project-Specific Events and Messages](#52-project-specific-events-and-messages)
  - [5.3 Custom Wrappers](#53-custom-wrappers)
- [6. Integration with Serilog](#6-integration-with-serilog)
  - [6.1 Basic Setup](#61-basic-setup)
  - [6.2 Using Enrichers](#62-using-enrichers)
- [7. Best Practices](#7-best-practices)
- [8. Troubleshooting](#8-troubleshooting)
- [9. Contributing](#9-contributing)
- [10. Summary](#10-summary)

---

## 1. Project Structure

The library is organized into a few small namespaces and files:

```text
src/NoNameLogger
└─ Application
   ├─ Logging
   │  ├─ Abstractions
   │  │  └─ ILoggingContext.cs
   │  │
   │  ├─ Context
   │  │  ├─ LoggingContext.cs
   │  │  ├─ LoggingContextBuilder.cs
   │  │  ├─ LoggingContextKeys.cs
   │  │  ├─ LoggingScope.cs
   │  │  └─ LoggingScopeExtensions.cs
   │  │
   │  ├─ Core
   │  │  ├─ LogEntryBuilder.cs
   │  │  ├─ LoggerExtensions.cs
   │  │  └─ TimedLogOperation.cs
   │  │
   │  └─ Conventions
   │     ├─ CommonLogEvents.cs
   │     └─ CommonLogMessages.cs
   │
   └─ Enricher
      ├─ ConsolePropertyFilterEnricher.cs
      └─ LoggingScopeEnricher.cs

samples/NoNameLogger.Demo
└─ Demonstrates basic usage patterns (console app)
```

> Note: Demo and runnable samples live under `samples/` to keep the NuGet package clean and domain-agnostic.

### 1.1 Abstractions

- **`ILoggingContext`**
  - Immutable key/value bag that can be attached to any log entry.
  - Exposes `IReadOnlyDictionary<string, object?> Properties` and `TryGet`.

### 1.2 Context

- **`LoggingContext`**
  - Concrete implementation of `ILoggingContext`.
  - Stores data internally in a `FrozenDictionary<string, object?>` for performance and thread-safety.

- **`LoggingContextBuilder`**
  - Fluent builder for constructing `LoggingContext` instances.
  - You can add arbitrary keys via `With(key, value)` / `WithMany(...)`.
  - Includes convenience methods for common keys:
    - `WithEnvironment`, `WithApplication`, `WithService`, `WithMachineName`
    - `WithUserId`, `WithUserName`, `WithTenant`
    - `WithCorrelationId`, `WithRequestId`, `WithSessionId`
    - `WithOperation`, `WithOperationId`
    - `WithHttpMethod`, `WithPath`, `WithRoute`, `WithStatusCode`, `WithClientIp`

- **`LoggingContextKeys`**
  - String constants for the well-known property names above.
  - Helps keep keys consistent across a codebase.

- **`LoggingScope`**
  - Provides ambient (async-local) storage for `ILoggingContext`.
  - Establish a context at the entry point of a request/job so all logs in that async flow can use it.
  - Key members:
    - `Current` – Gets the current ambient context (or `null` if none).
    - `HasCurrent` – Returns `true` if an ambient context is active.
    - `Begin(ILoggingContext context)` – Starts a new scope; returns `IDisposable` that restores the previous context on dispose.
    - `BeginWithProperties(...)` – Starts a nested scope by adding properties to the current context.
    - `GetEffectiveContext(ILoggingContext? explicitContext)` – Merges ambient and explicit contexts.

- **`LoggingScopeExtensions`**
  - Helper extension methods for working with ambient contexts:
    - `WithProperties(...)` – Creates a new context by cloning and adding properties.
    - `MergeWith(...)` – Merges two contexts.
    - `BuilderFromCurrent()` – Gets a builder initialized from the current ambient context.
    - `PushProperties(...)` – Starts a nested scope with additional properties.

### 1.3 Core

- **`LogEntryBuilder`**
  - Fluent builder for constructing a structured log entry and writing it to `ILogger`.
  - Lets you set:
    - Level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.
    - `EventId` via `.Event(EventId)` or `.Event(int id, string? name = null)`.
    - Message template and arguments via `.Message("...", args...)`.
    - Exception via `.Exception(ex)`.
    - Context via `.WithContext(ILoggingContext)`.
    - Additional properties via `.WithProperty(key, value)` / `.WithProperties(...)`.
  - Call `.Write()` at the end to actually log.
  - Internally builds a `Dictionary<string, object?>` state containing:
    - Context properties.
    - Custom properties.
    - The message template under `{OriginalFormat}` so providers can do structured logging.

- **`LoggerExtensions`**
  - Adds extensions on top of `ILogger`:
    - `Log(this ILogger logger)` → starts a fluent `LogEntryBuilder`.
    - `Log(this ILogger logger, ILoggingContext context)` → same, but with a context attached.
    - Shortcut methods:
      - `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical`.
      - Each takes: `EventId`, message template, optional `ILoggingContext`, optional `Exception`, optional extra properties, and `params args`.

- **`TimedLogOperation`**
  - Small helper that measures operation duration and logs start/completion/failure.

### 1.4 Conventions

- **`CommonLogEvents`**
  - Generic `EventId` sets grouped by category:
    - `System`, `Http`, `Database`, `Operation`, `Cache`, `Security`, `Business`,
      `FileSystem`, `Integration`, `Payload`, `Jobs`
  - Each category exposes several ready-made `EventId` values (see the source for the full list).
  - You can use them as-is or define your own equivalents.

- **`CommonLogMessages`**
  - Generic message templates for the most common events above
    (`System`, `Http`, `Database`, `Operation`, `Cache`, `Security`, `Business`).
  - Not every `EventId` has a matching template — use the ones provided, or supply your own message string to any log call.
  - Example:

    ```csharp
    public static class Operation
    {
        public const string Started = "Operation {OperationName} started";
        public const string Completed = "Operation {OperationName} completed in {ElapsedMs} ms";
        public const string Failed = "Operation {OperationName} failed in {ElapsedMs} ms. Error: {ErrorMessage}";
    }
    ```

### 1.5 Enricher

- **`LoggingScopeEnricher`**
  - Serilog enricher that automatically adds properties from `LoggingScope.Current` to log events.
  - Bridges ambient logging context with Serilog structured logging.

- **`ConsolePropertyFilterEnricher`**
  - Serilog enricher that filters out specified properties from console output.

### 1.6 Samples

Runnables live in `samples/NoNameLogger.Demo`.

- Demonstrates:
  - explicit context + fluent logging
  - ambient context via `LoggingScope`
  - timed operations
  - error handling patterns

---

## 2. Requirements

- **.NET**: .NET 8.0, .NET 9.0, or .NET 10.0 (multi-targeted)
- **Dependencies**:
  - `Microsoft.Extensions.Logging.Abstractions` (9.0.0)
  - `Serilog` (4.2.0) — required by the built-in enrichers
- **Providers**: Works with any provider that integrates with `ILogger` (Serilog, NLog, Seq, Application Insights, etc.)

NoNameLogger extends `ILogger` without replacing your existing logging infrastructure.

---

## 3. Getting Started

### 3.1 Installation

Install the NuGet package:

```bash
dotnet add package NoNameLogger
```

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

You can attach extra properties directly:

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

### 3.5 Ambient logging context (LoggingScope)

Instead of passing `ILoggingContext` through every method, establish an **ambient context** at the entry point of a request/job.

#### Starting a scope

```csharp
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
        return await _orderService.GetOrderAsync(orderId);
    }
}
```

#### Logging without passing context

```csharp
_logger.Log()
    .Information()
    .Event(CommonLogEvents.Operation.Started)
    .Message("Fetching order {OrderId}", orderId)
    .Write();
```

#### Adding extra properties in nested calls

```csharp
using (LoggingScope.BeginWithProperties(("ProductId", productId)))
{
    _logger.LogInformation(CommonLogEvents.Operation.Started, "Checking inventory", context: null);
}
```

---

## 4. Architecture

### 4.1 State Object and Structured Logging

NoNameLogger builds a `Dictionary<string, object?>` as the logging state containing:

- Context properties
- Additional properties
- Message template under `{OriginalFormat}`

Providers like Serilog recognize `{OriginalFormat}` and the state dictionary to produce structured logs.

### 4.2 Performance Considerations

- `LoggingContext` is immutable and uses `FrozenDictionary` for thread-safety and performance
- Builders are short-lived
- Minimal runtime overhead while providing a rich API

---

## 5. Extending the Framework

NoNameLogger is intentionally domain-agnostic. Common extension patterns:

### 5.1 Project-Specific Context Helpers

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

### 5.3 Custom Wrappers

Create wrappers for repeated patterns (API endpoints, background jobs, etc.) that prefill context, events, or messages.

---

## 6. Integration with Serilog

When using Serilog as the provider for `ILogger`, the state dictionary and `{OriginalFormat}` are recognized automatically.

### 6.1 Basic Setup

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

### 6.2 Using Enrichers

To automatically include ambient context properties in all log events:

```csharp
using NoNameLogger.Application.Enricher;

Log.Logger = new LoggerConfiguration()
    .Enrich.With<LoggingScopeEnricher>()
    .Enrich.With<ConsolePropertyFilterEnricher>()
    .WriteTo.Console()
    .CreateLogger();
```

> The enrichers live in the `NoNameLogger.Application.Enricher` namespace (separate from the namespaces in §3.1).
> The `.WriteTo.Console()` sink shown here requires the **`Serilog.Sinks.Console`** package, which is not a
> NoNameLogger dependency — add it (and any other sinks you use) to your application project.

---

## 7. Best Practices

- Keep context keys consistent (use `LoggingContextKeys` where possible)
- Centralize events and message templates (avoid scattering `EventId` and templates)
- Add properties intentionally (prioritize diagnostics value)
- Wrap critical flows in `TimedLogOperation`
- Keep domain-specific helpers in application projects, not in the core library

---

## 8. Troubleshooting

### Context properties not appearing in logs

- Ensure you pass context: `logger.Log(context)...` or `LogInformation(..., context: context, ...)`
- If using ambient context, ensure `LoggingScope.Begin(context)` is active
- For Serilog, use `LoggingScopeEnricher` to enrich ambient context properties

### Empty message text

- Ensure `.Message(...)` is called before `.Write()`

---

## 9. Contributing

Contributions are welcome.

### Commit Message Format

Use Conventional Commits:

```
<type>(<scope>): <description>
```

Examples:

```
feat(enricher): add LoggingScopeEnricher for automatic context enrichment
fix(context): handle null context in GetEffectiveContext
refactor(core): extract state building logic to separate method
chore(docs): update README with demo usage
```

---

## 10. Summary

NoNameLogger enhances `Microsoft.Extensions.Logging` by providing:

- Fluent structured logging via `LogEntryBuilder`
- Immutable `ILoggingContext` for consistent properties
- Centralized `EventId` sets and message templates
- Ambient context support via `LoggingScope`
- Optional Serilog enrichers for automatic context enrichment

Drop it into any .NET application using `ILogger` to improve logging structure, consistency, and maintainability.

