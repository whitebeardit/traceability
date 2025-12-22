# Traceability

Pacote NuGet para gerenciamento automático de correlation-id em aplicações .NET, com suporte para .NET 8 e .NET Framework 4.8.

> 📖 **[Quick Start](#quick-start)** | [Instalação](#instalação) | [Documentação Completa](#exemplos-de-uso)

## Motivação

Em arquiteturas distribuídas e microserviços, rastrear uma requisição através de múltiplos serviços é essencial para debugging, monitoramento e análise de performance. O **correlation-id** (também conhecido como correlation identifier ou request ID) é um identificador único que permite rastrear uma requisição desde sua origem até todas as chamadas subsequentes.

### Quando usar esta biblioteca?

Use o **Traceability** quando você precisa:

1. **Rastreabilidade em Microserviços**: Rastrear uma requisição através de múltiplos serviços em uma arquitetura distribuída, permitindo correlacionar logs de diferentes serviços usando o mesmo correlation-id.

2. **Debugging Simplificado**: Identificar rapidamente todos os logs relacionados a uma requisição específica, mesmo quando ela passa por vários serviços, facilitando a investigação de problemas.

3. **Análise de Performance**: Medir o tempo total de processamento de uma requisição através de múltiplos serviços, identificando gargalos na cadeia de chamadas.

4. **Monitoramento e Observabilidade**: Correlacionar métricas, traces e logs de diferentes serviços usando o mesmo identificador, melhorando a visibilidade do sistema.

5. **Suporte Multi-Framework**: Trabalhar com aplicações .NET 8.0 (ASP.NET Core) e .NET Framework 4.8 (ASP.NET Web API e ASP.NET Tradicional) usando a mesma biblioteca.

6. **Integração Automática**: Ter correlation-id automaticamente propagado em chamadas HTTP, adicionado aos logs (Serilog e Microsoft.Extensions.Logging) e gerenciado sem código boilerplate.

### Benefícios

- ✅ **Zero Configuração**: Funciona out-of-the-box com configuração mínima
- ✅ **Thread-Safe e Async-Safe**: Usa `AsyncLocal` para garantir isolamento correto em contextos assíncronos
- ✅ **Prevenção de Socket Exhaustion**: Integração nativa com `IHttpClientFactory` para gerenciamento eficiente de conexões HTTP
- ✅ **Integração com Logging**: Suporte automático para Serilog e Microsoft.Extensions.Logging
- ✅ **Propagação Automática**: Correlation-id é automaticamente propagado em todas as chamadas HTTP encadeadas

### Exemplo de Cenário Real

Imagine uma requisição de pedido que passa por três serviços:

```
Cliente → API Gateway → Serviço de Pedidos → Serviço de Pagamento → Serviço de Notificação
```

Sem correlation-id, você teria que procurar logs em cada serviço separadamente. Com o **Traceability**, todos os logs terão o mesmo correlation-id (`a1b2c3d4...`), permitindo buscar por este ID em todos os serviços e ver o fluxo completo da requisição.

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

### ASP.NET Core (.NET 8) - Zero Configuração

**1. Instale o pacote:**
```bash
dotnet add package Traceability
```

**2. Configure no `Program.cs` (uma única linha!):**

```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuração - tudo é automático!
// Source vem de TRACEABILITY_SERVICENAME ou assembly name
// Middleware é registrado automaticamente
// HttpClient é configurado automaticamente
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Com Source explícito (opcional):**
```csharp
builder.Services.AddTraceability("MyService");
```

**3. Use em um Controller:**

```csharp
using Microsoft.AspNetCore.Mvc;
using Traceability;

[ApiController]
[Route("api/[controller]")]
public class ValuesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id está automaticamente disponível
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

**4. Com Logging (Microsoft.Extensions.Logging):**

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);

// No Controller
public class ValuesController : ControllerBase
{
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id aparece automaticamente nos logs
        _logger.LogInformation("Processando requisição");
        return Ok();
    }
}
```

**Output nos Logs:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
```

**5. Com HttpClient (propagação automática):**

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
// HttpClient já está configurado automaticamente com CorrelationIdHandler!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// No Controller ou Serviço
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> CallExternalApiAsync()
    {
        // Correlation-id é automaticamente adicionado no header
        // Não precisa de .AddHttpMessageHandler<CorrelationIdHandler>()!
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return await response.Content.ReadAsStringAsync();
    }
}
```

**Opt-out (quando necessário):**

Se precisar de controle manual sobre a ordem do middleware ou configuração de HttpClient:

```csharp
builder.Services.AddTraceability("MyService", options =>
{
    options.AutoRegisterMiddleware = false;  // Desabilita auto-registro do middleware
    options.AutoConfigureHttpClient = false; // Desabilita auto-configuração de HttpClient
});

