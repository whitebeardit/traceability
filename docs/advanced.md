# Advanced Topics

Advanced features and use cases for Traceability.

## Socket Exhaustion Prevention

Traceability was designed to prevent socket exhaustion from the start. All HttpClient creation methods use `IHttpClientFactory`, which manages the HTTP connection pool and reuses sockets.

### How It Works

`IHttpClientFactory` manages the lifecycle of `HttpClient` instances:
- Reuses HTTP connections when possible
- Automatically manages the socket pool
- Prevents socket exhaustion even under high load

### Correct Usage

```csharp
// Configure in Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use in service or controller
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task CallApiAsync()
    {
        // IHttpClientFactory reuses connections, preventing socket exhaustion
        var client = _httpClientFactory.CreateClient("ExternalApi");
        await client.GetAsync("endpoint");
    }
}
```

## HttpClient with Polly

Integrate resilience policies with Traceability:

```csharp
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
```

## Custom JSON Template

Configure the JSON format of logs:

```csharp
var options = new TraceabilityOptions
{
    Source = "UserService",
    LogOutputFormat = LogOutputFormat.JsonIndented,
    LogIncludeData = true,
    LogIncludeTimestamp = true
};

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson(options)
    .WriteTo.Console(new JsonFormatter(options, indent: true))
    .CreateLogger();
```

## Asynchronous Isolation

Understand how correlation-id is isolated in asynchronous contexts:

```csharp
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

## Propagation in Call Chains

The correlation-id is automatically propagated in chained HTTP calls:

**Scenario:** Service A → Service B → Service C

1. Service A receives request without header → generates `abc123`
2. Service A calls Service B with header `X-Correlation-Id: abc123`
3. Service B reads header and uses `abc123` (doesn't generate new one)
4. Service B calls Service C with same header
5. Process continues until the end of the chain

**Rule**: Never generate a new correlation-id if one already exists in the request header.

## Manual Use of CorrelationContext

For special cases, you can use `CorrelationContext` manually:

```csharp
using Traceability;

// Get current correlation-id (creates if it doesn't exist)
var correlationId = CorrelationContext.Current;

// Check if it exists
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Get or create explicitly
var id = CorrelationContext.GetOrCreate();

// Try to get without creating (recommended to avoid unwanted creation)
if (CorrelationContext.TryGetValue(out var correlationId))
{
    // Use correlationId
}

// Clear context
CorrelationContext.Clear();
```

## Application Insights Integration

For Application Insights integration, you can use the correlation-id along with the .NET diagnostics system:

```csharp
// The correlation-id can be used as a custom property
var correlationId = CorrelationContext.Current;
telemetryClient.Context.Properties["CorrelationId"] = correlationId;
```

## Known Limitations

1. **.NET Framework 4.8**: 
   - ✅ **Zero-code**: `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod` - no manual configuration needed
   - ❌ Doesn't have native DI, so `TraceabilityOptions` must be configured via static `Configure()` methods (only if you need custom options)
   - ✅ For distributed tracing, configure OpenTelemetry externally (Traceability does not create spans)

2. **Format Validation**: Correlation-id format validation is optional and must be enabled via `TraceabilityOptions.ValidateCorrelationIdFormat`.

3. **IHttpClientFactory**: HttpClient creation methods require `IHttpClientFactory` (only available in .NET 8 for this package). For .NET Framework, use `CorrelationIdHandler` directly with your own HttpClient management.

4. **Messaging**: There is no support for correlation-id in messaging (RabbitMQ, Kafka, etc.) - only HTTP currently.

## Best Practices

1. **Always use IHttpClientFactory**: Prevents socket exhaustion
2. **Define Source**: Facilitates traceability in distributed environments
3. **Use environment variables**: Reduces verbosity and facilitates configuration
4. **Keep logs in JSON**: Ensures uniformity across services
5. **Don't modify existing correlation-id**: Preserves traceability in the chain
