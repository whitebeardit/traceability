# Lição 7: HttpClient

Nesta lição, você aprenderá a usar o Traceability com HttpClient para propagar correlation-id automaticamente.

## Configuração Básica

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
// HttpClient já está configurado automaticamente com CorrelationIdHandler!
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

## Usando HttpClient

**Controller:**
```csharp
public class ValuesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ValuesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Correlation-id é automaticamente adicionado no header
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("endpoint");
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
```

## O Que Acontece Automaticamente

Quando você faz uma chamada HTTP, o `CorrelationIdHandler` automaticamente:

1. ✅ Obtém o correlation-id do contexto atual
2. ✅ Adiciona o header `X-Correlation-Id` na requisição
3. ✅ Propaga o correlation-id para o serviço externo

**Requisição HTTP enviada:**
```http
GET /endpoint HTTP/1.1
Host: api.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

## Propagação em Cadeia

O correlation-id é automaticamente propagado em chamadas HTTP encadeadas:

**Cenário:** Serviço A → Serviço B → Serviço C

1. **Serviço A** recebe requisição sem header → gera `abc123`
2. **Serviço A** chama **Serviço B** com header `X-Correlation-Id: abc123`
3. **Serviço B** lê header e usa `abc123` (não gera novo)
4. **Serviço B** chama **Serviço C** com mesmo header `X-Correlation-Id: abc123`

**Resultado:** Todos os serviços na cadeia usam o mesmo correlation-id!

## Usando AddTraceableHttpClient (Recomendado)

Para garantir que o HttpClient está configurado corretamente:

**Program.cs:**
```csharp
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Uso:**
```csharp
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## Com Polly (Políticas de Resiliência)

**Program.cs:**
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

## Prevenção de Socket Exhaustion

**✅ Sempre use `IHttpClientFactory`:**

```csharp
// Correto - usa IHttpClientFactory
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

var client = _httpClientFactory.CreateClient("ExternalApi");
```

**❌ Nunca crie instâncias diretas:**

```csharp
// Incorreto - causa socket exhaustion
var client = new HttpClient();
```

## Exemplo Completo

**Program.cs:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});
```

**Service:**
```csharp
public class ExternalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync()
    {
        var client = _httpClientFactory.CreateClient("ExternalApi");
        var response = await client.GetAsync("data");
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Próximos Passos

Agora que você sabe usar HttpClient, vamos ver opções de configuração na [Lição 8: Configuração](08-configuration.md).


