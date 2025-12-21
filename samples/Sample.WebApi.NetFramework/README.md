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

```csharp
using Traceability.HttpClient;

var httpClient = TraceableHttpClientFactory.Create("https://api.example.com/");
var response = await httpClient.GetAsync("endpoint");
// O correlation-id é automaticamente adicionado ao header
```

## Notas

- O CorrelationIdMessageHandler deve ser adicionado primeiro na cadeia de MessageHandlers
- O correlation-id é automaticamente propagado para chamadas HTTP feitas com TraceableHttpClientFactory
- Para logging, use Serilog com CorrelationIdEnricher ou Microsoft.Extensions.Logging com CorrelationIdScopeProvider

