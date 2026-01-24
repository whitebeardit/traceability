# Metadata and Initial Context

## Project Information

- **Name**: Traceability
- **Version**: ![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version)
- **Type**: NuGet Package
- **License**: MIT
- **Author**: WhiteBeard IT

## Supported Frameworks

The package uses **multi-targeting** with three target frameworks:

- **.NET Standard 2.0**: Portable core library
  - Provides core functionality: CorrelationContext, HttpClient integration, Logging enrichers, Configuration
  - Compatible with .NET 6, 7, 8, .NET Framework 4.6.1+, and other .NET Standard 2.0 implementations
- **.NET 8.0**: Full ASP.NET Core integration
  - Automatic middleware registration
  - Dependency injection extensions
  - HttpContext integration
- **.NET Framework 4.8+**: Full ASP.NET Framework integration
  - Automatic HttpModule registration via `PreApplicationStartMethod`
  - Web API and MVC support
  - System.Web integration

## Main Dependencies

### .NET Standard 2.0 (Portable Core)
- `System.Diagnostics.DiagnosticSource` (7.0.2) - For ActivitySource/OpenTelemetry support
- `Serilog` (3.1.1) - PrivateAssets: all
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Polly` (7.2.3)

### .NET 8.0 (ASP.NET Core Integration)
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.AspNetCore.Hosting.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8 (ASP.NET Framework Integration)
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.AspNet.WebApi.Core` (5.2.9)
- `Microsoft.AspNet.Mvc` (5.2.9)
- `Microsoft.Web.Infrastructure` (2.0.1)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)
- `System.Diagnostics.DiagnosticSource` (7.0.2)

### Common (All Targets)
- `Serilog` (3.1.1) - PrivateAssets: all

## Namespace Structure

```
Traceability
├── Traceability                          # Core: CorrelationContext
├── Traceability.Core                     # Core business logic
│   ├── Traceability.Core.Interfaces      # Core interfaces (ICorrelationIdValidator, IActivityFactory, etc.)
│   └── Traceability.Core.Services        # Shared service implementations
├── Traceability.Configuration            # Configuration options
├── Traceability.Extensions               # Extensions for DI and middleware
├── Traceability.HttpClient               # HttpClient integration
├── Traceability.Logging                  # Logging integrations
├── Traceability.Middleware               # Middleware and HTTP handlers
└── Traceability.WebApi                   # Web API specific handlers
```
