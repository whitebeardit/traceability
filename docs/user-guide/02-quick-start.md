# Lição 2: Quick Start

Vamos começar a usar o Traceability! Nesta lição, você verá como configurar o pacote com o mínimo de código possível.

## Instalação

Primeiro, instale o pacote via NuGet:

```bash
dotnet add package Traceability
```

Ou via Package Manager Console:

```powershell
Install-Package Traceability
```

## Configuração Mínima (ASP.NET Core)

A configuração mais simples possível - apenas uma linha de código!

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuração - tudo é automático!
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Pronto!** Agora o Traceability está configurado e funcionando. Vamos ver como usar.

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

Faça uma requisição para o endpoint:

```bash
curl -X GET http://localhost:5000/api/values
```

**Resposta esperada:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab"
}
```

E no header da resposta:
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## O Que Aconteceu?

1. ✅ O middleware foi registrado automaticamente
2. ✅ Um correlation-id foi gerado automaticamente (GUID de 32 caracteres)
3. ✅ O correlation-id foi adicionado ao contexto assíncrono
4. ✅ O correlation-id foi retornado no header da resposta

## Com Source Explícito (Opcional)

Se você quiser definir o nome do serviço explicitamente:

```csharp
builder.Services.AddTraceability("MyService");
```

Isso adiciona o campo `Source` aos logs (veremos mais sobre isso na Lição 6).

## Próximos Passos

Agora que você viu o básico, vamos aprender mais sobre o `CorrelationContext` na [Lição 3: Uso Básico](03-basic-usage.md).

