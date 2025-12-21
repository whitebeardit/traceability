# Traceability - Arquitetura e Guia para LLMs

## METADATA E CONTEXTO INICIAL

### Informações do Projeto
- **Nome**: Traceability
- **Versão**: 1.0.0
- **Tipo**: Pacote NuGet
- **Licença**: MIT
- **Autor**: WhiteBeard IT

### Frameworks Suportados
- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

### Dependências Principais

#### .NET 8.0
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

#### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

#### Comum
- `Serilog` (3.1.1) - PrivateAssets: all

### Estrutura de Namespaces
```
Traceability
├── Traceability                          # Core: CorrelationContext
├── Traceability.Configuration            # Opções de configuração
├── Traceability.Extensions               # Extensões para DI e middleware
├── Traceability.HttpClient               # Integração com HttpClient
├── Traceability.Logging                  # Integrações de logging
├── Traceability.Middleware               # Middleware e handlers HTTP
└── Traceability.WebApi                   # Handlers específicos Web API
```

## ARQUITETURA DE ALTO NÍVEL

### Diagrama de Componentes

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

### Fluxo de Dados Principal

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
    Context->>Middleware: CorrelationId
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

## COMPONENTES CORE - DETALHAMENTO TÉCNICO

### 1. CorrelationContext

**Localização**: `src/Traceability/CorrelationContext.cs`

**Responsabilidade**: Gerenciar o correlation-id no contexto assíncrono da thread atual usando `AsyncLocal<string>`.

**API Pública**:
```csharp
public static class CorrelationContext
{
    // Propriedades
    public static string Current { get; set; }
    public static bool HasValue { get; }
    
    // Métodos
    public static string GetOrCreate();
    public static void Clear();
}
```

**Dependências**: Nenhuma (classe estática pura)

**Comportamento**:
- Usa `AsyncLocal<string>` para isolamento entre contextos assíncronos
- Gera GUID formatado sem hífens (32 caracteres) quando necessário
- Thread-safe e async-safe
- Isolamento automático entre diferentes contextos assíncronos

**Exemplo de Uso**:
```csharp
// Obter ou criar correlation-id
var correlationId = CorrelationContext.Current;

// Verificar se existe
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Limpar contexto
CorrelationContext.Clear();
```

**Decisões de Design**:
- `AsyncLocal` ao invés de `ThreadLocal` para suportar async/await corretamente
- GUID sem hífens para compatibilidade e legibilidade em logs
- Propriedade `Current` cria automaticamente se não existir (lazy initialization)

### 2. CorrelationIdMiddleware (ASP.NET Core)

**Localização**: `src/Traceability/Middleware/CorrelationIdMiddleware.cs`

**Condição de Compilação**: `#if NET8_0`

**Responsabilidade**: Middleware para ASP.NET Core que gerencia correlation-id automaticamente em requisições HTTP.

**API Pública**:
```csharp
public class CorrelationIdMiddleware
{
    public CorrelationIdMiddleware(RequestDelegate next);
    public Task InvokeAsync(HttpContext context);
}
```

**Dependências**:
- `Microsoft.AspNetCore.Http`
- `Traceability.CorrelationContext`

**Comportamento**:
1. Lê header `X-Correlation-Id` da requisição
2. Se existir, usa o valor e armazena em `CorrelationContext`
3. Se não existir, gera novo via `CorrelationContext.GetOrCreate()`
4. Adiciona correlation-id no header da resposta

**Header Padrão**: `X-Correlation-Id`

**Exemplo de Uso**:
```csharp
// Program.cs
app.UseCorrelationId();
```

### 3. CorrelationIdMessageHandler (ASP.NET Web API)

**Localização**: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`

**Condição de Compilação**: `#if NET48`

**Responsabilidade**: MessageHandler para ASP.NET Web API que gerencia correlation-id.

**API Pública**:
```csharp
public class CorrelationIdMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);
}
```

**Dependências**:
- `System.Net.Http`
- `System.Web.Http`
- `Traceability.CorrelationContext`

