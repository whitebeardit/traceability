# Lição 5: ASP.NET Framework

Nesta lição, você aprenderá a integrar o Traceability com ASP.NET Framework 4.8.

## ASP.NET Web API

### Configuração

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
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### Usando em um Controller

**ValuesController.cs:**
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

### Testando

**Requisição:**
```bash
curl -X GET http://localhost:8080/api/values
```

**Resposta:**
```json
{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd"
}
```

**Headers da resposta:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd
```

## ASP.NET Tradicional

### Configuração no web.config

**web.config:**
```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

### Configuração de Opções (Opcional)

**Global.asax.cs:**
```csharp
using Traceability.Middleware;
using Traceability.Configuration;

protected void Application_Start()
{
    CorrelationIdHttpModule.Configure(new TraceabilityOptions
    {
        HeaderName = "X-Correlation-Id",
        ValidateCorrelationIdFormat = true
    });
}
```

### Usando em uma Page

**MyPage.aspx.cs:**
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

## Configuração de Opções

Para configurar opções no .NET Framework, use os métodos estáticos `Configure()`:

**Para Web API:**
```csharp
CorrelationIdMessageHandler.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

**Para ASP.NET Tradicional:**
```csharp
CorrelationIdHttpModule.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

## Diferenças do .NET 8

No .NET Framework 4.8:

- ❌ Não há DI nativo, então use métodos estáticos `Configure()` para opções
- ❌ Não há `IHttpClientFactory`, então gerencie HttpClient manualmente
- ✅ Funcionalidade de correlation-id é idêntica

## Próximos Passos

Agora que você sabe integrar com ambos os frameworks, vamos ver como usar com logging na [Lição 6: Logging](06-logging.md).

