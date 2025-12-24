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
        // Correlation-id/trace-id is automatically available
        // Uses Activity.TraceId if OpenTelemetry is configured, otherwise uses AsyncLocal
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
        // 1. Creates a child Activity (span) for the HTTP call
        // 2. Adds X-Correlation-Id header (backward compatibility)
        // 3. Adds traceparent header (W3C Trace Context)
        // 4. Adds tracestate header if Activity has baggage
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
traceparent: 00-a1b2c3d4e5f6789012345678901234ab-0123456789abcdef-01
```

## Complete Example

See the complete example in `samples/Sample.WebApi.Net8/`.