**Comportamento**: Similar ao Middleware, mas adaptado para o pipeline do Web API.

**Exemplo de Uso**:
```csharp
// Global.asax.cs
GlobalConfiguration.Configure(config =>
{
    config.MessageHandlers.Add(new CorrelationIdMessageHandler());
});
```

### 4. CorrelationIdHttpModule (ASP.NET Tradicional)

**Localização**: `src/Traceability/Middleware/CorrelationIdHttpModule.cs`

**Condição de Compilação**: `#if NET48`

**Responsabilidade**: HttpModule para ASP.NET tradicional que gerencia correlation-id.

**API Pública**:
```csharp
public class CorrelationIdHttpModule : IHttpModule
{
    public void Init(HttpApplication context);
    public void Dispose();
}
```

**Dependências**:
- `System.Web`
- `Traceability.CorrelationContext`

**Comportamento**: Intercepta eventos `BeginRequest` e `PreSendRequestHeaders` do pipeline do IIS.

**Exemplo de Uso**:
```xml
<!-- web.config -->
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

### 5. CorrelationIdHandler (HttpClient)

**Localização**: `src/Traceability/HttpClient/CorrelationIdHandler.cs`

**Responsabilidade**: DelegatingHandler que adiciona automaticamente correlation-id nos headers das requisições HTTP.

**API Pública**:
```csharp
public class CorrelationIdHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);
    
    // .NET 8.0 apenas
    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken);
}
```

**Dependências**:
- `System.Net.Http`
- `Traceability.CorrelationContext`

**Comportamento**:
- Obtém `CorrelationContext.Current`
- Remove header existente (se houver)
- Adiciona `X-Correlation-Id` ao header da requisição

**Exemplo de Uso**:
```csharp
// Com IHttpClientFactory
services.AddHttpClient("MyClient")
    .AddHttpMessageHandler<CorrelationIdHandler>();

// Ou diretamente
var handler = new CorrelationIdHandler();
var client = new HttpClient(handler);
```

### 6. TraceableHttpClientFactory

**Localização**: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`

**Responsabilidade**: Factory para criar HttpClient configurado com correlation-id e políticas Polly.

**API Pública**:
```csharp
public class TraceableHttpClientFactory
{
    // ⚠️ OBSOLETO - Pode causar socket exhaustion
    [Obsolete]
    public static HttpClient Create(string? baseAddress = null);
    
    [Obsolete]
    public static HttpClient CreateWithPolicy(
        IAsyncPolicy<HttpResponseMessage> policy,
        string? baseAddress = null);
    
    // ✅ RECOMENDADO - Previne socket exhaustion (.NET 8)
    public static HttpClient CreateFromFactory(
        IHttpClientFactory factory,
        string? clientName = null,
        string? baseAddress = null);
}

// Método de extensão para IServiceCollection (.NET 8)
public static IHttpClientBuilder AddTraceableHttpClient(
    this IServiceCollection services,
    string clientName,
    Action<HttpClient>? configureClient = null);
```

**Dependências**:
- `Polly`
- `Traceability.HttpClient.CorrelationIdHandler`
- `.NET 8`: `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection`

**Comportamento**:
- Métodos obsoletos criam `HttpClient` diretamente (podem causar socket exhaustion)
- Métodos recomendados usam `IHttpClientFactory` que gerencia pool de conexões
- Suporta políticas Polly via `PolicyDelegatingHandler` interno ou `.AddPolicyHandler()`

**⚠️ IMPORTANTE - Socket Exhaustion**:
- Métodos `Create()` e `CreateWithPolicy()` estão obsoletos
- Sempre use `IHttpClientFactory` em aplicações de produção
- Use `AddTraceableHttpClient()` para configurar no DI

**Exemplo de Uso Recomendado**:
```csharp
// ✅ RECOMENDADO - Previne socket exhaustion
// Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy); // Com Polly

// No serviço ou controller
var client = _httpClientFactory.CreateClient("ExternalApi");
```

