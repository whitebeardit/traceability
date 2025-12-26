# Configuration

Traceability offers several configuration options to customize the package behavior.

## TraceabilityOptions

The `TraceabilityOptions` class allows you to configure all package options:

```csharp
public class TraceabilityOptions
{
    public string HeaderName { get; set; } = "X-Correlation-Id";
    public bool AlwaysGenerateNew { get; set; } = false;
    public bool ValidateCorrelationIdFormat { get; set; } = false;
    public string? Source { get; set; }
    public LogOutputFormat LogOutputFormat { get; set; } = LogOutputFormat.JsonCompact;
    public bool LogIncludeTimestamp { get; set; } = true;
    public bool LogIncludeLevel { get; set; } = true;
    public bool LogIncludeSource { get; set; } = true;
    public bool LogIncludeCorrelationId { get; set; } = true;
    public bool LogIncludeMessage { get; set; } = true;
    public bool LogIncludeData { get; set; } = true;
    public bool LogIncludeException { get; set; } = true;
    public LogEventLevel? MinimumLogLevel { get; set; }
    public bool AutoRegisterMiddleware { get; set; } = true;
    public bool AutoConfigureHttpClient { get; set; } = true;
    public bool UseAssemblyNameAsFallback { get; set; } = true;
}
```

## Configuration Options

### HeaderName
HTTP header name for correlation-id (default: "X-Correlation-Id")

```csharp
builder.Services.AddTraceability(options =>
{
    options.HeaderName = "X-Request-Id";
});
```

### AlwaysGenerateNew
If true, generates a new correlation-id even if one already exists in the context (default: false)

```csharp
builder.Services.AddTraceability(options =>
{
    options.AlwaysGenerateNew = true;
});
```

### ValidateCorrelationIdFormat
If true, validates the format of the correlation-id received in the header (default: false)

```csharp
builder.Services.AddTraceability(options =>
{
    options.ValidateCorrelationIdFormat = true;
});
```

### Source
Name of the origin/service that is generating the logs (optional, but recommended)

```csharp
builder.Services.AddTraceability(options =>
{
    options.Source = "UserService";
});
```

### LogOutputFormat
Output format for logs (default: JsonCompact)

```csharp
builder.Services.AddTraceability(options =>
{
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

### Log Options

Control which fields are included in logs:

```csharp
builder.Services.AddTraceability(options =>
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

### MinimumLogLevel

Minimum log level to filter events (optional).

**Configuration Priority:**
1. `LOG_LEVEL` environment variable (highest priority)
2. `TraceabilityOptions.MinimumLogLevel` defined in options
3. Information (default)

```csharp
builder.Services.AddTraceability(options =>
{
    options.MinimumLogLevel = LogEventLevel.Debug;
});
```

**Note:** The `LOG_LEVEL` environment variable has the highest priority to facilitate changing the log level in production without needing to recompile or redeploy the application.

### UseAssemblyNameAsFallback

If false, disables using the assembly name as fallback for Source when no Source is provided (default: true).

```csharp
builder.Services.AddTraceability(options =>
{
    options.UseAssemblyNameAsFallback = false;
});
```

### Auto-Registration

Control automatic registration of middleware and HttpClient:

```csharp
builder.Services.AddTraceability(options =>
{
    options.AutoRegisterMiddleware = false;  // Disables auto-registration of middleware
    options.AutoConfigureHttpClient = false; // Disables auto-configuration of HttpClient
});
```

If disabled, you will need to register manually:

```csharp
var app = builder.Build();
app.UseCorrelationId(); // Manual middleware registration
```

## Environment Variables

### TRACEABILITY_SERVICENAME

Defines the name of the service/origin that is generating the logs.

**Configuration Priority:**
1. `source` parameter provided explicitly (highest priority)
2. `TraceabilityOptions.Source` defined in options
3. `TRACEABILITY_SERVICENAME` environment variable
4. Assembly name (if `UseAssemblyNameAsFallback = true`, default: true)
5. If none is available, an exception will be thrown

**Configuration:**

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
```

**Windows CMD:**
```cmd
set TRACEABILITY_SERVICENAME=UserService
```

### LOG_LEVEL

Defines the minimum log level (Verbose, Debug, Information, Warning, Error, Fatal).

**Configuration Priority:**
1. `LOG_LEVEL` environment variable (highest priority)
2. `TraceabilityOptions.MinimumLogLevel` defined in options
3. Information (default)

**Configuration:**

**Linux/Mac:**
```bash
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:LOG_LEVEL="Information"
```

### TRACEABILITY_SPANS_ENABLED (.NET Framework 4.8)

Controls whether OpenTelemetry Activities (spans) are automatically created in .NET Framework 4.8.

**Default:** `true` (spans are enabled by default)

**Configuration Priority:**
1. `Traceability:SpansEnabled` in `appSettings` (Web.config)
2. `TRACEABILITY_SPANS_ENABLED` environment variable
3. `true` (default - spans enabled)

**Disable spans via appSettings (Web.config):**
```xml
<configuration>
  <appSettings>
    <add key="Traceability:SpansEnabled" value="false" />
  </appSettings>
</configuration>
```

**Disable spans via environment variable:**

**Linux/Mac:**
```bash
export TRACEABILITY_SPANS_ENABLED="false"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SPANS_ENABLED="false"
```

**Windows CMD:**
```cmd
set TRACEABILITY_SPANS_ENABLED=false
```

**Note:** This option only applies to .NET Framework 4.8. In .NET 8.0, spans are controlled by OpenTelemetry SDK configuration.

## Configuration Examples

### Basic Configuration

```csharp
builder.Services.AddTraceability("UserService");
```

### Configuration with Options

```csharp
builder.Services.AddTraceability(options =>
{
    options.Source = "UserService";
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

### Configuration with Source and Options

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
});
```

### Disable Auto-Registration

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
    options.AutoConfigureHttpClient = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Manual registration
```

## JSON Output

All logs generated by Traceability are always in JSON format to ensure uniformity across different applications and services.

The default JSON format includes:
- `Timestamp`: Log date and time
- `Level`: Log level (Information, Warning, Error, etc.)
- `Source`: Service name
- `CorrelationId`: Correlation ID (when available)
- `Message`: Log message
- `Data`: Serialized objects (when present)
- `Exception`: Exception information (when present)
