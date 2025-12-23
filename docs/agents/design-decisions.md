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

## Why not use .NET's `Activity`?

**Reason**:
- `Activity` is part of .NET's diagnostics system
- Heavier and more complex
- Requires additional configuration
- `AsyncLocal` is simpler and more direct for this use case
- Doesn't require additional dependencies

**When to consider `Activity`**:
- If you need Application Insights integration
- If you need complete distributed tracing
- If you need hierarchical spans and traces

## Why JSON Log Uniformization and Environment Variables?

**Reason**: Ensure that all logs from different applications and services follow the same pattern, facilitating analysis, correlation, and monitoring in distributed environments.

**Design Decisions**:

1. **Always JSON Output**:
   - **Reason**: JSON format is structured, easily parseable, and supported by all log aggregation tools (ELK, Splunk, etc.)
   - **Benefit**: Automatic uniformization across different applications and services
   - **Implementation**: All `WithTraceability()` and `WithTraceabilityJson()` methods ensure JSON output

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
