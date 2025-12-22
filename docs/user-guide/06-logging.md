# Lição 6: Logging

Nesta lição, você aprenderá a integrar o Traceability com sistemas de logging.

## Serilog

### Configuração Básica

**Program.cs:**
```csharp
using Traceability.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### Usando nos Logs

**Controller:**
```csharp
using Serilog;

public class ValuesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id aparece automaticamente nos logs
        Log.Information("Processando requisição");
        return Ok();
    }
}
```

**Output esperado:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processando requisição
```

### Template JSON

Para output em JSON:

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

**Output esperado (JSON):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","Message":"Processando requisição"}
```

## Microsoft.Extensions.Logging

### Configuração

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

### Usando nos Logs

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

**Output esperado:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      Processando requisição
```

## Campo Source

O campo `Source` identifica a origem/serviço que está gerando os logs. É essencial para unificar logs em ambientes distribuídos.

**Configuração:**
```csharp
builder.Services.AddTraceability("UserService");
```

Ou via variável de ambiente:
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Output com Source:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processando requisição
```

## Logs em Cadeia de Chamadas

Quando você tem uma cadeia de chamadas (Serviço A → Serviço B → Serviço C), todos os logs terão o mesmo correlation-id:

**Serviço A (Logs):**
```
[14:23:45 INF] ServiceA a1b2c3d4e5f6789012345678901234ab Recebendo requisição
[14:23:45 INF] ServiceA a1b2c3d4e5f6789012345678901234ab Chamando Serviço B
```

**Serviço B (Logs):**
```
[14:23:45 INF] ServiceB a1b2c3d4e5f6789012345678901234ab Recebendo requisição do Serviço A
[14:23:45 INF] ServiceB a1b2c3d4e5f6789012345678901234ab Chamando Serviço C
```

**Serviço C (Logs):**
```
[14:23:45 INF] ServiceC a1b2c3d4e5f6789012345678901234ab Recebendo requisição do Serviço B
[14:23:46 INF] ServiceC a1b2c3d4e5f6789012345678901234ab Processamento concluído
```

**Benefício:** Você pode buscar por `a1b2c3d4e5f6789012345678901234ab` em todos os logs e rastrear toda a cadeia de execução!

## Próximos Passos

Agora que você sabe usar logging, vamos ver como usar com HttpClient na [Lição 7: HttpClient](07-httpclient.md).

