# Spans and Trace Hierarchy

## Summary

**Yes, spans are created and propagated correctly!** The library creates a hierarchy of spans (Activities) that maintains the same trace-id, but each operation has its own unique span-id. This enables complete distributed tracing using the W3C Trace Context standard.

## What are Spans?

**Span** = An individual operation within a trace. Each span has:
- **Trace-ID**: Identifies the complete trace (same for all spans in the same trace)
- **Span-ID**: Identifies the specific operation (unique for each span)
- **Parent Span-ID**: Identifies the parent span (to create hierarchy)

## Span Hierarchy in Chain

### Scenario: App → .NET 8 → .NET 4.8 → .NET 8

```
Trace: abc123... (same trace-id in all spans)
│
├─ Span 1: HTTP Request (Service A - .NET 8)
│  ├─ Trace-ID: abc123...
│  ├─ Span-ID: span-1
│  └─ Parent: (root)
│     │
│     └─ Span 2: HTTP Client Call (Service A → B)
│        ├─ Trace-ID: abc123... (same trace)
│        ├─ Span-ID: span-2
│        └─ Parent: span-1
│           │
│           └─ Span 3: HTTP Request (Service B - .NET 4.8)
│              ├─ Trace-ID: abc123... (same trace)
│              ├─ Span-ID: span-3
│              └─ Parent: span-2
│                 │
│                 └─ Span 4: HTTP Client Call (Service B → C)
│                    ├─ Trace-ID: abc123... (same trace)
│                    ├─ Span-ID: span-4
│                    └─ Parent: span-3
│                       │
│                       └─ Span 5: HTTP Request (Service C - .NET 8)
│                          ├─ Trace-ID: abc123... (same trace)
│                          ├─ Span-ID: span-5
│                          └─ Parent: span-4
```

**Result:** All spans share the same trace-id (`abc123...`), but each has its own unique span-id, creating a complete execution flow hierarchy.

## How It Works

### 1. Service Receives Request (Server Span)

**Code (.NET 8 - CorrelationIdMiddleware):**
```csharp
// Create Activity (span) of type Server for received request
Activity? activity = null;
if (Activity.Current == null)
{
    activity = TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);
    // If traceparent exists in header, Activity will be created as child automatically
}
```

**Code (.NET 4.8 - CorrelationIdHttpModule):**
```csharp
// Create Activity (span) of type Server for received request
var activity = TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);
// If traceparent exists in header, Activity will be created as child automatically
```

### 2. Service Makes HTTP Call (Client Span)

**Code (CorrelationIdHandler - works in both .NET 4.8 and 8.0):**
```csharp
// Create child Activity (hierarchical span) for HTTP call
var parentActivity = Activity.Current; // Server span
using var activity = TraceabilityActivitySource.StartActivity(
    "HTTP Client", 
    ActivityKind.Client, 
    parentActivity); // Create as child of current span

if (activity != null)
{
    // Propagate trace context (W3C Trace Context)
    var traceParent = activity.Id; // Format: 00-{trace-id}-{span-id}-{flags}
    if (!request.Headers.Contains("traceparent"))
    {
        request.Headers.Add("traceparent", traceParent);
    }
}
```

### 3. Propagation via W3C Trace Context

The `traceparent` header contains:
```
traceparent: 00-{trace-id}-{span-id}-{flags}
```

**Example:**
```
traceparent: 00-a1b2c3d4e5f6789012345678901234ab-0123456789abcdef-01
             │  │                              │ │                │
             │  │                              │                └─ Flags
             │  │                              └─ Span-ID (unique)
             │  └─ Trace-ID (same for all)
             └─ Version (00 = W3C)
```

When the next service receives the `traceparent`:
- OpenTelemetry processes it automatically via DiagnosticSource
- Creates a new Activity (span) as child of the sent span
- Maintains the same trace-id, but creates new span-id
- Establishes parent-child hierarchy

## Complete Flow with Spans

### Step by Step

**1. App → Service A (.NET 8):**
```
Request: (no traceparent)
Service A creates:
  - Span 1: HTTP Request (Server)
    - Trace-ID: abc123...
    - Span-ID: span-1
    - Parent: (root)
```