**Exemplo de Uso Obsoleto (apenas para testes/prototipos)**:
```csharp
// ⚠️ OBSOLETO - Apenas para testes ou prototipação
var client = TraceableHttpClientFactory.Create("https://api.example.com/");
```

### 7. CorrelationIdEnricher (Serilog)

**Localização**: `src/Traceability/Logging/CorrelationIdEnricher.cs`

**Responsabilidade**: Enricher do Serilog que adiciona correlation-id aos logs.

**API Pública**:
```csharp
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory);
}
```

**Dependências**:
- `Serilog`
- `Traceability.CorrelationContext`

**Comportamento**:
- Adiciona propriedade `CorrelationId` a todos os eventos de log
- Obtém valor de `CorrelationContext.Current`

**Exemplo de Uso**:
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}")
    .CreateLogger();
```

### 8. CorrelationIdScopeProvider (Microsoft.Extensions.Logging)

**Localização**: `src/Traceability/Logging/CorrelationIdScopeProvider.cs`

**Responsabilidade**: Provider de scope para Microsoft.Extensions.Logging que adiciona correlation-id.

**API Pública**:
```csharp
public class CorrelationIdScopeProvider : IExternalScopeProvider
{
    public CorrelationIdScopeProvider(IExternalScopeProvider? innerProvider = null);
    public void ForEachScope<TState>(Action<object?, TState> callback, TState state);
    public IDisposable Push(object? state);
}
```

**Dependências**:
- `Microsoft.Extensions.Logging`
- `Traceability.CorrelationContext`

**Comportamento**:
- Adiciona `CorrelationId` ao scope de logging
- Suporta provider interno para encadeamento

**Exemplo de Uso**:
```csharp
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddScopeProvider(new CorrelationIdScopeProvider());
});
```

### 9. TraceabilityOptions

**Localização**: `src/Traceability/Configuration/TraceabilityOptions.cs`

**Responsabilidade**: Opções de configuração para o pacote.

**API Pública**:
```csharp
public class TraceabilityOptions
{
    public string HeaderName { get; set; } = "X-Correlation-Id";
    public bool AlwaysGenerateNew { get; set; } = false;
}
```

**Status**: Classe definida mas não totalmente utilizada no código atual. O header está hardcoded como `"X-Correlation-Id"` nos componentes.

**Nota para LLMs**: Esta classe existe para futuras extensões, mas atualmente não é utilizada pelos middlewares/handlers.

### 10. Extensions

#### ServiceCollectionExtensions

**Localização**: `src/Traceability/Extensions/ServiceCollectionExtensions.cs`

**Condição de Compilação**: `#if NET8_0`

**Métodos**:
```csharp
public static IServiceCollection AddTraceability(
    this IServiceCollection services,
    Action<TraceabilityOptions>? configureOptions = null);

public static IServiceCollection AddTraceableHttpClient<TClient>(
    this IServiceCollection services,
    string? baseAddress = null)
    where TClient : class, ITraceableHttpClient;
```

#### ApplicationBuilderExtensions

**Localização**: `src/Traceability/Extensions/ApplicationBuilderExtensions.cs`

**Condição de Compilação**: `#if NET8_0`

**Métodos**:
```csharp
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app);
```

#### HttpClientExtensions

**Localização**: `src/Traceability/Extensions/HttpClientExtensions.cs`

**Métodos**:
```csharp
public static HttpClient AddCorrelationIdHeader(
    this HttpClient client,
    HttpRequestMessage request);
```

## PADRÕES DE IMPLEMENTAÇÃO

### 1. Conditional Compilation

**Padrão**: Usar diretivas `#if` para código específico de framework.

**Regras**:
- `#if NET8_0` para código ASP.NET Core
- `#if NET48` para código .NET Framework
- Sempre fechar com `#endif`
- Código comum (sem diretiva) funciona em ambos

**Exemplo**:
```csharp
#if NET8_0
using Microsoft.AspNetCore.Http;
// Código específico .NET 8
#endif

#if NET48
using System.Web;
// Código específico .NET Framework
#endif

// Código comum
```

### 2. AsyncLocal para Isolamento Assíncrono

