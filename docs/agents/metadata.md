# Metadata e Contexto Inicial

## Informações do Projeto

- **Nome**: Traceability
- **Versão**: 1.0.0
- **Tipo**: Pacote NuGet
- **Licença**: MIT
- **Autor**: WhiteBeard IT

## Frameworks Suportados

- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

## Dependências Principais

### .NET 8.0
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

### Comum
- `Serilog` (3.1.1) - PrivateAssets: all

## Estrutura de Namespaces

```
Traceability
├── Traceability                          # Core: CorrelationContext
├── Traceability.Configuration            # Opções de configuração
├── Traceability.Extensions               # Extensões para DI e middleware
├── Traceability.HttpClient               # Integração com HttpClient
├── Traceability.Logging                  # Integrações de logging
├── Traceability.Middleware               # Middleware e handlers HTTP
└── Traceability.WebApi                   # Handlers específicos Web API
```


