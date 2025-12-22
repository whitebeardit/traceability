# Traceability - Arquitetura e Guia para LLMs

> **Nota**: Esta documentação foi refatorada em arquivos menores para facilitar navegação. Veja o [índice completo em `docs/agents/`](docs/agents/index.md).

## Índice Rápido

Esta documentação técnica está organizada nos seguintes arquivos:

### Informações Básicas
- [Metadata e Contexto Inicial](docs/agents/metadata.md) - Informações do projeto, dependências e estrutura de namespaces

### Arquitetura
- [Arquitetura de Alto Nível](docs/agents/architecture.md) - Diagramas de componentes e fluxos de dados

### Componentes
- [Componentes Core](docs/agents/components.md) - Detalhamento técnico de todos os componentes do pacote

### Padrões e Práticas
- [Padrões de Implementação](docs/agents/patterns.md) - Padrões de código e convenções utilizadas
- [Regras e Constraints](docs/agents/rules.md) - Regras obrigatórias e constraints de design
- [Decisões de Design](docs/agents/design-decisions.md) - Racionais por trás das decisões arquiteturais

### Guias
- [Guia de Modificação](docs/agents/modification-guide.md) - Como adicionar novos componentes e manter compatibilidade
- [Exemplos de Código](docs/agents/code-examples.md) - Exemplos práticos de uso

### Referência
- [Glossário](docs/agents/glossary.md) - Definições de termos técnicos

## Visão Geral

O Traceability é um pacote NuGet para gerenciamento automático de correlation-id em aplicações .NET, com suporte para .NET 8 e .NET Framework 4.8.

### Frameworks Suportados
- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

### Princípios Fundamentais

1. **AsyncLocal para Isolamento**: Sempre usar `AsyncLocal<string>` para contexto assíncrono
2. **Conditional Compilation**: Código específico de framework deve usar `#if NET8_0` ou `#if NET48`
3. **Header Padrão**: Sempre usar `X-Correlation-Id` como header padrão
4. **GUID sem Hífens**: Gerar correlation-id como GUID de 32 caracteres (sem hífens)
5. **Não Modificar Existente**: Nunca sobrescrever correlation-id existente no contexto
6. **Zero Configuração**: Funciona sem configuração, mas permite customização

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

**Para documentação completa, consulte**: [docs/agents/index.md](docs/agents/index.md)

**Última atualização**: Baseado na versão 1.0.0 do projeto Traceability
