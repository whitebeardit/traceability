# Padrões de Implementação

## 1. Conditional Compilation

**Padrão**: Usar diretivas `#if` para código específico de framework.

**Regras**:
- `#if NET8_0` para código ASP.NET Core
- `#if NET48` para código .NET Framework
- Sempre fechar com `#endif`
- Código comum (sem diretiva) funciona em ambos

**Exemplo**:
```csharp
#if NET8_0
using Microsoft.AspNetCore.Http;
// Código específico .NET 8
#endif

#if NET48
using System.Web;
// Código específico .NET Framework
#endif

// Código comum
```

## 2. AsyncLocal para Isolamento Assíncrono

**Padrão**: Sempre usar `AsyncLocal<string>` para armazenar correlation-id.

**Razão**: `AsyncLocal` preserva valores através de continuidades assíncronas, enquanto `ThreadLocal` não.

**Implementação**:
```csharp
private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
```

**Comportamento**:
- Cada contexto assíncrono tem seu próprio valor
- Valor é preservado através de `await`
- Isolado entre diferentes `Task.Run()` ou threads

## 3. Padrão Factory para HttpClient

**Padrão**: Usar factory para criar HttpClient configurado.

**Implementação**: `TraceableHttpClientFactory`

**Vantagens**:
- Centraliza configuração
- Facilita adição de handlers
- Suporta políticas Polly

## 4. Padrão DelegatingHandler

**Padrão**: Usar `DelegatingHandler` para interceptar requisições HTTP.

**Implementação**: `CorrelationIdHandler`

**Comportamento**:
- Intercepta antes de enviar requisição
- Adiciona header automaticamente
- Preserva pipeline de handlers

## 5. Padrão Enricher (Serilog)

**Padrão**: Implementar `ILogEventEnricher` para adicionar propriedades aos logs.

**Implementação**: `CorrelationIdEnricher`

**Comportamento**:
- Chamado para cada evento de log
- Adiciona propriedade `CorrelationId`
- Não modifica outros enrichers

## 6. Padrão ScopeProvider (Microsoft.Extensions.Logging)

**Padrão**: Implementar `IExternalScopeProvider` para adicionar scopes.

**Implementação**: `CorrelationIdScopeProvider`

**Comportamento**:
- Adiciona scope com `CorrelationId`
- Suporta provider interno (decorator pattern)
- Preserva scopes existentes

## 7. Convenções de Nomenclatura

- **Classes**: PascalCase
- **Métodos**: PascalCase
- **Propriedades**: PascalCase
- **Campos privados**: `_camelCase` (underscore prefix)
- **Constantes**: PascalCase
- **Namespaces**: `Traceability.[SubNamespace]`

## 8. Geração de Correlation-ID

**Formato**: GUID sem hífens (32 caracteres)

**Implementação**:
```csharp
private static string GenerateNew()
{
    return Guid.NewGuid().ToString("N");
}
```

**Razão**: 
- Compacto (32 chars vs 36 com hífens)
- Compatível com sistemas externos
- Legível em logs

## Estrutura de Diretórios

```
src/Traceability/
├── Configuration/
│   └── TraceabilityOptions.cs          # Opções de configuração
│
├── CorrelationContext.cs                # Core: Gerenciamento de contexto
│
├── Extensions/
│   ├── ApplicationBuilderExtensions.cs  # Extensão para IApplicationBuilder (.NET 8)
│   ├── HttpClientExtensions.cs          # Extensões para HttpClient
│   ├── LoggerConfigurationExtensions.cs # Extensão para LoggerConfiguration (Serilog)
│   └── ServiceCollectionExtensions.cs   # Extensão para IServiceCollection (.NET 8)
│
├── HttpClient/
│   ├── CorrelationIdHandler.cs          # DelegatingHandler para HttpClient
│   └── TraceableHttpClientFactory.cs    # Factory para criar HttpClient
│
├── Utilities/
│   └── TraceabilityUtilities.cs         # Utilitários compartilhados (GetServiceName, SanitizeSource)
│
├── Logging/
│   ├── CorrelationIdEnricher.cs        # Enricher para Serilog
│   ├── CorrelationIdScopeProvider.cs   # ScopeProvider para MEL
│   ├── DataEnricher.cs                  # Enricher que serializa objetos em "data"
│   ├── JsonFormatter.cs                 # Formatter JSON customizado
│   ├── SourceEnricher.cs               # Enricher Source para Serilog
│   └── SourceScopeProvider.cs          # ScopeProvider Source para MEL
│
├── Middleware/
│   ├── CorrelationIdHttpModule.cs      # HttpModule (.NET Framework)
│   └── CorrelationIdMiddleware.cs      # Middleware (.NET 8)
│
├── WebApi/
│   └── CorrelationIdMessageHandler.cs  # MessageHandler (.NET Framework Web API)
│
└── Traceability.csproj                  # Arquivo de projeto
```

### Descrição por Diretório

#### Configuration/
- **Propósito**: Classes de configuração
- **Quando usar**: Para adicionar novas opções de configuração
- **Arquivos**: `TraceabilityOptions.cs`

#### Extensions/
- **Propósito**: Métodos de extensão para facilitar uso
- **Quando usar**: Para adicionar métodos de conveniência
- **Arquivos**: Extensões para DI, middleware, HttpClient

#### HttpClient/
- **Propósito**: Integração com HttpClient
- **Quando usar**: Para modificar comportamento de HTTP ou adicionar novos handlers
- **Arquivos**: Handlers, factory, interfaces

#### Logging/
- **Propósito**: Integrações com sistemas de logging
- **Quando usar**: Para adicionar suporte a novos loggers
- **Arquivos**: Enrichers, scope providers

#### Middleware/
- **Propósito**: Middleware e handlers HTTP
- **Quando usar**: Para adicionar novos pontos de interceptação HTTP
- **Arquivos**: Middleware (.NET 8), HttpModule (.NET Framework)

#### WebApi/
- **Propósito**: Handlers específicos para ASP.NET Web API
- **Quando usar**: Para funcionalidades específicas do Web API
- **Arquivos**: MessageHandlers


