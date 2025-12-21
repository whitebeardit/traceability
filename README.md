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

### 4. HttpClient com Correlation-id

```csharp
using Traceability.HttpClient;

// Criar HttpClient que automaticamente adiciona correlation-id
var httpClient = TraceableHttpClientFactory.Create("https://api.example.com/");

// O correlation-id é automaticamente adicionado ao header X-Correlation-Id
var response = await httpClient.GetAsync("endpoint");
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

### 6. HttpClient com Polly

```csharp
using Traceability.HttpClient;
using Polly;
using Polly.Extensions.Http;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var httpClient = TraceableHttpClientFactory.CreateWithPolicy(
    retryPolicy,
    "https://api.example.com/");
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

Factory para criar HttpClient com correlation-id.

**Métodos:**
- `Create(string baseAddress = null)`: Cria HttpClient básico com correlation-id.
- `CreateWithPolicy(IAsyncPolicy<HttpResponseMessage> policy, string baseAddress = null)`: Cria HttpClient com política Polly.

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

## Troubleshooting

### O correlation-id não está sendo propagado

1. Certifique-se de que o middleware/handler está configurado corretamente.
2. Verifique se está usando `TraceableHttpClientFactory` ou `CorrelationIdHandler` para chamadas HTTP.
3. Em aplicações assíncronas, certifique-se de que o contexto assíncrono está sendo preservado.

### Correlation-id não aparece nos logs

1. Para Serilog: Certifique-se de que `CorrelationIdEnricher` está configurado.
2. Para Microsoft.Extensions.Logging: Certifique-se de que `CorrelationIdScopeProvider` está configurado.
3. Verifique o template de output do logger para incluir `{CorrelationId}`.

### Problemas com .NET Framework 4.8

1. Certifique-se de que as versões corretas das dependências estão instaladas.
2. Para Web API, adicione o `CorrelationIdMessageHandler` no `Global.asax.cs`.
3. Para ASP.NET tradicional, configure o `CorrelationIdHttpModule` no `web.config`.

## Contribuindo

Contribuições são bem-vindas! Por favor, abra uma issue ou pull request.

## Licença

MIT

## Versão

1.0.0

