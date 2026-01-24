# Design Decisions and Rationales

## Why `AsyncLocal` instead of `ThreadLocal`?

**Reason**: `AsyncLocal` preserves values across asynchronous continuations, while `ThreadLocal` does not.

**Example of problem with `ThreadLocal`**:
```csharp
// ❌ With ThreadLocal (doesn't work)
ThreadLocal<string> correlationId = new ThreadLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Context may change thread
// correlationId.Value may be null or different
```

**Solution with `AsyncLocal`**:
```csharp
// ✅ With AsyncLocal (works)
AsyncLocal<string> correlationId = new AsyncLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Value preserved
// correlationId.Value is still "abc123"
```

## Why multi-framework support?

**Reason**: 
- Many companies still use .NET Framework 4.8
- Gradual migration is common
- Need for traceability in both environments

**Trade-off**: More complex code with conditional compilation, but greater compatibility.

## Why use .NET's `Activity` (OpenTelemetry)?

**Reason**:
- `Activity` is the industry standard for distributed tracing (OpenTelemetry)
- Provides `TraceId` that is automatically propagated across services
- Enables hierarchical spans (parent/child relationships)
- Compatible with all OpenTelemetry-compatible observability tools (Jaeger, Zipkin, Application Insights, etc.)
- W3C Trace Context standard support (`traceparent`/`tracestate` are supported on inbound). Traceability propagates `traceparent` when trace context is available, but does not explicitly emit `tracestate`.
- Automatic integration with existing OpenTelemetry instrumentation

**Implementation Strategy**:
- **Correlation-ID Independence**: Correlation-ID is managed independently via `AsyncLocal<string>`, separate from OpenTelemetry's trace ID
- **Dual Tracking**: Both correlation-ID and trace ID appear in logs and spans independently
- **Automatic Creation**: Creates Activities automatically when OpenTelemetry is not configured
- **Backward Compatibility**: Maintains `X-Correlation-Id` header for services not using OpenTelemetry
- **Span Tags**: Traceability does not create spans (OpenTelemetry is configured externally)

**Benefits**:
- ✅ Industry-standard distributed tracing
- ✅ Automatic trace context propagation
- ✅ Hierarchical span relationships
- ✅ Compatible with all major observability platforms
- ✅ Works with or without OpenTelemetry SDK configured
- ✅ Maintains backward compatibility with existing correlation-id systems
- ✅ Independent correlation-ID tracking (business-level) separate from technical trace ID
- ✅ Correlate logs with traces by including both `CorrelationId` and `TraceId` in logs (TraceId via `Activity.Current` when available)

**When OpenTelemetry is configured**:
- Uses existing Activities created by OpenTelemetry SDK
- Automatically integrates with OpenTelemetry instrumentation
- Correlation-ID is propagated via `X-Correlation-Id` and added to logs via `CorrelationIdEnricher`
- No additional configuration needed

**When OpenTelemetry is not configured**:
- Traceability still provides correlation-id propagation and log enrichment (CorrelationId)
- TraceId/SpanId fields will not exist unless `Activity.Current` is provided by external instrumentation

## Why JSON Log Uniformization and Environment Variables?

**Reason**: Ensure that all logs from different applications and services follow the same pattern, facilitating analysis, correlation, and monitoring in distributed environments.

**Design Decisions**:

1. **Prefer JSON Output**:
   - **Reason**: JSON format is structured, easily parseable, and supported by all log aggregation tools (ELK, Splunk, etc.)
   - **Benefit**: Consistent, machine-friendly logs across services
   - **Implementation**: `WithTraceability()` and `WithTraceabilityJson()` configure enrichers/properties; the actual JSON output depends on configuring the Serilog sink with a JSON formatter (e.g., `Traceability.Logging.JsonFormatter`).

2. **Environment Variables for Source and LogLevel**:
   - **Reason**: Reduce verbosity in configuration and allow changes without recompilation
   - **Benefit**: Centralized configuration via environment variables facilitates management in production
   - **Priority**: Parameter > Options > Env Var > Error (ensures flexibility but enforces standard)

3. **Error When Source Not Available**:
   - **Reason**: Force all services to have Source defined to ensure traceability
   - **Benefit**: Prevents logs without origin identification, facilitating debugging in distributed environments
   - **Trade-off**: May be more restrictive, but ensures log quality

4. **Decision Flow for Source**:
   - **Reason**: Allow multiple configuration forms while maintaining clear priority
   - **Benefit**: Flexibility for different scenarios (development, testing, production)
   - **Implementation**: `TraceabilityUtilities.GetServiceName()` method centralizes decision logic
   - **Sanitization**: Source is automatically sanitized via `TraceabilityUtilities.SanitizeSource()` to ensure security

