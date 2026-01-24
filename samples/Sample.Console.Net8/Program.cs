using Traceability;
using Traceability.HttpClient;
using Traceability.Logging;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Configurar Serilog com CorrelationIdEnricher
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.With<CorrelationIdEnricher>()
    // When OpenTelemetry is configured externally, Activity.Current exists and these enrichers enable log-to-trace correlation.
    .Enrich.With<TraceContextEnricher>()
    .Enrich.With<RouteNameEnricher>()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {TraceId} {SpanId} {RouteName} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog();
});

var logger = loggerFactory.CreateLogger<Program>();

// Exemplo 1: Uso básico do CorrelationContext
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

// Exemplo 4: Múltiplas operações com o mesmo correlation-id
Console.WriteLine("=== Exemplo 4: Múltiplas operações com o mesmo correlation-id ===");
var initialCorrelationId = CorrelationContext.Current;
logger.LogInformation("Operação 1 com CorrelationId: {CorrelationId}", initialCorrelationId);

await Task.Delay(100);

logger.LogInformation("Operação 2 com CorrelationId: {CorrelationId}", CorrelationContext.Current);

await Task.Delay(100);

logger.LogInformation("Operação 3 com CorrelationId: {CorrelationId}", CorrelationContext.Current);

Console.WriteLine($"Todas as operações usaram o mesmo Correlation ID: {CorrelationContext.Current == initialCorrelationId}");
Console.WriteLine();

// Exemplo 5: Novo correlation-id em contexto assíncrono isolado
Console.WriteLine("=== Exemplo 5: Isolamento de correlation-id em contextos assíncronos ===");
var mainCorrelationId = CorrelationContext.Current;

var task = Task.Run(async () =>
{
    // Este contexto assíncrono terá seu próprio correlation-id
    var taskCorrelationId = CorrelationContext.GetOrCreate();
    await Task.Delay(50);
    logger.LogInformation("Task executada com CorrelationId: {CorrelationId}", taskCorrelationId);
    return taskCorrelationId;
});

var taskCorrelationId = await task;
Console.WriteLine($"Correlation ID principal: {mainCorrelationId}");
Console.WriteLine($"Correlation ID da task: {taskCorrelationId}");
Console.WriteLine($"São diferentes: {mainCorrelationId != taskCorrelationId}");
Console.WriteLine();

Console.WriteLine("Exemplos concluídos!");
Log.CloseAndFlush();
