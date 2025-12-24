# Lesson 11: Centralized Logging

In this lesson, you'll learn how to configure centralized logging with the most common log aggregators in the market, and understand why centralized logging is essential in distributed architectures.

## Why Centralize Logs?

In distributed architectures with multiple services, logs are scattered across different servers, containers, or services. Centralized logging provides:

1. **Unified View**: All logs from all services in one place
2. **Easy Search**: Find logs across services using CorrelationId
3. **Better Debugging**: Track a request through the entire system
4. **Performance Analysis**: Analyze request flow and identify bottlenecks
5. **Alerting**: Set up alerts based on log patterns across services

### How CorrelationId Helps

With CorrelationId, you can:
- **Search once, find everywhere**: Search for a single CorrelationId to see all logs related to a request
- **Track request flow**: See how a request flows through Service A → Service B → Service C
- **Debug faster**: Quickly identify all logs from a failed request across all services

**Example Scenario:**
```
Request with CorrelationId: abc123def456...

Service A logs: "Processing user request" (CorrelationId: abc123...)
Service B logs: "Fetching user data" (CorrelationId: abc123...)
Service C logs: "Saving order" (CorrelationId: abc123...)
Service B logs: "Returning response" (CorrelationId: abc123...)
Service A logs: "Request completed" (CorrelationId: abc123...)
```

By searching for `abc123...` in your centralizer, you see the complete request journey!

## Serilog - Common Sinks

Traceability automatically adds `Source` and `CorrelationId` to your logs. You just need to configure where to send them. Here are the most common centralized logging solutions:

### Seq

Seq is a popular log aggregator with a beautiful web interface and powerful query capabilities.

#### Installation

```bash
dotnet add package Serilog.Sinks.Seq
```

#### Basic Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter()) // Keep local console
    .WriteTo.Seq("http://localhost:5341") // Centralized
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Configuration with API Key

For production environments with authentication:

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq(
        serverUrl: "https://seq.yourcompany.com",
        apiKey: "your-api-key-here")
    .CreateLogger();
```

#### Configuration via Environment Variables

```csharp
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
var seqApiKey = Environment.GetEnvironmentVariable("SEQ_API_KEY");

var config = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter());

if (!string.IsNullOrEmpty(seqApiKey))
{
    config.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
}
else
{
    config.WriteTo.Seq(seqUrl);
}

Log.Logger = config.CreateLogger();
```

#### Searching in Seq

Once logs are in Seq, you can search by CorrelationId:

```
CorrelationId = "abc123def456789012345678901234ab"
```

Or use partial matching:
```
CorrelationId like "abc123"
```

**Benefits:**
- Beautiful web UI
- Real-time log streaming
- Powerful query language
- Free for single-user development

### Elasticsearch (ELK Stack)

Elasticsearch is part of the ELK (Elasticsearch, Logstash, Kibana) stack, widely used in enterprise environments.

#### Installation

```bash
dotnet add package Serilog.Sinks.Elasticsearch
```

#### Basic Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Sinks.Elasticsearch;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        IndexFormat = "logs-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
    })
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Advanced Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("https://elasticsearch.yourcompany.com:9200"))
    {
        IndexFormat = "logs-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        NumberOfShards = 1,
        NumberOfReplicas = 1,
        BufferBaseFilename = "./logs/elastic-buffer",
        BufferFileSizeLimitBytes = 5242880, // 5MB
        BufferLogShippingInterval = TimeSpan.FromSeconds(5),
        ConnectionTimeout = TimeSpan.FromSeconds(5)
    })
    .CreateLogger();
```

#### Searching in Elasticsearch/Kibana

In Kibana, you can search using:

```
CorrelationId:abc123*
```

Or use the full CorrelationId:
```
CorrelationId:"abc123def456789012345678901234ab"
```

**Benefits:**
- Highly scalable
- Powerful search capabilities
- Rich visualization in Kibana
- Enterprise-grade solution

### Application Insights (Azure)

Application Insights is Microsoft's application performance monitoring solution, perfect for Azure-hosted applications.

#### Installation

For Serilog:
```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

For Microsoft.Extensions.Logging:
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

#### Serilog Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Sinks.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

var telemetryConfiguration = TelemetryConfiguration.Active;
telemetryConfiguration.InstrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Microsoft.Extensions.Logging Configuration

**Program.cs:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddTraceability("UserService");
builder.Logging.AddApplicationInsights();
```