var app = builder.Build();
app.UseCorrelationId(); // Agora você controla a ordem manualmente
```

**Resultado:**
- ✅ Correlation-id gerado automaticamente em cada requisição
- ✅ Propagado automaticamente em chamadas HTTP
- ✅ Incluído automaticamente nos logs
- ✅ Retornado no header `X-Correlation-Id` da resposta

## Variáveis de Ambiente

O pacote Traceability suporta variáveis de ambiente para reduzir verbosidade na configuração e garantir uniformização de logs em todas as aplicações e serviços.

### Variáveis Suportadas

#### `TRACEABILITY_SERVICENAME`
Define o nome do serviço/origem que está gerando os logs. Este valor será adicionado ao campo `Source` em todos os logs.

**Prioridade de Configuração:**
1. Parâmetro `source` fornecido explicitamente (prioridade máxima)
2. `TraceabilityOptions.Source` definido nas opções
3. Variável de ambiente `TRACEABILITY_SERVICENAME`
4. Assembly name (se `UseAssemblyNameAsFallback = true`, padrão: true)
5. Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único

#### `LOG_LEVEL`
Define o nível mínimo de log (Verbose, Debug, Information, Warning, Error, Fatal).

**Prioridade de Configuração:**
1. Variável de ambiente `LOG_LEVEL` (prioridade máxima)
2. `TraceabilityOptions.MinimumLogLevel` definido nas opções
3. Information (padrão)

### Configuração

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
$env:LOG_LEVEL="Information"
```

**Windows CMD:**
```cmd
set TRACEABILITY_SERVICENAME=UserService
set LOG_LEVEL=Information
```

### Exemplos de Uso

#### Com Variável de Ambiente (Source Opcional)

```csharp
// Variável de ambiente TRACEABILITY_SERVICENAME="UserService" definida
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

// Source é opcional quando env var está definida
Log.Logger = new LoggerConfiguration()
    .WithTraceability() // source opcional - lê de TRACEABILITY_SERVICENAME
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Ou com AddTraceability
builder.Services.AddTraceability(); // source opcional
```

#### Com Parâmetro Explícito (Sobrescreve Env Var)

```csharp
// Mesmo com TRACEABILITY_SERVICENAME="UserService" definida
Log.Logger = new LoggerConfiguration()
    .WithTraceability("CustomService") // parâmetro tem prioridade sobre env var
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Ou com AddTraceability
builder.Services.AddTraceability("CustomService"); // sobrescreve env var
```

#### Erro Quando Não Há Source

```csharp
// Se TRACEABILITY_SERVICENAME não estiver definida e source não for fornecido
// Uma exceção será lançada para forçar o padrão único
try
{
    Log.Logger = new LoggerConfiguration()
        .WithTraceability() // source opcional, mas env var não existe
        .WriteTo.Console(new JsonFormatter())
        .CreateLogger();
}
catch (InvalidOperationException ex)
{
    // Exceção informa que Source deve ser fornecido
    // via parâmetro, options ou variável de ambiente
}
```

### Output JSON Obrigatório

**Importante:** Todos os logs gerados pelo Traceability são sempre em formato JSON para garantir uniformização entre diferentes aplicações e serviços, independente do framework (.NET 8 ou .NET Framework 4.8).

O formato JSON padrão inclui:
- `Timestamp`: Data e hora do log
- `Level`: Nível do log (Information, Warning, Error, etc.)
- `Source`: Nome do serviço (obtido de `TRACEABILITY_SERVICENAME` ou parâmetro)
- `CorrelationId`: ID de correlação (quando disponível)
- `Message`: Mensagem do log
- `Data`: Objetos serializados (quando presente)
- `Exception`: Informações de exceção (quando presente)

### ASP.NET Web API (.NET Framework 4.8)

**1. Instale o pacote via NuGet Package Manager ou CLI:**
```bash
Install-Package Traceability
```

**2. Configure no `Global.asax.cs`:**

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

**3. Use em um Controller:**

```csharp
using System.Web.Http;
using Traceability;

public class ValuesController : ApiController
{
    [HttpGet]
    public IHttpActionResult Get()
    {
        // Correlation-id está automaticamente disponível
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

**4. Com Serilog (recomendado para .NET Framework):**

```csharp
using Traceability.Extensions;
using Serilog;

// No Application_Start ou Startup
Log.Logger = new LoggerConfiguration()
    .WithTraceability("MyService") // Source + CorrelationId
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// No Controller
Log.Information("Processando requisição");
```

**Output nos Logs:**
```
[14:23:45 INF] MyService a1b2c3d4e5f6789012345678901234ab Processando requisição
```

### Console Application

**1. Instale o pacote:**
```bash
dotnet add package Traceability
```

**2. Use o CorrelationContext:**

```csharp
using Traceability;

