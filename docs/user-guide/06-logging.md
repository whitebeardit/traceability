# Lesson 6: Logging

In this lesson, you'll learn to integrate Traceability with logging systems.

## Serilog

### Basic Configuration

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

### Using in Logs

**Controller:**
```csharp
using Serilog;

public class ValuesController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id appears automatically in logs
        Log.Information("Processing request");
        return Ok();
    }
}
```

**Expected output:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processing request
```

**Note:** Both `CorrelationId` and `TraceId` appear in logs independently. Correlation-ID is managed by Traceability and is independent from OpenTelemetry's trace ID.

### JSON Template

For JSON output:

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

**Expected output (JSON):**
```json
{"Timestamp":"2024-01-15T14:23:45.123Z","Level":"Information","Source":"UserService","CorrelationId":"a1b2c3d4e5f6789012345678901234ab","TraceId":"xyz7890123456789012345678901234ab","Message":"Processing request"}
```

**Note:** Both `CorrelationId` and `TraceId` appear in logs independently. Correlation-ID is managed by Traceability and is independent from OpenTelemetry's trace ID.

## Microsoft.Extensions.Logging

### Configuration

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

### Using in Logs

**Controller:**
```csharp
public class ValuesController : ControllerBase
{
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Correlation-id appears automatically in logs
        _logger.LogInformation("Processing request");
        return Ok();
    }
}
```

**Expected output:**
```
info: MyApp.ValuesController[0]
      => CorrelationId: a1b2c3d4e5f6789012345678901234ab
      => TraceId: xyz7890123456789012345678901234ab
      Processing request
```

**Note:** Both `CorrelationId` and `TraceId` appear in logs independently. Correlation-ID is managed by Traceability and is independent from OpenTelemetry's trace ID.

## Source Field

The `Source` field identifies the origin/service that is generating the logs. It's essential for unifying logs in distributed environments.

**Configuration:**
```csharp
builder.Services.AddTraceability("UserService");
```

Or via environment variable:
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Output with Source:**
```
[14:23:45 INF] UserService a1b2c3d4e5f6789012345678901234ab Processing request
```

## Logs in Call Chains

When you have a call chain (Service A → Service B → Service C), all logs will have the same correlation-id:

**Service A (Logs):**
```
[14:23:45 INF] ServiceA a1b2c3d4e5f6789012345678901234ab Receiving request
[14:23:45 INF] ServiceA a1b2c3d4e5f6789012345678901234ab Calling Service B
```

**Service B (Logs):**
```
[14:23:45 INF] ServiceB a1b2c3d4e5f6789012345678901234ab Receiving request from Service A
[14:23:45 INF] ServiceB a1b2c3d4e5f6789012345678901234ab Calling Service C
```

**Service C (Logs):**
```
[14:23:45 INF] ServiceC a1b2c3d4e5f6789012345678901234ab Receiving request from Service B
[14:23:46 INF] ServiceC a1b2c3d4e5f6789012345678901234ab Processing completed
```

**Benefit:** You can search for `a1b2c3d4e5f6789012345678901234ab` across all logs and track the entire execution chain!

**Note:** All logs also include `TraceId` (OpenTelemetry's trace ID) independently. Both IDs appear in logs, enabling dual tracking for business (CorrelationId) and technical (TraceId) purposes.

## Next Steps

Now that you know how to use logging, let's see how to use with HttpClient in [Lesson 7: HttpClient](07-httpclient.md).