**Padrão**: Sempre usar `AsyncLocal<string>` para armazenar correlation-id.

**Razão**: `AsyncLocal` preserva valores através de continuidades assíncronas, enquanto `ThreadLocal` não.

**Implementação**:
```csharp
private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
```

**Comportamento**:
- Cada contexto assíncrono tem seu próprio valor
- Valor é preservado através de `await`
- Isolado entre diferentes `Task.Run()` ou threads

### 3. Padrão Factory para HttpClient

**Padrão**: Usar factory para criar HttpClient configurado.

**Implementação**: `TraceableHttpClientFactory`

**Vantagens**:
- Centraliza configuração
- Facilita adição de handlers
- Suporta políticas Polly

### 4. Padrão DelegatingHandler

**Padrão**: Usar `DelegatingHandler` para interceptar requisições HTTP.

**Implementação**: `CorrelationIdHandler`

**Comportamento**:
- Intercepta antes de enviar requisição
- Adiciona header automaticamente
- Preserva pipeline de handlers

### 5. Padrão Enricher (Serilog)

**Padrão**: Implementar `ILogEventEnricher` para adicionar propriedades aos logs.

**Implementação**: `CorrelationIdEnricher`

**Comportamento**:
- Chamado para cada evento de log
- Adiciona propriedade `CorrelationId`
- Não modifica outros enrichers

### 6. Padrão ScopeProvider (Microsoft.Extensions.Logging)

**Padrão**: Implementar `IExternalScopeProvider` para adicionar scopes.

**Implementação**: `CorrelationIdScopeProvider`

**Comportamento**:
- Adiciona scope com `CorrelationId`
- Suporta provider interno (decorator pattern)
- Preserva scopes existentes

### 7. Convenções de Nomenclatura

- **Classes**: PascalCase
- **Métodos**: PascalCase
- **Propriedades**: PascalCase
- **Campos privados**: `_camelCase` (underscore prefix)
- **Constantes**: PascalCase
- **Namespaces**: `Traceability.[SubNamespace]`

### 8. Geração de Correlation-ID

**Formato**: GUID sem hífens (32 caracteres)

**Implementação**:
```csharp
private static string GenerateNew()
{
    return Guid.NewGuid().ToString("N");
}
```

**Razão**: 
- Compacto (32 chars vs 36 com hífens)
- Compatível com sistemas externos
- Legível em logs

## ESTRUTURA DE DIRETÓRIOS - MAPA COMPLETO

```
src/Traceability/
├── Configuration/
│   └── TraceabilityOptions.cs          # Opções de configuração
│
├── CorrelationContext.cs                # Core: Gerenciamento de contexto
│
├── Extensions/
│   ├── ApplicationBuilderExtensions.cs  # Extensão para IApplicationBuilder (.NET 8)
│   ├── HttpClientExtensions.cs          # Extensões para HttpClient
│   └── ServiceCollectionExtensions.cs   # Extensão para IServiceCollection (.NET 8)
│
├── HttpClient/
│   ├── CorrelationIdHandler.cs          # DelegatingHandler para HttpClient
│   ├── ITraceableHttpClient.cs          # Interface (não implementada)
│   └── TraceableHttpClientFactory.cs    # Factory para criar HttpClient
│
├── Logging/
│   ├── CorrelationIdEnricher.cs        # Enricher para Serilog
│   └── CorrelationIdScopeProvider.cs    # ScopeProvider para MEL
│
├── Middleware/
│   ├── CorrelationIdHttpModule.cs      # HttpModule (.NET Framework)
│   └── CorrelationIdMiddleware.cs      # Middleware (.NET 8)
│
├── WebApi/
│   └── CorrelationIdMessageHandler.cs  # MessageHandler (.NET Framework Web API)
│
└── Traceability.csproj                  # Arquivo de projeto
```

### Descrição por Diretório

#### Configuration/
- **Propósito**: Classes de configuração
- **Quando usar**: Para adicionar novas opções de configuração
- **Arquivos**: `TraceabilityOptions.cs`