// O correlation-id é gerado automaticamente quando necessário
var correlationId = CorrelationContext.Current;

// Usar em logs, chamadas HTTP, etc.
Console.WriteLine($"Correlation ID: {correlationId}");
```

**3. Com Serilog:**

```csharp
using Traceability.Extensions;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// Gerar correlation-id
var correlationId = CorrelationContext.GetOrCreate();

// Logs incluem correlation-id automaticamente
Log.Information("Processando tarefa");
Log.Information("Tarefa concluída");
```

**Output:**
```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processando tarefa
[14:23:46 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Tarefa concluída
```

> 💡 **Nota:** O correlation-id é um GUID de 32 caracteres (sem hífens) gerado automaticamente.

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

**Exemplo Completo - ASP.NET Core (.NET 8)**

Aqui está um exemplo completo com controller e output esperado:

**Program.cs:**

```csharp
using Traceability.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog com Traceability (recomendado)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WithTraceability("UserService") // Source + CorrelationId
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// AddTraceability() agora configura defaults e integrações automaticamente.
// Use a sobrecarga com Source para padronizar logs em ambientes distribuídos.
builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

// Configurar HttpClient traceable (CorrelationIdHandler é adicionado automaticamente)
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://whitebeard.dev/");
});

var app = builder.Build();

app.UseCorrelationId();
app.MapControllers();

app.Run();
```

**Controller:**

```csharp
using Microsoft.AspNetCore.Mvc;
using Traceability;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class ValuesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(IHttpClientFactory httpClientFactory, ILogger<ValuesController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        var correlationId = CorrelationContext.Current;
        _logger.LogInformation("Processando requisição com CorrelationId: {CorrelationId}", correlationId);

        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("posts/1");
        
        var content = await response.Content.ReadAsStringAsync();
        
        return Ok(new
        {
            CorrelationId = correlationId,
            Message = "Requisição processada com sucesso",
            Data = content
        });
    }
}
```

**Output Esperado:**

**1. Logs no Console (Serilog):**

```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processando requisição com CorrelationId: a1b2c3d4e5f6789012345678901234ab
[14:23:46 INF] UserService a1b2c3d4e5f6789012345678901234ab Requisição externa concluída
```

**2. Requisição HTTP (sem correlation-id):**

```bash
curl -X GET http://localhost:5000/api/values/test
```

**3. Resposta HTTP (com correlation-id no header):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab

{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "message": "Requisição processada com sucesso",
  "data": "{ ... }"
}
```

**4. Requisição HTTP Externa (chamada do HttpClient):**

O HttpClient automaticamente adiciona o correlation-id no header:

```http
GET /posts/1 HTTP/1.1
Host: whitebeard.dev
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

### 2. ASP.NET Core - Com Serilog

```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("Sample.WebApi.Net8") // Source + CorrelationId
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
```

**Output Esperado nos Logs:**

```
[14:23:45 INF] Sample.WebApi.Net8 a1b2c3d4e5f6789012345678901234ab Processando requisição GET /api/values
[14:23:45 INF] Sample.WebApi.Net8 a1b2c3d4e5f6789012345678901234ab Chamada externa realizada com sucesso
[14:23:45 INF] Sample.WebApi.Net8 a1b2c3d4e5f6789012345678901234ab Resposta enviada ao cliente
```

O correlation-id aparece automaticamente em todos os logs graças ao `WithTraceability()` (que adiciona `CorrelationIdEnricher`).

### 3. ASP.NET Core - Com Microsoft.Extensions.Logging

```csharp
using Traceability.Extensions;

builder.Services.AddTraceability("Sample.WebApi.Net8");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

**Output Esperado nos Logs:**

```
info: Sample.WebApi.Net8.Controllers.ApiController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Iniciando chamada externa com CorrelationId: a1b2c3d4e5f6789012345678901234ab
```

O correlation-id aparece no scope do log automaticamente.

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

**Exemplo Completo - Console Application**

Aqui está um exemplo completo de aplicação console com output esperado:

**Console Application (.NET 8):**

