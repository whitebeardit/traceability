# Observability Tools Integration

## Summary

**Yes, the library works perfectly with Grafana, Jaeger, Zipkin, Application Insights, and other observability tools!** ✅

The library creates Activities (spans) with all the information needed for:
- ✅ Visual timeline (Grafana Tempo, Jaeger UI)
- ✅ Sequence diagrams of the flow
- ✅ Timing of each operation
- ✅ Complete parent-child hierarchy
- ✅ Log correlation with traces

## What is Exported

### 1. Spans (Activities) with Complete Metadata

Each span contains:

#### **Automatic Timestamps**
- `StartTime`: Start timestamp (automatic by Activity)
- `Duration`: Operation duration (automatically calculated)
- `EndTime`: End timestamp (StartTime + Duration)

#### **Standard OpenTelemetry Tags**

**Server Spans (received requests):**
- `http.method`: HTTP method (GET, POST, etc.)
- `http.url`: Complete request URL
- `http.scheme`: Scheme (http, https)
- `http.host`: Request host
- `http.user_agent`: User-Agent (when available)
- `http.request_content_length`: Request content size
- `http.request_content_type`: Request content type
- `http.status_code`: HTTP response status
- `http.response_content_length`: Response content size (when available)

**Client Spans (HTTP calls):**
- `http.method`: HTTP method
- `http.url`: Complete URL
- `http.scheme`: Scheme
- `http.host`: Host
- `http.status_code`: HTTP response status

**Error Tags (when exception occurs):**
- `error`: `true`
- `error.type`: Exception type
- `error.message`: Exception message
- `Status`: `ActivityStatusCode.Error`

#### **Parent-Child Hierarchy**
- `TraceId`: Same for all spans in the same trace
- `SpanId`: Unique for each span
- `ParentId`: Parent span ID (for hierarchy)

#### **W3C Trace Context**
- `traceparent`: Header propagated automatically
- `tracestate`: Header propagated when there's baggage

## How It Works with Grafana Tempo

### Visual Timeline

Grafana Tempo shows a complete timeline with:

```
Timeline (Grafana Tempo):
│
├─ [0ms] Service A - HTTP Request (Server)
│  │ Duration: 150ms
│  │ Status: 200 OK
│  │
│  └─ [10ms] Service A → B - HTTP Client Call (Client)
│     │ Duration: 120ms
│     │ Status: 200 OK
│     │
│     └─ [20ms] Service B - HTTP Request (Server)
│        │ Duration: 100ms
│        │ Status: 200 OK
│        │
│        └─ [30ms] Service B → C - HTTP Client Call (Client)
│           │ Duration: 80ms
│           │ Status: 200 OK
│           │
│           └─ [40ms] Service C - HTTP Request (Server)
│              │ Duration: 60ms
│              │ Status: 200 OK
```

### Sequence Diagram

Grafana can generate sequence diagrams showing:

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Service A   │     │ Service B   │     │ Service C   │
│ (.NET 8)    │     │ (.NET 4.8)  │     │ (.NET 8)    │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │─── HTTP ─────────>│                   │
       │  traceparent      │                   │
       │                   │                   │
       │                   │─── HTTP ─────────>│
       │                   │  traceparent      │
       │                   │                   │
       │                   │<── Response ──────│
       │                   │                   │
       │<── Response ──────│                   │
       │                   │                   │
```

## Configuration for Grafana Tempo

### 1. Configure OpenTelemetry Exporter

```csharp
// .NET 8
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Traceability") // ActivitySource name
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://tempo:4318/v1/traces");
        }));
```

### 2. Configure Grafana Data Source

In Grafana, add Tempo as a data source:
- URL: `http://tempo:3200`
- Type: Tempo

### 3. Visualize Traces

In Grafana, create a Trace panel:
- Query: `{service_name="YourService"}`
- Visualization: Timeline or Flamegraph

## What You'll See in Grafana

### 1. Timeline Stack