#### Extensions/
- **Propósito**: Métodos de extensão para facilitar uso
- **Quando usar**: Para adicionar métodos de conveniência
- **Arquivos**: Extensões para DI, middleware, HttpClient

#### HttpClient/
- **Propósito**: Integração com HttpClient
- **Quando usar**: Para modificar comportamento de HTTP ou adicionar novos handlers
- **Arquivos**: Handlers, factory, interfaces

#### Logging/
- **Propósito**: Integrações com sistemas de logging
- **Quando usar**: Para adicionar suporte a novos loggers
- **Arquivos**: Enrichers, scope providers

#### Middleware/
- **Propósito**: Middleware e handlers HTTP
- **Quando usar**: Para adicionar novos pontos de interceptação HTTP
- **Arquivos**: Middleware (.NET 8), HttpModule (.NET Framework)

#### WebApi/
- **Propósito**: Handlers específicos para ASP.NET Web API
- **Quando usar**: Para funcionalidades específicas do Web API
- **Arquivos**: MessageHandlers

## FLUXOS DE DADOS DETALHADOS

### Fluxo: Requisição ASP.NET Core (.NET 8)

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

### Fluxo: Requisição ASP.NET Framework 4.8

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

### Propagação em Chamadas HTTP Encadeadas

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

### Integração com Logging

#### Serilog

```mermaid
graph TD
    A[Log.Information] --> B[CorrelationIdEnricher]
    B --> C[CorrelationContext.Current]
    C --> D[Adicionar CorrelationId property]
    D --> E[Log Event com CorrelationId]
```

#### Microsoft.Extensions.Logging

```mermaid
graph TD
    A[logger.LogInformation] --> B[CorrelationIdScopeProvider]
    B --> C[CorrelationContext.Current]
    C --> D[Push Scope com CorrelationId]
    D --> E[Log com CorrelationId no scope]
```

## REGRAS E CONSTRAINTS PARA LLMs

### Regras Obrigatórias

1. **Sempre usar `AsyncLocal` para contexto assíncrono**
   - ❌ Nunca usar `ThreadLocal`
   - ❌ Nunca usar variáveis estáticas simples
   - ✅ Sempre usar `AsyncLocal<string>`

2. **Sempre compilar condicionalmente código específico de framework**
   - ❌ Nunca misturar código .NET 8 e .NET Framework sem `#if`
   - ✅ Usar `#if NET8_0` para código ASP.NET Core
   - ✅ Usar `#if NET48` para código .NET Framework
   - ✅ Sempre fechar com `#endif`

3. **Header padrão: `X-Correlation-Id`**
   - ✅ Usar constante `"X-Correlation-Id"` (futuramente via `TraceabilityOptions`)
   - ❌ Nunca usar outros nomes de header sem configuração

4. **GUID formatado sem hífens (32 caracteres)**
   - ✅ Usar `Guid.NewGuid().ToString("N")`
   - ❌ Nunca usar `ToString()` sem parâmetro (36 caracteres)

5. **Nunca modificar correlation-id existente**
   - ✅ Se header existe na requisição, usar o valor
   - ✅ Se header não existe, gerar novo
   - ❌ Nunca sobrescrever correlation-id existente no contexto

6. **Isolamento assíncrono obrigatório**
   - ✅ Cada contexto assíncrono isolado mantém seu próprio correlation-id
   - ✅ `Task.Run()` cria novo contexto isolado
   - ✅ `await` preserva contexto

### Constraints de Design

1. **Sem dependências circulares**: Componentes não devem depender uns dos outros circularmente
2. **Thread-safe**: Todas as operações devem ser thread-safe
3. **Async-safe**: Todas as operações devem funcionar corretamente com async/await
4. **Zero configuração por padrão**: Funciona sem configuração, mas permite customização

### Validações Obrigatórias

