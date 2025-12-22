# Exemplos - ASP.NET Framework 4.8

Exemplos práticos de uso do Traceability em aplicações .NET Framework.

## ASP.NET Web API

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

**Controller:**
```csharp
using System.Web.Http;
using Traceability;

public class ValuesController : ApiController
{
    [HttpGet]
    public IHttpActionResult Get()
    {
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

## ASP.NET Tradicional

**web.config:**
```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

**Global.asax.cs (opcional - para configurar opções):**
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

**Page/Controller:**
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

## Exemplo com Serilog

```csharp
using Traceability.Extensions;
using Serilog;

// No Application_Start ou Startup
Log.Logger = new LoggerConfiguration()
    .WithTraceability("MyService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// No Controller
Log.Information("Processando requisição");
```

**Output nos Logs:**
```
[14:23:45 INF] MyService a1b2c3d4e5f6789012345678901234ab Processando requisição
```

## Exemplo Completo

Veja o exemplo completo em `samples/Sample.WebApi.NetFramework/`.

