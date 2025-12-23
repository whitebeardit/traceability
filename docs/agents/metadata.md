# Metadata and Initial Context

## Project Information

- **Name**: Traceability
- **Version**: 1.1.0
- **Type**: NuGet Package
- **License**: MIT
- **Author**: WhiteBeard IT

## Supported Frameworks

- **.NET 8.0**: Full support for ASP.NET Core
- **.NET Framework 4.8**: Support for ASP.NET Web API and Traditional ASP.NET

## Main Dependencies

### .NET 8.0
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

### Common
- `Serilog` (3.1.1) - PrivateAssets: all

## Namespace Structure

```
Traceability
├── Traceability                          # Core: CorrelationContext
├── Traceability.Configuration            # Configuration options
├── Traceability.Extensions               # Extensions for DI and middleware
├── Traceability.HttpClient               # HttpClient integration
├── Traceability.Logging                  # Logging integrations
├── Traceability.Middleware               # Middleware and HTTP handlers
└── Traceability.WebApi                   # Web API specific handlers
```
