# Traceability

![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version)

NuGet package for automatic correlation-id management in .NET applications, with support for .NET 8 and .NET Framework 4.8.

> 📖 **[Quick Start](#quick-start)** | [User Manual](docs/user-guide/index.md) | [Complete Documentation](docs/index.md) | [Examples](docs/examples/aspnet-core.md)

## Motivation

In distributed architectures and microservices, tracking a request across multiple services is essential for debugging, monitoring, and performance analysis. The **correlation-id** is a unique identifier that allows you to track a request from its origin to all subsequent calls.

### When to use this library?

Use **Traceability** when you need:

1. **Traceability in Microservices**: Track a request across multiple services
2. **Simplified Debugging**: Quickly identify all logs related to a request
3. **Performance Analysis**: Measure total processing time across multiple services
4. **Monitoring and Observability**: Correlate metrics, traces, and logs from different services

## Features

- ✅ Automatic correlation-id management using `AsyncLocal`
- ✅ Support for .NET 8.0 and .NET Framework 4.8
- ✅ Middleware for ASP.NET Core (.NET 8)
- ✅ HttpModule and MessageHandler for ASP.NET (.NET Framework 4.8)
- ✅ Automatic integration with HttpClient
- ✅ Support for Serilog and Microsoft.Extensions.Logging
- ✅ Integration with Polly for resilience policies
- ✅ Automatic propagation in chained HTTP calls

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
- ✅ Correlation-id automatically generated on each request
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

- **.NET 8.0**: Full support for ASP.NET Core
- **.NET Framework 4.8**: Support for ASP.NET Web API and Traditional ASP.NET

## Contributing

Contributions are welcome! Please open an issue or pull request.

For developers who want to contribute:
- **[CI/CD and Releases](docs/development/ci-cd.md)** - Versioning and publishing process
- **[Technical Documentation](AGENTS.md)** - Complete architecture and technical guide

## License

MIT
