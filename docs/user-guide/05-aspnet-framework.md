# Lesson 5: ASP.NET Framework

In this lesson, you'll learn to integrate Traceability with ASP.NET Framework 4.8.

## ASP.NET Web API

### Configuration

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

### Using in a Controller

**ValuesController.cs:**
```csharp
using System.Web.Http;
using Traceability;

public class ValuesController : ApiController
{
    [HttpGet]
    public IHttpActionResult Get()
    {
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

### Testing

**Request:**
```bash
curl -X GET http://localhost:8080/api/values
```

**Response:**
```json
{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd"
}
```

**Response headers:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd
```

## Traditional ASP.NET

### Configuration in web.config

**web.config:**
```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

### Options Configuration (Optional)

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

### Using in a Page

**MyPage.aspx.cs:**
```csharp
using Traceability;

public class MyPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var correlationId = CorrelationContext.Current;
        // Use correlation-id
    }
}
```

## Options Configuration

To configure options in .NET Framework, use the static `Configure()` methods:

**For Web API:**
```csharp
CorrelationIdMessageHandler.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

**For Traditional ASP.NET:**
```csharp
CorrelationIdHttpModule.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

## Differences from .NET 8

In .NET Framework 4.8:

- ❌ There's no native DI, so use static `Configure()` methods for options
- ❌ There's no `IHttpClientFactory`, so manage HttpClient manually
- ✅ Correlation-id functionality is identical

## Next Steps

Now that you know how to integrate with both frameworks, let's see how to use with logging in [Lesson 6: Logging](06-logging.md).
