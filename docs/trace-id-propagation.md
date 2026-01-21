# Correlation-ID Propagation Between Services

## Summary

**Yes, the correlation-ID will be the same and logs will be logged with the same correlation-ID** when a .NET Framework 4.8 service calls another .NET 8.0 service via HTTP, as long as both use the Traceability library correctly.

**Important**: Correlation-ID is **independent** from OpenTelemetry's trace ID. Both IDs appear in logs and spans, but they serve different purposes:
- **Correlation-ID**: Used for business-level request tracking across services
- **Trace ID**: OpenTelemetry's distributed tracing identifier (automatically managed by OpenTelemetry)

## How It Works

### Propagation Flow

1. **.NET 4.8 Service (Source)**
   - Receives request (or generates new correlation-ID)
   - `CorrelationIdHandler` adds the correlation-ID to the `X-Correlation-Id` header of the HTTP request
   - Makes HTTP call to .NET 8.0 service

2. **.NET 8.0 Service (Destination)**
   - `CorrelationIdMiddleware` reads the `X-Correlation-Id` header from the request
   - If the header exists, uses the header value
   - If it doesn't exist, generates a new correlation-ID
   - All logs use the same correlation-ID

### Relevant Code

#### 1. Sending (CorrelationIdHandler)
```csharp
// Add X-Correlation-Id header
if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
{
    var headerName = CorrelationIdHeader;
    if (request.Headers.Contains(headerName))
    {
        request.Headers.Remove(headerName);
    }
    request.Headers.Add(headerName, correlationId);
}
```

#### 2. Receiving (.NET 8.0 - CorrelationIdMiddleware)
```csharp
// Try to get correlation-id from request header
var correlationId = context.Request.Headers[headerName].FirstOrDefault();

// If it doesn't exist or AlwaysGenerateNew is enabled, generate a new one
if (string.IsNullOrEmpty(correlationId) || _options.AlwaysGenerateNew)
{
    correlationId = CorrelationContext.GetOrCreate();
}
else
{
    // If it exists, use the header value (PRIORITY)
    CorrelationContext.Current = correlationId;
}
```

#### 3. Receiving (.NET 4.8 - CorrelationIdHttpModule/MessageHandler)
```csharp
// IMPORTANT: Read header FIRST to ensure header correlation-ID has priority
var correlationId = request.Headers[headerName];

// If it exists in header, use header value
if (!string.IsNullOrEmpty(correlationId) && !options.AlwaysGenerateNew)
{
    CorrelationContext.Current = correlationId!;
}
else
{
    correlationId = CorrelationContext.GetOrCreate();
}
```

## Practical Example

### Scenario: Service A (.NET 4.8) → Service B (.NET 8.0)

**1. Client calls Service A:**
```
GET /api/process HTTP/1.1
Host: service-a.example.com
```

**2. Service A generates correlation-ID:**
- Correlation-ID generated: `a1b2c3d4e5f6789012345678901234ab`
- Service A logs: `{"CorrelationId": "a1b2c3d4e5f6789012345678901234ab", "TraceId": "xyz789...", ...}`

**3. Service A calls Service B:**
```
GET /api/process HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-xyz789...-0123456789abcdef-01
```

