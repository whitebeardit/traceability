# Exemplos - ASP.NET Core (.NET 8)

Exemplos práticos de uso do Traceability em aplicações ASP.NET Core.

## Exemplo Básico

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Controller:**
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
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

## Exemplo com Serilog

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

**Output nos Logs:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processando requisição
```

## Exemplo com HttpClient

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

**Controller:**
```csharp
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

## Exemplo Completo

Veja o exemplo completo em `samples/Sample.WebApi.Net8/`.

