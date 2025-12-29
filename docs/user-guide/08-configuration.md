# Lesson 8: Configuration

In this lesson, you'll learn to configure advanced Traceability options.

## Basic Options

### HeaderName

Customize the HTTP header name:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
});
```

### ValidateCorrelationIdFormat

Enable correlation-id format validation:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.ValidateCorrelationIdFormat = true;
});
```

### AlwaysGenerateNew

Generate a new correlation-id even if one already exists:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AlwaysGenerateNew = true;
});
```

## Log Options

### LogOutputFormat

Choose the log output format:

**Note:** This option currently exists in the API surface, but it is **not wired** into the built-in formatters/sinks. To control output format, configure your logger (for example, for Serilog use `Traceability.Logging.JsonFormatter` with `indent: true` to get indented JSON).

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

Available options:
- `JsonCompact` (default) - JSON in one line
- `JsonIndented` - Indented JSON
- `Text` - Text format

### Log Fields

Control which fields are included in logs:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.LogIncludeTimestamp = true;
    options.LogIncludeLevel = true;
    options.LogIncludeSource = true;
    options.LogIncludeCorrelationId = true;
    options.LogIncludeMessage = true;
    options.LogIncludeData = true;
    options.LogIncludeException = true;
});
```

## Auto-Registration

### Disable Middleware Auto-Registration

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Manual registration
app.MapControllers();
app.Run();
```

### Disable HttpClient Auto-Configuration

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoConfigureHttpClient = false;
});

// Configure HttpClient manually
builder.Services.AddHttpClient("ExternalApi")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

## Environment Variables

### TRACEABILITY_SERVICENAME

Set the service name via environment variable:

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
```

With the variable set, you can use:

```csharp
// Source comes automatically from TRACEABILITY_SERVICENAME
builder.Services.AddTraceability();
```

### LOG_LEVEL

Set the minimum log level:

**Linux/Mac:**
```bash
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:LOG_LEVEL="Information"
```

## Configuration Priority

For Source (ServiceName):

1. `source` parameter provided explicitly (highest priority)
2. `TraceabilityOptions.Source` defined in options
3. `TRACEABILITY_SERVICENAME` environment variable
4. Assembly name (if `UseAssemblyNameAsFallback = true`, default: true)
5. If none is available, an exception will be thrown

## Complete Example

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
    options.LogIncludeData = true;
    options.AutoRegisterMiddleware = false;
});
```

## Next Steps

Now that you know how to configure options, let's see complete practical examples in [Lesson 9: Practical Examples](09-examples.md).
