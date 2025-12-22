# Quick Start

Comece a usar o Traceability em minutos com configuração mínima.

## ASP.NET Core (.NET 8) - Zero Configuração

### 1. Instale o pacote

```bash
dotnet add package Traceability
```

### 2. Configure no `Program.cs` (uma única linha!)

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

### 3. Use em um Controller

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

### 4. Com Logging (Microsoft.Extensions.Logging)

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

### 5. Com HttpClient (propagação automática)

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

**Resultado:**
- ✅ Correlation-id gerado automaticamente em cada requisição
- ✅ Propagado automaticamente em chamadas HTTP
- ✅ Incluído automaticamente nos logs
- ✅ Retornado no header `X-Correlation-Id` da resposta

## Variáveis de Ambiente

Para reduzir verbosidade, você pode usar variáveis de ambiente:

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

Com a variável de ambiente definida, você pode usar:

```csharp
// Source vem automaticamente de TRACEABILITY_SERVICENAME
builder.Services.AddTraceability();
```

## Próximos Passos

- Consulte o [Manual do Usuário](user-guide/index.md) para um guia progressivo completo
- Veja [Exemplos](examples/aspnet-core.md) para mais cenários de uso
- Leia [Configuração](configuration.md) para opções avançadas


