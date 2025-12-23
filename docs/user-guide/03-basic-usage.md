# Lesson 3: Basic Usage

In this lesson, you'll learn to use `CorrelationContext` directly to manage correlation-id.

## CorrelationContext

`CorrelationContext` is a static class that manages correlation-id in the current thread's asynchronous context.

## Properties and Methods

### Current

Gets or sets the current correlation-id. If it doesn't exist, creates a new one automatically.

```csharp
using Traceability;

// Get correlation-id (creates if it doesn't exist)
var correlationId = CorrelationContext.Current;
Console.WriteLine($"Correlation ID: {correlationId}");
```

**Expected output:**
```
Correlation ID: a1b2c3d4e5f6789012345678901234ab
```

### HasValue

Checks if a correlation-id exists in the context.

```csharp
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
    Console.WriteLine($"Correlation ID exists: {id}");
}
else
{
    Console.WriteLine("No correlation-id in context");
}
```

### GetOrCreate()

Gets the existing correlation-id or creates a new one explicitly.

```csharp
var correlationId = CorrelationContext.GetOrCreate();
Console.WriteLine($"Correlation ID: {correlationId}");
```

### TryGetValue()

Attempts to get the existing correlation-id **without creating a new one** if it doesn't exist. Returns `true` if a correlation-id exists, `false` otherwise.

```csharp
if (CorrelationContext.TryGetValue(out var correlationId))
{
    Console.WriteLine($"Correlation ID found: {correlationId}");
}
else
{
    Console.WriteLine("No correlation-id in context");
}
```

**Why use `TryGetValue()`?**
- Avoids creating correlation-id unintentionally
- Useful when you only want to read the value if it already exists

### Clear()

Clears the correlation-id from the context.

```csharp
CorrelationContext.Clear();
```

## Complete Example

```csharp
using Traceability;

// Example 1: Get or create
var correlationId1 = CorrelationContext.Current;
Console.WriteLine($"Correlation ID 1: {correlationId1}");

// Example 2: Check if exists
if (CorrelationContext.HasValue)
{
    var correlationId2 = CorrelationContext.Current;
    Console.WriteLine($"Correlation ID 2: {correlationId2}");
    // correlationId1 and correlationId2 are equal
}

// Example 3: Try to get without creating
if (CorrelationContext.TryGetValue(out var correlationId3))
{
    Console.WriteLine($"Correlation ID 3: {correlationId3}");
}

// Example 4: Clear context
CorrelationContext.Clear();
Console.WriteLine($"After Clear, HasValue: {CorrelationContext.HasValue}"); // False
```

**Expected output:**
```
Correlation ID 1: a1b2c3d4e5f6789012345678901234ab
Correlation ID 2: a1b2c3d4e5f6789012345678901234ab
Correlation ID 3: a1b2c3d4e5f6789012345678901234ab
After Clear, HasValue: False
```

## Preservation in Asynchronous Operations

The correlation-id is preserved across asynchronous operations:

```csharp
var correlationIdBefore = CorrelationContext.GetOrCreate();
Console.WriteLine($"Before await: {correlationIdBefore}");

await Task.Delay(100);

var correlationIdAfter = CorrelationContext.Current;
Console.WriteLine($"After await: {correlationIdAfter}");
Console.WriteLine($"Preserved: {correlationIdBefore == correlationIdAfter}"); // True
```

**Expected output:**
```
Before await: a1b2c3d4e5f6789012345678901234ab
After await: a1b2c3d4e5f6789012345678901234ab
Preserved: True
```

## Correlation-ID Format

The correlation-id is a GUID formatted **without hyphens** (32 characters):

- ✅ Correct format: `a1b2c3d4e5f6789012345678901234ab` (32 characters)
- ❌ Incorrect format: `a1b2c3d4-e5f6-7890-1234-5678901234ab` (36 characters with hyphens)

## Next Steps

Now that you know how to use `CorrelationContext`, let's see how to integrate it with ASP.NET Core in [Lesson 4: ASP.NET Core](04-aspnet-core.md).
