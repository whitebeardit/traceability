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
        CorrelationContext[CorrelationContext<br/>AsyncLocal Storage]
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

    CorrelationContext --> HttpClientHandler
    CorrelationContext --> SerilogEnricher
    CorrelationContext --> MELScopeProvider

    HttpClientFactory --> HttpClientHandler
    TraceabilityOptions -.-> ASPNETCore
```

## Main Data Flow

```mermaid
sequenceDiagram
    participant Client as HTTP Client
    participant Middleware as Middleware/Handler
    participant Context as CorrelationContext
    participant App as Application
    participant HttpClient as HttpClient
    participant Logger as Logger
    participant External as External Service

    Client->>Middleware: HTTP Request
    Middleware->>Context: Read X-Correlation-Id header
    alt Header exists
        Context->>Context: Use header value
    else Header doesn't exist
        Context->>Context: Generate new GUID
    end
    Context-->>Middleware: CorrelationId
    Middleware->>App: Process request
    App->>Logger: Log with CorrelationId
    App->>HttpClient: External HTTP call
    HttpClient->>Context: Get CorrelationId
    HttpClient->>External: Request with X-Correlation-Id
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
    participant Context as CorrelationContext
    participant Controller
    participant Logger
    participant HttpClient as HttpClient+Handler
    participant External

    Client->>Middleware: HTTP Request
    Middleware->>Middleware: Read X-Correlation-Id header
    alt Header exists
        Middleware->>Context: Current = headerValue
    else Header doesn't exist
        Middleware->>Context: GetOrCreate()
        Context->>Context: Generate GUID
    end
    Context-->>Middleware: correlationId
    Middleware->>Middleware: Add response header
    Middleware->>Controller: Invoke next()
    Controller->>Context: Current (get ID)
    Context-->>Controller: correlationId
    Controller->>Logger: Log with CorrelationId
    Controller->>HttpClient: SendAsync()
    HttpClient->>Context: Current
    Context-->>HttpClient: correlationId
    HttpClient->>HttpClient: Add X-Correlation-Id header
    HttpClient->>External: HTTP Request
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
    A[Service A] -->|X-Correlation-Id: abc123| B[Service B]
    B -->|X-Correlation-Id: abc123| C[Service C]
    C -->|X-Correlation-Id: abc123| D[Service D]
    
    style A fill:#e1f5ff
    style B fill:#e1f5ff
    style C fill:#e1f5ff
    style D fill:#e1f5ff
```

**Behavior**:
1. Service A receives request without header → generates `abc123`
2. Service A calls Service B with header `X-Correlation-Id: abc123`
3. Service B reads header and uses `abc123` (doesn't generate new one)
4. Service B calls Service C with same header
5. Process continues until the end of the chain

**Rule**: Never generate a new correlation-id if one already exists in the request header.

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
