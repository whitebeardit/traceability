# Lição 1: O que é Traceability?

## O Que É Correlation-ID?

**Correlation-ID** (também conhecido como correlation identifier ou request ID) é um identificador único usado para rastrear uma requisição através de múltiplos serviços em uma arquitetura distribuída.

### Exemplo Prático

Imagine uma requisição de pedido que passa por três serviços:

```
Cliente → API Gateway → Serviço de Pedidos → Serviço de Pagamento → Serviço de Notificação
```

Sem correlation-id, você teria que procurar logs em cada serviço separadamente. Com o **Traceability**, todos os logs terão o mesmo correlation-id (`a1b2c3d4...`), permitindo buscar por este ID em todos os serviços e ver o fluxo completo da requisição.

## Quando Usar o Traceability?

Use o **Traceability** quando você precisa:

1. **Rastreabilidade em Microserviços**: Rastrear uma requisição através de múltiplos serviços em uma arquitetura distribuída, permitindo correlacionar logs de diferentes serviços usando o mesmo correlation-id.

2. **Debugging Simplificado**: Identificar rapidamente todos os logs relacionados a uma requisição específica, mesmo quando ela passa por vários serviços, facilitando a investigação de problemas.

3. **Análise de Performance**: Medir o tempo total de processamento de uma requisição através de múltiplos serviços, identificando gargalos na cadeia de chamadas.

4. **Monitoramento e Observabilidade**: Correlacionar métricas, traces e logs de diferentes serviços usando o mesmo identificador, melhorando a visibilidade do sistema.

5. **Suporte Multi-Framework**: Trabalhar com aplicações .NET 8.0 (ASP.NET Core) e .NET Framework 4.8 (ASP.NET Web API e ASP.NET Tradicional) usando a mesma biblioteca.

6. **Integração Automática**: Ter correlation-id automaticamente propagado em chamadas HTTP, adicionado aos logs (Serilog e Microsoft.Extensions.Logging) e gerenciado sem código boilerplate.

## Benefícios

- ✅ **Zero Configuração**: Funciona out-of-the-box com configuração mínima
- ✅ **Thread-Safe e Async-Safe**: Usa `AsyncLocal` para garantir isolamento correto em contextos assíncronos
- ✅ **Prevenção de Socket Exhaustion**: Integração nativa com `IHttpClientFactory` para gerenciamento eficiente de conexões HTTP
- ✅ **Integração com Logging**: Suporte automático para Serilog e Microsoft.Extensions.Logging
- ✅ **Propagação Automática**: Correlation-id é automaticamente propagado em todas as chamadas HTTP encadeadas

## Como Funciona?

1. **Requisição HTTP chega**: O middleware/handler lê o header `X-Correlation-Id` (se existir) ou gera um novo GUID
2. **Correlation-id armazenado**: O ID é armazenado no contexto assíncrono usando `AsyncLocal`
3. **Propagação automática**: Todas as chamadas HTTP subsequentes incluem automaticamente o correlation-id no header
4. **Logs automáticos**: Todos os logs incluem automaticamente o correlation-id
5. **Resposta HTTP**: O correlation-id é retornado no header da resposta

## Próximos Passos

Agora que você entende o que é Traceability, vamos começar a usá-lo! Continue para a [Lição 2: Quick Start](02-quick-start.md).

