# Implementation Patterns

## 1. Conditional Compilation

**Pattern**: Use `#if` directives for framework-specific code.

**Rules**:
- `#if NET8_0` for ASP.NET Core code
- `#if NET48` for .NET Framework code
- Always close with `#endif`
- Common code (without directive) works in both

**Example**:
```csharp
#if NET8_0
using Microsoft.AspNetCore.Http;
// .NET 8 specific code
#endif

#if NET48
using System.Web;
// .NET Framework specific code
#endif

// Common code
```

## 2. AsyncLocal for Asynchronous Isolation

**Pattern**: Always use `AsyncLocal<string>` to store correlation-id.

**Reason**: `AsyncLocal` preserves values across asynchronous continuations, while `ThreadLocal` does not.

**Implementation**:
```csharp
private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
```

**Behavior**:
- Each asynchronous context has its own value
- Value is preserved across `await`
- Isolated between different `Task.Run()` or threads

## 3. Factory Pattern for HttpClient

**Pattern**: Use factory to create configured HttpClient.

**Implementation**: `TraceableHttpClientFactory`

**Advantages**:
- Centralizes configuration
- Facilitates adding handlers
- Supports Polly policies

## 4. DelegatingHandler Pattern

**Pattern**: Use `DelegatingHandler` to intercept HTTP requests.

**Implementation**: `CorrelationIdHandler`

**Behavior**:
- Intercepts before sending request
- Automatically adds header
- Preserves handler pipeline

## 5. Enricher Pattern (Serilog)

**Pattern**: Implement `ILogEventEnricher` to add properties to logs.

**Implementation**: `CorrelationIdEnricher`

**Behavior**:
- Called for each log event
- Adds `CorrelationId` property
- Doesn't modify other enrichers

## 6. ScopeProvider Pattern (Microsoft.Extensions.Logging)

**Pattern**: Implement `IExternalScopeProvider` to add scopes.

**Implementation**: `CorrelationIdScopeProvider`

**Behavior**:
- Adds scope with `CorrelationId`
- Supports internal provider (decorator pattern)
- Preserves existing scopes

## 7. Naming Conventions

- **Classes**: PascalCase
- **Methods**: PascalCase
- **Properties**: PascalCase
- **Private fields**: `_camelCase` (underscore prefix)
- **Constants**: PascalCase
- **Namespaces**: `Traceability.[SubNamespace]`

## 8. Correlation-ID Generation

**Format**: GUID without hyphens (32 characters)

**Implementation**:
```csharp
private static string GenerateNew()
{
    return Guid.NewGuid().ToString("N");
}
```

**Reason**: 
- Compact (32 chars vs 36 with hyphens)
- Compatible with external systems
- Readable in logs

## Directory Structure

```
src/Traceability/
├── Configuration/
│   └── TraceabilityOptions.cs          # Configuration options
│
├── CorrelationContext.cs                # Core: Context management
│
├── Extensions/
│   ├── ApplicationBuilderExtensions.cs  # Extension for IApplicationBuilder (.NET 8)
│   ├── HttpClientExtensions.cs          # Extensions for HttpClient
│   ├── LoggerConfigurationExtensions.cs # Extension for LoggerConfiguration (Serilog)
│   └── ServiceCollectionExtensions.cs   # Extension for IServiceCollection (.NET 8)
│
├── HttpClient/
│   ├── CorrelationIdHandler.cs          # DelegatingHandler for HttpClient
│   └── TraceableHttpClientFactory.cs    # Factory to create HttpClient
│
├── Utilities/
│   └── TraceabilityUtilities.cs         # Shared utilities (GetServiceName, SanitizeSource)
│
├── Logging/
│   ├── CorrelationIdEnricher.cs        # Enricher for Serilog
│   ├── CorrelationIdScopeProvider.cs   # ScopeProvider for MEL
│   ├── DataEnricher.cs                  # Enricher that serializes objects in "data"
│   ├── JsonFormatter.cs                 # Custom JSON formatter
│   ├── SourceEnricher.cs               # Source Enricher for Serilog
│   └── SourceScopeProvider.cs          # Source ScopeProvider for MEL
│
├── Middleware/
│   ├── CorrelationIdHttpModule.cs      # HttpModule (.NET Framework)
│   └── CorrelationIdMiddleware.cs      # Middleware (.NET 8)
│
├── WebApi/
│   └── CorrelationIdMessageHandler.cs  # MessageHandler (.NET Framework Web API)
│
└── Traceability.csproj                  # Project file
```

### Description by Directory

#### Configuration/
- **Purpose**: Configuration classes
- **When to use**: To add new configuration options
- **Files**: `TraceabilityOptions.cs`

#### Extensions/
- **Purpose**: Extension methods to facilitate usage
- **When to use**: To add convenience methods
- **Files**: Extensions for DI, middleware, HttpClient

#### HttpClient/
- **Purpose**: HttpClient integration
- **When to use**: To modify HTTP behavior or add new handlers
- **Files**: Handlers, factory, interfaces

#### Logging/
- **Purpose**: Logging system integrations
- **When to use**: To add support for new loggers
- **Files**: Enrichers, scope providers

#### Middleware/
- **Purpose**: Middleware and HTTP handlers
- **When to use**: To add new HTTP interception points
- **Files**: Middleware (.NET 8), HttpModule (.NET Framework)

#### WebApi/
- **Purpose**: Handlers specific to ASP.NET Web API
- **When to use**: For Web API specific functionality
- **Files**: MessageHandlers