```
Trace: abc123...
│
├─ [0ms - 150ms] Service A - HTTP Request
│  ├─ http.method: GET
│  ├─ http.url: /api/process
│  ├─ http.status_code: 200
│  └─ [10ms - 130ms] Service A → B - HTTP Client Call
│     ├─ http.method: GET
│     ├─ http.url: http://service-b/api/process
│     ├─ http.status_code: 200
│     └─ [20ms - 120ms] Service B - HTTP Request
│        ├─ http.method: GET
│        ├─ http.url: /api/process
│        ├─ http.status_code: 200
│        └─ [30ms - 110ms] Service B → C - HTTP Client Call
│           ├─ http.method: GET
│           ├─ http.url: http://service-c/api/process
│           ├─ http.status_code: 200
│           └─ [40ms - 100ms] Service C - HTTP Request
│              ├─ http.method: GET
│              ├─ http.url: /api/process
│              └─ http.status_code: 200
```

### 2. Flamegraph

Shows hierarchy visually:
- Width = Duration
- Height = Depth in hierarchy
- Colors = Different services/operations

### 3. Sequence Diagram

Shows the temporal order of calls between services.

### 4. Metrics per Span

- Average response time
- Error rate
- Throughput
- Latency p95/p99

## Compatibility with Other Tools

### Jaeger
- ✅ Supports W3C Trace Context
- ✅ Visualizes span hierarchy
- ✅ Timeline and flamegraph
- ✅ Sequence diagram

### Zipkin
- ✅ Supports W3C Trace Context
- ✅ Visualizes spans and dependencies
- ✅ Timeline

### Application Insights
- ✅ Supports OpenTelemetry
- ✅ Visualizes distributed tracing
- ✅ Application Map

### Datadog
- ✅ Supports OpenTelemetry
- ✅ APM with distributed tracing
- ✅ Service Map

### New Relic
- ✅ Supports OpenTelemetry
- ✅ Distributed tracing
- ✅ Service Map

## Information Exported per Span

### Example of Exported Span

```json
{
  "traceId": "a1b2c3d4e5f6789012345678901234ab",
  "spanId": "0123456789abcdef",
  "parentSpanId": "fedcba9876543210",
  "name": "HTTP Request",
  "kind": "SERVER",
  "startTime": "2024-01-15T10:30:00.123Z",
  "endTime": "2024-01-15T10:30:00.273Z",
  "duration": 150000000, // nanoseconds
  "status": {
    "code": "OK"
  },
  "attributes": {
    "http.method": "GET",
    "http.url": "/api/process",
    "http.scheme": "https",
    "http.host": "service-a.example.com",
    "http.status_code": 200,
    "http.user_agent": "Mozilla/5.0...",
    "http.request_content_length": 1024
  }
}
```

## Library Guarantees

1. ✅ **Automatic Timestamps**: Each Activity has automatic StartTime and Duration
2. ✅ **Standard Tags**: All tags follow OpenTelemetry standard
3. ✅ **Preserved Hierarchy**: Parent-child is maintained across services
4. ✅ **W3C Trace Context**: Headers propagated correctly
5. ✅ **Error Status**: Errors are marked with appropriate tags
6. ✅ **Compatibility**: Works with any tool that supports OpenTelemetry

## Verification

To verify if it's working:

1. **Configure an OpenTelemetry exporter:**
   ```csharp
   services.AddOpenTelemetry()
       .WithTracing(builder => builder
           .AddSource("Traceability")
           .AddConsoleExporter()); // For debug
   ```

2. **Make a request and check the console:**
   - Should show spans with timestamps
   - Should show parent-child hierarchy
   - Should show all tags

3. **Configure Grafana Tempo:**
   - Should visualize complete timeline
   - Should show sequence diagram
   - Should show timing of each operation

## Conclusion

The library is **100% compatible** with modern observability tools:

- ✅ **Grafana Tempo**: Timeline, flamegraph, sequence diagrams
- ✅ **Jaeger**: Complete trace visualization
- ✅ **Zipkin**: Distributed tracing
- ✅ **Application Insights**: APM and distributed tracing
- ✅ **Datadog/New Relic**: Complete APM

**Everything works perfectly!** 🎉
