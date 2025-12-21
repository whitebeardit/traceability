using System;
using System.Threading.Tasks;
using Traceability;

namespace Sample.Console.NetFramework
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Exemplo 1: Uso básico do CorrelationContext
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

            // Exemplo 3: Múltiplas operações com o mesmo correlation-id
            Console.WriteLine("=== Exemplo 3: Múltiplas operações com o mesmo correlation-id ===");
            var initialCorrelationId = CorrelationContext.Current;
            Console.WriteLine($"Operação 1 com CorrelationId: {initialCorrelationId}");

            await Task.Delay(100);

            Console.WriteLine($"Operação 2 com CorrelationId: {CorrelationContext.Current}");

            await Task.Delay(100);

            Console.WriteLine($"Operação 3 com CorrelationId: {CorrelationContext.Current}");

            Console.WriteLine($"Todas as operações usaram o mesmo Correlation ID: {CorrelationContext.Current == initialCorrelationId}");
            Console.WriteLine();

            Console.WriteLine("Exemplos concluídos!");
            Console.ReadKey();
        }
    }
}

