# Tópicos Avançados

Recursos avançados e casos de uso do Traceability.

## Prevenção de Socket Exhaustion

O Traceability foi projetado para prevenir socket exhaustion desde o início. Todos os métodos de criação de HttpClient usam `IHttpClientFactory`, que gerencia o pool de conexões HTTP e reutiliza sockets.

### Como Funciona

O `IHttpClientFactory` gerencia o ciclo de vida dos `HttpClient`:
- Reutiliza conexões HTTP quando possível
- Gerencia o pool de sockets automaticamente
- Previne socket exhaustion mesmo em alta carga

### Uso Correto

```csharp
// Configure no Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use no serviço ou controller
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task CallApiAsync()
    {
        // IHttpClientFactory reutiliza conexões, prevenindo socket exhaustion
        var client = _httpClientFactory.CreateClient("ExternalApi");
        await client.GetAsync("endpoint");
    }
}
```

## HttpClient com Polly

Integre políticas de resiliência com o Traceability:

```csharp
using Polly;
using Polly.Extensions.Http;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.AddPolicyHandler(retryPolicy);
```

## Template JSON Customizado

Configure o formato JSON dos logs:

```csharp
var options = new TraceabilityOptions
{
    Source = "UserService",
    LogOutputFormat = LogOutputFormat.JsonIndented,
    LogIncludeData = true,
    LogIncludeTimestamp = true
};

Log.Logger = new LoggerConfiguration()
    .WithTraceabilityJson(options)
    .WriteTo.Console(new JsonFormatter(options, indent: true))
    .CreateLogger();
```

## Isolamento Assíncrono

Entenda como o correlation-id é isolado em contextos assíncronos:

```csharp
// Contexto principal
var mainId = CorrelationContext.GetOrCreate();

// Task isolada terá seu próprio contexto
var task = Task.Run(async () =>
{
    // Este contexto é isolado
    var taskId = CorrelationContext.GetOrCreate();
    await Task.Delay(100);
    return taskId;
});

var taskId = await task;

// mainId e taskId são diferentes
Console.WriteLine($"Main: {mainId}, Task: {taskId}");
```

## Propagação em Cadeia de Chamadas

O correlation-id é automaticamente propagado em chamadas HTTP encadeadas:

**Cenário:** Serviço A → Serviço B → Serviço C

1. Serviço A recebe requisição sem header → gera `abc123`
2. Serviço A chama Serviço B com header `X-Correlation-Id: abc123`
3. Serviço B lê header e usa `abc123` (não gera novo)
4. Serviço B chama Serviço C com mesmo header
5. Processo continua até o fim da cadeia

**Regra**: Nunca gerar novo correlation-id se já existir no header da requisição.

## Uso Manual do CorrelationContext

Para casos especiais, você pode usar o `CorrelationContext` manualmente:

```csharp
using Traceability;

// Obter correlation-id atual (cria se não existir)
var correlationId = CorrelationContext.Current;

// Verificar se existe
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
}

// Obter ou criar explicitamente
var id = CorrelationContext.GetOrCreate();

// Tentar obter sem criar (recomendado para evitar criação indesejada)
if (CorrelationContext.TryGetValue(out var correlationId))
{
    // Usar correlationId
}

// Limpar contexto
CorrelationContext.Clear();
```

## Integração com Application Insights

Para integração com Application Insights, você pode usar o correlation-id junto com o sistema de diagnóstico do .NET:

```csharp
// O correlation-id pode ser usado como propriedade customizada
var correlationId = CorrelationContext.Current;
telemetryClient.Context.Properties["CorrelationId"] = correlationId;
```

## Limitações Conhecidas

1. **.NET Framework 4.8**: Não tem DI nativo, então `TraceabilityOptions` deve ser configurado via métodos estáticos `Configure()` em `CorrelationIdHttpModule` e `CorrelationIdMessageHandler`.

2. **Validação de Formato**: A validação de formato do correlation-id é opcional e deve ser habilitada via `TraceabilityOptions.ValidateCorrelationIdFormat`.

3. **IHttpClientFactory**: Os métodos de criação de HttpClient requerem `IHttpClientFactory` (disponível apenas em .NET 8 para este pacote). Para .NET Framework, use `CorrelationIdHandler` diretamente com seu próprio gerenciamento de HttpClient.

4. **Mensageria**: Não há suporte para correlation-id em mensageria (RabbitMQ, Kafka, etc.) - apenas HTTP atualmente.

## Melhores Práticas

1. **Sempre use IHttpClientFactory**: Previne socket exhaustion
2. **Defina Source**: Facilita rastreabilidade em ambientes distribuídos
3. **Use variáveis de ambiente**: Reduz verbosidade e facilita configuração
4. **Mantenha logs em JSON**: Garante uniformização entre serviços
5. **Não modifique correlation-id existente**: Preserva rastreabilidade na cadeia


