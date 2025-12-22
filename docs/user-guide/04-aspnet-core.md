# Lição 4: ASP.NET Core

Nesta lição, você aprenderá a integrar o Traceability com ASP.NET Core (.NET 8).

## Configuração Básica

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**O que acontece automaticamente:**
- ✅ Middleware é registrado automaticamente via `IStartupFilter`
- ✅ HttpClient é configurado automaticamente com `CorrelationIdHandler`
- ✅ Correlation-id é gerado automaticamente em cada requisição

## Usando em um Controller

**ValuesController.cs:**
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

## Testando

**Requisição:**
```bash
curl -X GET http://localhost:5000/api/values
```

**Resposta:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab"
}
```

**Headers da resposta:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Requisição com Correlation-ID Existente

Se você enviar uma requisição com o header `X-Correlation-Id`, o middleware reutiliza o valor:

**Requisição:**
```bash
curl -X GET http://localhost:5000/api/values \
  -H "X-Correlation-Id: 12345678901234567890123456789012"
```

**Resposta:**
```json
{
  "correlationId": "12345678901234567890123456789012"
}
```

O mesmo correlation-id é retornado, garantindo rastreabilidade na cadeia.

## Exemplo com Logging

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

**Controller:**
```csharp
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

**Output nos logs:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
```

## Exemplo com HttpClient

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
// HttpClient já está configurado automaticamente!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

**Controller:**
```csharp
public class ValuesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ValuesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Correlation-id é automaticamente adicionado no header
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

A requisição HTTP externa automaticamente inclui o header:
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Desabilitar Auto-Registro (Avançado)

Se você precisar de controle manual sobre a ordem do middleware:

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Registro manual
app.MapControllers();
app.Run();
```

## Próximos Passos

Agora que você sabe integrar com ASP.NET Core, vamos ver como fazer o mesmo com .NET Framework na [Lição 5: ASP.NET Framework](05-aspnet-framework.md), ou pule para [Lição 6: Logging](06-logging.md) se você só usa .NET 8.


