# Lesson 5: ASP.NET Framework

In this lesson, you'll learn to integrate Traceability with ASP.NET Framework 4.8 using **zero-code** automatic instrumentation.

## Zero-Code Setup

**Step 1: Install the package**

```bash
Install-Package WhiteBeard.Traceability
```

**Step 2: That's it!** 

The library automatically:
- ✅ Registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`
- ✅ Manages correlation-id automatically

**No code needed!** No `web.config` changes needed! Everything works automatically.

## How It Works

The library uses ASP.NET's `PreApplicationStartMethod` feature to automatically register the `CorrelationIdHttpModule` before your application starts. This means:

- No manual registration in `Global.asax.cs`
- No manual registration in `web.config`
- Everything happens automatically

## ASP.NET Web API

### Basic Example (Zero-Code)

**Global.asax.cs** (only for Web API routes - Traceability is automatic):
```csharp
using System.Web;
using System.Web.Http;

public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        // Only configure Web API routes - Traceability is automatic!
        GlobalConfiguration.Configure(config =>
        {
            config.MapHttpAttributeRoutes();
        });
    }
}
```

**ValuesController.cs:**
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

### Testing

**Request:**
```bash
curl -X GET http://localhost:8080/api/values
```

**Response:**
```json
{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd"
}
```

**Response headers:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd
```

**What happened automatically:**
- ✅ `CorrelationIdHttpModule` intercepted the request
- ✅ Correlation-id was generated
- ✅ Correlation-id was returned in response header

## Traditional ASP.NET

### Basic Example (Zero-Code)

**No web.config changes needed!** The `CorrelationIdHttpModule` is automatically registered.

**MyPage.aspx.cs:**
```csharp
using Traceability;

public class MyPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        // Use correlation-id
    }
}
```

## Example with Serilog

**Global.asax.cs:**
```csharp
using System.Web;
using System.Web.Http;
using Traceability.Extensions;
using Serilog;

public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        // Configure Serilog with Traceability
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WithTraceability("MyService") // Automatically adds CorrelationIdEnricher
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application started. Configuring Web API...");

        // Configure Web API routes
        GlobalConfiguration.Configure(config =>
        {
            config.MapHttpAttributeRoutes();
        });

        Log.Information("Web API configured. Ready for requests...");
    }

    protected void Application_End()
    {
        Log.CloseAndFlush();
    }
}
```

**Controller:**
```csharp
using System.Web.Http;
using Serilog;

public class ValuesController : ApiController
{
    [HttpGet]
    [Route("api/values")]
    public IHttpActionResult Get()
    {
        // Correlation-id appears automatically in logs
        Log.Information("Processing request");
        return Ok(new { Message = "Success" });
    }
}
```

**Log Output:**
```
[14:23:45 INF] MyService a1b2c3d4e5f6789012345678901234ab Processing request
```

## Advanced: Manual Configuration (Optional)

If you need manual control, you can configure manually. However, **this is not needed** for most scenarios.

### Manual Options Configuration

To configure options in .NET Framework, use the static `Configure()` methods:

**For Web API:**
```csharp
CorrelationIdMessageHandler.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

**For Traditional ASP.NET:**
```csharp
CorrelationIdHttpModule.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

### Manual Module Registration (Not Recommended)

**For Web API:**
```csharp
GlobalConfiguration.Configure(config =>
{
    // Manual registration (not needed - automatic via PreApplicationStartMethod)
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

## Differences from .NET 8

In .NET Framework 4.8:

- ✅ **Zero-code**: Automatic registration via `PreApplicationStartMethod` (no manual setup needed)
- ❌ There's no native DI, so use static `Configure()` methods for options (if needed)
- ❌ There's no `IHttpClientFactory`, so manage HttpClient manually
- ✅ Correlation-id functionality is identical
- ✅ If OpenTelemetry is configured externally, logs can include `TraceId/SpanId` via `Activity.Current`

## Next Steps

Now that you know how to integrate with both frameworks, let's see how to use with logging in [Lesson 6: Logging](06-logging.md).
