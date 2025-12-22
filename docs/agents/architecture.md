# Arquitetura de Alto Nível

## Diagrama de Componentes

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

## Fluxo de Dados Principal

```mermaid
sequenceDiagram
    participant Client as Cliente HTTP
    participant Middleware as Middleware/Handler
    participant Context as CorrelationContext
    participant App as Aplicação
    participant HttpClient as HttpClient
    participant Logger as Logger
    participant External as Serviço Externo

    Client->>Middleware: Requisição HTTP
    Middleware->>Context: Ler X-Correlation-Id header
    alt Header existe
        Context->>Context: Usar valor do header
    else Header não existe
        Context->>Context: Gerar novo GUID
    end
    Context-->>Middleware: CorrelationId
    Middleware->>App: Processar requisição
    App->>Logger: Log com CorrelationId
    App->>HttpClient: Chamada HTTP externa
    HttpClient->>Context: Obter CorrelationId
    HttpClient->>External: Requisição com X-Correlation-Id
    External-->>HttpClient: Resposta
    HttpClient-->>App: Resposta
    App-->>Middleware: Resposta
    Middleware->>Context: Obter CorrelationId
    Middleware->>Client: Resposta com X-Correlation-Id header
```

## Fluxo: Requisição ASP.NET Core (.NET 8)

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
    Middleware->>Middleware: Ler X-Correlation-Id header
    alt Header existe
        Middleware->>Context: Current = headerValue
    else Header não existe
        Middleware->>Context: GetOrCreate()
        Context->>Context: Gerar GUID
    end
    Context-->>Middleware: correlationId
    Middleware->>Middleware: Adicionar header resposta
    Middleware->>Controller: Invoke next()
    Controller->>Context: Current (obter ID)
    Context-->>Controller: correlationId
    Controller->>Logger: Log com CorrelationId
    Controller->>HttpClient: SendAsync()
    HttpClient->>Context: Current
    Context-->>HttpClient: correlationId
    HttpClient->>HttpClient: Adicionar X-Correlation-Id header
    HttpClient->>External: HTTP Request
    External-->>HttpClient: HTTP Response
    HttpClient-->>Controller: Response
    Controller-->>Middleware: Response
    Middleware->>Client: HTTP Response com X-Correlation-Id
```

## Fluxo: Requisição ASP.NET Framework 4.8

```mermaid
sequenceDiagram
    participant Client
    participant HttpModule as CorrelationIdHttpModule
    participant Context as CorrelationContext
    participant Page
    participant Logger

    Client->>HttpModule: HTTP Request (BeginRequest)
    HttpModule->>HttpModule: Ler X-Correlation-Id header
    alt Header existe
        HttpModule->>Context: Current = headerValue
    else Header não existe
        HttpModule->>Context: GetOrCreate()
    end
    HttpModule->>Page: Processar requisição
    Page->>Context: Current
    Context-->>Page: correlationId
    Page->>Logger: Log
    Page-->>HttpModule: Response
    HttpModule->>Context: Current
    Context-->>HttpModule: correlationId
    HttpModule->>HttpModule: Adicionar header resposta (PreSendRequestHeaders)
    HttpModule->>Client: HTTP Response com X-Correlation-Id
```

## Propagação em Chamadas HTTP Encadeadas

```mermaid
graph LR
    A[Serviço A] -->|X-Correlation-Id: abc123| B[Serviço B]
    B -->|X-Correlation-Id: abc123| C[Serviço C]
    C -->|X-Correlation-Id: abc123| D[Serviço D]
    
    style A fill:#e1f5ff
    style B fill:#e1f5ff
    style C fill:#e1f5ff
    style D fill:#e1f5ff
```

**Comportamento**:
1. Serviço A recebe requisição sem header → gera `abc123`
2. Serviço A chama Serviço B com header `X-Correlation-Id: abc123`
3. Serviço B lê header e usa `abc123` (não gera novo)
4. Serviço B chama Serviço C com mesmo header
5. Processo continua até o fim da cadeia

**Regra**: Nunca gerar novo correlation-id se já existir no header da requisição.

## Integração com Logging

### Serilog

```mermaid
graph TD
    A[Log.Information] --> B[CorrelationIdEnricher]
    B --> C[CorrelationContext.TryGetValue]
    C --> D{CorrelationId existe?}
    D -->|Sim| E[Adicionar CorrelationId property]
    D -->|Não| F[Não adiciona nada]
    E --> G[Log Event com CorrelationId]
    F --> H[Log Event sem CorrelationId]
```

### Microsoft.Extensions.Logging

```mermaid
graph TD
    A[logger.LogInformation] --> B[CorrelationIdScopeProvider]
    B --> C[CorrelationContext.TryGetValue]
    C --> D{CorrelationId existe?}
    D -->|Sim| E[Push Scope com CorrelationId]
    D -->|Não| F[Não adiciona scope]
    E --> G[Log com CorrelationId no scope]
    F --> H[Log sem CorrelationId]
```


