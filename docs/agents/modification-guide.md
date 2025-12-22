# Guia de Modificação para LLMs

## Como Adicionar Novo Componente

1. **Identificar localização**
   - Verificar estrutura de diretórios
   - Escolher diretório apropriado ou criar novo

2. **Definir responsabilidade única**
   - Componente deve ter uma responsabilidade clara
   - Seguir padrões existentes

3. **Implementar com conditional compilation**
   ```csharp
   #if NET8_0
   // Código .NET 8
   #endif
   
   #if NET48
   // Código .NET Framework
   #endif
   ```

4. **Adicionar dependências**
   - Usar `CorrelationContext` se precisar de correlation-id
   - Seguir padrão de injeção de dependência se aplicável

5. **Adicionar testes**
   - Criar arquivo em `tests/Traceability.Tests/`
   - Testar ambos os frameworks se aplicável

6. **Atualizar documentação**
   - Adicionar XML comments
   - Atualizar README.md se necessário
   - Atualizar documentação em `docs/agents/`

## Como Manter Compatibilidade Multi-Framework

1. **Identificar código específico de framework**
   - APIs diferentes entre .NET 8 e .NET Framework
   - Namespaces diferentes

2. **Usar conditional compilation**
   ```csharp
   #if NET8_0
   using Microsoft.AspNetCore.Http;
   #endif
   
   #if NET48
   using System.Web;
   #endif
   ```

3. **Testar em ambos os frameworks**
   - Executar testes para .NET 8
   - Executar testes para .NET Framework 4.8

4. **Manter API pública consistente**
   - Mesma interface pública em ambos os frameworks
   - Comportamento idêntico

## Onde Adicionar Testes

**Estrutura de Testes**:
```
tests/Traceability.Tests/
├── CorrelationContextTests.cs
├── CorrelationIdHandlerTests.cs
├── CorrelationIdMiddlewareTests.cs
├── LoggingTests.cs
└── TraceableHttpClientFactoryTests.cs
```

**Padrão de Teste**:
- Usar xUnit
- Usar FluentAssertions para assertions
- Usar Moq para mocks quando necessário
- Nomear testes: `MethodName_Scenario_ExpectedBehavior`

**Exemplo**:
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

## Como Atualizar Documentação

1. **XML Comments**
   - Adicionar `<summary>` para classes públicas
   - Adicionar `<param>` para parâmetros
   - Adicionar `<returns>` para retornos
   - Adicionar `<example>` quando útil

2. **README.md**
   - Adicionar exemplos de uso
   - Atualizar seções relevantes
   - Manter consistência com código

3. **Documentação em `docs/agents/`**
   - Adicionar novo componente em `components.md`
   - Atualizar diagramas em `architecture.md` se necessário
   - Adicionar exemplos de uso em `code-examples.md`

## Referências Rápidas

### Arquivos Principais
- Core: `src/Traceability/CorrelationContext.cs`
- Middleware (.NET 8): `src/Traceability/Middleware/CorrelationIdMiddleware.cs`
- HttpModule (.NET Framework): `src/Traceability/Middleware/CorrelationIdHttpModule.cs`
- MessageHandler: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`
- HttpClient Handler: `src/Traceability/HttpClient/CorrelationIdHandler.cs`
- Factory: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`
- Utilities: `src/Traceability/Utilities/TraceabilityUtilities.cs`
- Serilog CorrelationId: `src/Traceability/Logging/CorrelationIdEnricher.cs`
- Serilog Source: `src/Traceability/Logging/SourceEnricher.cs`
- Serilog Data: `src/Traceability/Logging/DataEnricher.cs`
- Serilog JSON Formatter: `src/Traceability/Logging/JsonFormatter.cs`
- MEL CorrelationId: `src/Traceability/Logging/CorrelationIdScopeProvider.cs`
- MEL Source: `src/Traceability/Logging/SourceScopeProvider.cs`
- Configuration: `src/Traceability/Configuration/TraceabilityOptions.cs`

### Exemplos
- ASP.NET Core: `samples/Sample.WebApi.Net8/`
- Console: `samples/Sample.Console.Net8/`
- .NET Framework: `samples/Sample.WebApi.NetFramework/`

### Testes
- Todos os testes: `tests/Traceability.Tests/`

