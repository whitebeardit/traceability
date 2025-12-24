# Traceability Documentation

Welcome to the complete documentation for the Traceability package for automatic correlation-id management in .NET applications.

## Quick Start

- [Quick Start](getting-started.md) - Get started in minutes
- [Installation](installation.md) - How to install the package

## Guides

- [User Manual](user-guide/index.md) - Progressive guide for beginners
- [Configuration](configuration.md) - Detailed configuration options
- [API Reference](api-reference.md) - Complete API documentation
- [Advanced Topics](advanced.md) - Advanced features and use cases

## Examples

- [ASP.NET Core](examples/aspnet-core.md) - Examples for .NET 8
- [ASP.NET Framework](examples/aspnet-framework.md) - Examples for .NET Framework 4.8
- [Console Application](examples/console.md) - Examples for console applications
- [HTTP Requests](examples/http-requests.md) - HTTP request examples

## Support

- [Troubleshooting](troubleshooting.md) - Common problem solutions
- [FAQ](troubleshooting.md#faq) - Frequently asked questions

## Technical Documentation

For developers who want to contribute or understand the internal architecture:

- [CI/CD and Releases](development/ci-cd.md) - CI/CD pipeline and release process
- [Documentation for LLMs](../AGENTS.md) - Complete architecture and technical guide

## What is Traceability?

Traceability is a NuGet package that automatically manages correlation-id in .NET applications, allowing you to track requests across multiple services in distributed architectures.

### Key Features

- ✅ Automatic correlation-id management using `AsyncLocal`
- ✅ Support for .NET 8.0 and .NET Framework 4.8
- ✅ Middleware for ASP.NET Core (.NET 8)
- ✅ HttpModule and MessageHandler for ASP.NET (.NET Framework 4.8)
- ✅ Automatic integration with HttpClient
- ✅ Support for Serilog and Microsoft.Extensions.Logging
- ✅ Integration with Polly for resilience policies
- ✅ Automatic propagation in chained HTTP calls

### When to Use?

Use Traceability when you need:

1. **Microservices Traceability**: Track a request across multiple services
2. **Simplified Debugging**: Quickly identify all logs related to a request
3. **Performance Analysis**: Measure total processing time across multiple services
4. **Monitoring and Observability**: Correlate metrics, traces, and logs from different services

## Supported Frameworks

- **.NET 8.0**: Full support for ASP.NET Core
- **.NET Framework 4.8**: Support for ASP.NET Web API and Traditional ASP.NET

## Quick Installation

```bash
dotnet add package WhiteBeard.Traceability
```

For more details, see [Installation](installation.md).

## Quick Example

```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuration - everything is automatic!
builder.Services.AddTraceability("MyService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

For more examples, see [Quick Start](getting-started.md) or the [User Manual](user-guide/index.md).

