# Modification Guide for LLMs

## How to Add New Component

1. **Identify location**
   - Check directory structure
   - Choose appropriate directory or create new one

2. **Define single responsibility**
   - Component should have a clear responsibility
   - Follow existing patterns

3. **Implement with conditional compilation**
   ```csharp
   #if NET8_0
   // .NET 8 code
   #endif
   
   #if NET48
   // .NET Framework code
   #endif
   ```

4. **Add dependencies**
   - Use `CorrelationContext` if you need correlation-id
   - Follow dependency injection pattern if applicable
   - Use existing interfaces from `Traceability.Core.Interfaces` when possible:
     - `ICorrelationIdValidator` for validation
     - `ICorrelationIdExtractor` for extraction
     - `IActivityFactory` for activity creation
     - `IActivityTagProvider` for adding tags
   - Use constants from `Traceability.Core.Constants` instead of magic strings
   - Use `ITraceabilityOptionsProvider` for accessing options (NET48: `StaticTraceabilityOptionsProvider`, NET8: `DITraceabilityOptionsProvider`)
   - Use existing interfaces from `Traceability.Core.Interfaces` when possible:
     - `ICorrelationIdValidator` for validation
     - `ICorrelationIdExtractor` for extraction
     - `IActivityFactory` for activity creation
     - `IActivityTagProvider` for adding tags
   - Use constants from `Traceability.Core.Constants` instead of magic strings
   - Use `ITraceabilityOptionsProvider` for accessing options (NET48: `StaticTraceabilityOptionsProvider`, NET8: `DITraceabilityOptionsProvider`)

5. **Add tests**
   - Create file in `tests/Traceability.Tests/`
   - Test both frameworks if applicable

6. **Update documentation**
   - Add XML comments
   - Update README.md if necessary
   - Update documentation in `docs/agents/`

## How to Maintain Multi-Framework Compatibility

1. **Identify framework-specific code**
   - Different APIs between .NET 8 and .NET Framework
   - Different namespaces

2. **Use conditional compilation**
   ```csharp
   #if NET8_0
   using Microsoft.AspNetCore.Http;
   #endif
   
   #if NET48
   using System.Web;
   #endif
   ```

3. **Test in both frameworks**
   - Run tests for .NET 8
   - Run tests for .NET Framework 4.8

4. **Maintain consistent public API**
   - Same public interface in both frameworks
   - Identical behavior

## Where to Add Tests

**Test Structure**:
```
tests/Traceability.Tests/
├── CorrelationContextTests.cs
├── CorrelationIdHandlerTests.cs
├── CorrelationIdMiddlewareTests.cs
├── LoggingTests.cs
└── TraceableHttpClientFactoryTests.cs
```

**Test Pattern**:
- Use xUnit
- Use FluentAssertions for assertions
- Use Moq for mocks when necessary
- Name tests: `MethodName_Scenario_ExpectedBehavior`

**Example**:
```csharp
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
```

## How to Update Documentation

1. **XML Comments**
   - Add `<summary>` for public classes
   - Add `<param>` for parameters
   - Add `<returns>` for returns
   - Add `<example>` when useful

2. **README.md**
   - Add usage examples
   - Update relevant sections
   - Maintain consistency with code

3. **Documentation in `docs/agents/`**
   - Add new component in `components.md`
   - Update diagrams in `architecture.md` if necessary
   - Add usage examples in `code-examples.md`

## Quick References

### Main Files
- Core: `src/Traceability/CorrelationContext.cs`
- Constants: `src/Traceability/Core/Constants.cs`
- Core Interfaces: `src/Traceability/Core/Interfaces/`
- Core Services: `src/Traceability/Core/Services/`
- Middleware (.NET 8): `src/Traceability/Middleware/CorrelationIdMiddleware.cs`
- HttpModule (.NET Framework): `src/Traceability/Middleware/CorrelationIdHttpModule.cs`
- MessageHandler: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`
- HttpClient Handler: `src/Traceability/HttpClient/CorrelationIdHandler.cs`
- Factory: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`
- Route Template Helper: `src/Traceability/WebApi/RouteTemplateHelper.cs`
- Route Template Resolver: `src/Traceability/WebApi/IRouteTemplateResolver.cs`
- Utilities: `src/Traceability/Utilities/TraceabilityUtilities.cs`
- Serilog CorrelationId: `src/Traceability/Logging/CorrelationIdEnricher.cs`
- Serilog Source: `src/Traceability/Logging/SourceEnricher.cs`
- Serilog Data: `src/Traceability/Logging/DataEnricher.cs`
- Serilog JSON Formatter: `src/Traceability/Logging/JsonFormatter.cs`
- MEL CorrelationId: `src/Traceability/Logging/CorrelationIdScopeProvider.cs`
- MEL Source: `src/Traceability/Logging/SourceScopeProvider.cs`
- Configuration: `src/Traceability/Configuration/TraceabilityOptions.cs`
- Options Provider: `src/Traceability/Configuration/ITraceabilityOptionsProvider.cs`

### Examples
- ASP.NET Core: `samples/Sample.WebApi.Net8/`
- Console: `samples/Sample.Console.Net8/`
- .NET Framework: `samples/Sample.WebApi.NetFramework/`

### Tests
- All tests: `tests/Traceability.Tests/`
