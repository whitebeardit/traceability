# Troubleshooting

Common problem solutions when using Traceability.

## Correlation-id is not being propagated

### Possible Causes and Solutions

1. **Middleware/Handler is not configured**
   - **For ASP.NET Core (.NET 8)**: Make sure `AddTraceability()` was called in `Program.cs`
   - **For .NET Framework 4.8**: The `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod` - no manual configuration needed
   - If you disabled automatic registration, check if `app.UseCorrelationId()` is in the pipeline (.NET 8) or if module is registered in `web.config` (.NET Framework)

2. **HttpClient is not using CorrelationIdHandler**
   - **For ASP.NET Core (.NET 8)**: HttpClient is automatically configured when using `IHttpClientFactory` - no manual setup needed
   - **For .NET Framework 4.8**: Manually create HttpClient with `CorrelationIdHandler`:
     ```csharp
     var handler = new CorrelationIdHandler { InnerHandler = new HttpClientHandler() };
     var client = new HttpClient(handler);
     ```
   - Check if `AutoConfigureHttpClient` is not disabled in options (.NET 8)

3. **Asynchronous context is not being preserved**
   - Make sure you're not using `Task.Run()` without preserving the context
   - Use `await` correctly to preserve the asynchronous context

## Correlation-id does not appear in logs

### For Serilog

1. Use `WithTraceability("YourOrigin")` or configure `SourceEnricher` + `CorrelationIdEnricher` manually
2. Check the logger output template to include `{CorrelationId}`

**Example:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();
```

### For Microsoft.Extensions.Logging (.NET 8)

1. Call `AddTraceability("YourOrigin")` and enable scopes in Console (`IncludeScopes = true`)
2. Check if the logger is configured correctly

**Example:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

## Problems with .NET Framework 4.8

### Zero-Code Setup (Default)

**The library works automatically!** Just install the package - no configuration needed.

- ✅ `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod`
- ✅ `ActivityListener` is automatically initialized
- ✅ OpenTelemetry Activities (spans) are automatically created
- ✅ Spans are automatically named using route templates

### If Zero-Code Doesn't Work

1. **Check if package is installed correctly**
   ```bash
   Install-Package WhiteBeard.Traceability
   ```

2. **Check if spans are disabled**
   - Verify `appSettings['Traceability:SpansEnabled']` is not set to `false` in `Web.config`
   - Verify `TRACEABILITY_SPANS_ENABLED` environment variable is not set to `false`

3. **Check if PreApplicationStartMethod is working**
   - The library uses `PreApplicationStartMethod` to auto-register
   - This should work automatically - no manual setup needed
   - If it doesn't work, check application startup logs for errors

### Manual Configuration (Advanced - Not Recommended)

If you need manual control, you can configure manually:

**For Web API:**
```csharp
GlobalConfiguration.Configure(config =>
{
    // Manual registration (not needed - automatic via PreApplicationStartMethod)
    config.MessageHandlers.Add(new CorrelationIdMessageHandler());
    config.MapHttpAttributeRoutes();
});
```

**For Traditional ASP.NET:**
```xml
<!-- web.config -->
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

**Options Configuration (Optional):**
```csharp
CorrelationIdHttpModule.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

**Note**: Manual configuration is only needed for edge cases. The default zero-code approach works for 99% of scenarios.

## Source is not being defined

### Error: InvalidOperationException

If you receive an exception stating that Source must be provided:

1. **Provide Source explicitly:**
   ```csharp
   builder.Services.AddTraceability("UserService");
   ```

2. **Or set environment variable:**
   ```bash
   export TRACEABILITY_SERVICENAME="UserService"
   ```

3. **Or configure in options:**
   ```csharp
   builder.Services.AddTraceability(options =>
   {
       options.Source = "UserService";
   });
   ```

## HttpClient causing socket exhaustion

### Solution

Always use `IHttpClientFactory` instead of creating direct instances of `HttpClient`.

**❌ Incorrect:**
```csharp
var client = new HttpClient(); // Causes socket exhaustion
```

**✅ Correct:**
```csharp
// Configure in Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use in service
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## FAQ

### Q: Can I use correlation-id in console applications?

A: Yes! Use `CorrelationContext.GetOrCreate()` to manually generate a correlation-id.

### Q: Is correlation-id preserved in Task.Run()?

A: No. `Task.Run()` creates a new isolated asynchronous context. Use `await` to preserve the context.

### Q: Can I customize the header name?

A: Yes, use `TraceabilityOptions.HeaderName` to define a custom name.

### Q: How to disable auto-registration of middleware?

A: **For .NET 8**: Set `AutoRegisterMiddleware = false` in options and register manually with `app.UseCorrelationId()`.

**For .NET Framework 4.8**: The `CorrelationIdHttpModule` is automatically registered via `PreApplicationStartMethod`. To disable spans, set `Traceability:SpansEnabled=false` in `appSettings` or `TRACEABILITY_SPANS_ENABLED=false` environment variable.

### Q: How to disable automatic spans in .NET Framework 4.8?

A: Set `Traceability:SpansEnabled=false` in `Web.config`:
```xml
<appSettings>
  <add key="Traceability:SpansEnabled" value="false" />
</appSettings>
```

Or set environment variable:
```powershell
$env:TRACEABILITY_SPANS_ENABLED="false"
```

### Q: Are logs always in JSON?

A: No, logs format depends on your logger configuration. Traceability enriches logs with correlation-id, but the format is controlled by your logger (Serilog, Microsoft.Extensions.Logging, etc.).

## Still having problems?

If you're still experiencing issues:

1. Check the [Technical Documentation](../AGENTS.md) to understand the internal architecture
2. See the [Examples](examples/aspnet-core.md) for working implementations
3. Open an issue in the project repository
