# Lição 8: Configuração

Nesta lição, você aprenderá a configurar opções avançadas do Traceability.

## Opções Básicas

### HeaderName

Customize o nome do header HTTP:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
});
```

### ValidateCorrelationIdFormat

Habilite validação do formato do correlation-id:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.ValidateCorrelationIdFormat = true;
});
```

### AlwaysGenerateNew

Gere um novo correlation-id mesmo se já existir:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AlwaysGenerateNew = true;
});
```

## Opções de Log

### LogOutputFormat

Escolha o formato de saída dos logs:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

Opções disponíveis:
- `JsonCompact` (padrão) - JSON em uma linha
- `JsonIndented` - JSON indentado
- `Text` - Formato texto

### Campos de Log

Controle quais campos são incluídos nos logs:

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.LogIncludeTimestamp = true;
    options.LogIncludeLevel = true;
    options.LogIncludeSource = true;
    options.LogIncludeCorrelationId = true;
    options.LogIncludeMessage = true;
    options.LogIncludeData = true;
    options.LogIncludeException = true;
});
```

## Auto-Registro

### Desabilitar Auto-Registro do Middleware

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Registro manual
app.MapControllers();
app.Run();
```

### Desabilitar Auto-Configuração de HttpClient

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoConfigureHttpClient = false;
});

// Configure HttpClient manualmente
builder.Services.AddHttpClient("ExternalApi")
    .AddHttpMessageHandler<CorrelationIdHandler>();
```

## Variáveis de Ambiente

### TRACEABILITY_SERVICENAME

Defina o nome do serviço via variável de ambiente:

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
```

Com a variável definida, você pode usar:

```csharp
// Source vem automaticamente de TRACEABILITY_SERVICENAME
builder.Services.AddTraceability();
```

### LOG_LEVEL

Defina o nível mínimo de log:

**Linux/Mac:**
```bash
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:LOG_LEVEL="Information"
```

## Prioridade de Configuração

Para Source (ServiceName):

1. Parâmetro `source` fornecido explicitamente (prioridade máxima)
2. `TraceabilityOptions.Source` definido nas opções
3. Variável de ambiente `TRACEABILITY_SERVICENAME`
4. Assembly name (se `UseAssemblyNameAsFallback = true`, padrão: true)
5. Se nenhum estiver disponível, uma exceção será lançada

## Exemplo Completo

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
    options.LogIncludeData = true;
    options.AutoRegisterMiddleware = false;
});
```

## Próximos Passos

Agora que você sabe configurar opções, vamos ver exemplos práticos completos na [Lição 9: Exemplos Práticos](09-examples.md).