```csharp
using Traceability;
using Traceability.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

// Configurar Serilog com Traceability (Source + CorrelationId)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<Program>();

// Exemplo 1: Uso básico
Console.WriteLine("=== Exemplo 1: Uso básico do CorrelationContext ===");
var correlationId = CorrelationContext.GetOrCreate();
Console.WriteLine($"Correlation ID gerado: {correlationId}");
Console.WriteLine($"Correlation ID atual: {CorrelationContext.Current}");
Console.WriteLine();

// Exemplo 2: Logging com correlation-id
Console.WriteLine("=== Exemplo 2: Logging com correlation-id ===");
logger.LogInformation("Mensagem de log com correlation-id automático");
Console.WriteLine();

// Exemplo 3: Correlation-id preservado em operações assíncronas
Console.WriteLine("=== Exemplo 3: Correlation-id preservado em operações assíncronas ===");
var correlationIdBefore = CorrelationContext.Current;
logger.LogInformation("Correlation ID antes da operação assíncrona: {CorrelationId}", correlationIdBefore);

await Task.Delay(100);

var correlationIdAfter = CorrelationContext.Current;
logger.LogInformation("Correlation ID após operação assíncrona: {CorrelationId}", correlationIdAfter);
Console.WriteLine($"Correlation ID preservado: {correlationIdBefore == correlationIdAfter}");
Console.WriteLine();

Log.CloseAndFlush();
```

**Output Esperado (.NET 8):**

```
=== Exemplo 1: Uso básico do CorrelationContext ===
Correlation ID gerado: a1b2c3d4e5f6789012345678901234ab
Correlation ID atual: a1b2c3d4e5f6789012345678901234ab

=== Exemplo 2: Logging com correlation-id ===
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Mensagem de log com correlation-id automático

=== Exemplo 3: Correlation-id preservado em operações assíncronas ===
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Correlation ID antes da operação assíncrona: a1b2c3d4e5f6789012345678901234ab
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Correlation ID após operação assíncrona: a1b2c3d4e5f6789012345678901234ab
Correlation ID preservado: True
```

**Console Application (.NET Framework 4.8):**

```csharp
using System;
using System.Threading.Tasks;
using Traceability;

namespace Sample.Console.NetFramework
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Exemplo 1: Uso básico
            Console.WriteLine("=== Exemplo 1: Uso básico do CorrelationContext ===");
            var correlationId = CorrelationContext.GetOrCreate();
            Console.WriteLine($"Correlation ID gerado: {correlationId}");
            Console.WriteLine($"Correlation ID atual: {CorrelationContext.Current}");
            Console.WriteLine();

            // Exemplo 2: Correlation-id preservado em operações assíncronas
            Console.WriteLine("=== Exemplo 2: Correlation-id preservado em operações assíncronas ===");
            var correlationIdBefore = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID antes da operação assíncrona: {correlationIdBefore}");

            await Task.Delay(100);

            var correlationIdAfter = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID após operação assíncrona: {correlationIdAfter}");
            Console.WriteLine($"Correlation ID preservado: {correlationIdBefore == correlationIdAfter}");
            Console.WriteLine();

            // Exemplo 3: Múltiplas operações com o mesmo correlation-id
            Console.WriteLine("=== Exemplo 3: Múltiplas operações com o mesmo correlation-id ===");
            var initialCorrelationId = CorrelationContext.Current;
            Console.WriteLine($"Operação 1 com CorrelationId: {initialCorrelationId}");

            await Task.Delay(100);

            Console.WriteLine($"Operação 2 com CorrelationId: {CorrelationContext.Current}");

            await Task.Delay(100);

            Console.WriteLine($"Operação 3 com CorrelationId: {CorrelationContext.Current}");
            Console.WriteLine($"Todas as operações usaram o mesmo Correlation ID: {CorrelationContext.Current == initialCorrelationId}");
            Console.WriteLine();

            Console.WriteLine("Exemplos concluídos!");
            Console.ReadKey();
        }
    }
}
```

**Output Esperado (.NET Framework 4.8):**

```
=== Exemplo 1: Uso básico do CorrelationContext ===
Correlation ID gerado: f1e2d3c4b5a6978012345678901234cd
Correlation ID atual: f1e2d3c4b5a6978012345678901234cd

=== Exemplo 2: Correlation-id preservado em operações assíncronas ===
Correlation ID antes da operação assíncrona: f1e2d3c4b5a6978012345678901234cd
Correlation ID após operação assíncrona: f1e2d3c4b5a6978012345678901234cd
Correlation ID preservado: True

=== Exemplo 3: Múltiplas operações com o mesmo correlation-id ===
Operação 1 com CorrelationId: f1e2d3c4b5a6978012345678901234cd
Operação 2 com CorrelationId: f1e2d3c4b5a6978012345678901234cd
Operação 3 com CorrelationId: f1e2d3c4b5a6978012345678901234cd
Todas as operações usaram o mesmo Correlation ID: True

Exemplos concluídos!
```

### 8. Logging Automático com Correlation-id

#### Serilog

```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Todos os logs automaticamente incluem CorrelationId
Log.Information("Mensagem de log");
```

**Output Esperado:**

```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Mensagem de log
```

#### Microsoft.Extensions.Logging

