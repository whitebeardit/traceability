# Traceability

Pacote NuGet para gerenciamento automático de correlation-id em aplicações .NET, com suporte para .NET 8 e .NET Framework 4.8.

> 📖 **[Quick Start](#quick-start)** | [Manual do Usuário](docs/user-guide/index.md) | [Documentação Completa](docs/index.md) | [Exemplos](docs/examples/aspnet-core.md)

## Motivação

Em arquiteturas distribuídas e microserviços, rastrear uma requisição através de múltiplos serviços é essencial para debugging, monitoramento e análise de performance. O **correlation-id** é um identificador único que permite rastrear uma requisição desde sua origem até todas as chamadas subsequentes.

### Quando usar esta biblioteca?

Use o **Traceability** quando você precisa:

1. **Rastreabilidade em Microserviços**: Rastrear uma requisição através de múltiplos serviços
2. **Debugging Simplificado**: Identificar rapidamente todos os logs relacionados a uma requisição
3. **Análise de Performance**: Medir o tempo total de processamento através de múltiplos serviços
4. **Monitoramento e Observabilidade**: Correlacionar métricas, traces e logs de diferentes serviços

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
dotnet add package WhiteBeard.Traceability
```

## Quick Start

### ASP.NET Core (.NET 8) - Zero Configuração

**1. Instale o pacote:**
```bash
dotnet add package WhiteBeard.Traceability
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

## Documentação

- **[Manual do Usuário](docs/user-guide/index.md)** - Guia progressivo para iniciantes
- **[Quick Start](docs/getting-started.md)** - Comece a usar em minutos
- **[Instalação](docs/installation.md)** - Guia de instalação
- **[Configuração](docs/configuration.md)** - Opções de configuração detalhadas
- **[Referência da API](docs/api-reference.md)** - Documentação completa da API
- **[Exemplos](docs/examples/aspnet-core.md)** - Exemplos práticos
- **[Troubleshooting](docs/troubleshooting.md)** - Solução de problemas comuns
- **[Tópicos Avançados](docs/advanced.md)** - Recursos avançados

## Exemplos Rápidos

### Com Logging

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);

// No Controller
_logger.LogInformation("Processando requisição");
// Output: => CorrelationId: a1b2c3d4e5f6789012345678901234ab
```

### Com HttpClient

```csharp
// Program.cs
builder.Services.AddTraceability("MyService");
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// No Controller
var client = _httpClientFactory.CreateClient("ExternalApi");
// Correlation-id é automaticamente adicionado no header
```

## Frameworks Suportados

- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

## Contribuindo

Contribuições são bem-vindas! Por favor, abra uma issue ou pull request.

Para desenvolvedores que desejam contribuir:
- **[CI/CD e Releases](docs/development/ci-cd.md)** - Processo de versionamento e publicação
- **[Documentação Técnica](AGENTS.md)** - Arquitetura e guia técnico completo

## Licença

MIT

## Versão

1.0.0
