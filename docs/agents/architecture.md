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
        CorrelationContext[CorrelationContext<br/>Activity.TraceId / AsyncLocal]
        ActivitySource[TraceabilityActivitySource<br/>OpenTelemetry Activities]
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

    ASPNETCore --> ActivitySource
    HttpClientHandler --> ActivitySource

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
    participant ActivitySource as TraceabilityActivitySource
    participant Context as CorrelationContext
    participant App as Application
    participant HttpClient as HttpClient
    participant Logger as Logger
    participant External as External Service

    Client->>Middleware: HTTP Request
    Middleware->>ActivitySource: Create Activity (if not exists)
    ActivitySource-->>Middleware: Activity with TraceId
    Middleware->>Context: Read X-Correlation-Id header
    alt Header exists
        Context->>Context: Use header value
    else Header doesn't exist
        Context->>Context: GetOrCreate (Activity.TraceId or new GUID)
    end
    Context-->>Middleware: CorrelationId/TraceId
    Middleware->>App: Process request
    App->>Logger: Log with CorrelationId
    App->>HttpClient: External HTTP call
    HttpClient->>ActivitySource: Create child Activity
    ActivitySource-->>HttpClient: Child Activity
    HttpClient->>Context: Get CorrelationId/TraceId
    HttpClient->>External: Request with X-Correlation-Id + traceparent
    External-->>HttpClient: Response
    HttpClient->>ActivitySource: Stop Activity
    HttpClient-->>App: Response
    App-->>Middleware: Response
    Middleware->>Context: Get CorrelationId
    Middleware->>ActivitySource: Stop Activity
    Middleware->>Client: Response with X-Correlation-Id header
```

## Flow: ASP.NET Core Request (.NET 8)

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as CorrelationIdMiddleware
    participant ActivitySource as TraceabilityActivitySource
    participant Context as CorrelationContext
    participant Controller
    participant Logger
    participant HttpClient as HttpClient+Handler
    participant External

    Client->>Middleware: HTTP Request
    Middleware->>ActivitySource: Create Activity (if Activity.Current == null)
    ActivitySource-->>Middleware: Activity with TraceId
    Middleware->>Middleware: Read X-Correlation-Id header
    alt Header exists
        Middleware->>Context: Current = headerValue
    else Header doesn't exist
        Middleware->>Context: GetOrCreate()
        Context->>Context: Use Activity.TraceId or Generate GUID
    end
    Context-->>Middleware: correlationId/traceId
    Middleware->>Middleware: Add response header
    Middleware->>Middleware: Add HTTP tags to Activity
    Middleware->>Controller: Invoke next()
    Controller->>Context: Current (get ID)
    Context-->>Controller: correlationId/traceId
    Controller->>Logger: Log with CorrelationId
    Controller->>HttpClient: SendAsync()
    HttpClient->>ActivitySource: Create child Activity
    ActivitySource-->>HttpClient: Child Activity
    HttpClient->>Context: Current (Activity.TraceId)
    Context-->>HttpClient: traceId
    HttpClient->>HttpClient: Add X-Correlation-Id + traceparent headers
    HttpClient->>HttpClient: Add HTTP tags to Activity
    HttpClient->>External: HTTP Request (with trace context)
    External-->>HttpClient: HTTP Response
    HttpClient->>ActivitySource: Stop child Activity
    HttpClient-->>Controller: Response
    Controller-->>Middleware: Response
    Middleware->>ActivitySource: Stop Activity
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
    
    style A fill:#e1f5ff
    style B fill:#e1f5ff
    style C fill:#e1f5ff
    style D fill:#e1f5ff
```

**Behavior**:
1. Service A receives request without header → creates Activity with TraceId `abc123`
2. Service A calls Service B with headers:
   - `X-Correlation-Id: abc123` (backward compatibility)
   - `traceparent: 00-abc123...` (W3C Trace Context)
3. Service B reads headers and uses `abc123` (doesn't generate new one)
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
    
    style Root fill:#e1f5ff
    style Child1 fill:#fff4e1
    style Child2 fill:#fff4e1
    style Child3 fill:#fff4e1
```

**Behavior**:
- Each HTTP request creates a root Activity (span)
- Each outgoing HTTP call creates a child Activity (span)
- Activities maintain parent-child relationships for hierarchical tracing
- All Activities share the same TraceId for correlation
- W3C Trace Context headers (`traceparent`, `tracestate`) propagate across services

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
