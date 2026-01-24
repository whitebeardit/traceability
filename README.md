# Traceability

![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version)
![Build Status](https://img.shields.io/github/actions/workflow/status/whitebeardit/traceability/.github/workflows/ci.yml?branch=main&label=build&style=flat-square)
![License](https://img.shields.io/github/license/whitebeardit/traceability?style=flat-square)
![NuGet Downloads](https://img.shields.io/nuget/dt/WhiteBeard.Traceability?style=flat-square&label=downloads)
![.NET Version](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%208.0%20%7C%20Framework%204.8-512BD4?style=flat-square&logo=dotnet)
![GitHub Stars](https://img.shields.io/github/stars/whitebeardit/traceability?style=flat-square&logo=github)
![GitHub Forks](https://img.shields.io/github/forks/whitebeardit/traceability?style=flat-square&logo=github)

## 💝 Support this project

If you find this project useful, consider supporting its development:

<a href="https://www.buymeacoffee.com/almerindo" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## 🤖 Get help with AI

Have questions about the API, implementation, or documentation? Ask DeepWiki, an AI agent that can help you understand and use Traceability.

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/whitebeardit/traceability)

---

NuGet package for automatic correlation-id management in .NET applications, with support for .NET Standard 2.0+ (core), .NET 8 (ASP.NET Core), and .NET Framework 4.8+ (ASP.NET Framework).

> 📖 **[Quick Start](#quick-start)** | [User Manual](docs/user-guide/index.md) | [Complete Documentation](docs/index.md) | [Examples](docs/examples/aspnet-core.md)
>
> **Indexed Documentation (Devin DeepWiki):** [DeepWiki for `whitebeardit/traceability`](https://deepwiki.com/whitebeardit/traceability)

## Motivation

In distributed architectures and microservices, tracking a request across multiple services is essential for debugging, monitoring, and performance analysis. The **correlation-id** is a unique identifier that allows you to track a request from its origin to all subsequent calls.

### When to use this library?

Use **Traceability** when you need:

1. **Traceability in Microservices**: Track a request across multiple services
2. **Simplified Debugging**: Quickly identify all logs related to a request
3. **Performance Analysis**: Measure total processing time across multiple services
4. **Monitoring and Observability**: Correlate metrics, traces, and logs from different services

## Features

- ✅ **Zero-code/Zero-config**: Works automatically - just install the package!
- ✅ Automatic correlation-id management using `AsyncLocal<string>` (independent from OpenTelemetry TraceId)
- ✅ OpenTelemetry integration with automatic Activity (span) creation
- ✅ Adds `correlation.id` tag to spans (search by correlation-ID in Grafana Tempo)
- ✅ Automatic span naming using route templates (e.g., `GET api/values/{id}`)
- ✅ W3C Trace Context propagation (`traceparent`, `tracestate` headers)
- ✅ **Portable core** via .NET Standard 2.0 (works with .NET 6, 7, 8, and compatible frameworks)
- ✅ **Full support for .NET 8.0** (ASP.NET Core with automatic middleware registration)
- ✅ **Full support for .NET Framework 4.8+** (ASP.NET Web API and Traditional ASP.NET with automatic HttpModule registration)
- ✅ **Zero-code for .NET Framework 4.8**: Automatic registration via `PreApplicationStartMethod`
- ✅ **Zero-config for .NET 8.0**: Automatic middleware and HttpClient registration
- ✅ Middleware for ASP.NET Core (.NET 8)
- ✅ HttpModule and MessageHandler for ASP.NET (.NET Framework 4.8)
- ✅ Automatic integration with HttpClient
- ✅ Support for Serilog and Microsoft.Extensions.Logging
- ✅ Integration with Polly for resilience policies
- ✅ Automatic propagation in chained HTTP calls
- ✅ Hierarchical span relationships for distributed tracing

## Installation

```bash
dotnet add package WhiteBeard.Traceability
```

## Quick Start

### ASP.NET Core (.NET 8) - Zero Configuration

**1. Install the package:**
```bash
dotnet add package WhiteBeard.Traceability
```

**2. Configure in `Program.cs` (just one line!):**

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

**3. Use in a Controller:**

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

**Result:**
- ✅ Correlation-id automatically generated on each request (if not provided via `X-Correlation-Id`)
- ✅ OpenTelemetry Activity (span) automatically created
- ✅ Span automatically named using route template (e.g., `GET api/values`)
- ✅ Automatically propagated in HTTP calls (with W3C Trace Context)
- ✅ Automatically included in logs
- ✅ Returned in the `X-Correlation-Id` response header
- ✅ Compatible with all OpenTelemetry-compatible observability tools

### ASP.NET Framework 4.8 - Zero Code

**1. Install the package:**
```bash
Install-Package WhiteBeard.Traceability
```

**2. That's it!** No code needed!

The library automatically:
- ✅ Registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`
- ✅ Initializes `ActivityListener` for OpenTelemetry spans
- ✅ Creates Activities (spans) for each HTTP request
- ✅ Names spans using route templates (e.g., `GET api/values/{id}`)
- ✅ Manages correlation-id automatically

**Optional: Configure Serilog**
```csharp
// Global.asax.cs
using Traceability.Extensions;
using Serilog;

protected void Application_Start()
{
    Log.Logger = new LoggerConfiguration()
        .WithTraceability("MyService")
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    GlobalConfiguration.Configure(config =>
    {
        config.MapHttpAttributeRoutes();
    });
}
```

**3. Use in a Controller:**
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

**Result:**
- ✅ Correlation-id automatically generated for each request
- ✅ OpenTelemetry Activity (span) automatically created
- ✅ Span automatically named using route template
- ✅ Automatically propagated in HTTP calls
- ✅ Automatically included in logs (if Serilog is configured)
- ✅ Returned in the `X-Correlation-Id` response header

**Opt-out: Disable Automatic Spans**

If you need to disable automatic span creation:

**Option 1: appSettings in Web.config**
```xml
<appSettings>
  <add key="Traceability:SpansEnabled" value="false" />
</appSettings>
```

**Option 2: Environment Variable**
```powershell
$env:TRACEABILITY_SPANS_ENABLED="false"
```

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

With the environment variable defined, you can use:

```csharp
// Source comes automatically from TRACEABILITY_SERVICENAME
builder.Services.AddTraceability();
```

## Documentation

- **[User Manual](docs/user-guide/index.md)** - Progressive guide for beginners
- **[Quick Start](docs/getting-started.md)** - Get started in minutes
- **[Installation](docs/installation.md)** - Installation guide
- **[Configuration](docs/configuration.md)** - Detailed configuration options
- **[API Reference](docs/api-reference.md)** - Complete API documentation
- **[Examples](docs/examples/aspnet-core.md)** - Practical examples
- **[Troubleshooting](docs/troubleshooting.md)** - Common problem solutions
- **[Advanced Topics](docs/advanced.md)** - Advanced features

## Quick Examples

### With Logging

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);

// In Controller
_logger.LogInformation("Processing request");
// Output: => CorrelationId: a1b2c3d4e5f6789012345678901234ab
```

### With HttpClient

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// In Controller
var client = _httpClientFactory.CreateClient("ExternalApi");
// Correlation-id is automatically added to the header
```

## Supported Frameworks

The package uses **multi-targeting** to provide the best experience for each platform:

- **.NET Standard 2.0**: Portable core library (CorrelationContext, HttpClient integration, Logging enrichers, Configuration)
  - Compatible with .NET 6, 7, 8, .NET Framework 4.6.1+, and other .NET Standard 2.0 implementations
  - Provides core functionality without framework-specific dependencies
- **.NET 8.0**: Full ASP.NET Core integration
  - Automatic middleware registration
  - Dependency injection extensions
  - HttpContext integration
- **.NET Framework 4.8+**: Full ASP.NET Framework integration
  - Automatic HttpModule registration via `PreApplicationStartMethod`
  - Web API and MVC support
  - System.Web integration

**Note**: The package automatically selects the appropriate implementation based on your target framework. Core functionality (correlation-id management, HttpClient propagation, logging) is available on all platforms via the .NET Standard 2.0 target.

## Contributing

Contributions are welcome! Please open an issue or pull request.

For developers who want to contribute:
- **[CI/CD and Releases](docs/development/ci-cd.md)** - Versioning and publishing process
- **[Technical Documentation](AGENTS.md)** - Complete architecture and technical guide

## License

MIT