**4. Service B receives and uses correlation-ID:**
- Reads header `X-Correlation-Id`: `a1b2c3d4e5f6789012345678901234ab`
- Sets `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Service B logs: `{"CorrelationId": "a1b2c3d4e5f6789012345678901234ab", "TraceId": "xyz789...", ...}`

**Result:** Both services use the same correlation-ID in logs! ✅

**Note:** The trace ID (`xyz789...`) is managed by OpenTelemetry and may be different from the correlation-ID. Both IDs are independent and appear in logs and spans.

## Correlation-ID vs Trace ID

### Correlation-ID
- **Purpose**: Business-level request tracking
- **Source**: `X-Correlation-Id` header or generated GUID
- **Management**: Managed by `CorrelationContext` (AsyncLocal-based)
- **Propagation**: Via `X-Correlation-Id` HTTP header
- **Tag in Spans**: `correlation.id` (allows searching in Grafana Tempo)
- **Log Property**: `CorrelationId`

### Trace ID (OpenTelemetry)
- **Purpose**: Distributed tracing (technical)
- **Source**: OpenTelemetry Activity (automatically generated)
- **Management**: Managed by OpenTelemetry SDK
- **Propagation**: Via `traceparent` HTTP header (W3C Trace Context)
- **Tag in Spans**: `trace.trace_id` (standard OpenTelemetry)
- **Log Property**: `TraceId`

### Both IDs in Logs

Both IDs appear independently in logs:

```json
{
  "Timestamp": "2024-01-15T14:23:45.123Z",
  "Level": "Information",
  "Source": "UserService",
  "CorrelationId": "a1b2c3d4e5f6789012345678901234ab",
  "TraceId": "xyz7890123456789012345678901234ab",
  "Message": "Processing request"
}
```

This enables:
- **Business tracking**: Search logs by `CorrelationId` to track a business request
- **Technical tracing**: Use `TraceId` for distributed tracing analysis
- **Dual correlation**: Both IDs available for different use cases

## Important Configuration

### `AlwaysGenerateNew` Option

If `AlwaysGenerateNew = true`, a new correlation-ID will be generated even if the header exists. Use only for testing or specific scenarios.

```csharp
var options = new TraceabilityOptions
{
    AlwaysGenerateNew = false // Default: false (reuses correlation-ID from header)
};
```

## Verification

To verify if propagation is working:

1. **Check HTTP headers:**
   ```bash
   curl -v http://service-b.example.com/api/test \
     -H "X-Correlation-Id: test-123"
   ```

2. **Check logs:**
   - Both services should have the same `CorrelationId` in logs
   - The correlation-ID should appear in the `X-Correlation-Id` response header
   - Both `CorrelationId` and `TraceId` should appear in logs (independent)

3. **Check spans:**
   - All spans should have `correlation.id` tag with the same value
   - Trace ID is managed separately by OpenTelemetry

4. **End-to-end test:**
   - Make a request to Service A
   - Service A calls Service B
   - Verify that both logs have the same correlation-ID
   - Verify that both spans have the same `correlation.id` tag

## Complex Call Chain

### Scenario: App → .NET 8 → .NET 4.8 → .NET 8

**Yes, all logs will have the same correlation-ID!** ✅

The library ensures correct propagation in chains of any size and combination of .NET versions.

### Detailed Flow

```
┌─────────────┐
│   App       │ (origin - may or may not have correlation-ID)
└──────┬──────┘
       │ HTTP Request
       │ (no X-Correlation-Id)
       ▼
┌─────────────────────┐
│ Service A (.NET 8)  │
│ - Receives no header│
│ - Generates         │
│   correlation-ID:   │
│   "abc123..."       │
│ - Log: abc123...    │
└──────┬──────────────┘
       │ HTTP Request
       │ X-Correlation-Id: abc123...
       ▼
┌─────────────────────┐
│ Service B (.NET 4.8)│
│ - Receives header:  │
│   "abc123..."       │
│ - Uses header:      │
│   CorrelationContext│
│   .Current =        │
│   "abc123..."       │
│ - Log: abc123...    │
└──────┬──────────────┘
       │ HTTP Request
       │ X-Correlation-Id: abc123...
       ▼
┌─────────────────────┐
│ Service C (.NET 8)  │
│ - Receives header:  │
│   "abc123..."       │
│ - Uses header:      │
│   CorrelationContext│
│   .Current =        │
│   "abc123..."       │
│ - Log: abc123...  │
└─────────────────────┘

Result: All logs have correlation-ID "abc123..." ✅
```

### Code That Ensures Propagation

#### 1. Sending (CorrelationIdHandler - works in both .NET 4.8 and 8.0)
```csharp
// Always adds current correlation-ID to header
if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
{
    var headerName = CorrelationIdHeader;
    if (request.Headers.Contains(headerName))
    {
        request.Headers.Remove(headerName); // Remove if exists
    }
    request.Headers.Add(headerName, correlationId); // Add current
}
```

#### 2. Receiving (.NET 8 - CorrelationIdMiddleware)
```csharp
// Read header first
var correlationId = context.Request.Headers[headerName].FirstOrDefault();