Ao adicionar/modificar código, verificar:
- [ ] Compilação condicional correta (`#if NET8_0` / `#if NET48`)
- [ ] Uso de `AsyncLocal` para contexto
- [ ] Header `X-Correlation-Id` usado consistentemente
- [ ] GUID gerado sem hífens
- [ ] Não modifica correlation-id existente
- [ ] Testes unitários adicionados/atualizados
- [ ] Documentação XML atualizada

## DECISÕES DE DESIGN E RACIONAIS

### Por que `AsyncLocal` ao invés de `ThreadLocal`?

**Razão**: `AsyncLocal` preserva valores através de continuidades assíncronas, enquanto `ThreadLocal` não.

**Exemplo do problema com `ThreadLocal`**:
```csharp
// ❌ Com ThreadLocal (não funciona)
ThreadLocal<string> correlationId = new ThreadLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Contexto pode mudar de thread
// correlationId.Value pode ser null ou diferente
```

**Solução com `AsyncLocal`**:
```csharp
// ✅ Com AsyncLocal (funciona)
AsyncLocal<string> correlationId = new AsyncLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Valor preservado
// correlationId.Value ainda é "abc123"
```

### Por que suporte multi-framework?

**Razão**: 
- Muitas empresas ainda usam .NET Framework 4.8
- Migração gradual é comum
- Necessidade de rastreabilidade em ambos os ambientes

**Trade-off**: Código mais complexo com conditional compilation, mas maior compatibilidade.

### Por que não usar `Activity` do .NET?

**Razão**:
- `Activity` é parte do sistema de diagnóstico do .NET
- Mais pesado e complexo
- Requer configuração adicional
- `AsyncLocal` é mais simples e direto para este caso de uso
- Não requer dependências adicionais

**Quando considerar `Activity`**:
- Se precisar de integração com Application Insights
- Se precisar de distributed tracing completo
- Se precisar de spans e traces hierárquicos

### Trade-offs e Limitações Conhecidas

1. **✅ RESOLVIDO**: `TraceabilityOptions` agora está totalmente integrado
   - Header pode ser customizado via `HeaderName`
   - `AlwaysGenerateNew` é usado nos middlewares/handlers
   - `ValidateCorrelationIdFormat` adicionado para validação opcional

2. **Limitação**: `ITraceableHttpClient` interface existe mas não há implementação
   - Interface definida mas não utilizada

3. **Trade-off**: Conditional compilation aumenta complexidade
   - Benefício: Suporte multi-framework
   - Custo: Mais difícil de manter

4. **Limitação**: Não há suporte para correlation-id em mensageria (RabbitMQ, Kafka, etc.)
   - Apenas HTTP atualmente

5. **Trade-off**: GUID simples ao invés de IDs mais informativos
   - Benefício: Simples, único, sem colisões
   - Custo: Não contém informação semântica

6. **⚠️ IMPORTANTE - Socket Exhaustion**: Métodos estáticos `TraceableHttpClientFactory.Create()` e `CreateWithPolicy()` estão obsoletos
   - **Problema**: Criar `HttpClient` diretamente causa socket exhaustion em aplicações de alta carga
   - **Solução**: Sempre usar `IHttpClientFactory` com `AddTraceableHttpClient()` ou `AddHttpMessageHandler<CorrelationIdHandler>()`
   - **Métodos seguros disponíveis**: `CreateFromFactory()` e `AddTraceableHttpClient()` (extensão)
   - **Status**: Métodos obsoletos marcados com `[Obsolete]` e documentação atualizada

## GUIA DE MODIFICAÇÃO PARA LLMs

### Como Adicionar Novo Componente

1. **Identificar localização**
   - Verificar estrutura de diretórios
   - Escolher diretório apropriado ou criar novo

2. **Definir responsabilidade única**
   - Componente deve ter uma responsabilidade clara
   - Seguir padrões existentes

3. **Implementar com conditional compilation**
   ```csharp
   #if NET8_0
   // Código .NET 8
   #endif
   
   #if NET48
   // Código .NET Framework
   #endif
   ```

4. **Adicionar dependências**
   - Usar `CorrelationContext` se precisar de correlation-id
   - Seguir padrão de injeção de dependência se aplicável