```csharp
using Traceability.Extensions;

// Em apps .NET 8 (Host/DI), basta registrar Traceability e habilitar scopes do Console.
builder.Services.AddTraceability("ConsoleApp");
builder.Logging.AddConsole(options => options.IncludeScopes = true);

// O correlation-id é automaticamente incluído no scope
logger.LogInformation("Mensagem de log");
```

**Output Esperado:**

```
info: Program[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Mensagem de log
```

### 9. Template JSON Padrão com Serialização Automática de Objetos

O pacote Traceability oferece suporte para template JSON padrão configurável que inclui automaticamente: Timestamp, Level, Source, CorrelationId, Message, Data (objetos serializados) e Exception.

**Importante:** Todos os logs gerados pelo Traceability são sempre em formato JSON para garantir uniformização entre diferentes aplicações e serviços, independente do framework (.NET 8 ou .NET Framework 4.8).

#### Uso Básico

```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

// Configurar logger com template JSON
// Source pode vir de variável de ambiente TRACEABILITY_SERVICENAME
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService") // ou .WithTraceabilityJson() se env var estiver definida
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Exemplo 1: Log simples (sem objeto)
Log.Information("Serviço iniciado");

// Exemplo 2: Log com objeto (objeto será serializado automaticamente)
var user = new { UserId = 123, UserName = "john.doe" };
Log.Information("Processando requisição {@User}", user);
```

**Output Esperado - Exemplo 1 (sem objeto):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Serviço iniciado"}
```

**Output Esperado - Exemplo 2 (com objeto):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Processando requisição","Data":{"UserId":123,"UserName":"john.doe"}}
```

#### Configuração Básica

```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

// Configuração simples com template JSON padrão
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Uso no código - Com objeto (objeto será serializado no campo "data")
var user = new { UserId = 123, UserName = "john.doe" };
Log.Information("Processando requisição {@User}", user);

// Uso no código - Sem objeto (apenas mensagem)
Log.Information("Requisição processada com sucesso");
```

**Output Esperado (JSON) - Com objeto:**

```json
{
  "Timestamp": "2024-01-15T14:23:45.123Z",
  "Level": "Information",
  "Source": "UserService",
  "CorrelationId": "a1b2c3d4e5f6789012345678901234ab",
  "Message": "Processando requisição",
  "Data": {
    "UserId": 123,
    "UserName": "john.doe"
  }
}
```

**Output Esperado (JSON) - Sem objeto:**

```json
{
  "Timestamp": "2024-01-15T14:23:46.456Z",
  "Level": "Information",
  "Source": "UserService",
  "CorrelationId": "a1b2c3d4e5f6789012345678901234ab",
  "Message": "Requisição processada com sucesso"
}
```

#### Configuração Customizada

```csharp
using Traceability.Extensions;
using Traceability.Configuration;
using Traceability.Logging;
using Serilog;

// Configuração com opções customizadas
var options = new TraceabilityOptions
{
    Source = "UserService",
    LogOutputFormat = LogOutputFormat.JsonIndented,
    LogIncludeData = true,
    LogIncludeTimestamp = true,
    LogIncludeLevel = true
};

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService", opt =>
    {
        opt.LogOutputFormat = LogOutputFormat.JsonIndented;
        opt.LogIncludeData = true;
        opt.LogIncludeTimestamp = true;
        opt.LogIncludeLevel = true;
    })
    .WriteTo.Console(new JsonFormatter(options, indent: true))
    .CreateLogger();

// Uso
var order = new { OrderId = 456, Total = 99.99 };
Log.Information("Pedido processado {@Order}", order);
```

**Output Esperado (JSON Indentado):**

```json
{
  "Timestamp": "2024-01-15T14:23:45.123Z",
  "Level": "Information",
  "Source": "UserService",
  "CorrelationId": "a1b2c3d4e5f6789012345678901234ab",
  "Message": "Pedido processado",
  "Data": {
    "OrderId": 456,
    "Total": 99.99
  }
}
```

#### Exemplo Completo - ASP.NET Core

```csharp
// Program.cs
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog com template JSON padrão
var options = new TraceabilityOptions
{
    Source = "UserService",
    LogOutputFormat = LogOutputFormat.JsonCompact,
    LogIncludeData = true
};

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson(options)
    .WriteTo.Console(new JsonFormatter(options))
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.UseCorrelationId();
app.MapControllers();
app.Run();
```

**Controller:**

```csharp
using Microsoft.AspNetCore.Mvc;
using Traceability;
using Serilog;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = new { UserId = id, UserName = "john.doe", Email = "john@example.com" };
        
        // O objeto será automaticamente serializado no campo "data"
        Log.Information("Usuário encontrado {@User}", user);
        
        return Ok(user);
    }
}
```

**Output Esperado:**