#### Searching in Application Insights

Use KQL (Kusto Query Language) to search:

```kql
traces
| where customDimensions.CorrelationId == "abc123def456789012345678901234ab"
| order by timestamp asc
```

Or search across all telemetry types:
```kql
union traces, exceptions, requests
| where customDimensions.CorrelationId == "abc123def456789012345678901234ab"
| order by timestamp asc
```

**Benefits:**
- Native Azure integration
- Automatic correlation with metrics and traces
- Powerful analytics
- Built-in alerting

### Datadog

Datadog is a cloud monitoring and logging platform with excellent .NET support.

#### Installation

```bash
dotnet add package Serilog.Sinks.Datadog.Logs
```

#### Basic Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Sinks.Datadog.Logs;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.DatadogLogs(
        apiKey: builder.Configuration["Datadog:ApiKey"],
        source: "csharp",
        host: Environment.MachineName,
        tags: new[] { "env:production", "service:UserService" })
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Advanced Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.DatadogLogs(
        apiKey: builder.Configuration["Datadog:ApiKey"],
        source: "csharp",
        host: Environment.MachineName,
        tags: new[] { 
            $"env:{builder.Environment.EnvironmentName}",
            $"service:UserService",
            $"version:{Assembly.GetExecutingAssembly().GetName().Version}"
        },
        service: "UserService",
        configuration: new DatadogConfiguration())
    .CreateLogger();
```

#### Searching in Datadog

In Datadog Logs, search using:

```
CorrelationId:abc123def456789012345678901234ab
```

Or use partial matching:
```
CorrelationId:abc123*
```

**Benefits:**
- Cloud-native solution
- Excellent .NET integration
- Unified logs, metrics, and traces
- Powerful dashboards

### Splunk

Splunk is an enterprise log management and analysis platform.

#### Installation

```bash
dotnet add package Serilog.Sinks.Splunk
```

#### Basic Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Sinks.Splunk;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.EventCollector(
        splunkHost: "https://splunk.yourcompany.com:8088",
        eventCollectorToken: builder.Configuration["Splunk:Token"],
        host: Environment.MachineName)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Advanced Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.EventCollector(
        splunkHost: "https://splunk.yourcompany.com:8088",
        eventCollectorToken: builder.Configuration["Splunk:Token"],
        host: Environment.MachineName,
        source: "UserService",
        sourceType: "_json",
        batchInterval: TimeSpan.FromSeconds(5),
        batchSizeLimit: 100)
    .CreateLogger();
```

#### Searching in Splunk

Use SPL (Splunk Processing Language):

```
index=main CorrelationId="abc123def456789012345678901234ab"
```

Or with wildcards:
```
index=main CorrelationId="abc123*"
```

**Benefits:**
- Enterprise-grade solution
- Powerful SPL query language
- Excellent for compliance and security
- Highly scalable

### Grafana Loki

Loki is a horizontally-scalable log aggregation system inspired by Prometheus, designed to work with Grafana.

#### Installation

```bash
dotnet add package Serilog.Sinks.Grafana.Loki
```

#### Basic Configuration

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.GrafanaLoki("http://localhost:3100")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

#### Configuration with Labels

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.GrafanaLoki(
        uri: "http://loki.yourcompany.com:3100",
        labels: new[]
        {
            new LokiLabel { Key = "job", Value = "UserService" },
            new LokiLabel { Key = "environment", Value = builder.Environment.EnvironmentName }
        })
    .CreateLogger();
```

#### Searching in Grafana Loki

Use LogQL (Loki Query Language):

```
{job="UserService"} |= "abc123def456789012345678901234ab"
```

Or search by CorrelationId field:
```
{job="UserService"} | json | CorrelationId="abc123def456789012345678901234ab"
```

**Benefits:**
- Lightweight and cost-effective
- Integrates seamlessly with Grafana
- Prometheus-inspired design
- Great for Kubernetes environments

## Microsoft.Extensions.Logging

For applications using `Microsoft.Extensions.Logging` instead of Serilog, you can still centralize logs:

### Application Insights

**Program.cs:**
```csharp
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddTraceability("UserService");
builder.Logging.AddApplicationInsights();

