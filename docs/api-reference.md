# API Reference

Complete documentation of the Traceability public API.

## CorrelationContext

Static class for managing correlation-id in the asynchronous context.

### Properties

#### Current
```csharp
public static string Current { get; set; }
```
Gets or sets the current correlation-id. Creates a new one if it doesn't exist.

#### HasValue
```csharp
public static bool HasValue { get; }
```
Checks if a correlation-id exists in the context.

### Methods

#### GetOrCreate()
```csharp
public static string GetOrCreate()
```
Gets the existing correlation-id or creates a new one.

#### TryGetValue()
```csharp
public static bool TryGetValue(out string? value)
```
Attempts to get the existing correlation-id without creating a new one if it doesn't exist. Returns `true` if a correlation-id exists, `false` otherwise.

#### Clear()
```csharp
public static void Clear()
```
Clears the correlation-id from the context.

## Extensions

### ServiceCollectionExtensions

#### AddTraceability
```csharp
public static IServiceCollection AddTraceability(
    this IServiceCollection services,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Registers Traceability services in the DI container.

**Parameters:**
- `source` (optional): Name of the origin/service. If not provided, it will be read from `TraceabilityOptions.Source`, `TRACEABILITY_SERVICENAME` environment variable, or assembly name (if `UseAssemblyNameAsFallback = true`).
- `configureOptions` (optional): Action to configure additional options.

**Examples:**
```csharp
// With explicit source
builder.Services.AddTraceability("UserService");

// With source and options
builder.Services.AddTraceability("UserService", options => {
    options.HeaderName = "X-Custom-Id";
});

// Without source (uses env var or assembly name)
builder.Services.AddTraceability();

// Options only (source comes from env var or assembly name)
builder.Services.AddTraceability(configureOptions: options => {
    options.HeaderName = "X-Custom-Id";
});
```

#### AddTraceabilityLogging
```csharp
public static IServiceCollection AddTraceabilityLogging(
    this IServiceCollection services,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Adds traceability services with logging configured. Only configures the Source for log origin identification, without automatically registering middleware or HttpClient.

**Parameters:**
- `source` (optional): Name of the origin/service. If not provided, it will be read from `TraceabilityOptions.Source`, `TRACEABILITY_SERVICENAME` environment variable, or assembly name (if `UseAssemblyNameAsFallback = true`).
- `configureOptions` (optional): Action to configure additional options.

**Note:** This method only configures logging. For complete configuration (middleware + HttpClient + logging), use `AddTraceability()`.

**Parâmetros:**
- `source` (opcional): Nome da origem/serviço. Se não fornecido, será lido de `TraceabilityOptions.Source`, variável de ambiente `TRACEABILITY_SERVICENAME`, ou assembly name (se `UseAssemblyNameAsFallback = true`).
- `configureOptions` (opcional): Ação para configurar opções adicionais.

**Exemplos:**
```csharp
// Com source explícito
builder.Services.AddTraceability("UserService");

// Com source e opções
builder.Services.AddTraceability("UserService", options => {
    options.HeaderName = "X-Custom-Id";
});

// Sem source (usa env var ou assembly name)
builder.Services.AddTraceability();

// Apenas opções (source vem de env var ou assembly name)
builder.Services.AddTraceability(configureOptions: options => {
    options.HeaderName = "X-Custom-Id";
});
```

#### AddTraceabilityLogging
```csharp
public static IServiceCollection AddTraceabilityLogging(
    this IServiceCollection services,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Adiciona os serviços de traceability com logging configurado. Configura apenas o Source para identificação da origem dos logs, sem registrar middleware ou HttpClient automaticamente.

**Parâmetros:**
- `source` (opcional): Nome da origem/serviço. Se não fornecido, será lido de `TraceabilityOptions.Source`, variável de ambiente `TRACEABILITY_SERVICENAME`, ou assembly name (se `UseAssemblyNameAsFallback = true`).
- `configureOptions` (opcional): Ação para configurar opções adicionais.

**Nota:** Este método configura apenas o logging. Para configuração completa (middleware + HttpClient + logging), use `AddTraceability()`.

#### AddTraceableHttpClient
```csharp
public static IHttpClientBuilder AddTraceableHttpClient(
    this IServiceCollection services,
    string clientName,
    Action<HttpClient>? configureClient = null)
```
Configures HttpClient with CorrelationIdHandler automatically.

### ApplicationBuilderExtensions

#### UseCorrelationId
```csharp
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
```
Registers the CorrelationIdMiddleware in the HTTP pipeline.

### LoggerConfigurationExtensions

#### WithTraceability
```csharp
public static LoggerConfiguration WithTraceability(
    this LoggerConfiguration config,
    string? source = null)
```
Adds SourceEnricher and CorrelationIdEnricher to Serilog.

#### WithTraceabilityJson
```csharp
public static LoggerConfiguration WithTraceabilityJson(
    this LoggerConfiguration config,
    string? source = null,
    Action<TraceabilityOptions>? configureOptions = null)
```
Adds SourceEnricher, CorrelationIdEnricher, and DataEnricher for JSON template.

## Middleware

### CorrelationIdMiddleware (ASP.NET Core)

Middleware that automatically manages correlation-id.

**Usage:**
```csharp
app.UseCorrelationId();
```

## Handlers

### CorrelationIdMessageHandler (ASP.NET Web API)

MessageHandler for ASP.NET Web API.

**Note**: In .NET Framework 4.8, the `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod` - manual registration is not needed.

**Manual Usage (Advanced - Not Recommended):**
```csharp
config.MessageHandlers.Add(new CorrelationIdMessageHandler());
```

### CorrelationIdHttpModule (Traditional ASP.NET)

HttpModule for traditional ASP.NET applications.

**Note**: The `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod` - no manual configuration needed.

**Manual Configuration (Advanced - Not Recommended):** Via web.config (see examples)

### CorrelationIdHandler (HttpClient)

DelegatingHandler that adds correlation-id to HTTP headers.

**Usage:**
```csharp
services.AddHttpClient("MyClient")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

## Factory

### TraceableHttpClientFactory

Factory for creating HttpClient with correlation-id using IHttpClientFactory.

**Methods (.NET 8):**
```csharp
public static HttpClient CreateFromFactory(
    IHttpClientFactory factory,
    string? clientName = null,
    string? baseAddress = null)
```

**Usage example:**
```csharp
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

## Logging

### CorrelationIdEnricher (Serilog)

Enricher that adds correlation-id to Serilog logs.

**Usage:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .CreateLogger();
```

### CorrelationIdScopeProvider (Microsoft.Extensions.Logging)

Provider that adds correlation-id to the logging scope.

**Usage:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

## Configuration

### TraceabilityOptions

Configuration options for the package.

See [Configuration](configuration.md) for complete details.
