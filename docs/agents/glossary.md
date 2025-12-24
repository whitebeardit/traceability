# Glossary of Terms

- **Correlation-ID**: Unique identifier used to track a request across multiple services
- **AsyncLocal**: .NET class that stores values specific to an asynchronous context
- **DelegatingHandler**: Base class for HTTP handlers that can be chained
- **Enricher**: Serilog component that adds properties to log events
- **ScopeProvider**: Microsoft.Extensions.Logging component that manages logging scopes
- **Conditional Compilation**: Conditional compilation using `#if` directives to include code based on conditions
- **Middleware**: Component in HTTP pipeline that processes requests and responses
- **MessageHandler**: Handler in ASP.NET Web API pipeline
- **HttpModule**: Module in traditional ASP.NET pipeline
- **Source**: Field that identifies the origin/service that is generating the logs
- **Traceability**: Ability to track a request across multiple services using correlation-id
- **Socket Exhaustion**: Problem that occurs when many HTTP connections are created without reuse, exhausting available sockets
- **IHttpClientFactory**: .NET factory that manages HTTP connection pool, preventing socket exhaustion