```json
{
  "Timestamp": "2024-01-15T14:23:45.123Z",
  "Level": "Information",
  "Source": "UserService",
  "CorrelationId": "a1b2c3d4e5f6789012345678901234ab",
  "Message": "Usuário encontrado",
  "Data": {
    "UserId": 123,
    "UserName": "john.doe",
    "Email": "john@example.com"
  }
}
```

#### Opções de Configuração

O `TraceabilityOptions` oferece as seguintes opções para customizar o template JSON:

- `LogOutputFormat`: Formato de saída (JsonCompact, JsonIndented, Text)
- `LogIncludeTimestamp`: Incluir timestamp (padrão: true)
- `LogIncludeLevel`: Incluir level (padrão: true)
- `LogIncludeSource`: Incluir Source (padrão: true)
- `LogIncludeCorrelationId`: Incluir CorrelationId (padrão: true)
- `LogIncludeMessage`: Incluir Message (padrão: true)
- `LogIncludeData`: Incluir campo Data para objetos (padrão: true)
- `LogIncludeException`: Incluir Exception (padrão: true)

#### Como Funciona

1. **DataEnricher**: Detecta automaticamente objetos complexos nas propriedades do log e os serializa no campo `data`
2. **JsonFormatter**: Formata os logs em JSON estruturado com base nas opções configuradas
3. **Serialização Automática**: Quando você passa um objeto usando `{@Objeto}`, o Serilog o serializa e o `DataEnricher` o move para o campo `data`

**Exemplos de Uso:**

```csharp
// Exemplo 1: Log apenas com mensagem (sem objeto)
Log.Information("Serviço iniciado");
// Output: JSON com Timestamp, Level, Source, CorrelationId, Message

// Exemplo 2: Log com objeto (objeto será serializado em "Data")
var user = new { UserId = 123, UserName = "john.doe" };
Log.Information("Usuário autenticado {@User}", user);
// Output: JSON com Timestamp, Level, Source, CorrelationId, Message, Data

// Exemplo 3: Log com múltiplos objetos (todos serão agrupados em "Data")
var user = new { UserId = 123, UserName = "john.doe" };
var order = new { OrderId = 456, Total = 99.99 };
Log.Information("Processando pedido {@User} {@Order}", user, order);
// Output: JSON com Data contendo ambos os objetos

// Exemplo 4: Log com exceção
try {
    // código
} catch (Exception ex) {
    Log.Error(ex, "Erro ao processar requisição");
}
// Output: JSON com Exception serializada
```

**Nota**: O `DataEnricher` ignora propriedades primitivas (strings, números, etc.) e propriedades conhecidas (Source, CorrelationId, etc.), movendo apenas objetos complexos para o campo `data`. Se não houver objetos complexos, o campo `Data` não será incluído no JSON.

### 10. ASP.NET Tradicional (.NET Framework 4.8)

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

**Exemplo Completo - ASP.NET Web API (.NET Framework 4.8)**

Aqui está um exemplo completo com Global.asax, Controller e output esperado:

**Global.asax.cs:**

```csharp
using System.Web.Http;
using Traceability.WebApi;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            // Adicionar CorrelationIdMessageHandler
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        });
    }
}
```

**Controller:**

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Traceability;
using Traceability.HttpClient;

public class ValuesController : ApiController
{
    // Reutilizar HttpClient para evitar socket exhaustion
    private static readonly HttpClient _httpClient = CreateHttpClient();
    
    private static HttpClient CreateHttpClient()
    {
        var handler = new CorrelationIdHandler
        {
            InnerHandler = new HttpClientHandler()
        };
        return new HttpClient(handler)
        {
            BaseAddress = new System.Uri("https://whitebeard.dev/")
        };
    }

    [HttpGet]
    [Route("api/values/test")]
    public async Task<IHttpActionResult> Test()
    {
        var correlationId = CorrelationContext.Current;
        
        // HttpClient automaticamente adiciona correlation-id no header
        var response = await _httpClient.GetAsync("posts/1");
        var content = await response.Content.ReadAsStringAsync();
        
        return Ok(new
        {
            CorrelationId = correlationId,
            Message = "Requisição processada com sucesso",
            Data = content
        });
    }
}
```

**Output Esperado:**

**1. Requisição HTTP (sem correlation-id):**

```bash
curl -X GET http://localhost:8080/api/values/test
```

**2. Resposta HTTP (com correlation-id no header):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd

{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd",
  "message": "Requisição processada com sucesso",
  "data": "{ ... }"
}
```

**3. Requisição HTTP Externa (chamada do HttpClient):**

O HttpClient automaticamente adiciona o correlation-id no header:

```http
GET /posts/1 HTTP/1.1
Host: whitebeard.dev
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd
```

**Nota:** No .NET Framework 4.8, é importante reutilizar o mesmo `HttpClient` para evitar socket exhaustion. Crie uma instância estática ou use um padrão singleton.

