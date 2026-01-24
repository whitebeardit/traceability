# High-Level Architecture

## Component Diagram

```mermaid
graph TB
    subgraph "Entry Points"
        ASPNETCore[ASP.NET Core<br/>Middleware]
        ASPNETWebAPI[ASP.NET Web API<br/>MessageHandler]
        ASPNETTraditional[ASP.NET Traditional<br/>HttpModule]
        ConsoleApp[Console Application<br/>Direct Usage]
    end

    subgraph "Core Layer"
        CorrelationContext[CorrelationContext<br/>AsyncLocal (independent)]
        ActivityCurrent[Activity.Current (External)<br/>Trace Context]
    end

    subgraph "HTTP Integration"
        HttpClientHandler[CorrelationIdHandler<br/>DelegatingHandler]
        HttpClientFactory[TraceableHttpClientFactory<br/>Factory Pattern]
    end

    subgraph "Logging Integration"
        SerilogEnricher[CorrelationIdEnricher<br/>Serilog]
        MELScopeProvider[CorrelationIdScopeProvider<br/>Microsoft.Extensions.Logging]
    end

    subgraph "Configuration"
        TraceabilityOptions[TraceabilityOptions<br/>Configuration]
    end

    ASPNETCore --> CorrelationContext
    ASPNETWebAPI --> CorrelationContext
    ASPNETTraditional --> CorrelationContext
    ConsoleApp --> CorrelationContext

    ASPNETCore -.-> ActivityCurrent
    HttpClientHandler -.-> ActivityCurrent

    CorrelationContext --> HttpClientHandler
    CorrelationContext --> SerilogEnricher
    CorrelationContext --> MELScopeProvider
    ActivitySource --> CorrelationContext

    HttpClientFactory --> HttpClientHandler
    TraceabilityOptions -.-> ASPNETCore
```

## Main Data Flow

```mermaid
sequenceDiagram
    participant Client as HTTP Client
    participant Middleware as Middleware/Handler
    participant ActivityCurrent as Activity.Current (External)
    participant Context as CorrelationContext
    participant App as Application
    participant HttpClient as HttpClient
    participant Logger as Logger
    participant External as External Service

    Client->>Middleware: HTTP Request
    Note over Middleware: Traceability does not create spans.\nWhen OpenTelemetry is configured externally, Activity.Current may exist.
    Middleware->>Context: Read X-Correlation-Id header
    alt Header exists
        Context->>Context: Use header value
    else Header doesn't exist
        Context->>Context: GetOrCreate (generate new GUID)
    end
    Context-->>Middleware: CorrelationId
    Note over Middleware: CorrelationId is set in CorrelationContext and returned in response header.
    Middleware->>App: Process request
    App->>Logger: Log with CorrelationId and TraceId
    App->>HttpClient: External HTTP call
    Note over HttpClient: No span is created by Traceability.
    HttpClient->>Context: Get CorrelationId
    HttpClient->>External: Request with X-Correlation-Id + traceparent
    External-->>HttpClient: Response
    HttpClient-->>App: Response
    App-->>Middleware: Response
    Middleware->>Context: Get CorrelationId
    Middleware->>Client: Response with X-Correlation-Id header
```

## Flow: ASP.NET Core Request (.NET 8)

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as CorrelationIdMiddleware
    participant ActivityCurrent as Activity.Current (External)
    participant Context as CorrelationContext
    participant Controller
    participant Logger
    participant HttpClient as HttpClient+Handler
    participant External

    Client->>Middleware: HTTP Request
    Note over Middleware: Traceability does not create spans.\nIf OpenTelemetry is configured, Activity.Current exists.
    Middleware->>Middleware: Read X-Correlation-Id header
    alt Header exists
        Middleware->>Context: Current = headerValue
    else Header doesn't exist
        Middleware->>Context: GetOrCreate()
        Context->>Context: Generate new GUID
    end
    Context-->>Middleware: correlationId
    Middleware->>Middleware: Add response header
    Note over Middleware: No span tags are added by Traceability.
    Middleware->>Controller: Invoke next()
    Controller->>Context: Current (get ID)
    Context-->>Controller: correlationId
    Controller->>Logger: Log with CorrelationId and TraceId
    Controller->>HttpClient: SendAsync()
    Note over HttpClient: No child span is created by Traceability.
    HttpClient->>Context: Current (get correlation-ID)
    Context-->>HttpClient: correlationId
    HttpClient->>HttpClient: Add X-Correlation-Id + traceparent headers
    Note over HttpClient: Traceability adds X-Correlation-Id and propagates traceparent best-effort.
    HttpClient->>External: HTTP Request (with trace context)
    External-->>HttpClient: HTTP Response
    HttpClient-->>Controller: Response
    Controller-->>Middleware: Response
    Middleware->>Client: HTTP Response with X-Correlation-Id
