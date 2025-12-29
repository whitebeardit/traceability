# Sample.WebApi.NetFramework

This is an example of using the Traceability package in an ASP.NET Web API application on .NET Framework 4.8.

## Configuration

### Recommended (zero-code): no Traceability registration needed

After installing the package, Traceability auto-registers `CorrelationIdHttpModule` via `PreApplicationStartMethod`.

In most cases, you only need to configure your Web API routes in `Global.asax.cs`:

```csharp
using System.Web;
using System.Web.Http;

public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### Advanced (manual): add MessageHandler in Global.asax.cs

You can manually register the handler for edge cases (not recommended in normal usage):

```csharp
using System.Web.Http;
using Traceability.WebApi;

public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(config =>
        {
            // Add the CorrelationIdMessageHandler
            config.MessageHandlers.Add(new CorrelationIdMessageHandler());
            
            // Your other configurations...
            config.MapHttpAttributeRoutes();
        });
    }
}
```

### 2. Use CorrelationContext in Controllers

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

### 3. Use HttpClient with Correlation-id

For .NET Framework 4.8, you need to manage HttpClient manually. Use `CorrelationIdHandler` to automatically add the correlation-id:

```csharp
using System.Net.Http;
using Traceability.HttpClient;

// Configure HttpClient once (reuse to avoid socket exhaustion)
var handler = new CorrelationIdHandler
{
    InnerHandler = new HttpClientHandler()
};
var httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri("https://api.example.com/")
};

// Use the same HttpClient for multiple requests
var response = await httpClient.GetAsync("endpoint");
// The correlation-id is automatically added to the header
```

**Important**: Reuse the same `HttpClient` for multiple requests. Do not create a new `HttpClient` for each call to avoid socket exhaustion.

## Notes

- The CorrelationIdMessageHandler must be added first in the MessageHandlers chain
- The correlation-id is automatically propagated for HTTP calls made with TraceableHttpClientFactory
- For logging, use Serilog with CorrelationIdEnricher or Microsoft.Extensions.Logging with CorrelationIdScopeProvider