## Exemplos de Requisições HTTP

Esta seção mostra exemplos práticos de como o correlation-id é gerenciado em requisições HTTP.

### Requisição sem Correlation-ID (Gera Novo)

Quando uma requisição é feita sem o header `X-Correlation-Id`, o middleware/handler gera automaticamente um novo correlation-id.

**Requisição:**

```bash
curl -X GET http://localhost:5000/api/values/test
```

Ou via HTTP:

```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
```

**Resposta (.NET 8 - ASP.NET Core):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab

{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "message": "Requisição processada com sucesso"
}
```

**Resposta (.NET Framework 4.8 - Web API):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd

{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd",
  "message": "Requisição processada com sucesso"
}
```

### Requisição com Correlation-ID (Reutiliza Existente)

Quando uma requisição é feita com o header `X-Correlation-Id`, o middleware/handler reutiliza o valor fornecido.

**Requisição:**

```bash
curl -X GET http://localhost:5000/api/values/test \
  -H "X-Correlation-Id: 12345678901234567890123456789012"
```

Ou via HTTP:

```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
X-Correlation-Id: 12345678901234567890123456789012
```

**Resposta (.NET 8 - ASP.NET Core):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: 12345678901234567890123456789012

{
  "correlationId": "12345678901234567890123456789012",
  "message": "Requisição processada com sucesso"
}
```

**Resposta (.NET Framework 4.8 - Web API):**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: 12345678901234567890123456789012

{
  "correlationId": "12345678901234567890123456789012",
  "message": "Requisição processada com sucesso"
}
```

**Observação:** O mesmo correlation-id é retornado na resposta, garantindo rastreabilidade em toda a cadeia de chamadas.

### Propagação em Cadeia de Chamadas

O correlation-id é automaticamente propagado em chamadas HTTP encadeadas. Veja o exemplo:

**Cenário:** Serviço A → Serviço B → Serviço C

**1. Cliente chama Serviço A (sem correlation-id):**

```http
GET /api/service-a/process HTTP/1.1
Host: service-a.example.com
```

**Resposta do Serviço A:**

```http
HTTP/1.1 200 OK
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**2. Serviço A chama Serviço B (correlation-id propagado automaticamente):**

O HttpClient do Serviço A automaticamente adiciona o correlation-id:

```http
GET /api/service-b/data HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**3. Serviço B chama Serviço C (correlation-id propagado automaticamente):**

```http
GET /api/service-c/process HTTP/1.1
Host: service-c.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**Resultado:** Todos os serviços na cadeia usam o mesmo correlation-id (`a1b2c3d4e5f6789012345678901234ab`), permitindo rastrear toda a requisição através dos logs de todos os serviços.

### Exemplo com Postman

**Configuração no Postman:**

1. Crie uma nova requisição
2. Na aba "Headers", adicione:
   - Key: `X-Correlation-Id`
   - Value: `12345678901234567890123456789012` (opcional - se não fornecer, será gerado)

**Requisição:**

```
GET http://localhost:5000/api/values/test
Headers:
  X-Correlation-Id: 12345678901234567890123456789012
```

**Resposta:**

```json
{
  "correlationId": "12345678901234567890123456789012",
  "message": "Requisição processada com sucesso"
}
```

E no header da resposta:

```
X-Correlation-Id: 12345678901234567890123456789012
```

## Exemplos de Logs

Esta seção mostra exemplos de como o correlation-id aparece nos logs com diferentes frameworks de logging.

### Serilog com WithTraceability (RECOMENDADO)

**Configuração:**

```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService") // Source + CorrelationId
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Código de Exemplo:**

```csharp
var correlationId = CorrelationContext.GetOrCreate();
Log.Information("Processando requisição");
Log.Information("Chamando serviço externo");
Log.Information("Requisição concluída");
```

**Output Esperado (.NET 8):**

```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processando requisição
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Chamando serviço externo
[14:23:46 INF] UserService a1b2c3d4e5f6789012345678901234ab Requisição concluída
```

**Output Esperado (.NET Framework 4.8):**

```
[14:23:45 INF] f1e2d3c4b5a6978012345678901234cd Processando requisição
[14:23:45 INF] f1e2d3c4b5a6978012345678901234cd Chamando serviço externo
[14:23:46 INF] f1e2d3c4b5a6978012345678901234cd Requisição concluída
```

**Template Customizado:**

Você pode customizar o template de output para incluir mais informações:

