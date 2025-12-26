# Examples - ASP.NET Framework 4.8

Practical examples of using Traceability in .NET Framework applications with **zero-code** automatic instrumentation.

## Zero-Code Setup

**Just install the package - that's it!**

```bash
Install-Package WhiteBeard.Traceability
```

The library automatically:
- ✅ Registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`
- ✅ Initializes `ActivityListener` for OpenTelemetry spans
- ✅ Creates Activities (spans) for each HTTP request
- ✅ Names spans using route templates (e.g., `GET api/values/{id}`)
- ✅ Manages correlation-id automatically

**No code needed!** No `web.config` changes needed! Everything works automatically.

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

**Controller:**
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
        // Activity (span) is automatically created with name "GET api/values"
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

**What happens automatically:**
- ✅ `CorrelationIdHttpModule` is automatically registered
- ✅ OpenTelemetry Activity (span) is automatically created
- ✅ Span is automatically named: `GET api/values`
- ✅ Correlation-id is automatically generated
- ✅ Correlation-id is returned in `X-Correlation-Id` response header

## Traditional ASP.NET

### Basic Example (Zero-Code)

**No web.config changes needed!** The `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod`.

**Page/Controller:**
```csharp
using Traceability;

public class MyPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Correlation-id is automatically available
        // Activity (span) is automatically created
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
using Traceability;
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

## Example with HttpClient

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using Traceability.HttpClient;

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
        // Child Activity (span) is automatically created for the HTTP call
        var response = await client.GetAsync("https://api.example.com/endpoint");
        return await response.Content.ReadAsStringAsync();
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

## Opt-out: Disable Automatic Spans

If you need to disable automatic span creation:

**Option 1: appSettings in Web.config**
```xml
<configuration>
  <appSettings>
    <add key="Traceability:SpansEnabled" value="false" />
  </appSettings>
</configuration>
```

**Option 2: Environment Variable**
```powershell
$env:TRACEABILITY_SPANS_ENABLED="false"
```

## Advanced: Manual Configuration (Optional)

If you need manual control over the registration, you can configure manually. However, **this is not needed** - the library handles everything automatically.

### Manual Configuration for Web API

**Global.asax.cs:**
```csharp
using System.Web.Http;
using Traceability.WebApi;

public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            // Manual registration (not needed - automatic via PreApplicationStartMethod)
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### Manual Configuration for Traditional ASP.NET

**web.config:**
```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

**Global.asax.cs (optional - to configure options):**
```csharp
using Traceability.Middleware;
using Traceability.Configuration;

protected void Application_Start()
{
    CorrelationIdHttpModule.Configure(new TraceabilityOptions
    {
        HeaderName = "X-Correlation-Id",
        ValidateCorrelationIdFormat = true
    });
}
```

**Note**: Manual configuration is only needed for edge cases. The default zero-code approach works for 99% of scenarios.

## Complete Example

See the complete example in `traceability-examples/NetFramework48Tests/` which demonstrates zero-code usage.
