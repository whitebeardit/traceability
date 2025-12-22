# Referência da API

Documentação completa da API pública do Traceability.

## CorrelationContext

Classe estática para gerenciar o correlation-id no contexto assíncrono.

### Propriedades

#### Current
```csharp
public static string Current { get; set; }
```
Obtém ou define o correlation-id atual. Cria um novo se não existir.

#### HasValue
```csharp
public static bool HasValue { get; }
```
Verifica se existe um correlation-id no contexto.

### Métodos

#### GetOrCreate()
```csharp
public static string GetOrCreate()
```
Obtém o correlation-id existente ou cria um novo.

#### TryGetValue()
```csharp
public static bool TryGetValue(out string? value)
```
Tenta obter o correlation-id existente sem criar um novo se não existir. Retorna `true` se um correlation-id existe, `false` caso contrário.

#### Clear()
```csharp
public static void Clear()
```
Limpa o correlation-id do contexto.

## Extensions

### ServiceCollectionExtensions

#### AddTraceability
```csharp
public static IServiceCollection AddTraceability(
    this IServiceCollection services,
    Action<TraceabilityOptions>? configureOptions = null)

public static IServiceCollection AddTraceability(
    this IServiceCollection services,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Registra os serviços do Traceability no container de DI.

#### AddTraceableHttpClient
```csharp
public static IHttpClientBuilder AddTraceableHttpClient(
    this IServiceCollection services,
    string clientName,
    Action<HttpClient>? configureClient = null)
```
Configura HttpClient com CorrelationIdHandler automaticamente.

### ApplicationBuilderExtensions

#### UseCorrelationId
```csharp
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
```
Registra o middleware CorrelationIdMiddleware no pipeline HTTP.

### LoggerConfigurationExtensions

#### WithTraceability
```csharp
public static LoggerConfiguration WithTraceability(
    this LoggerConfiguration config,
    string? source = null)
```
Adiciona SourceEnricher e CorrelationIdEnricher ao Serilog.

#### WithTraceabilityJson
```csharp
public static LoggerConfiguration WithTraceabilityJson(
    this LoggerConfiguration config,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Adiciona SourceEnricher, CorrelationIdEnricher e DataEnricher para template JSON.

## Middleware

### CorrelationIdMiddleware (ASP.NET Core)

Middleware que gerencia correlation-id automaticamente.

**Uso:**
```csharp
app.UseCorrelationId();
```

## Handlers

### CorrelationIdMessageHandler (ASP.NET Web API)

MessageHandler para ASP.NET Web API.

**Uso:**
```csharp
config.MessageHandlers.Add(new CorrelationIdMessageHandler());
```

### CorrelationIdHttpModule (ASP.NET Tradicional)

HttpModule para aplicações ASP.NET tradicionais.

**Configuração:** Via web.config (veja exemplos)

### CorrelationIdHandler (HttpClient)

DelegatingHandler que adiciona correlation-id aos headers HTTP.

**Uso:**
```csharp
services.AddHttpClient("MyClient")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

## Factory

### TraceableHttpClientFactory

Factory para criar HttpClient com correlation-id usando IHttpClientFactory.

**Métodos (.NET 8):**
```csharp
public static HttpClient CreateFromFactory(
    IHttpClientFactory factory,
    string? clientName = null,
    string? baseAddress = null)
```

**Exemplo de uso:**
```csharp
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

## Logging

### CorrelationIdEnricher (Serilog)

Enricher que adiciona correlation-id aos logs do Serilog.

**Uso:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .CreateLogger();
```

### CorrelationIdScopeProvider (Microsoft.Extensions.Logging)

Provider que adiciona correlation-id ao scope de logging.

**Uso:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

## Configuration

### TraceabilityOptions

Opções de configuração para o pacote.

Veja [Configuração](configuration.md) para detalhes completos.


