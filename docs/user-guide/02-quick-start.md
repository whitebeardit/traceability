# Lesson 2: Quick Start

Let's start using Traceability! In this lesson, you'll see how to configure the package with minimal code.

## Installation

First, install the package via NuGet:

```bash
dotnet add package WhiteBeard.Traceability
```

Or via Package Manager Console:

```powershell
Install-Package WhiteBeard.Traceability
```

## Minimal Configuration

### ASP.NET Core (.NET 8) - Zero Config

The simplest possible configuration - just one line of code!

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuration - everything is automatic!
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Done!** Now Traceability is configured and working.

### ASP.NET Framework 4.8 - Zero Code

The simplest possible setup - just install the package!

**1. Install the package:**
```bash
Install-Package WhiteBeard.Traceability
```

**2. That's it!** No code needed!

The library automatically:
- ✅ Registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`
- ✅ Initializes `ActivityListener` for OpenTelemetry spans
- ✅ Creates Activities (spans) for each HTTP request
- ✅ Names spans using route templates

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
            .WithTraceability("MyService")
            .WriteTo.Console()
            .CreateLogger();

        // Configure Web API routes (standard setup)
        GlobalConfiguration.Configure(config =>
        {
            config.MapHttpAttributeRoutes();
        });
    }
}
```

**Done!** Traceability is working automatically - no manual registration needed!

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

Make a request to the endpoint:

```bash
curl -X GET http://localhost:5000/api/values
```

**Expected response:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab"
}
```

And in the response header:
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## What Happened?

1. ✅ The middleware was automatically registered
2. ✅ An OpenTelemetry Activity (span) was automatically created
3. ✅ A correlation-id/trace-id was automatically generated (from Activity.TraceId or 32-character GUID)
4. ✅ The correlation-id was added to the asynchronous context
5. ✅ The correlation-id was returned in the response header
6. ✅ Ready for distributed tracing with OpenTelemetry-compatible tools

## With Explicit Source (Optional)

If you want to define the service name explicitly:

```csharp
builder.Services.AddTraceability("MyService");
```

This adds the `Source` field to logs (we'll see more about this in Lesson 6).

## Next Steps

Now that you've seen the basics, let's learn more about `CorrelationContext` in [Lesson 3: Basic Usage](03-basic-usage.md).