**Example of Problem Solved**:
```csharp
// ❌ Before: Each service configures Source differently
// Service A
Log.Logger = new LoggerConfiguration()
    .Enrich.With(new SourceEnricher("ServiceA"))
    .WriteTo.Console() // Different text format
    .CreateLogger();

// Service B
Log.Logger = new LoggerConfiguration()
    .Enrich.With(new SourceEnricher("ServiceB"))
    .WriteTo.File("log.txt") // Different format
    .CreateLogger();

// ✅ Now: All services follow the same pattern
// export TRACEABILITY_SERVICENAME="ServiceA"
Log.Logger = new LoggerConfiguration()
    .WithTraceability() // Source from env var, JSON output
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// export TRACEABILITY_SERVICENAME="ServiceB"
Log.Logger = new LoggerConfiguration()
    .WithTraceability() // Source from env var, JSON output
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

## Trade-offs and Known Limitations

1. **✅ RESOLVED**: `TraceabilityOptions` is now fully integrated
   - Header can be customized via `HeaderName`
   - `AlwaysGenerateNew` is used in middlewares/handlers
   - `ValidateCorrelationIdFormat` added for optional validation

2. **✅ RESOLVED**: `ITraceableHttpClient` interface was removed
   - Unused interface was removed to simplify API
   - `AddTraceableHttpClient<TClient>` now works with any class (no interface constraint)

3. **Trade-off**: Conditional compilation increases complexity
   - Benefit: Multi-framework support
   - Cost: More difficult to maintain

4. **Limitation**: No support for correlation-id in messaging (RabbitMQ, Kafka, etc.)
   - Only HTTP currently

5. **Trade-off**: Simple GUID instead of more informative IDs
   - Benefit: Simple, unique, no collisions
   - Cost: Doesn't contain semantic information

6. **✅ RESOLVED - Socket Exhaustion**: Methods that caused socket exhaustion were removed
   - **Implemented solution**: All HttpClient creation methods use `IHttpClientFactory`
   - **Available methods**: `CreateFromFactory()` and `AddTraceableHttpClient()` (extension)
   - **Status**: Clean API that enforces best practices from the start

## Security and Robustness Improvements Implemented

### Stack Overflow Protections
- **JsonFormatter**: Limit of 10 levels in InnerException chains
- **DataEnricher**: Limit of 10 levels of depth in nested objects
- **DataEnricher**: Automatic circular reference detection

### OutOfMemoryException Protections
- **DataEnricher**: Limit of 1000 elements per collection (Dictionary, Structure, Sequence)
- Informative messages when limits are reached

### Validation and Sanitization
- **HeaderName**: Automatic validation with fallback to default "X-Correlation-Id"
- **Source**: Automatic sanitization to remove invalid characters and limit size (100 characters)
- **CorrelationId**: Optional format validation (maximum size 128 characters)

### Thread-Safety
- **.NET Framework**: Static configuration now uses `volatile` and `lock` to ensure thread-safety
- **CorrelationContext**: Implementation improvements to ensure thread-safety in all scenarios

## Refactoring Decisions (Clean Code and Clean Architecture)

### Why Create Abstractions (Interfaces)

**Reason**: To eliminate code duplication and improve testability and maintainability.

**Implementation**:
- Created interfaces (`ICorrelationIdValidator`, `ICorrelationIdExtractor`, `IActivityFactory`, `IActivityTagProvider`) to abstract common functionality
- Extracted duplicated logic from `CorrelationIdMiddleware`, `CorrelationIdHttpModule`, and `CorrelationIdMessageHandler` into shared services
- All three components now use the same implementations, reducing code duplication from ~300 lines to 0

**Benefits**:
- Single source of truth for validation, extraction, activity creation, and tag provisioning
- Easy to test by mocking interfaces
- Changes in one place affect all usages
- Better separation of concerns

**Trade-off**: More files and abstractions, but significantly improved maintainability.

### Why Extract Constants

**Reason**: Eliminate magic strings throughout the codebase.

**Implementation**:
- Created `Constants.cs` with nested classes: `HttpHeaders`, `ActivityTags`, `ActivityNames`, `HttpContextKeys`
- Replaced all hardcoded strings with constants

**Benefits**:
- Reduced risk of typos
- Easier refactoring (change in one place)
- Better IDE support (autocomplete, find usages)
- Self-documenting code

### Why Create ITraceabilityOptionsProvider

**Reason**: Remove dependency on static volatile fields in NET48 while maintaining backward compatibility.

**Implementation**:
- Created `ITraceabilityOptionsProvider` interface
- Implemented `StaticTraceabilityOptionsProvider` for NET48 (thread-safe wrapper over static field)
- Implemented `DITraceabilityOptionsProvider` for NET8 (uses `IOptions<T>` via DI)
- Refactored `CorrelationIdHttpModule` and `CorrelationIdMessageHandler` to use provider
- Maintained static `Configure()` method for backward compatibility

**Benefits**:
- Better testability (can inject mock provider)
- Cleaner architecture (no direct static field access)
- Thread-safety maintained
- Backward compatibility preserved

### Why Refactor RouteTemplateHelper

**Reason**: Improve testability and organization of reflection-heavy code.

**Implementation**:
- Created `IRouteTemplateResolver` interface
- Converted `RouteTemplateHelper` from static class to instance class implementing interface
- Extracted reflection logic to `HttpRouteDataExtractor`
- Extracted route matching logic to `RouteMatcher`
- Maintained static methods for backward compatibility

**Benefits**:
- Better testability (can inject mock resolver)
- Clearer separation of concerns
- Easier to understand and maintain
- Backward compatibility preserved

### Refactoring Principles Applied

1. **Extract, don't alter**: All extracted logic was copied EXACTLY from existing code
2. **Maintain 100% functional compatibility**: All tests continue to pass without modification
3. **Preserve public APIs**: All public methods and properties remain unchanged
4. **Backward compatibility**: Static methods and configuration methods still work
5. **Test after each step**: Validated that all tests pass after each refactoring phase
