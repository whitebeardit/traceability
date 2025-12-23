# Lesson 2: Quick Start

Let's start using Traceability! In this lesson, you'll see how to configure the package with minimal code.

## Installation

First, install the package via NuGet:

```bash
dotnet add package WhiteBeard.Traceability
```

Or via Package Manager Console:

```powershell
Install-Package WhiteBeard.Traceability
```

## Minimal Configuration (ASP.NET Core)

The simplest possible configuration - just one line of code!

**Program.cs:**
```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuration - everything is automatic!
builder.Services.AddTraceability();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Done!** Now Traceability is configured and working. Let's see how to use it.

## Using in a Controller

**ValuesController.cs:**
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
        // Correlation-id is automatically available
        var correlationId = CorrelationContext.Current;
        return Ok(new { CorrelationId = correlationId });
    }
}
```

## Testing

Make a request to the endpoint:

```bash
curl -X GET http://localhost:5000/api/values
```

**Expected response:**
```json
{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab"
}
```

And in the response header:
```
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## What Happened?

1. ✅ The middleware was automatically registered
2. ✅ A correlation-id was automatically generated (32-character GUID)
3. ✅ The correlation-id was added to the asynchronous context
4. ✅ The correlation-id was returned in the response header

## With Explicit Source (Optional)

If you want to define the service name explicitly:

```csharp
builder.Services.AddTraceability("MyService");
```

This adds the `Source` field to logs (we'll see more about this in Lesson 6).

## Next Steps

Now that you've seen the basics, let's learn more about `CorrelationContext` in [Lesson 3: Basic Usage](03-basic-usage.md).
