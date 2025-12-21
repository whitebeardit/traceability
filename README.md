# Traceability

Pacote NuGet para gerenciamento automático de correlation-id em aplicações .NET, com suporte para .NET 8 e .NET Framework 4.8.

## Características

- ✅ Gerenciamento automático de correlation-id usando `AsyncLocal`
- ✅ Suporte para .NET 8.0 e .NET Framework 4.8
- ✅ Middleware para ASP.NET Core (.NET 8)
- ✅ HttpModule e MessageHandler para ASP.NET (.NET Framework 4.8)
- ✅ Integração automática com HttpClient
- ✅ Suporte para Serilog e Microsoft.Extensions.Logging
- ✅ Integração com Polly para políticas de resiliência
- ✅ Propagação automática em chamadas HTTP encadeadas

## Instalação

```bash
dotnet add package Traceability
```

## Quick Start

### ASP.NET Core (.NET 8)

```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();

// Adicionar middleware (deve ser antes dos controllers)
app.UseCorrelationId();

app.MapControllers();
app.Run();
```

### ASP.NET Web API (.NET Framework 4.8)

```csharp
using System.Web.Http;
using Traceability.WebApi;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### Console Application

```csharp
using Traceability;

// O correlation-id é gerado automaticamente quando necessário
var correlationId = CorrelationContext.Current;

// Usar em logs, chamadas HTTP, etc.
Console.WriteLine($"Correlation ID: {correlationId}");
```

## Exemplos de Uso

### 1. ASP.NET Core - Configuração Básica

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCorrelationId();
app.MapControllers();

app.Run();
```

### 2. ASP.NET Core - Com Serilog

```csharp
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
```

### 3. ASP.NET Core - Com Microsoft.Extensions.Logging

```csharp
using Traceability.Logging;

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddScopeProvider(new CorrelationIdScopeProvider());
});
```

### 4. HttpClient com Correlation-id (RECOMENDADO - Previne Socket Exhaustion)

```csharp
// Program.cs - Configure o HttpClient no DI
using Traceability.Extensions;
using Traceability.HttpClient;

builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Controller ou Serviço
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CallApiAsync()
    {
        // IHttpClientFactory gerencia o pool de HttpClient, prevenindo socket exhaustion
        var client = _httpClientFactory.CreateClient("ExternalApi");
        // O correlation-id é automaticamente adicionado ao header X-Correlation-Id
        var response = await client.GetAsync("endpoint");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### 5. HttpClient com Dependency Injection (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddHttpMessageHandler<CorrelationIdHandler>();

// Controller
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Get()
    {
        var client = _httpClientFactory.CreateClient("ExternalApi");
        // O correlation-id é automaticamente adicionado
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

### 6. HttpClient com Polly (RECOMENDADO)

```csharp
// Program.cs
using Traceability.Extensions;
using Traceability.HttpClient;
using Polly;
using Polly.Extensions.Http;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Configure com IHttpClientFactory para prevenir socket exhaustion
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddPolicyHandler(retryPolicy);

// No controller ou serviço
var client = _httpClientFactory.CreateClient("ExternalApi");
```

### 7. Uso Manual do CorrelationContext

```csharp
using Traceability;

// Obter correlation-id atual (cria se não existir)
var correlationId = CorrelationContext.Current;

// Verificar se existe
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Obter ou criar explicitamente
var id = CorrelationContext.GetOrCreate();

// Limpar contexto
CorrelationContext.Clear();
```

### 8. Logging Automático com Correlation-id

#### Serilog

```csharp
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Todos os logs automaticamente incluem CorrelationId
Log.Information("Mensagem de log");
```

#### Microsoft.Extensions.Logging

```csharp
using Traceability.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddScopeProvider(new CorrelationIdScopeProvider());
});

var logger = loggerFactory.CreateLogger<Program>();

// O correlation-id é automaticamente incluído no scope
logger.LogInformation("Mensagem de log");
```

### 9. ASP.NET Tradicional (.NET Framework 4.8)

#### Configuração no web.config

```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

#### Uso no código

```csharp
using Traceability;

public class MyPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var correlationId = CorrelationContext.Current;
        // Usar correlation-id
    }
}
```

## API Reference

### CorrelationContext

Classe estática para gerenciar o correlation-id no contexto assíncrono.

#### Propriedades

- `Current` (string): Obtém ou define o correlation-id atual. Cria um novo se não existir.
- `HasValue` (bool): Verifica se existe um correlation-id no contexto.

#### Métodos

- `GetOrCreate()`: Obtém o correlation-id existente ou cria um novo.
- `Clear()`: Limpa o correlation-id do contexto.

### CorrelationIdMiddleware (ASP.NET Core)

Middleware que gerencia correlation-id automaticamente.

