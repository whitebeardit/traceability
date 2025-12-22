# Configuração

O Traceability oferece várias opções de configuração para customizar o comportamento do pacote.

## TraceabilityOptions

A classe `TraceabilityOptions` permite configurar todas as opções do pacote:

```csharp
public class TraceabilityOptions
{
    public string HeaderName { get; set; } = "X-Correlation-Id";
    public bool AlwaysGenerateNew { get; set; } = false;
    public bool ValidateCorrelationIdFormat { get; set; } = false;
    public string? Source { get; set; }
    public LogOutputFormat LogOutputFormat { get; set; } = LogOutputFormat.JsonCompact;
    public bool LogIncludeTimestamp { get; set; } = true;
    public bool LogIncludeLevel { get; set; } = true;
    public bool LogIncludeSource { get; set; } = true;
    public bool LogIncludeCorrelationId { get; set; } = true;
    public bool LogIncludeMessage { get; set; } = true;
    public bool LogIncludeData { get; set; } = true;
    public bool LogIncludeException { get; set; } = true;
    public bool AutoRegisterMiddleware { get; set; } = true;
    public bool AutoConfigureHttpClient { get; set; } = true;
    public bool UseAssemblyNameAsFallback { get; set; } = true;
}
```

## Opções de Configuração

### HeaderName
Nome do header HTTP para correlation-id (padrão: "X-Correlation-Id")

```csharp
builder.Services.AddTraceability(options =>
{
    options.HeaderName = "X-Request-Id";
});
```

### AlwaysGenerateNew
Se true, gera um novo correlation-id mesmo se já existir um no contexto (padrão: false)

```csharp
builder.Services.AddTraceability(options =>
{
    options.AlwaysGenerateNew = true;
});
```

### ValidateCorrelationIdFormat
Se true, valida o formato do correlation-id recebido no header (padrão: false)

```csharp
builder.Services.AddTraceability(options =>
{
    options.ValidateCorrelationIdFormat = true;
});
```

### Source
Nome da origem/serviço que está gerando os logs (opcional, mas recomendado)

```csharp
builder.Services.AddTraceability(options =>
{
    options.Source = "UserService";
});
```

### LogOutputFormat
Formato de saída para logs (padrão: JsonCompact)

```csharp
builder.Services.AddTraceability(options =>
{
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

### Opções de Log

Controle quais campos são incluídos nos logs:

```csharp
builder.Services.AddTraceability(options =>
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

### Auto-Registro

Controle o registro automático de middleware e HttpClient:

```csharp
builder.Services.AddTraceability(options =>
{
    options.AutoRegisterMiddleware = false;  // Desabilita auto-registro do middleware
    options.AutoConfigureHttpClient = false; // Desabilita auto-configuração de HttpClient
});
```

Se desabilitar, você precisará registrar manualmente:

```csharp
var app = builder.Build();
app.UseCorrelationId(); // Registro manual do middleware
```

## Variáveis de Ambiente

### TRACEABILITY_SERVICENAME

Define o nome do serviço/origem que está gerando os logs.

**Prioridade de Configuração:**
1. Parâmetro `source` fornecido explicitamente (prioridade máxima)
2. `TraceabilityOptions.Source` definido nas opções
3. Variável de ambiente `TRACEABILITY_SERVICENAME`
4. Assembly name (se `UseAssemblyNameAsFallback = true`, padrão: true)
5. Se nenhum estiver disponível, uma exceção será lançada

**Configuração:**

**Linux/Mac:**
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Windows PowerShell:**
```powershell
$env:TRACEABILITY_SERVICENAME="UserService"
```

**Windows CMD:**
```cmd
set TRACEABILITY_SERVICENAME=UserService
```

### LOG_LEVEL

Define o nível mínimo de log (Verbose, Debug, Information, Warning, Error, Fatal).

**Prioridade de Configuração:**
1. Variável de ambiente `LOG_LEVEL` (prioridade máxima)
2. `TraceabilityOptions.MinimumLogLevel` definido nas opções
3. Information (padrão)

**Configuração:**

**Linux/Mac:**
```bash
export LOG_LEVEL="Information"
```

**Windows PowerShell:**
```powershell
$env:LOG_LEVEL="Information"
```

## Exemplos de Configuração

### Configuração Básica

```csharp
builder.Services.AddTraceability("UserService");
```

### Configuração com Opções

```csharp
builder.Services.AddTraceability(options =>
{
    options.Source = "UserService";
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
    options.LogOutputFormat = LogOutputFormat.JsonIndented;
});
```

### Configuração com Source e Opções

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.HeaderName = "X-Request-Id";
    options.ValidateCorrelationIdFormat = true;
});
```

### Desabilitar Auto-Registro

```csharp
builder.Services.AddTraceability("UserService", options =>
{
    options.AutoRegisterMiddleware = false;
    options.AutoConfigureHttpClient = false;
});

var app = builder.Build();
app.UseCorrelationId(); // Registro manual
```

## Output JSON

Todos os logs gerados pelo Traceability são sempre em formato JSON para garantir uniformização entre diferentes aplicações e serviços.

O formato JSON padrão inclui:
- `Timestamp`: Data e hora do log
- `Level`: Nível do log (Information, Warning, Error, etc.)
- `Source`: Nome do serviço
- `CorrelationId`: ID de correlação (quando disponível)
- `Message`: Mensagem do log
- `Data`: Objetos serializados (quando presente)
- `Exception`: Informações de exceção (quando presente)

