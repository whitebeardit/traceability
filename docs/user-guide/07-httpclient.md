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

1. ✅ Gets the correlation-id from the current context
2. ✅ Adds the `X-Correlation-Id` header to the request
3. ✅ Propagates the correlation-id to the external service

**HTTP request sent:**
```http
GET /endpoint HTTP/1.1
Host: api.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Propagation in Chain

The correlation-id is automatically propagated in chained HTTP calls:

**Scenario:** Service A → Service B → Service C

1. **Service A** receives request without header → generates `abc123`
2. **Service A** calls **Service B** with header `X-Correlation-Id: abc123`
3. **Service B** reads header and uses `abc123` (doesn't generate new one)
4. **Service B** calls **Service C** with same header `X-Correlation-Id: abc123`

**Result:** All services in the chain use the same correlation-id!

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

## Next Steps

Now that you know how to use HttpClient, let's see configuration options in [Lesson 8: Configuration](08-configuration.md).