// If exists and AlwaysGenerateNew = false, use header (PRIORITY)
if (!string.IsNullOrEmpty(correlationId) && !_options.AlwaysGenerateNew)
{
    CorrelationContext.Current = correlationId; // Propagate to logs
}
else
{
    correlationId = CorrelationContext.GetOrCreate(); // Generate new only if necessary
}
```

#### 3. Receiving (.NET 4.8 - CorrelationIdHttpModule/MessageHandler)
```csharp
// Read header FIRST (before creating Activity)
var correlationId = request.Headers[headerName];

// If exists and AlwaysGenerateNew = false, use header (PRIORITY)
if (!string.IsNullOrEmpty(correlationId) && !options.AlwaysGenerateNew)
{
    CorrelationContext.Current = correlationId!; // Propagate to logs
}
else
{
    correlationId = CorrelationContext.GetOrCreate(); // Generate new only if necessary
}
```

### Complete Practical Example

**1. App makes request to Service A (.NET 8):**
```
GET /api/process HTTP/1.1
Host: service-a.example.com
```

**2. Service A (.NET 8) - Generates correlation-ID:**
- Correlation-ID generated: `a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"CorrelationId": "a1b2c3d4e5f6789012345678901234ab", "TraceId": "xyz789...", "message": "Processing..."}`

**3. Service A calls Service B (.NET 4.8):**
```
GET /api/process HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-xyz789...-0123456789abcdef-01
```

**4. Service B (.NET 4.8) - Receives and propagates:**
- Reads header: `X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"CorrelationId": "a1b2c3d4e5f6789012345678901234ab", "TraceId": "xyz789...", "message": "Processing..."}`

**5. Service B calls Service C (.NET 8):**
```
GET /api/process HTTP/1.1
Host: service-c.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-xyz789...-0123456789abcdef-01
```

**6. Service C (.NET 8) - Receives and propagates:**
- Reads header: `X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"CorrelationId": "a1b2c3d4e5f6789012345678901234ab", "TraceId": "xyz789...", "message": "Processing..."}`

**Final Result:**
- ✅ Service A: `CorrelationId: "a1b2c3d4e5f6789012345678901234ab"`
- ✅ Service B: `CorrelationId: "a1b2c3d4e5f6789012345678901234ab"`
- ✅ Service C: `CorrelationId: "a1b2c3d4e5f6789012345678901234ab"`

**All logs have the same correlation-ID!** 🎉

**Note:** The trace ID (`xyz789...`) is managed by OpenTelemetry and may be different from the correlation-ID. Both IDs are independent.

## Library Guarantees

1. **Header Priority**: The `X-Correlation-Id` header always has priority
2. **Automatic Propagation**: The `CorrelationIdHandler` always adds the current correlation-ID to the header
3. **Compatibility**: Works between any combination of .NET 4.8 and .NET 8.0
4. **Preservation**: The correlation-ID is preserved throughout the chain, as long as `AlwaysGenerateNew = false`
5. **Independence**: Correlation-ID is independent from OpenTelemetry trace ID
6. **Span Tags**: Correlation-ID is automatically added as `correlation.id` tag to all spans for searching in Grafana Tempo

## Chain Verification

To verify if propagation is working throughout the chain:

1. **Make a request to the first service**
2. **Track the correlation-ID in each service's logs**
3. **Verify that all have the same correlation-ID**
4. **Verify that all spans have the same `correlation.id` tag**

Verification example:
```bash
# 1. Make request
curl http://service-a/api/process

# 2. Check logs (all should have the same correlation-ID)
# Service A: {"CorrelationId": "abc123...", "TraceId": "xyz789...", ...}
# Service B: {"CorrelationId": "abc123...", "TraceId": "xyz789...", ...}
# Service C: {"CorrelationId": "abc123...", "TraceId": "xyz789...", ...}

# 3. Search spans in Grafana Tempo
# Query: {correlation.id="abc123..."}
```

## Conclusion

The library ensures that the correlation-ID is propagated correctly between services, regardless of .NET version (4.8 or 8.0), as long as:

1. ✅ All services use the Traceability library
2. ✅ The `CorrelationIdHandler` is configured in the HttpClient of each service that makes HTTP calls
3. ✅ The middleware/handler is configured in each service that receives requests
4. ✅ The `AlwaysGenerateNew` option is set to `false` (default) in all services

**Propagation works in chains of any size and combination of versions!** ✅

**Important**: Correlation-ID is independent from OpenTelemetry's trace ID. Both IDs appear in logs and spans, enabling dual tracking for business and technical purposes.