**Uso:**
```csharp
app.UseCorrelationId();
```

### CorrelationIdMessageHandler (ASP.NET Web API)

MessageHandler para ASP.NET Web API.

**Uso:**
```csharp
config.MessageHandlers.Add(new CorrelationIdMessageHandler());
```

### CorrelationIdHttpModule (ASP.NET Tradicional)

HttpModule para aplicações ASP.NET tradicionais.

**Configuração:** Via web.config (veja exemplo acima)

### TraceableHttpClientFactory

Factory para criar HttpClient com correlation-id usando IHttpClientFactory. Previne socket exhaustion ao reutilizar conexões HTTP.

**Métodos (.NET 8):**
- `CreateFromFactory(IHttpClientFactory factory, string? clientName = null, string? baseAddress = null)`: Cria HttpClient usando IHttpClientFactory que gerencia o pool de conexões.
- `AddTraceableHttpClient(this IServiceCollection services, string clientName, Action<HttpClient>? configureClient = null)`: Método de extensão para configurar HttpClient no DI com CorrelationIdHandler automaticamente.

**Exemplo de uso:**
```csharp
// Configure no Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use no serviço ou controller
var client = _httpClientFactory.CreateClient("ExternalApi");
```

### CorrelationIdHandler

DelegatingHandler que adiciona correlation-id aos headers HTTP.

**Uso:**
```csharp
services.AddHttpClient("MyClient")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

### Logging

#### CorrelationIdEnricher (Serilog)

Enricher que adiciona correlation-id aos logs do Serilog.

**Uso:**
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.With<CorrelationIdEnricher>()
    .CreateLogger();
```

#### CorrelationIdScopeProvider (Microsoft.Extensions.Logging)

Provider que adiciona correlation-id ao scope de logging.

**Uso:**
```csharp
builder.Services.AddLogging(builder =>
{
    builder.AddScopeProvider(new CorrelationIdScopeProvider());
});
```

## Prevenção de Socket Exhaustion

Este pacote foi projetado para prevenir socket exhaustion desde o início. Todos os métodos de criação de HttpClient usam `IHttpClientFactory`, que gerencia o pool de conexões HTTP e reutiliza sockets, evitando o esgotamento.

### Como Funciona

O `IHttpClientFactory` gerencia o ciclo de vida dos `HttpClient`:
- Reutiliza conexões HTTP quando possível
- Gerencia o pool de sockets automaticamente
- Previne socket exhaustion mesmo em alta carga

### Uso Correto

```csharp
// Configure no Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use no serviço ou controller
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task CallApiAsync()
    {
        // IHttpClientFactory reutiliza conexões, prevenindo socket exhaustion
        var client = _httpClientFactory.CreateClient("ExternalApi");
        await client.GetAsync("endpoint");
    }
}
```

## Limitações

1. **.NET Framework 4.8**: Não tem DI nativo, então `TraceabilityOptions` deve ser configurado via métodos estáticos `Configure()` em `CorrelationIdHttpModule` e `CorrelationIdMessageHandler`.
2. **Validação de Formato**: A validação de formato do correlation-id é opcional e deve ser habilitada via `TraceabilityOptions.ValidateCorrelationIdFormat`.
3. **IHttpClientFactory**: Os métodos de criação de HttpClient requerem `IHttpClientFactory` (disponível apenas em .NET 8 para este pacote). Para .NET Framework, use `CorrelationIdHandler` diretamente com seu próprio gerenciamento de HttpClient.

## Troubleshooting

### O correlation-id não está sendo propagado

1. Certifique-se de que o middleware/handler está configurado corretamente.
2. Verifique se está usando `IHttpClientFactory` com `AddTraceableHttpClient()` ou `AddHttpMessageHandler<CorrelationIdHandler>()` para chamadas HTTP.
3. Em aplicações assíncronas, certifique-se de que o contexto assíncrono está sendo preservado.

### Correlation-id não aparece nos logs

1. Para Serilog: Certifique-se de que `CorrelationIdEnricher` está configurado.
2. Para Microsoft.Extensions.Logging: Certifique-se de que `CorrelationIdScopeProvider` está configurado.
3. Verifique o template de output do logger para incluir `{CorrelationId}`.

### Problemas com .NET Framework 4.8

1. Certifique-se de que as versões corretas das dependências estão instaladas.
2. Para Web API, adicione o `CorrelationIdMessageHandler` no `Global.asax.cs`.
3. Para ASP.NET tradicional, configure o `CorrelationIdHttpModule` no `web.config`.
4. Para configurar opções, use `CorrelationIdHttpModule.Configure()` ou `CorrelationIdMessageHandler.Configure()` antes de usar.

## Contribuindo

Contribuições são bem-vindas! Por favor, abra uma issue ou pull request.

## Licença

MIT

## Versão

1.0.0

