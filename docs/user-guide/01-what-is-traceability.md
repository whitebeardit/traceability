# Lesson 1: What is Traceability?

## What is Correlation-ID?

**Correlation-ID** (also known as correlation identifier or request ID) is a unique identifier used to track a request across multiple services in a distributed architecture.

### Practical Example

Imagine an order request that goes through three services:

```
Client → API Gateway → Order Service → Payment Service → Notification Service
```

Without correlation-id, you would have to search logs in each service separately. With **Traceability**, all logs will have the same correlation-id (`a1b2c3d4...`), allowing you to search for this ID across all services and see the complete request flow.

## When to Use Traceability?

Use **Traceability** when you need:

1. **Microservices Traceability**: Track a request across multiple services in a distributed architecture, allowing you to correlate logs from different services using the same correlation-id.

2. **Simplified Debugging**: Quickly identify all logs related to a specific request, even when it passes through multiple services, facilitating problem investigation.

3. **Performance Analysis**: Measure the total processing time of a request across multiple services, identifying bottlenecks in the call chain.

4. **Monitoring and Observability**: Correlate metrics, traces, and logs from different services using the same identifier, improving system visibility.

5. **Multi-Framework Support**: Portable core via .NET Standard 2.0 (works with .NET 6, 7, 8, and compatible frameworks), with full integrations for .NET 8.0 (ASP.NET Core) and .NET Framework 4.8+ (ASP.NET Web API and Traditional ASP.NET).

6. **Automatic Integration**: Have correlation-id automatically propagated in HTTP calls, added to logs (Serilog and Microsoft.Extensions.Logging), and managed without boilerplate code.

## Benefits

- ✅ **Zero Configuration**: Works out-of-the-box with minimal configuration
- ✅ **Thread-Safe and Async-Safe**: Uses `AsyncLocal` to ensure correct isolation in asynchronous contexts
- ✅ **Socket Exhaustion Prevention**: Native integration with `IHttpClientFactory` for efficient HTTP connection management
- ✅ **Logging Integration**: Automatic support for Serilog and Microsoft.Extensions.Logging
- ✅ **Automatic Propagation**: Correlation-id is automatically propagated in all chained HTTP calls

## How Does It Work?

1. **HTTP request arrives**: The middleware/handler reads the `X-Correlation-Id` header (if it exists) or generates a new GUID
2. **Correlation-id stored**: The ID is stored in the asynchronous context using `AsyncLocal`
3. **Automatic propagation**: All subsequent HTTP calls automatically include the correlation-id in the header
4. **Automatic logs**: All logs automatically include the correlation-id
5. **HTTP response**: The correlation-id is returned in the response header

## Next Steps

Now that you understand what Traceability is, let's start using it! Continue to [Lesson 2: Quick Start](02-quick-start.md).
