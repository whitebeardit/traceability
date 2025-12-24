# Traceability - Architecture and Guide for LLMs

> **Note**: This documentation has been refactored into smaller files for easier navigation. See the [complete index at `docs/agents/`](docs/agents/index.md).

## Quick Index

This technical documentation is organized in the following files:

### Basic Information
- [Metadata and Initial Context](docs/agents/metadata.md) - Project information, dependencies and namespace structure

### Architecture
- [High-Level Architecture](docs/agents/architecture.md) - Component diagrams and data flows

### Components
- [Core Components](docs/agents/components.md) - Technical details of all package components

### Patterns and Practices
- [Implementation Patterns](docs/agents/patterns.md) - Code patterns and conventions used
- [Rules and Constraints](docs/agents/rules.md) - Mandatory rules and design constraints
- [Design Decisions](docs/agents/design-decisions.md) - Rationale behind architectural decisions

### Guides
- [Modification Guide](docs/agents/modification-guide.md) - How to add new components and maintain compatibility
- [Code Examples](docs/agents/code-examples.md) - Practical usage examples

### Reference
- [Glossary](docs/agents/glossary.md) - Technical term definitions

## Overview

Traceability is a NuGet package for automatic correlation-id management in .NET applications, with support for .NET 8 and .NET Framework 4.8.

### Supported Frameworks
- **.NET 8.0**: Full support for ASP.NET Core
- **.NET Framework 4.8**: Support for ASP.NET Web API and Traditional ASP.NET

### Fundamental Principles

1. **AsyncLocal for Isolation**: Always use `AsyncLocal<string>` for async context
2. **Conditional Compilation**: Framework-specific code must use `#if NET8_0` or `#if NET48`
3. **Default Header**: Always use `X-Correlation-Id` as the default header
4. **GUID without Hyphens**: Generate correlation-id as a 32-character GUID (without hyphens)
5. **Don't Modify Existing**: Never overwrite existing correlation-id in context
6. **Zero Configuration**: Works without configuration, but allows customization

## Project Structure

```
src/Traceability/
├── Configuration/          # Configuration options
├── CorrelationContext.cs    # Core: Context management
├── Extensions/             # Extension methods
├── HttpClient/             # HttpClient integration
├── Logging/                # Logging integrations
├── Middleware/             # Middleware and HTTP handlers
├── Utilities/              # Shared utilities
└── WebApi/                 # Web API specific handlers
```

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

**For complete documentation, see**: [docs/agents/index.md](docs/agents/index.md)

**Last updated**: Based on version ![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version) of the Traceability project