**2. Service A → Service B (.NET 4.8):**
```
Request with:
  - X-Correlation-Id: abc123...
  - traceparent: 00-abc123...-span-1-01

Service A creates:
  - Span 2: HTTP Client Call (Client)
    - Trace-ID: abc123... (same)
    - Span-ID: span-2
    - Parent: span-1

Service B receives and creates:
  - Span 3: HTTP Request (Server)
    - Trace-ID: abc123... (same)
    - Span-ID: span-3
    - Parent: span-2 (from traceparent)
```

**3. Service B → Service C (.NET 8):**
```
Request with:
  - X-Correlation-Id: abc123...
  - traceparent: 00-abc123...-span-3-01

Service B creates:
  - Span 4: HTTP Client Call (Client)
    - Trace-ID: abc123... (same)
    - Span-ID: span-4
    - Parent: span-3

Service C receives and creates:
  - Span 5: HTTP Request (Server)
    - Trace-ID: abc123... (same)
    - Span-ID: span-5
    - Parent: span-4 (from traceparent)
```

## Span Types

### ActivityKind.Server
- Created when a service **receives** an HTTP request
- Represents request processing on the server
- Created by: `CorrelationIdMiddleware`, `CorrelationIdHttpModule`, `CorrelationIdMessageHandler`

### ActivityKind.Client
- Created when a service **makes** an HTTP call
- Represents the HTTP call to another service
- Created by: `CorrelationIdHandler`
- Always a child of the current Server span

## Hierarchy Visualization

```
Trace: abc123...
│
├─ [Server] Service A - HTTP Request
│  │ Trace-ID: abc123...
│  │ Span-ID: span-1
│  │
│  └─ [Client] Service A → B - HTTP Client Call
│     │ Trace-ID: abc123...
│     │ Span-ID: span-2
│     │ Parent: span-1
│     │
│     └─ [Server] Service B - HTTP Request
│        │ Trace-ID: abc123...
│        │ Span-ID: span-3
│        │ Parent: span-2
│        │
│        └─ [Client] Service B → C - HTTP Client Call
│           │ Trace-ID: abc123...
│           │ Span-ID: span-4
│           │ Parent: span-3
│           │
│           └─ [Server] Service C - HTTP Request
│              │ Trace-ID: abc123...
│              │ Span-ID: span-5
│              │ Parent: span-4
```

## Benefits

1. **Complete Tracking**: Each operation has its own span, allowing complete flow tracking
2. **Preserved Hierarchy**: Parent-child relationship is maintained across services
3. **Same Trace-ID**: All spans share the same trace-id for correlation
4. **W3C Standard**: Uses W3C Trace Context standard, compatible with observability tools
5. **Compatibility**: Works between .NET 4.8 and .NET 8.0

## Integration with Observability Tools

Spans are automatically exported to observability tools that support OpenTelemetry:

- **Application Insights**
- **Jaeger**
- **Zipkin**
- **Datadog**
- **New Relic**
- **Grafana Tempo**

These tools can visualize the complete span hierarchy and correlate logs using the trace-id.

## Verification

To verify if spans are being created correctly:

1. **Check HTTP headers:**
   ```bash
   curl -v http://service-b/api/test \
     -H "X-Correlation-Id: test-123"
   ```
   Should include: `traceparent: 00-...`

2. **Configure an OpenTelemetry exporter:**
   ```csharp
   services.AddOpenTelemetry()
       .WithTracing(builder => builder
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddConsoleExporter()); // For debug
   ```

3. **Visualize in console or observability tool:**
   - Should show complete span hierarchy
   - All should have the same trace-id
   - Each span should have its own span-id

## Conclusion

The library ensures that:

1. ✅ **Spans are created** for each operation (Server and Client)
2. ✅ **Hierarchy is preserved** across services
3. ✅ **Trace-ID is shared** among all spans
4. ✅ **W3C Trace Context is propagated** via `traceparent` header
5. ✅ **Compatibility** between .NET 4.8 and .NET 8.0

**Result**: Complete distributed tracing with span hierarchy preserved throughout the call chain! 🎉
