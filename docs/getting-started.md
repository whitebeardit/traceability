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
 
> Observação: para tracing distribuído (spans, exporters), configure OpenTelemetry na aplicação. Traceability enriquece logs via `Activity.Current` quando disponível.

## ASP.NET Framework 4.8 - Zero Code

### 1. Install the package

```bash
Install-Package WhiteBeard.Traceability
```

Or via Package Manager Console:
```powershell
Install-Package WhiteBeard.Traceability
```

### 2. That's it! Everything works automatically

**No code needed!** The library automatically:
- ✅ Registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`
- ✅ Manages correlation-id automatically
- ✅ Propagates correlation-id in response headers

**Global.asax.cs** (only if you want to configure Serilog):
```csharp
using System.Web;
using System.Web.Http;
using Traceability.Extensions;
using Serilog;

public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        // Optional: Configure Serilog with Traceability
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WithTraceability("MyService") // Automatically adds CorrelationIdEnricher
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Configure Web API routes (standard Web API setup)
        GlobalConfiguration.Configure(config =>
        {
            config.MapHttpAttributeRoutes();
        });
    }

    protected void Application_End()
    {
        Log.CloseAndFlush();
    }
}
```

**Note**: The `CorrelationIdHttpModule` is automatically registered - no need to add it to `web.config`!

### 3. Use in a Controller

```csharp
using System.Web.Http;
using Traceability;

public class ValuesController : ApiController
{
    [HttpGet]
    [Route("api/values")]
    public IHttpActionResult Get()
    {
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

### 4. With Serilog Logging

```csharp
// Global.asax.cs
Log.Logger = new LoggerConfiguration()
    .WithTraceability("MyService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// In Controller
Log.Information("Processing request");
```

**Log Output:**
```
[14:23:45 INF] MyService a1b2c3d4e5f6789012345678901234ab Processing request
```

### 5. With HttpClient (automatic propagation)

```csharp
// In Controller or Service
public class MyService
{
    public async Task<string> CallExternalApiAsync()
    {
        // Create HttpClient with CorrelationIdHandler
        var handler = new CorrelationIdHandler
        {
            InnerHandler = new HttpClientHandler()
        };
        var client = new HttpClient(handler);
        
        // Correlation-id is automatically added to the header
        var response = await client.GetAsync("https://api.example.com/endpoint");
        return await response.Content.ReadAsStringAsync();
    }
}
```

**Result:**
- ✅ Correlation-id automatically generated for each request
- ✅ Automatically propagated in HTTP calls
- ✅ Automatically included in logs (if Serilog is configured)
- ✅ Returned in the `X-Correlation-Id` response header
 
> Observação: Traceability **não cria spans**. Para tracing distribuído, configure OpenTelemetry na aplicação. Traceability enriquece logs via `Activity.Current` quando disponível.

### Advanced: Manual Configuration (Optional)

If you need manual control, you can still configure manually:

**For Web API:**
```csharp
// Global.asax.cs
GlobalConfiguration.Configure(config =>
{
    config.MessageHandlers.Add(new CorrelationIdMessageHandler());
});
```

**For Traditional ASP.NET:**
```xml
<!-- web.config -->
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

However, **manual configuration is not needed** - the library handles everything automatically via `PreApplicationStartMethod`.

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


