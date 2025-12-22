# Lição 9: Exemplos Práticos

Nesta lição, você verá exemplos práticos completos com output esperado.

## Exemplo 1: ASP.NET Core Completo

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog com Traceability
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
builder.Services.AddControllers();

var app = builder.Build();
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
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var correlationId = CorrelationContext.Current;
        
        Log.Information("Buscando usuário {UserId}", id);

        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync($"users/{id}");
        
        var content = await response.Content.ReadAsStringAsync();
        
        Log.Information("Usuário encontrado");
        
        return Ok(new { CorrelationId = correlationId, Data = content });
    }
}
```

**Requisição:**
```bash
curl -X GET http://localhost:5000/api/users/123
```

**Output nos Logs (JSON):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Buscando usuário 123"}
{"Timestamp":"2024-01-15T14:23:46.456Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Usuário encontrado"}
```

**Resposta HTTP:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "data": "{ ... }"
}
```

**Headers da resposta:**
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Exemplo 2: Console Application

**Program.cs:**
```csharp
using Traceability;
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

var correlationId = CorrelationContext.GetOrCreate();
Log.Information("Iniciando processamento");

await ProcessarDadosAsync();

Log.Information("Processamento concluído");
Log.CloseAndFlush();

async Task ProcessarDadosAsync()
{
    Log.Information("Processando dados");
    await Task.Delay(100);
    Log.Information("Dados processados");
}
```

**Output esperado:**
```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Iniciando processamento
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processando dados
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Dados processados
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processamento concluído
```

## Exemplo 3: Propagação em Cadeia

**Serviço A (API Gateway):**
```csharp
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet("process")]
    public async Task<IActionResult> Process()
    {
        var correlationId = CorrelationContext.Current;
        Log.Information("Recebendo requisição no Gateway");

        var client = _httpClientFactory.CreateClient("OrderService");
        var response = await client.GetAsync("orders/process");
        
        Log.Information("Resposta recebida do OrderService");
        return Ok();
    }
}
```

**Serviço B (Order Service):**
```csharp
public class OrderController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet("orders/process")]
    public async Task<IActionResult> Process()
    {
        var correlationId = CorrelationContext.Current;
        Log.Information("Processando pedido no OrderService");

        var client = _httpClientFactory.CreateClient("PaymentService");
        var response = await client.PostAsync("payments/process", null);
        
        Log.Information("Pagamento processado");
        return Ok();
    }
}
```

**Logs do Serviço A:**
```
[14:23:45 INF] Gateway a1b2c3d4e5f6789012345678901234ab Recebendo requisição no Gateway
[14:23:46 INF] Gateway a1b2c3d4e5f6789012345678901234ab Resposta recebida do OrderService
```

**Logs do Serviço B:**
```
[14:23:45 INF] OrderService a1b2c3d4e5f6789012345678901234ab Processando pedido no OrderService
[14:23:46 INF] OrderService a1b2c3d4e5f6789012345678901234ab Pagamento processado
```

**Benefício:** Você pode buscar por `a1b2c3d4e5f6789012345678901234ab` em ambos os serviços e ver o fluxo completo!

## Próximos Passos

Agora que você viu exemplos práticos, vamos ver como resolver problemas comuns na [Lição 10: Troubleshooting](10-troubleshooting.md).

