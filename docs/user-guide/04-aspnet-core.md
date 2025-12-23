# Lesson 4: ASP.NET Core

In this lesson, you'll learn to integrate Traceability with ASP.NET Core (.NET 8).

## Basic Configuration

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

**What happens automatically:**
- ✅ Middleware is automatically registered via `IStartupFilter`
- ✅ HttpClient is automatically configured with `CorrelationIdHandler`
- ✅ Correlation-id is automatically generated for each request

## Using in a Controller

**ValuesController.cs:**
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
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

## Testing

**Request:**
```bash
curl -X GET http://localhost:5000/api/values
```

**Response:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab"
}
```

**Response headers:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Request with Existing Correlation-ID

If you send a request with the `X-Correlation-Id` header, the middleware reuses the value:

**Request:**
```bash
curl -X GET http://localhost:5000/api/values \
  -H "X-Correlation-Id: 12345678901234567890123456789012"
```

**Response:**
```json
{
  "correlationId": "12345678901234567890123456789012"
}
```

The same correlation-id is returned, ensuring traceability in the chain.

## Example with Logging

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

**Controller:**
```csharp
public class ValuesController : ControllerBase
{
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id appears automatically in logs
        _logger.LogInformation("Processing request");
        return Ok();
    }
}
```

**Log output:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processing request
```

## Example with HttpClient

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
// HttpClient is already automatically configured!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

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

The external HTTP request automatically includes the header:
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Disable Auto-Registration (Advanced)

If you need manual control over middleware order:

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Manual registration
app.MapControllers();
app.Run();
```

## Next Steps

Now that you know how to integrate with ASP.NET Core, let's see how to do the same with .NET Framework in [Lesson 5: ASP.NET Framework](05-aspnet-framework.md), or skip to [Lesson 6: Logging](06-logging.md) if you only use .NET 8.
