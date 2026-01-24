# Traceability - Documentation for LLMs

This directory contains the complete technical documentation of the Traceability project, organized to facilitate use by LLMs and developers.

## Index

### Basic Information
- [Metadata and Initial Context](metadata.md) - Project information, dependencies and namespace structure

### Architecture
- [High-Level Architecture](architecture.md) - Component diagrams and data flows

### Components
- [Core Components](components.md) - Technical details of all package components

### Patterns and Practices
- [Implementation Patterns](patterns.md) - Code patterns and conventions used
- [Rules and Constraints](rules.md) - Mandatory rules and design constraints
- [Design Decisions](design-decisions.md) - Rationales behind architectural decisions

### Guides
- [Modification Guide](modification-guide.md) - How to add new components and maintain compatibility
- [Code Examples](code-examples.md) - Practical usage examples

### Reference
- [Glossary](glossary.md) - Technical term definitions

## Project Structure

```
src/Traceability/
├── Configuration/          # Configuration options
├── Core/                   # Core business logic and abstractions
│   ├── Interfaces/         # Core interfaces (ICorrelationIdValidator, IActivityFactory, etc.)
│   ├── Services/           # Shared service implementations
│   └── Constants.cs        # Shared constants
├── CorrelationContext.cs    # Core: Context management
├── Extensions/             # Extension methods
├── HttpClient/             # HttpClient integration
├── Logging/                # Logging integrations
├── Middleware/             # Middleware and HTTP handlers
├── Utilities/              # Shared utilities
└── WebApi/                 # Web API specific handlers
```

## Supported Frameworks

The package uses **multi-targeting** with three target frameworks:

- **.NET Standard 2.0**: Portable core library (CorrelationContext, HttpClient, Logging, Configuration)
  - Compatible with .NET 6, 7, 8, .NET Framework 4.6.1+, and other .NET Standard 2.0 implementations
- **.NET 8.0**: Full ASP.NET Core integration
  - Automatic middleware registration
  - Dependency injection extensions
  - HttpContext integration
- **.NET Framework 4.8+**: Full ASP.NET Framework integration
  - Automatic HttpModule registration via `PreApplicationStartMethod`
  - Web API and MVC support
  - System.Web integration

## Fundamental Principles

1. **Correlation-ID Independence**: Correlation-ID is managed via `AsyncLocal<string>` and is **independent** from OpenTelemetry `Activity.TraceId`
2. **Conditional Compilation**: Framework-specific code should use `#if NET8_0`, `#if NET48`, or `#if NETSTANDARD2_0` as appropriate
3. **Default Header**: Always use `X-Correlation-Id` as default header (for backward compatibility)
4. **W3C Trace Context**: Read inbound `traceparent`/`tracestate` when present, and propagate `traceparent` when trace context is available. Traceability does not explicitly emit `tracestate`.
5. **GUID without Hyphens**: Generate correlation-id as 32-character GUID (without hyphens) when correlation-ID needs to be created
6. **Don't Modify Existing**: Never overwrite existing correlation-id in context
7. **Zero Configuration**: Works without configuration, but allows customization
8. **Automatic Activity Creation**: Creates OpenTelemetry Activities automatically when OpenTelemetry SDK is not configured
9. **Span Searchability**: Add `correlation.id` tag to spans when correlation-ID is available (enables Grafana Tempo search)

## Quick References

### Main Files
- Core: `src/Traceability/CorrelationContext.cs`
- Middleware (.NET 8): `src/Traceability/Middleware/CorrelationIdMiddleware.cs`
- HttpModule (.NET Framework): `src/Traceability/Middleware/CorrelationIdHttpModule.cs`
- MessageHandler: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`
- HttpClient Handler: `src/Traceability/HttpClient/CorrelationIdHandler.cs`
- Factory: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`
- Configuration: `src/Traceability/Configuration/TraceabilityOptions.cs`

### Examples
- ASP.NET Core: `samples/Sample.WebApi.Net8/`
- Console: `samples/Sample.Console.Net8/`
- .NET Framework: `samples/Sample.WebApi.NetFramework/`

### Tests
- All tests: `tests/Traceability.Tests/`

---

**Last update**: Based on version ![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version) of the Traceability project