// CorrelationId will appear in Application Insights automatically
```

The CorrelationId will be available in Application Insights as a custom property in the scope.

### Custom Provider

You can create a custom logging provider that sends logs to your centralizer:

```csharp
public class CustomLoggingProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CustomLogger(categoryName);
    }

    public void Dispose() { }
}

public class CustomLogger : ILogger
{
    private readonly string _categoryName;

    public CustomLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Extract CorrelationId from scope
        var correlationId = CorrelationContext.TryGetValue(out var id) ? id : null;
        
        // Send to your centralizer
        SendToCentralizer(new
        {
            Level = logLevel.ToString(),
            Category = _categoryName,
            CorrelationId = correlationId,
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }

    private void SendToCentralizer(object logEntry)
    {
        // Implement your centralizer API call here
        // Example: HTTP POST to your log aggregator
    }
}
```

## Configuration via appsettings.json

You can configure Serilog sinks via `appsettings.json` for easier environment-specific configuration:

**appsettings.json:**
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WithTraceabilityJson("UserService") // Adds Source and CorrelationId
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddTraceability("UserService");
```

**Note:** `WithTraceabilityJson()` must be called after `ReadFrom.Configuration()` to ensure Traceability enrichers are applied.

## Best Practices

### 1. Keep Local Console for Development

Always keep console output for local development:

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter()) // Always keep this
    .WriteTo.Seq("http://localhost:5341") // Add centralizer
    .CreateLogger();
```

### 2. Configure Retry and Buffer

For production, configure retry and buffering to handle network issues:

```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq("http://seq-server:5341", 
        bufferBaseFilename: "./logs/seq-buffer",
        batchPostingLimit: 50,
        period: TimeSpan.FromSeconds(2))
    .CreateLogger();
```

### 3. Use Environment Variables

Store sensitive configuration in environment variables:

```csharp
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") 
    ?? "http://localhost:5341";
var seqApiKey = Environment.GetEnvironmentVariable("SEQ_API_KEY");

// Use in configuration
```

### 4. Use JSON Format

Always use JSON format for centralized logs to ensure uniformity:

```csharp
.WithTraceabilityJson("UserService") // Ensures JSON output
.WriteTo.Seq("http://seq-server:5341") // JSON is sent automatically
```

### 5. Configure Log Levels per Environment

Use different log levels for different environments:

```csharp
var minimumLevel = builder.Environment.IsDevelopment()
    ? LogEventLevel.Debug
    : LogEventLevel.Information;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(minimumLevel)
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq("http://seq-server:5341")
    .CreateLogger();
```

## Complete Example

Here's a complete example using Seq with environment-based configuration:

**Program.cs:**
```csharp
using Traceability.Extensions;
using Traceability.Logging;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with Traceability and Seq
var seqUrl = builder.Configuration["Seq:ServerUrl"] 
    ?? Environment.GetEnvironmentVariable("SEQ_URL") 
    ?? "http://localhost:5341";
var seqApiKey = builder.Configuration["Seq:ApiKey"] 
    ?? Environment.GetEnvironmentVariable("SEQ_API_KEY");

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WithTraceabilityJson("UserService")
    .WriteTo.Console(new JsonFormatter());

// Add Seq sink
if (!string.IsNullOrEmpty(seqApiKey))
{
    loggerConfig.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
}
else
{
    loggerConfig.WriteTo.Seq(seqUrl);
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add Traceability
builder.Services.AddTraceability("UserService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**appsettings.json:**
```json
{
  "Seq": {
    "ServerUrl": "http://localhost:5341"
  }
}
```

**Expected Behavior:**
1. Logs are written to console (for local viewing)
2. Logs are sent to Seq (for centralized viewing)
3. All logs include `Source` and `CorrelationId` fields
4. You can search in Seq using: `CorrelationId = "abc123..."`

**Searching in Seq:**
When a request flows through multiple services, you can search for the CorrelationId in Seq and see all related logs:

```
CorrelationId = "abc123def456789012345678901234ab"
```

Results will show logs from all services that processed this request, making it easy to debug distributed systems!

## Next Steps

Now that you know how to centralize logs, you have a complete traceability solution:
- CorrelationId automatically propagated across services
- Logs automatically enriched with CorrelationId
- Centralized logging for easy search and debugging

For more advanced topics, see [Advanced Topics](../advanced.md) or return to the [User Manual Index](index.md).

