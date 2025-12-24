# Reference Code Examples

## Example 1: Basic CorrelationContext Usage

```csharp
using Traceability;

// Get or create correlation-id
var correlationId = CorrelationContext.Current;

// Check if exists
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Get explicitly or create
var id = CorrelationContext.GetOrCreate();

// Clear context
CorrelationContext.Clear();
```

## Example 2: ASP.NET Core - Zero Configuration

```csharp
// Program.cs
using Traceability.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Traceability (recommended)
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

builder.Host.UseSerilog();

// Zero configuration - everything is automatic!
// - Middleware is automatically registered via IStartupFilter
// - HttpClient is automatically configured with CorrelationIdHandler
// - Source comes from TRACEABILITY_SERVICENAME or assembly name
builder.Services.AddTraceability("UserService");

// HttpClient is already automatically configured!
// No need for .AddHttpMessageHandler<CorrelationIdHandler>()
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

builder.Services.AddControllers();

var app = builder.Build();

// Middleware is already automatically registered!
// No need for app.UseCorrelationId()

app.MapControllers();
app.Run();
```

## Example 3: Usage in Controller

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
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        
        _logger.LogInformation("Processing request. CorrelationId: {CorrelationId}", correlationId);

        // HttpClient automatically adds correlation-id
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");

        return Ok(new { CorrelationId = correlationId });
    }
}
```

## Example 4: HttpClient with IHttpClientFactory

```csharp
// Program.cs - Configure HttpClient
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

// In service or controller
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## Example 5: Unit Test

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

## Example 6: Asynchronous Isolation

```csharp
using Traceability;

// Main context
var mainId = CorrelationContext.GetOrCreate();

// Isolated task will have its own context
var task = Task.Run(async () =>
{
    // This context is isolated
    var taskId = CorrelationContext.GetOrCreate();
    await Task.Delay(100);
    return taskId;
});

var taskId = await task;

// mainId and taskId are different
Console.WriteLine($"Main: {mainId}, Task: {taskId}");
```
