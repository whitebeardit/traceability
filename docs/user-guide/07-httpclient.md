# Lesson 7: HttpClient

In this lesson, you'll learn to use Traceability with HttpClient to automatically propagate correlation-id.

## Basic Configuration

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
// HttpClient is already automatically configured with CorrelationIdHandler!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

## Using HttpClient

**Controller:**
```csharp
public class ValuesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ValuesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Correlation-id is automatically added to header
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

## What Happens Automatically

When you make an HTTP call, `CorrelationIdHandler` automatically:

1. ✅ Gets the correlation-id/trace-id from the current context (without implicitly creating a new one)
2. ✅ Adds the `X-Correlation-Id` header to the request (backward compatibility)
3. ✅ Adds the `traceparent` header (W3C Trace Context) when trace context is available (best-effort, W3C-valid only)
4. ✅ Propagates the headers to the external service

**HTTP request sent:**
```http
GET /endpoint HTTP/1.1
Host: api.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-a1b2c3d4e5f6789012345678901234ab-0123456789abcdef-01
```

**Notes**:
- Traceability does not explicitly emit the `tracestate` header. If you need `tracestate` propagation, rely on OpenTelemetry SDK/instrumentation.
- `traceparent` propagation is best-effort: Traceability only emits it when a valid W3C `traceparent` value is available. Legacy/hierarchical `Activity.Id` values are not valid `traceparent` and are not emitted.

The `traceparent` header follows the W3C Trace Context standard and enables distributed tracing across services. The `X-Correlation-Id` header is maintained for backward compatibility with services not using OpenTelemetry.

### .NET 8: HttpClient spans are opt-in (default: disabled)

On **.NET 8**, Traceability does **not** create HttpClient child spans by default to avoid duplication with the built-in `System.Net.Http` / OpenTelemetry instrumentation.

To enable Traceability-created HttpClient spans on .NET 8, use either:

- `TraceabilityOptions.Net8HttpClientSpansEnabled = true`, or
- `TRACEABILITY_NET8_HTTPCLIENT_SPANS_ENABLED=true`

## Propagation in Chain

The correlation-id/trace-id is automatically propagated in chained HTTP calls with W3C Trace Context:

**Scenario:** Service A → Service B → Service C

1. **Service A** receives request without header → creates Activity with TraceId `abc123`
2. **Service A** calls **Service B** with headers:
   - `X-Correlation-Id: abc123` (backward compatibility)
   - `traceparent: 00-abc123...` (W3C Trace Context)
3. **Service B** reads headers and uses `abc123` (doesn't generate new one)
4. **Service B** creates child Activity (span) maintaining trace hierarchy
5. **Service B** calls **Service C** with same headers

**Result:** All services in the chain use the same trace-id and maintain hierarchical span relationships for complete distributed tracing!

## Using AddTraceableHttpClient (Recommended)

To ensure HttpClient is configured correctly:

**Program.cs:**
```csharp
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Usage:**
```csharp
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## With Polly (Resilience Policies)

**Program.cs:**
```csharp
using Polly;
using Polly.Extensions.Http;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddPolicyHandler(retryPolicy);
```

## Socket Exhaustion Prevention

**✅ Always use `IHttpClientFactory`:**

```csharp
// Correct - uses IHttpClientFactory
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

var client = _httpClientFactory.CreateClient("ExternalApi");
```

**❌ Never create direct instances:**

```csharp
// Incorrect - causes socket exhaustion
var client = new HttpClient();
```

## Complete Example

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

**Service:**
```csharp
public class ExternalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("data");
        return await response.Content.ReadAsStringAsync();
    }
}
```

## OpenTelemetry Integration

Each HTTP call propagates trace context, and can create a child Activity (span) when enabled that:

- **Maintains Trace Hierarchy**: Child spans are linked to parent spans
- **Propagates Trace Context**: W3C Trace Context headers enable distributed tracing
- **Adds HTTP Tags**: Automatically adds HTTP method, URL, status code, etc. to spans
- **Tracks Errors**: Automatically marks spans with error information if request fails

**Compatible with**:
- Jaeger
- Zipkin
- Application Insights
- Any OpenTelemetry-compatible observability tool

## Next Steps

Now that you know how to use HttpClient, let's see configuration options in [Lesson 8: Configuration](08-configuration.md).
