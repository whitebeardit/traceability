# Lesson 9: Practical Examples

In this lesson, you'll see complete practical examples with expected output.

## Example 1: Complete ASP.NET Core

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Traceability
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Controller:**
```csharp
using Microsoft.AspNetCore.Mvc;
using Traceability;
using Serilog;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var correlationId = CorrelationContext.Current;
        
        Log.Information("Fetching user {UserId}", id);

        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync($"users/{id}");
        
        var content = await response.Content.ReadAsStringAsync();
        
        Log.Information("User found");
        
        return Ok(new { CorrelationId = correlationId, Data = content });
    }
}
```

**Request:**
```bash
curl -X GET http://localhost:5000/api/users/123
```

**Log Output (JSON):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Fetching user 123"}
{"Timestamp":"2024-01-15T14:23:46.456Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"User found"}
```

**HTTP Response:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "data": "{ ... }"
}
```

**Response headers:**
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**Note**: An OpenTelemetry Activity (span) is created for each request when Traceability (or your OpenTelemetry instrumentation) creates/owns the server Activity.

When making HTTP calls, Traceability propagates `X-Correlation-Id` and `traceparent` (when trace context is available). Traceability does not explicitly emit `tracestate`.

On .NET 8, Traceability-created HttpClient spans are opt-in (see Lesson 7).

## Example 2: Console Application

**Program.cs:**
```csharp
using Traceability;
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

var correlationId = CorrelationContext.GetOrCreate();
Log.Information("Starting processing");

await ProcessDataAsync();

Log.Information("Processing completed");
Log.CloseAndFlush();

async Task ProcessDataAsync()
{
    Log.Information("Processing data");
    await Task.Delay(100);
    Log.Information("Data processed");
}
```

**Expected output:**
```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Starting processing
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processing data
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Data processed
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processing completed
```

## Example 3: Propagation in Chain

**Service A (API Gateway):**
```csharp
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet("process")]
    public async Task<IActionResult> Process()
    {
        var correlationId = CorrelationContext.Current;
        Log.Information("Receiving request at Gateway");

        var client = _httpClientFactory.CreateClient("OrderService");
        var response = await client.GetAsync("orders/process");
        
        Log.Information("Response received from OrderService");
        return Ok();
    }
}
```

**Service B (Order Service):**
```csharp
public class OrderController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet("orders/process")]
    public async Task<IActionResult> Process()
    {
        var correlationId = CorrelationContext.Current;
        Log.Information("Processing order in OrderService");

        var client = _httpClientFactory.CreateClient("PaymentService");
        var response = await client.PostAsync("payments/process", null);
        
        Log.Information("Payment processed");
        return Ok();
    }
}
```

**Service A Logs:**
```
[14:23:45 INF] Gateway a1b2c3d4e5f6789012345678901234ab Receiving request at Gateway
[14:23:46 INF] Gateway a1b2c3d4e5f6789012345678901234ab Response received from OrderService
```

**Service B Logs:**
```
[14:23:45 INF] OrderService a1b2c3d4e5f6789012345678901234ab Processing order in OrderService
[14:23:46 INF] OrderService a1b2c3d4e5f6789012345678901234ab Payment processed
```

**HTTP Request from Service A to Service B:**
```http
GET /orders/process HTTP/1.1
Host: order-service.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-a1b2c3d4e5f6789012345678901234ab-0123456789abcdef-01
```

**Benefits:**
- ✅ Search for `a1b2c3d4e5f6789012345678901234ab` in both services and see the complete flow
- ✅ OpenTelemetry Activities (spans) maintain hierarchical relationships
- ✅ W3C Trace Context enables distributed tracing across services
- ✅ Compatible with observability tools (Jaeger, Zipkin, Application Insights)

## Next Steps

Now that you've seen practical examples, let's see how to resolve common problems in [Lesson 10: Troubleshooting](10-troubleshooting.md).