```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{Source}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Output com Template Customizado:**

```
[2024-01-15 14:23:45.123] [INF] [UserService] [a1b2c3d4e5f6789012345678901234ab] Processando requisição
[2024-01-15 14:23:45.456] [INF] [UserService] [a1b2c3d4e5f6789012345678901234ab] Chamando serviço externo
[2024-01-15 14:23:46.789] [INF] [UserService] [a1b2c3d4e5f6789012345678901234ab] Requisição concluída
```

### Microsoft.Extensions.Logging (scopes) com AddTraceability (RECOMENDADO)

**Configuração (.NET 8):**

```csharp
using Traceability.Extensions;

// AddTraceability registra/decorate IExternalScopeProvider para incluir
// CorrelationId e (opcionalmente) Source nos scopes do logging.
builder.Services.AddTraceability("UserService");

// Para exibir scopes no console:
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

**Código de Exemplo:**

```csharp
var logger = loggerFactory.CreateLogger<MyService>();
var correlationId = CorrelationContext.GetOrCreate();

logger.LogInformation("Processando requisição");
logger.LogInformation("Chamando serviço externo");
logger.LogInformation("Requisição concluída");
```

**Output Esperado (.NET 8):**

```
info: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
info: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Chamando serviço externo
info: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Requisição concluída
```

**Nota (.NET Framework 4.8):**

O pacote não faz integração automática de DI/logging no .NET Framework. Para logs, prefira Serilog + `WithTraceability()` (ou `SourceEnricher` + `CorrelationIdEnricher`).

### Comparação: Serilog vs Microsoft.Extensions.Logging

**Serilog (Mais Compacto):**

```
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Processando requisição
```

**Microsoft.Extensions.Logging (Mais Detalhado):**

```
info: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
```

**Vantagens de cada um:**

- **Serilog:** Output mais compacto, ideal para logs em produção, fácil de parsear
- **Microsoft.Extensions.Logging:** Mais informações contextuais, integração nativa com .NET, suporte a scopes aninhados

### Exemplo de Logs em Cadeia de Chamadas

Quando você tem uma cadeia de chamadas (Serviço A → Serviço B → Serviço C), todos os logs terão o mesmo correlation-id:

**Serviço A (Logs):**

```
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Recebendo requisição
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Chamando Serviço B
[14:23:46 INF] a1b2c3d4e5f6789012345678901234ab Resposta recebida do Serviço B
```

**Serviço B (Logs):**

```
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Recebendo requisição do Serviço A
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Chamando Serviço C
[14:23:46 INF] a1b2c3d4e5f6789012345678901234ab Resposta recebida do Serviço C
```

**Serviço C (Logs):**

```
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Recebendo requisição do Serviço B
[14:23:46 INF] a1b2c3d4e5f6789012345678901234ab Processamento concluído
```

**Benefício:** Você pode buscar por `a1b2c3d4e5f6789012345678901234ab` em todos os logs e rastrear toda a cadeia de execução!

### Exemplo de Logs com Erros

Quando ocorre um erro, o correlation-id ajuda a rastrear toda a requisição que falhou:

**Serilog:**

```
[14:23:45 INF] a1b2c3d4e5f6789012345678901234ab Processando requisição
[14:23:45 ERR] a1b2c3d4e5f6789012345678901234ab Erro ao chamar serviço externo
System.Net.Http.HttpRequestException: Connection timeout
   at MyApp.MyService.CallExternalServiceAsync()
   at MyApp.MyService.ProcessRequestAsync()
```

**Microsoft.Extensions.Logging:**

```
info: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
fail: MyApp.MyService[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Erro ao chamar serviço externo
      System.Net.Http.HttpRequestException: Connection timeout
         at MyApp.MyService.CallExternalServiceAsync()
         at MyApp.MyService.ProcessRequestAsync()
```

## API Reference

### CorrelationContext

Classe estática para gerenciar o correlation-id no contexto assíncrono.

#### Propriedades

- `Current` (string): Obtém ou define o correlation-id atual. Cria um novo se não existir.
- `HasValue` (bool): Verifica se existe um correlation-id no contexto.

#### Métodos

- `GetOrCreate()`: Obtém o correlation-id existente ou cria um novo.
- `TryGetValue(out string? value)`: Tenta obter o correlation-id existente sem criar um novo se não existir. Retorna `true` se um correlation-id existe, `false` caso contrário.
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
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .CreateLogger();
```

#### CorrelationIdScopeProvider (Microsoft.Extensions.Logging)

Provider que adiciona correlation-id ao scope de logging.

**Uso:**
```csharp
// Em .NET 8: registre Traceability (ele decora o IExternalScopeProvider) e habilite scopes no console.
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
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

1. Para Serilog: use `WithTraceability("SuaOrigem")` (ou configure `SourceEnricher` + `CorrelationIdEnricher`).
2. Para Microsoft.Extensions.Logging (.NET 8): chame `AddTraceability("SuaOrigem")` e habilite scopes no Console (`IncludeScopes = true`).
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



