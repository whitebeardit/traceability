using System;
using System.Net.Http;
using System.Threading.Tasks;
using Traceability;
using Traceability.HttpClient;

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

            // Exemplo 2: HttpClient com correlation-id
            Console.WriteLine("=== Exemplo 2: HttpClient com correlation-id ===");
            var httpClient = TraceableHttpClientFactory.Create("https://jsonplaceholder.typicode.com/");

            try
            {
                var response = await httpClient.GetAsync("posts/1");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Chamada HTTP realizada com sucesso. Response length: {content.Length}");
                Console.WriteLine("O correlation-id foi automaticamente adicionado ao header.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao realizar chamada HTTP: {ex.Message}");
            }
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

