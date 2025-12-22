# Decisões de Design e Racionais

## Por que `AsyncLocal` ao invés de `ThreadLocal`?

**Razão**: `AsyncLocal` preserva valores através de continuidades assíncronas, enquanto `ThreadLocal` não.

**Exemplo do problema com `ThreadLocal`**:
```csharp
// ❌ Com ThreadLocal (não funciona)
ThreadLocal<string> correlationId = new ThreadLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Contexto pode mudar de thread
// correlationId.Value pode ser null ou diferente
```

**Solução com `AsyncLocal`**:
```csharp
// ✅ Com AsyncLocal (funciona)
AsyncLocal<string> correlationId = new AsyncLocal<string>();
correlationId.Value = "abc123";
await SomeAsyncMethod(); // Valor preservado
// correlationId.Value ainda é "abc123"
```

## Por que suporte multi-framework?

**Razão**: 
- Muitas empresas ainda usam .NET Framework 4.8
- Migração gradual é comum
- Necessidade de rastreabilidade em ambos os ambientes

**Trade-off**: Código mais complexo com conditional compilation, mas maior compatibilidade.

## Por que não usar `Activity` do .NET?

**Razão**:
- `Activity` é parte do sistema de diagnóstico do .NET
- Mais pesado e complexo
- Requer configuração adicional
- `AsyncLocal` é mais simples e direto para este caso de uso
- Não requer dependências adicionais

**Quando considerar `Activity`**:
- Se precisar de integração com Application Insights
- Se precisar de distributed tracing completo
- Se precisar de spans e traces hierárquicos

## Por que Uniformização de Logs JSON e Variáveis de Ambiente?

**Razão**: Garantir que todos os logs de diferentes aplicações e serviços sigam o mesmo padrão, facilitando análise, correlação e monitoramento em ambientes distribuídos.

**Decisões de Design**:

1. **Output Sempre JSON**:
   - **Razão**: Formato JSON é estruturado, facilmente parseável e suportado por todas as ferramentas de log aggregation (ELK, Splunk, etc.)
   - **Benefício**: Uniformização automática entre diferentes aplicações e serviços
   - **Implementação**: Todos os métodos `WithTraceability()` e `WithTraceabilityJson()` garantem output JSON

2. **Variáveis de Ambiente para Source e LogLevel**:
   - **Razão**: Reduzir verbosidade na configuração e permitir alteração sem recompilação
   - **Benefício**: Configuração centralizada via variáveis de ambiente facilita gerenciamento em produção
   - **Prioridade**: Parâmetro > Options > Env Var > Erro (garante flexibilidade mas força padrão)

3. **Erro Quando Source Não Disponível**:
   - **Razão**: Forçar que todos os serviços tenham Source definido para garantir rastreabilidade
   - **Benefício**: Previne logs sem identificação de origem, facilitando debugging em ambientes distribuídos
   - **Trade-off**: Pode ser mais restritivo, mas garante qualidade dos logs

4. **Fluxo de Decisão para Source**:
   - **Razão**: Permitir múltiplas formas de configuração mantendo prioridade clara
   - **Benefício**: Flexibilidade para diferentes cenários (desenvolvimento, testes, produção)
   - **Implementação**: Método `TraceabilityUtilities.GetServiceName()` centraliza a lógica de decisão
   - **Sanitização**: Source é automaticamente sanitizado via `TraceabilityUtilities.SanitizeSource()` para garantir segurança

**Exemplo do Problema Resolvido**:
```csharp
// ❌ Antes: Cada serviço configura Source de forma diferente
// Serviço A
Log.Logger = new LoggerConfiguration()
    .Enrich.With(new SourceEnricher("ServiceA"))
    .WriteTo.Console() // Formato texto diferente
    .CreateLogger();

// Serviço B
Log.Logger = new LoggerConfiguration()
    .Enrich.With(new SourceEnricher("ServiceB"))
    .WriteTo.File("log.txt") // Formato diferente
    .CreateLogger();

// ✅ Agora: Todos os serviços seguem o mesmo padrão
// export TRACEABILITY_SERVICENAME="ServiceA"
Log.Logger = new LoggerConfiguration()
    .WithTraceability() // Source da env var, output JSON
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// export TRACEABILITY_SERVICENAME="ServiceB"
Log.Logger = new LoggerConfiguration()
    .WithTraceability() // Source da env var, output JSON
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

## Trade-offs e Limitações Conhecidas

1. **✅ RESOLVIDO**: `TraceabilityOptions` agora está totalmente integrado
   - Header pode ser customizado via `HeaderName`
   - `AlwaysGenerateNew` é usado nos middlewares/handlers
   - `ValidateCorrelationIdFormat` adicionado para validação opcional

2. **✅ RESOLVIDO**: `ITraceableHttpClient` interface foi removida
   - Interface não utilizada foi removida para simplificar a API
   - `AddTraceableHttpClient<TClient>` agora funciona com qualquer classe (sem constraint de interface)

3. **Trade-off**: Conditional compilation aumenta complexidade
   - Benefício: Suporte multi-framework
   - Custo: Mais difícil de manter

4. **Limitação**: Não há suporte para correlation-id em mensageria (RabbitMQ, Kafka, etc.)
   - Apenas HTTP atualmente

5. **Trade-off**: GUID simples ao invés de IDs mais informativos
   - Benefício: Simples, único, sem colisões
   - Custo: Não contém informação semântica

6. **✅ RESOLVIDO - Socket Exhaustion**: Métodos que causavam socket exhaustion foram removidos
   - **Solução implementada**: Todos os métodos de criação de HttpClient usam `IHttpClientFactory`
   - **Métodos disponíveis**: `CreateFromFactory()` e `AddTraceableHttpClient()` (extensão)
   - **Status**: API limpa que força uso de boas práticas desde o início

## Melhorias de Segurança e Robustez Implementadas

### Proteções contra Stack Overflow
- **JsonFormatter**: Limite de 10 níveis em cadeias de InnerException
- **DataEnricher**: Limite de 10 níveis de profundidade em objetos aninhados
- **DataEnricher**: Detecção automática de referências circulares

### Proteções contra OutOfMemoryException
- **DataEnricher**: Limite de 1000 elementos por coleção (Dictionary, Structure, Sequence)
- Mensagens informativas quando limites são atingidos

### Validação e Sanitização
- **HeaderName**: Validação automática com fallback para "X-Correlation-Id" padrão
- **Source**: Sanitização automática para remover caracteres inválidos e limitar tamanho (100 caracteres)
- **CorrelationId**: Validação opcional de formato (tamanho máximo 128 caracteres)

### Thread-Safety
- **.NET Framework**: Configuração estática agora usa `volatile` e `lock` para garantir thread-safety
- **CorrelationContext**: Melhorias na implementação para garantir thread-safety em todos os cenários