```

## Flow: ASP.NET Framework 4.8 Request

```mermaid
sequenceDiagram
    participant Client
    participant HttpModule as CorrelationIdHttpModule
    participant Context as CorrelationContext
    participant Page
    participant Logger

    Client->>HttpModule: HTTP Request (BeginRequest)
    HttpModule->>HttpModule: Read X-Correlation-Id header
    alt Header exists
        HttpModule->>Context: Current = headerValue
    else Header doesn't exist
        HttpModule->>Context: GetOrCreate()
    end
    HttpModule->>Page: Process request
    Page->>Context: Current
    Context-->>Page: correlationId
    Page->>Logger: Log
    Page-->>HttpModule: Response
    HttpModule->>Context: Current
    Context-->>HttpModule: correlationId
    HttpModule->>HttpModule: Add response header (PreSendRequestHeaders)
    HttpModule->>Client: HTTP Response with X-Correlation-Id
```

## Propagation in Chained HTTP Calls

```mermaid
graph LR
    A[Service A] -->|"X-Correlation-Id: abc123<br/>traceparent: 00-..."| B[Service B]
    B -->|"X-Correlation-Id: abc123<br/>traceparent: 00-..."| C[Service C]
    C -->|"X-Correlation-Id: abc123<br/>traceparent: 00-..."| D[Service D]
```

**Behavior**:
1. Service A receives request without header → creates Activity with TraceId `4bf92f...` and generates correlation-ID `abc123...`
2. Service A calls Service B with headers:
   - `X-Correlation-Id: abc123...` (correlation-ID)
   - `traceparent: 00-4bf92f...` (W3C Trace Context / trace ID)
3. Service B reads headers and uses correlation-ID `abc123...` (doesn't generate new one)
4. Service B creates child Activity (span) maintaining trace hierarchy
5. Service B calls Service C with same headers
6. Process continues until the end of the chain

**Rule**: Never generate a new correlation-id if one already exists in the request header. Always propagate W3C Trace Context for distributed tracing compatibility.

## Activity Hierarchy (Spans)

```mermaid
graph TD
    Root[Root Activity<br/>HTTP Request<br/>Service A] --> Child1[Child Activity<br/>HTTP Client Call<br/>Service A → B]
    Child1 --> Child2[Child Activity<br/>HTTP Client Call<br/>Service B → C]
    Child2 --> Child3[Child Activity<br/>HTTP Client Call<br/>Service C → D]
```

**Behavior**:
- Traceability does not create spans. Activity hierarchy is owned by OpenTelemetry SDK/instrumentation.
- W3C Trace Context: Traceability propagates `traceparent` when trace context is available via `Activity.Current` (best-effort W3C-valid only). Traceability does not emit `tracestate`.

## Logging Integration

### Serilog

```mermaid
graph TD
    A[Log.Information] --> B[CorrelationIdEnricher]
    B --> C[CorrelationContext.TryGetValue]
    C --> D{CorrelationId exists?}
    D -->|Yes| E[Add CorrelationId property]
    D -->|No| F[Don't add anything]
    E --> G[Log Event with CorrelationId]
    F --> H[Log Event without CorrelationId]
```

### Microsoft.Extensions.Logging

```mermaid
graph TD
    A[logger.LogInformation] --> B[CorrelationIdScopeProvider]
    B --> C[CorrelationContext.TryGetValue]
    C --> D{CorrelationId exists?}
    D -->|Yes| E[Push Scope with CorrelationId]
    D -->|No| F[Don't add scope]
    E --> G[Log with CorrelationId in scope]
    F --> H[Log without CorrelationId]
```
