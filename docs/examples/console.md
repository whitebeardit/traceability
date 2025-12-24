# Examples - Console Application

Practical examples of using Traceability in console applications.

## Basic Example

```csharp
using Traceability;

// Correlation-id is automatically generated when needed
var correlationId = CorrelationContext.Current;

// Use in logs, HTTP calls, etc.
Console.WriteLine($"Correlation ID: {correlationId}");
```

## Example with Serilog

```csharp
using Traceability.Extensions;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// Generate correlation-id
var correlationId = CorrelationContext.GetOrCreate();

// Logs automatically include correlation-id
Log.Information("Processing task");
Log.Information("Task completed");
```

**Output:**
```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processing task
[14:23:46 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Task completed
```

## Complete Example (.NET 8)

```csharp
using Traceability;
using Traceability.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog with Traceability (Source + CorrelationId)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<Program>();

// Example 1: Basic usage
Console.WriteLine("=== Example 1: Basic CorrelationContext usage ===");
var correlationId = CorrelationContext.GetOrCreate();
Console.WriteLine($"Generated Correlation ID: {correlationId}");
Console.WriteLine($"Current Correlation ID: {CorrelationContext.Current}");
Console.WriteLine();

// Example 2: Logging with correlation-id
Console.WriteLine("=== Example 2: Logging with correlation-id ===");
logger.LogInformation("Log message with automatic correlation-id");
Console.WriteLine();

// Example 3: Correlation-id preserved in asynchronous operations
Console.WriteLine("=== Example 3: Correlation-id preserved in asynchronous operations ===");
var correlationIdBefore = CorrelationContext.Current;
logger.LogInformation("Correlation ID before async operation: {CorrelationId}", correlationIdBefore);

await Task.Delay(100);

var correlationIdAfter = CorrelationContext.Current;
logger.LogInformation("Correlation ID after async operation: {CorrelationId}", correlationIdAfter);
Console.WriteLine($"Correlation ID preserved: {correlationIdBefore == correlationIdAfter}");
Console.WriteLine();

Log.CloseAndFlush();
```

## Complete Example (.NET Framework 4.8)

```csharp
using System;
using System.Threading.Tasks;
using Traceability;

namespace Sample.Console.NetFramework
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Example 1: Basic usage
            Console.WriteLine("=== Example 1: Basic CorrelationContext usage ===");
            var correlationId = CorrelationContext.GetOrCreate();
            Console.WriteLine($"Generated Correlation ID: {correlationId}");
            Console.WriteLine($"Current Correlation ID: {CorrelationContext.Current}");
            Console.WriteLine();

            // Example 2: Correlation-id preserved in asynchronous operations
            Console.WriteLine("=== Example 2: Correlation-id preserved in asynchronous operations ===");
            var correlationIdBefore = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID before async operation: {correlationIdBefore}");

            await Task.Delay(100);

            var correlationIdAfter = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID after async operation: {correlationIdAfter}");
            Console.WriteLine($"Correlation ID preserved: {correlationIdBefore == correlationIdAfter}");
            Console.WriteLine();

            Console.WriteLine("Examples completed!");
            Console.ReadKey();
        }
    }
}
```

## Complete Example

See the complete example in `samples/Sample.Console.Net8/` and `samples/Sample.Console.NetFramework/`.