5. **Adicionar testes**
   - Criar arquivo em `tests/Traceability.Tests/`
   - Testar ambos os frameworks se aplicável

6. **Atualizar documentação**
   - Adicionar XML comments
   - Atualizar README.md se necessário
   - Atualizar este AGENTS.md

### Como Manter Compatibilidade Multi-Framework

1. **Identificar código específico de framework**
   - APIs diferentes entre .NET 8 e .NET Framework
   - Namespaces diferentes

2. **Usar conditional compilation**
   ```csharp
   #if NET8_0
   using Microsoft.AspNetCore.Http;
   #endif
   
   #if NET48
   using System.Web;
   #endif
   ```

3. **Testar em ambos os frameworks**
   - Executar testes para .NET 8
   - Executar testes para .NET Framework 4.8

4. **Manter API pública consistente**
   - Mesma interface pública em ambos os frameworks
   - Comportamento idêntico

### Onde Adicionar Testes

**Estrutura de Testes**:
```
tests/Traceability.Tests/
├── CorrelationContextTests.cs
├── CorrelationIdHandlerTests.cs
├── CorrelationIdMiddlewareTests.cs
├── LoggingTests.cs
└── TraceableHttpClientFactoryTests.cs
```

**Padrão de Teste**:
- Usar xUnit
- Usar FluentAssertions para assertions
- Usar Moq para mocks quando necessário
- Nomear testes: `MethodName_Scenario_ExpectedBehavior`

**Exemplo**:
```csharp
[Fact]
public void Current_WhenNoValue_ShouldGenerateNew()
{
    // Arrange
    CorrelationContext.Clear();
    
    // Act
    var correlationId = CorrelationContext.Current;
    
    // Assert
    correlationId.Should().NotBeNullOrEmpty();
    correlationId.Length.Should().Be(32);
}
```

### Como Atualizar Documentação

1. **XML Comments**
   - Adicionar `<summary>` para classes públicas
   - Adicionar `<param>` para parâmetros
   - Adicionar `<returns>` para retornos
   - Adicionar `<example>` quando útil

2. **README.md**
   - Adicionar exemplos de uso
   - Atualizar seções relevantes
   - Manter consistência com código

3. **AGENTS.md** (este arquivo)
   - Adicionar novo componente na seção "COMPONENTES CORE"
   - Atualizar diagramas se necessário
   - Adicionar exemplos de uso

## EXEMPLOS DE CÓDIGO DE REFERÊNCIA

### Exemplo 1: Uso Básico do CorrelationContext

```csharp
using Traceability;

// Obter ou criar correlation-id
var correlationId = CorrelationContext.Current;

// Verificar se existe
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Obter explicitamente ou criar
var id = CorrelationContext.GetOrCreate();

// Limpar contexto
CorrelationContext.Clear();
```

### Exemplo 2: ASP.NET Core - Configuração Completa

```csharp
// Program.cs
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Adicionar traceability
builder.Services.AddTraceability();

// Adicionar HttpClient traceable
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddHttpMessageHandler<Traceability.HttpClient.CorrelationIdHandler>();

builder.Services.AddControllers();

var app = builder.Build();

// Adicionar middleware (antes dos controllers)
app.UseCorrelationId();

app.MapControllers();
app.Run();
```

### Exemplo 3: Uso em Controller

```csharp
using Traceability;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyController> _logger;

    public MyController(IHttpClientFactory httpClientFactory, ILogger<MyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Correlation-id está automaticamente disponível
        var correlationId = CorrelationContext.Current;
        
        _logger.LogInformation("Processando requisição. CorrelationId: {CorrelationId}", correlationId);

        // HttpClient automaticamente adiciona correlation-id
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");

        return Ok(new { CorrelationId = correlationId });
    }
}
```

### Exemplo 4: HttpClient com Factory

```csharp
using Traceability.HttpClient;

// Criar HttpClient básico
var client = TraceableHttpClientFactory.Create("https://api.example.com/");

// Criar com política Polly
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var clientWithPolicy = TraceableHttpClientFactory.CreateWithPolicy(
    retryPolicy,
    "https://api.example.com/");
```

