# Exemplos - Console Application

Exemplos práticos de uso do Traceability em aplicações console.

## Exemplo Básico

```csharp
using Traceability;

// O correlation-id é gerado automaticamente quando necessário
var correlationId = CorrelationContext.Current;

// Usar em logs, chamadas HTTP, etc.
Console.WriteLine($"Correlation ID: {correlationId}");
```

## Exemplo com Serilog

```csharp
using Traceability.Extensions;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}")
    .CreateLogger();

// Gerar correlation-id
var correlationId = CorrelationContext.GetOrCreate();

// Logs incluem correlation-id automaticamente
Log.Information("Processando tarefa");
Log.Information("Tarefa concluída");
```

**Output:**
```
[14:23:45 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Processando tarefa
[14:23:46 INF] ConsoleApp a1b2c3d4e5f6789012345678901234ab Tarefa concluída
```

## Exemplo Completo (.NET 8)

```csharp
using Traceability;
using Traceability.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

// Configurar Serilog com Traceability (Source + CorrelationId)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WithTraceability("ConsoleApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Source} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<Program>();

// Exemplo 1: Uso básico
Console.WriteLine("=== Exemplo 1: Uso básico do CorrelationContext ===");
var correlationId = CorrelationContext.GetOrCreate();
Console.WriteLine($"Correlation ID gerado: {correlationId}");
Console.WriteLine($"Correlation ID atual: {CorrelationContext.Current}");
Console.WriteLine();

// Exemplo 2: Logging com correlation-id
Console.WriteLine("=== Exemplo 2: Logging com correlation-id ===");
logger.LogInformation("Mensagem de log com correlation-id automático");
Console.WriteLine();

// Exemplo 3: Correlation-id preservado em operações assíncronas
Console.WriteLine("=== Exemplo 3: Correlation-id preservado em operações assíncronas ===");
var correlationIdBefore = CorrelationContext.Current;
logger.LogInformation("Correlation ID antes da operação assíncrona: {CorrelationId}", correlationIdBefore);

await Task.Delay(100);

var correlationIdAfter = CorrelationContext.Current;
logger.LogInformation("Correlation ID após operação assíncrona: {CorrelationId}", correlationIdAfter);
Console.WriteLine($"Correlation ID preservado: {correlationIdBefore == correlationIdAfter}");
Console.WriteLine();

Log.CloseAndFlush();
```

## Exemplo Completo (.NET Framework 4.8)

```csharp
using System;
using System.Threading.Tasks;
using Traceability;

namespace Sample.Console.NetFramework
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Exemplo 1: Uso básico
            Console.WriteLine("=== Exemplo 1: Uso básico do CorrelationContext ===");
            var correlationId = CorrelationContext.GetOrCreate();
            Console.WriteLine($"Correlation ID gerado: {correlationId}");
            Console.WriteLine($"Correlation ID atual: {CorrelationContext.Current}");
            Console.WriteLine();

            // Exemplo 2: Correlation-id preservado em operações assíncronas
            Console.WriteLine("=== Exemplo 2: Correlation-id preservado em operações assíncronas ===");
            var correlationIdBefore = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID antes da operação assíncrona: {correlationIdBefore}");

            await Task.Delay(100);

            var correlationIdAfter = CorrelationContext.Current;
            Console.WriteLine($"Correlation ID após operação assíncrona: {correlationIdAfter}");
            Console.WriteLine($"Correlation ID preservado: {correlationIdBefore == correlationIdAfter}");
            Console.WriteLine();

            Console.WriteLine("Exemplos concluídos!");
            Console.ReadKey();
        }
    }
}
```

## Exemplo Completo

Veja o exemplo completo em `samples/Sample.Console.Net8/` e `samples/Sample.Console.NetFramework/`.


