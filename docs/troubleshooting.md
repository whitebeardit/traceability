# Troubleshooting

Solução de problemas comuns ao usar o Traceability.

## O correlation-id não está sendo propagado

### Possíveis Causas e Soluções

1. **Middleware/Handler não está configurado**
   - Certifique-se de que o middleware/handler está configurado corretamente
   - Para ASP.NET Core: Verifique se `AddTraceability()` foi chamado ou se `app.UseCorrelationId()` está no pipeline
   - Para .NET Framework: Verifique se `CorrelationIdMessageHandler` ou `CorrelationIdHttpModule` está configurado

2. **HttpClient não está usando CorrelationIdHandler**
   - Certifique-se de que está usando `IHttpClientFactory` com `AddTraceableHttpClient()` ou `.AddHttpMessageHandler<CorrelationIdHandler>()`
   - Verifique se `AutoConfigureHttpClient` não está desabilitado nas opções

3. **Contexto assíncrono não está sendo preservado**
   - Certifique-se de que não está usando `Task.Run()` sem preservar o contexto
   - Use `await` corretamente para preservar o contexto assíncrono

## Correlation-id não aparece nos logs

### Para Serilog

1. Use `WithTraceability("SuaOrigem")` ou configure `SourceEnricher` + `CorrelationIdEnricher` manualmente
2. Verifique o template de output do logger para incluir `{CorrelationId}`

**Exemplo:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WithTraceability("UserService")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();
```

### Para Microsoft.Extensions.Logging (.NET 8)

1. Chame `AddTraceability("SuaOrigem")` e habilite scopes no Console (`IncludeScopes = true`)
2. Verifique se o logger está configurado corretamente

**Exemplo:**
```csharp
builder.Services.AddTraceability("UserService");
builder.Logging.AddConsole(options => options.IncludeScopes = true);
```

## Problemas com .NET Framework 4.8

### Dependências

1. Certifique-se de que as versões corretas das dependências estão instaladas
2. Verifique se todas as referências NuGet estão corretas

### Web API

1. Adicione o `CorrelationIdMessageHandler` no `Global.asax.cs`
2. Verifique se o handler está na ordem correta do pipeline

**Exemplo:**
```csharp
GlobalConfiguration.Configure(config =>
{
    config.MessageHandlers.Add(new CorrelationIdMessageHandler());
    config.MapHttpAttributeRoutes();
});
```

### ASP.NET Tradicional

1. Configure o `CorrelationIdHttpModule` no `web.config`
2. Verifique se o módulo está registrado corretamente

**Exemplo (web.config):**
```xml
<system.webServer>
  <modules>
    <add name="CorrelationIdHttpModule" 
         type="Traceability.Middleware.CorrelationIdHttpModule, Traceability" />
  </modules>
</system.webServer>
```

### Configuração de Opções

Para configurar opções, use `CorrelationIdHttpModule.Configure()` ou `CorrelationIdMessageHandler.Configure()` antes de usar.

**Exemplo:**
```csharp
CorrelationIdMessageHandler.Configure(new TraceabilityOptions
{
    HeaderName = "X-Correlation-Id",
    ValidateCorrelationIdFormat = true
});
```

## Source não está sendo definido

### Erro: InvalidOperationException

Se você receber uma exceção informando que Source deve ser fornecido:

1. **Forneça Source explicitamente:**
   ```csharp
   builder.Services.AddTraceability("UserService");
   ```

2. **Ou defina variável de ambiente:**
   ```bash
   export TRACEABILITY_SERVICENAME="UserService"
   ```

3. **Ou configure nas opções:**
   ```csharp
   builder.Services.AddTraceability(options =>
   {
       options.Source = "UserService";
   });
   ```

## HttpClient causando socket exhaustion

### Solução

Sempre use `IHttpClientFactory` ao invés de criar instâncias diretas de `HttpClient`.

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

1. Verifique a [Documentação Técnica](../AGENTS.md) para entender a arquitetura interna
2. Consulte os [Exemplos](examples/aspnet-core.md) para ver implementações funcionais
3. Abra uma issue no repositório do projeto

