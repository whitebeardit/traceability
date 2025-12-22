# Exemplos de Código de Referência

## Exemplo 1: Uso Básico do CorrelationContext

```csharp
using Traceability;

// Obter ou criar correlation-id
var correlationId = CorrelationContext.Current;

// Verificar se existe
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Obter explicitamente ou criar
var id = CorrelationContext.GetOrCreate();

// Limpar contexto
CorrelationContext.Clear();
```

## Exemplo 2: ASP.NET Core - Zero Configuração

```csharp
// Program.cs
using Traceability.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog com Traceability (recomendado)
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

builder.Host.UseSerilog();

// Zero configuração - tudo é automático!
// - Middleware é registrado automaticamente via IStartupFilter
// - HttpClient é configurado automaticamente com CorrelationIdHandler
// - Source vem de TRACEABILITY_SERVICENAME ou assembly name
builder.Services.AddTraceability("UserService");

// HttpClient já está configurado automaticamente!
// Não precisa de .AddHttpMessageHandler<CorrelationIdHandler>()
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

builder.Services.AddControllers();

var app = builder.Build();

// Middleware já está registrado automaticamente!
// Não precisa de app.UseCorrelationId()

app.MapControllers();
app.Run();
```

## Exemplo 3: Uso em Controller

```csharp
using Traceability;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyController> _logger;

    public MyController(IHttpClientFactory httpClientFactory, ILogger<MyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Correlation-id está automaticamente disponível
        var correlationId = CorrelationContext.Current;
        
        _logger.LogInformation("Processando requisição. CorrelationId: {CorrelationId}", correlationId);

        // HttpClient automaticamente adiciona correlation-id
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");

        return Ok(new { CorrelationId = correlationId });
    }
}
```

## Exemplo 4: HttpClient com IHttpClientFactory

```csharp
// Program.cs - Configure o HttpClient
using Traceability.Extensions;
using Traceability.HttpClient;
using Polly;
using Polly.Extensions.Http;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddPolicyHandler(retryPolicy);

// No serviço ou controller
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## Exemplo 5: Teste Unitário

```csharp
using FluentAssertions;
using Traceability;
using Xunit;

public class CorrelationContextTests
{
    [Fact]
    public void Current_WhenNoValue_ShouldGenerateNew()
    {
        // Arrange
        CorrelationContext.Clear();

        // Act
        var correlationId = CorrelationContext.Current;

        // Assert
        correlationId.Should().NotBeNullOrEmpty();
        correlationId.Length.Should().Be(32);
    }
}
```

## Exemplo 6: Isolamento Assíncrono

```csharp
using Traceability;

// Contexto principal
var mainId = CorrelationContext.GetOrCreate();

// Task isolada terá seu próprio contexto
var task = Task.Run(async () =>
{
    // Este contexto é isolado
    var taskId = CorrelationContext.GetOrCreate();
    await Task.Delay(100);
    return taskId;
});

var taskId = await task;

// mainId e taskId são diferentes
Console.WriteLine($"Main: {mainId}, Task: {taskId}");
```

