# Traceability - Documentação para LLMs

Este diretório contém a documentação técnica completa do projeto Traceability, organizada para facilitar o uso por LLMs e desenvolvedores.

## Índice

### Informações Básicas
- [Metadata e Contexto Inicial](metadata.md) - Informações do projeto, dependências e estrutura de namespaces

### Arquitetura
- [Arquitetura de Alto Nível](architecture.md) - Diagramas de componentes e fluxos de dados

### Componentes
- [Componentes Core](components.md) - Detalhamento técnico de todos os componentes do pacote

### Padrões e Práticas
- [Padrões de Implementação](patterns.md) - Padrões de código e convenções utilizadas
- [Regras e Constraints](rules.md) - Regras obrigatórias e constraints de design
- [Decisões de Design](design-decisions.md) - Racionais por trás das decisões arquiteturais

### Guias
- [Guia de Modificação](modification-guide.md) - Como adicionar novos componentes e manter compatibilidade
- [Exemplos de Código](code-examples.md) - Exemplos práticos de uso

### Referência
- [Glossário](glossary.md) - Definições de termos técnicos

## Estrutura do Projeto

```
src/Traceability/
├── Configuration/          # Opções de configuração
├── CorrelationContext.cs    # Core: Gerenciamento de contexto
├── Extensions/             # Métodos de extensão
├── HttpClient/             # Integração com HttpClient
├── Logging/                # Integrações de logging
├── Middleware/             # Middleware e handlers HTTP
├── Utilities/              # Utilitários compartilhados
└── WebApi/                 # Handlers específicos Web API
```

## Frameworks Suportados

- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

## Princípios Fundamentais

1. **AsyncLocal para Isolamento**: Sempre usar `AsyncLocal<string>` para contexto assíncrono
2. **Conditional Compilation**: Código específico de framework deve usar `#if NET8_0` ou `#if NET48`
3. **Header Padrão**: Sempre usar `X-Correlation-Id` como header padrão
4. **GUID sem Hífens**: Gerar correlation-id como GUID de 32 caracteres (sem hífens)
5. **Não Modificar Existente**: Nunca sobrescrever correlation-id existente no contexto
6. **Zero Configuração**: Funciona sem configuração, mas permite customização

## Referências Rápidas

### Arquivos Principais
- Core: `src/Traceability/CorrelationContext.cs`
- Middleware (.NET 8): `src/Traceability/Middleware/CorrelationIdMiddleware.cs`
- HttpModule (.NET Framework): `src/Traceability/Middleware/CorrelationIdHttpModule.cs`
- MessageHandler: `src/Traceability/WebApi/CorrelationIdMessageHandler.cs`
- HttpClient Handler: `src/Traceability/HttpClient/CorrelationIdHandler.cs`
- Factory: `src/Traceability/HttpClient/TraceableHttpClientFactory.cs`
- Configuration: `src/Traceability/Configuration/TraceabilityOptions.cs`

### Exemplos
- ASP.NET Core: `samples/Sample.WebApi.Net8/`
- Console: `samples/Sample.Console.Net8/`
- .NET Framework: `samples/Sample.WebApi.NetFramework/`

### Testes
- Todos os testes: `tests/Traceability.Tests/`

---

**Última atualização**: Baseado na versão 1.0.0 do projeto Traceability

