# Lição 10: Troubleshooting

Nesta lição, você aprenderá a resolver problemas comuns ao usar o Traceability.

## Correlation-id não está sendo propagado

### Problema

O correlation-id não aparece nas chamadas HTTP ou nos logs.

### Soluções

1. **Verifique se o middleware está configurado:**
   ```csharp
   // Certifique-se de que AddTraceability() foi chamado
   builder.Services.AddTraceability("UserService");
   ```

2. **Verifique se HttpClient está usando CorrelationIdHandler:**
   ```csharp
   // Use AddTraceableHttpClient ou adicione o handler manualmente
   builder.Services.AddTraceableHttpClient("ExternalApi", client =>
   {
       client.BaseAddress = new Uri("https://api.example.com/");
   });
   ```

3. **Verifique se AutoConfigureHttpClient não está desabilitado:**
   ```csharp
   builder.Services.AddTraceability("UserService", options =>
   {
       options.AutoConfigureHttpClient = true; // Deve ser true (padrão)
   });
   ```

## Correlation-id não aparece nos logs

### Para Serilog

**Solução:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService") // Adicione esta linha
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();
```

### Para Microsoft.Extensions.Logging

**Solução:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true); // Habilite scopes
```

## Source não está sendo definido

### Erro: InvalidOperationException

Se você receber uma exceção informando que Source deve ser fornecido:

**Solução 1: Forneça Source explicitamente**
```csharp
builder.Services.AddTraceability("UserService");
```

**Solução 2: Defina variável de ambiente**
```bash
export TRACEABILITY_SERVICENAME="UserService"
```

**Solução 3: Configure nas opções**
```csharp
builder.Services.AddTraceability(options =>
{
    options.Source = "UserService";
});
```

## Problemas com .NET Framework 4.8

### Web API - Handler não funciona

**Solução:**
```csharp
// Certifique-se de que o handler está no Global.asax.cs
GlobalConfiguration.Configure(config =>
{
    config.MessageHandlers.Add(new CorrelationIdMessageHandler());
    config.MapHttpAttributeRoutes();
});
```

### ASP.NET Tradicional - Módulo não funciona

**Solução:**
```xml
<!-- Certifique-se de que o módulo está no web.config -->
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

## HttpClient causando socket exhaustion

### Problema

Muitas conexões HTTP são criadas sem reutilização.

### Solução

**❌ Incorreto:**
```csharp
var client = new HttpClient(); // Causa socket exhaustion
```

**✅ Correto:**
```csharp
// Configure no Program.cs
builder.Services.AddTraceableHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
});

// Use no serviço
var client = _httpClientFactory.CreateClient("ExternalApi");
```

## Correlation-id não preservado em Task.Run()

### Problema

O correlation-id não é preservado quando você usa `Task.Run()`.

### Explicação

`Task.Run()` cria um novo contexto assíncrono isolado. O correlation-id não é preservado automaticamente.

### Solução

Use `await` ao invés de `Task.Run()` quando possível:

**❌ Incorreto:**
```csharp
Task.Run(() =>
{
    var id = CorrelationContext.Current; // Pode ser diferente ou null
});
```

**✅ Correto:**
```csharp
await ProcessarAsync(); // Preserva o contexto

async Task ProcessarAsync()
{
    var id = CorrelationContext.Current; // Preservado
}
```

## FAQ

### P: Posso usar correlation-id em aplicações console?

R: Sim! Use `CorrelationContext.GetOrCreate()` para gerar um correlation-id manualmente.

### P: O correlation-id é preservado em Task.Run()?

R: Não. `Task.Run()` cria um novo contexto assíncrono isolado. Use `await` para preservar o contexto.

### P: Posso customizar o nome do header?

R: Sim, use `TraceabilityOptions.HeaderName` para definir um nome customizado.

### P: Como desabilitar o auto-registro do middleware?

R: Defina `AutoRegisterMiddleware = false` nas opções e registre manualmente com `app.UseCorrelationId()`.

### P: Os logs são sempre em JSON?

R: Sim, todos os logs gerados pelo Traceability são sempre em formato JSON para garantir uniformização.

## Ainda com problemas?

Se você ainda está enfrentando problemas:

1. Verifique a [Documentação Técnica](../../AGENTS.md) para entender a arquitetura interna
2. Consulte os [Exemplos](../examples/aspnet-core.md) para ver implementações funcionais
3. Abra uma issue no repositório do projeto

## Parabéns!

Você completou o manual do usuário! Agora você sabe:

- ✅ O que é Traceability e quando usar
- ✅ Como configurar e usar o pacote
- ✅ Como integrar com logging e HttpClient
- ✅ Como configurar opções avançadas
- ✅ Como resolver problemas comuns

Continue explorando:
- [Documentação Completa](../index.md)
- [Exemplos Práticos](../examples/aspnet-core.md)
- [API Reference](../api-reference.md)