### Exemplo 5: Teste Unitário

```csharp
using FluentAssertions;
using Traceability;
using Xunit;

public class CorrelationContextTests
{
    [Fact]
    public void Current_WhenNoValue_ShouldGenerateNew()
    {
        // Arrange
        CorrelationContext.Clear();

        // Act
        var correlationId = CorrelationContext.Current;

        // Assert
        correlationId.Should().NotBeNullOrEmpty();
        correlationId.Length.Should().Be(32);
    }
}
```

### Exemplo 6: Isolamento Assíncrono

```csharp
using Traceability;

// Contexto principal
var mainId = CorrelationContext.GetOrCreate();

// Task isolada terá seu próprio contexto
var task = Task.Run(async () =>
{
    // Este contexto é isolado
    var taskId = CorrelationContext.GetOrCreate();
    await Task.Delay(100);
    return taskId;
});

var taskId = await task;

// mainId e taskId são diferentes
Console.WriteLine($"Main: {mainId}, Task: {taskId}");
```

## GLOSSÁRIO DE TERMOS

- **Correlation-ID**: Identificador único usado para rastrear uma requisição através de múltiplos serviços
- **AsyncLocal**: Classe do .NET que armazena valores específicos de um contexto assíncrono
- **DelegatingHandler**: Classe base para handlers HTTP que podem ser encadeados
- **Enricher**: Componente do Serilog que adiciona propriedades aos eventos de log
- **ScopeProvider**: Componente do Microsoft.Extensions.Logging que gerencia scopes de logging
- **Conditional Compilation**: Compilação condicional usando diretivas `#if` para incluir código baseado em condições
- **Middleware**: Componente no pipeline HTTP que processa requisições e respostas
- **MessageHandler**: Handler no pipeline do ASP.NET Web API
- **HttpModule**: Módulo no pipeline do ASP.NET tradicional

## CHECKLIST DE VALIDAÇÃO PARA MODIFICAÇÕES

Ao fazer modificações no código, verificar:

### Código
- [ ] Compilação condicional correta (`#if NET8_0` / `#if NET48`)
- [ ] Uso de `AsyncLocal` para contexto assíncrono
- [ ] Header `X-Correlation-Id` usado consistentemente
- [ ] GUID gerado sem hífens (`ToString("N")`)
- [ ] Não modifica correlation-id existente
- [ ] Thread-safe e async-safe
- [ ] XML comments adicionados/atualizados

### Testes
- [ ] Testes unitários adicionados/atualizados
- [ ] Testes passam para ambos os frameworks
- [ ] Cobertura de casos edge

### Documentação
- [ ] README.md atualizado se necessário
- [ ] AGENTS.md atualizado se necessário
- [ ] Exemplos de uso atualizados

### Compatibilidade
- [ ] Funciona em .NET 8.0
- [ ] Funciona em .NET Framework 4.8
- [ ] Sem breaking changes (ou documentados)

## REFERÊNCIAS RÁPIDAS

### Arquivos Principais
- Core: `src/Traceability/CorrelationContext.cs`
- Middleware (.NET 8): `src/Traceability/Middleware/CorrelationIdMiddleware.cs`
- HttpModule (.NET Framework): `src/Traceability/Middleware/CorrelationIdHttpModule.cs`
- MessageHandler: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`
- HttpClient Handler: `src/Traceability/HttpClient/CorrelationIdHandler.cs`
- Factory: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`
- Serilog: `src/Traceability/Logging/CorrelationIdEnricher.cs`
- MEL: `src/Traceability/Logging/CorrelationIdScopeProvider.cs`

### Exemplos
- ASP.NET Core: `samples/Sample.WebApi.Net8/`
- Console: `samples/Sample.Console.Net8/`
- .NET Framework: `samples/Sample.WebApi.NetFramework/`

### Testes
- Todos os testes: `tests/Traceability.Tests/`

---

**Última atualização**: Baseado na versão 1.0.0 do projeto Traceability

