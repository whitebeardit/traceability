# Examples - ASP.NET Core (.NET 8)

Practical examples of using Traceability in ASP.NET Core applications.

## Basic Example

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Controller:**
```csharp
using Microsoft.AspNetCore.Mvc;
using Traceability;

[ApiController]
[Route("api/[controller]")]
public class ValuesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-ID is automatically available
        // Correlation-ID is independent from OpenTelemetry TraceId (Activity.TraceId)
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

**Note**: An OpenTelemetry Activity (span) is automatically created for each request, providing distributed tracing capabilities.

## Example with Serilog

**Program.cs:**
```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Log Output:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processing request
```

## Example with HttpClient

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

**Controller:**
```csharp
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // HttpClient automatically:
        // 1. Adds X-Correlation-Id header (backward compatibility)
        // 2. Adds traceparent header (W3C Trace Context) when trace context is available
        // 3. Optionally creates a child Activity (span) for the HTTP call on .NET 8
        //    when enabled via TraceabilityOptions.Net8HttpClientSpansEnabled or
        //    TRACEABILITY_NET8_HTTPCLIENT_SPANS_ENABLED=true
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

**HTTP Request Headers Sent:**
```http
GET /endpoint HTTP/1.1
Host: api.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

## Complete Example

See the complete example in `samples/Sample.WebApi.Net8/`.
