# Quick Start

Get started with Traceability in minutes with minimal configuration.

## ASP.NET Core (.NET 8) - Zero Configuration

### 1. Install the package

```bash
dotnet add package WhiteBeard.Traceability
```

### 2. Configure in `Program.cs` (just one line!)

```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuration - everything is automatic!
// Source comes from TRACEABILITY_SERVICENAME or assembly name
// Middleware is registered automatically
// HttpClient is configured automatically
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**With explicit Source (optional):**
```csharp
builder.Services.AddTraceability("MyService");
```

### 3. Use in a Controller

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

### 4. With Logging (Microsoft.Extensions.Logging)

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);

// In Controller
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

**Log Output:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processing request
```

### 5. With HttpClient (automatic propagation)

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
// HttpClient is already automatically configured with CorrelationIdHandler!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// In Controller or Service
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CallExternalApiAsync()
    {
        // Correlation-id is automatically added to the header
        // No need for .AddHttpMessageHandler<CorrelationIdHandler>()!
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return await response.Content.ReadAsStringAsync();
    }
}
```

**Result:**
- ✅ Correlation-id automatically generated for each request
- ✅ Automatically propagated in HTTP calls
- ✅ Automatically included in logs
- ✅ Returned in the `X-Correlation-Id` response header

## Environment Variables

To reduce verbosity, you can use environment variables:

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
$env:LOG_LEVEL="Information"
```

With the environment variable set, you can use:

```csharp
// Source comes automatically from TRACEABILITY_SERVICENAME
builder.Services.AddTraceability();
```

## Next Steps

- See the [User Manual](user-guide/index.md) for a complete progressive guide
- Check [Examples](examples/aspnet-core.md) for more use cases
- Read [Configuration](configuration.md) for advanced options


