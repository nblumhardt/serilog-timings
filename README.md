# Serilog Timings [![Build status](https://ci.appveyor.com/api/projects/status/hkb5woe4ek7im249/branch/master?svg=true)](https://ci.appveyor.com/project/NicholasBlumhardt/serilog-timings/branch/master)

Extends Serilog with timed operations. Built with some specific requirements in mind:

 * One operation produces exactly one log event (events are raised at the completion of an operation)
 * Natural and fully-templated messages
 * Events for a single operation have a single event type, across both success and failure cases (only the logging level and `Outcome` properties change)

This keeps noise in the log to a minimum, and makes it easy to extract and manipulate timing 
information on a per-operation basis.

Serilog's support for structured data makes it a great way to collect timing information. It's easy 
to get started with in development, because the timings are printed to the same output as other
log messages (the console, files, etc.) so a metrics server doesn't have to be available all the time.

### Installation

The library is published as _SerilogTimings_ on NuGet.

```powershell
Install-Package SerilogTimings
```

.NET 4.5+ and .NET Core are supported.

### Getting started

Before your timings will go anywhere, [install and configure Serilog](http://serilog.net).

Types are in the `SerilogTimings` namespace.

```csharp
using SerilogTimings;
```

The simplest use case is to time an operation, without explicitly recording success/failure:

```csharp
using (Operation.Time("Submitting payment for {OrderId}", order.Id))
{
    // Timed block of code goes here
}
```

At the completion of the `using` block, a message will be written to the log like:

```
[INF] Submitting payment for order-12345 completed in 456 ms
```

The operation description passed to `Time()` is a message template; the event written to the log
extends it with `" {Outcome} in {Elapsed} ms"`.

 * All events raised by SerilogTimings carry an `Elapsed` property in milliseconds
 * `Outcome` will always be `"completed"` when the `Time()` method is used

All of the properties from the description, plus the outcome and timing, will be recorded as
first-class properties on the log event.

Operations that can either _succeed or fail_, or _that produce a result_, can be created with
`Operation.Begin()`:

```csharp
using (var op = Operation.Begin("Retrieving orders for {CustomerId}", customer.Id)
{
	// Timed block of code goes here

	op.Complete();
}
```

Using `op.Complete()` will produce the same kind of result as in the first example:

```
[INF] Retrieving orders for customer-67890 completed in 7 ms
```

Additional methods on `Operation` allow more detailed results to be captured:

```csharp
    op.Complete("Rows", orders.Rows.Length);
```

This will not change the text of the log message, but the property `Rows` will be attached to it for
later filtering and analysis.

If the operation is not completed by calling `Complete()`, it is assumed to have failed and a
warning-level event will be written to the log instead:

```
[WRN] Retrieving orders for customer-67890 abandoned in 1234 ms
```

In this case the `Outcome` property will be `"abandoned"`.

To suppress this message, for example when an operation turns out to be inapplicable, use
`op.Cancel()`. Once `Cancel()` has been called, no event will be written by the operation on
either completion or abandonment.

### Use with `ILogger`

If a contextual `ILogger` is available, the extensions methods `TimeOperation()` and
`BeginOperation()` can be used to write operation timings through it:

```csharp
using (logger.TimeOperation("Submitting payment for {OrderId}", order.Id))
{
    // Timed block of code goes here
}
```

These otherwise behave identically to `Operation.Time()` and `Operation.Begin()`.

### Leveling

Timings are most useful in production, so timing events are recorded at the `Information` level and
higher, which should generally be collected all the time.

### Precision

All timings are recorded in milliseconds. The library assumes that if you're writing the kind of
code that needs sub-millisecond timings, you'll want to use `Stopwatch` directly for tighter control.

### `LogContext` support

If your application enables the Serilog `LogContext` feature using `Enrich.FromLogContext()` on
the `LoggerConfiguration`, _SerilogTimings_ will add an `OperationId` property to all events inside
timing blocks automatically.

This is **highly recommended**, because it makes it much easier to trace from a timing result back 
through the operation that raised it.

### Caveats

One important usage note: because the event is not written until the completion of the `using` block
(or call to `Complete()`), arguments to `Begin()` or `Time()` are not captured until then; don't
pass parameters to these methods that mutate during the operation.

### How does this relate to SerilogMetrics?

[SerilogMetrics](https://github.com/serilog-metrics/serilog-metrics) is a mature metrics solution
for Serilog that includes timings as well as counters, gauges and more. _SerilogTimings_ is an 
alternative implementaion of timings only, designed with some different stylistic preferences and
goals. You should definitely check out SerilogMetrics as well, to see if it's more to your tastes!
