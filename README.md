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

**Output Esperado:**

Quando uma requisição HTTP é feita, o middleware automaticamente:
- Gera um correlation-id se não existir no header `X-Correlation-Id`
- Adiciona o correlation-id no header de resposta `X-Correlation-Id`

**Exemplo de Requisição/Resposta:**

```http
GET /api/values HTTP/1.1
Host: localhost:5000
```

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab

{
  "value": "test"
}
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

**Output Esperado:**

O MessageHandler automaticamente:
- Gera um correlation-id se não existir no header `X-Correlation-Id`
- Adiciona o correlation-id no header de resposta `X-Correlation-Id`

**Exemplo de Requisição/Resposta:**

```http
GET /api/values HTTP/1.1
Host: localhost:8080
```

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd

{
  "value": "test"
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

**Output Esperado:**

```
Correlation ID: 1a2b3c4d5e6f7890123456789012345ef
```

O correlation-id é um GUID de 32 caracteres (sem hífens) gerado automaticamente.

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
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
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
Host: jsonplaceholder.typicode.com
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
            BaseAddress = new System.Uri("https://jsonplaceholder.typicode.com/")
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
Host: jsonplaceholder.typicode.com
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



