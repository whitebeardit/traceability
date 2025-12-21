# Sample.WebApi.NetFramework

Este é um exemplo de uso do pacote Traceability em uma aplicação ASP.NET Web API no .NET Framework 4.8.

## Configuração

### 1. Adicionar MessageHandler no Global.asax.cs

```csharp
using System.Web.Http;
using Traceability.WebApi;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            // Adicionar o CorrelationIdMessageHandler
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            
            // Suas outras configurações...
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### 2. Usar CorrelationContext nos Controllers

```csharp
using System.Web.Http;
using Traceability;

public class ApiController : ApiController
{
    public IHttpActionResult Get()
    {
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

### 3. Usar HttpClient com Correlation-id

Para .NET Framework 4.8, você precisa gerenciar o HttpClient manualmente. Use `CorrelationIdHandler` para adicionar o correlation-id automaticamente:

```csharp
using System.Net.Http;
using Traceability.HttpClient;

// Configure o HttpClient uma vez (reutilize para evitar socket exhaustion)
var handler = new CorrelationIdHandler
{
    InnerHandler = new HttpClientHandler()
};
var httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri("https://api.example.com/")
};

// Use o mesmo HttpClient para múltiplas requisições
var response = await httpClient.GetAsync("endpoint");
// O correlation-id é automaticamente adicionado ao header
```

**Importante**: Reutilize o mesmo `HttpClient` para múltiplas requisições. Não crie um novo `HttpClient` a cada chamada para evitar socket exhaustion.

## Notas

- O CorrelationIdMessageHandler deve ser adicionado primeiro na cadeia de MessageHandlers
- O correlation-id é automaticamente propagado para chamadas HTTP feitas com TraceableHttpClientFactory
- Para logging, use Serilog com CorrelationIdEnricher ou Microsoft.Extensions.Logging com CorrelationIdScopeProvider

