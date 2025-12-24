# Trace-ID Propagation Between Services

## Summary

**Yes, the trace-id will be the same and logs will be logged with the same trace-id** when a .NET Framework 4.8 service calls another .NET 8.0 service via HTTP, as long as both use the Traceability library correctly.

## How It Works

### Propagation Flow

1. **.NET 4.8 Service (Source)**
   - Receives request (or generates new trace-id)
   - `CorrelationIdHandler` adds the trace-id to the `X-Correlation-Id` header of the HTTP request
   - Makes HTTP call to .NET 8.0 service

2. **.NET 8.0 Service (Destination)**
   - `CorrelationIdMiddleware` reads the `X-Correlation-Id` header from the request
   - If the header exists, uses the header value (priority over Activity)
   - If it doesn't exist, generates a new trace-id
   - All logs use the same trace-id

### Relevant Code

#### 1. Sending (CorrelationIdHandler)
```csharp
// Add X-Correlation-Id for compatibility
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
// IMPORTANT: Read header FIRST to ensure header trace-id has priority
var correlationId = request.Headers[headerName];

// If it exists in header, use header value (priority over Activity)
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

**2. Service A generates trace-id:**
- Trace-ID generated: `a1b2c3d4e5f6789012345678901234ab`
- Service A logs: `{"traceId": "a1b2c3d4e5f6789012345678901234ab", ...}`

**3. Service A calls Service B:**
```
GET /api/process HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**4. Service B receives and uses trace-id:**
- Reads header `X-Correlation-Id`: `a1b2c3d4e5f6789012345678901234ab`
- Sets `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Service B logs: `{"traceId": "a1b2c3d4e5f6789012345678901234ab", ...}`

**Result:** Both services use the same trace-id in logs! ✅

## Compatibility Between Formats

The library supports both Activity formats:

- **W3C Format** (preferred): Used in .NET 8.0
- **Hierarchical Format** (fallback): Used in .NET Framework 4.8

The `TryGetTraceIdFromActivity` helper ensures that the trace-id is extracted correctly regardless of format, and it's always propagated via the `X-Correlation-Id` header to ensure compatibility.

## Important Configuration

### `AlwaysGenerateNew` Option

If `AlwaysGenerateNew = true`, a new trace-id will be generated even if the header exists. Use only for testing or specific scenarios.

```csharp
var options = new TraceabilityOptions
{
    AlwaysGenerateNew = false // Default: false (reuses trace-id from header)
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
   - Both services should have the same `traceId` in logs
   - The trace-id should appear in the `X-Correlation-Id` response header

3. **End-to-end test:**
   - Make a request to Service A
   - Service A calls Service B
   - Verify that both logs have the same trace-id

## Complex Call Chain

### Scenario: App → .NET 8 → .NET 4.8 → .NET 8

**Yes, all logs will have the same trace-id!** ✅

The library ensures correct propagation in chains of any size and combination of .NET versions.

### Detailed Flow

```
┌─────────────┐
│   App       │ (origin - may or may not have trace-id)
└──────┬──────┘
       │ HTTP Request
       │ (no X-Correlation-Id)
       ▼
┌─────────────────────┐
│ Service A (.NET 8)  │
│ - Receives no header│
│ - Generates trace-id:│
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

Result: All logs have trace-id "abc123..." ✅
```

### Code That Ensures Propagation

#### 1. Sending (CorrelationIdHandler - works in both .NET 4.8 and 8.0)
```csharp
// Always adds current trace-id to header
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

**2. Service A (.NET 8) - Generates trace-id:**
- Trace-ID generated: `a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"traceId": "a1b2c3d4e5f6789012345678901234ab", "message": "Processing..."}`

**3. Service A calls Service B (.NET 4.8):**
```
GET /api/process HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**4. Service B (.NET 4.8) - Receives and propagates:**
- Reads header: `X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"traceId": "a1b2c3d4e5f6789012345678901234ab", "message": "Processing..."}`

**5. Service B calls Service C (.NET 8):**
```
GET /api/process HTTP/1.1
Host: service-c.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**6. Service C (.NET 8) - Receives and propagates:**
- Reads header: `X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab`
- `CorrelationContext.Current = "a1b2c3d4e5f6789012345678901234ab"`
- Log: `{"traceId": "a1b2c3d4e5f6789012345678901234ab", "message": "Processing..."}`

**Final Result:**
- ✅ Service A: `traceId: "a1b2c3d4e5f6789012345678901234ab"`
- ✅ Service B: `traceId: "a1b2c3d4e5f6789012345678901234ab"`
- ✅ Service C: `traceId: "a1b2c3d4e5f6789012345678901234ab"`

**All logs have the same trace-id!** 🎉

### Library Guarantees

1. **Header Priority**: The `X-Correlation-Id` header always has priority over Activity trace-id
2. **Automatic Propagation**: The `CorrelationIdHandler` always adds the current trace-id to the header
3. **Compatibility**: Works between any combination of .NET 4.8 and .NET 8.0
4. **Preservation**: The trace-id is preserved throughout the chain, as long as `AlwaysGenerateNew = false`

### Chain Verification

To verify if propagation is working throughout the chain:

1. **Make a request to the first service**
2. **Track the trace-id in each service's logs**
3. **Verify that all have the same trace-id**

Verification example:
```bash
# 1. Make request
curl http://service-a/api/process

# 2. Check logs (all should have the same trace-id)
# Service A: {"traceId": "abc123...", ...}
# Service B: {"traceId": "abc123...", ...}
# Service C: {"traceId": "abc123...", ...}
```

## Conclusion

The library ensures that the trace-id is propagated correctly between services, regardless of .NET version (4.8 or 8.0), as long as:

1. ✅ All services use the Traceability library
2. ✅ The `CorrelationIdHandler` is configured in the HttpClient of each service that makes HTTP calls
3. ✅ The middleware/handler is configured in each service that receives requests
4. ✅ The `AlwaysGenerateNew` option is set to `false` (default) in all services

**Propagation works in chains of any size and combination of versions!** ✅
