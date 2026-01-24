using Traceability;
using Traceability.HttpClient;
using Traceability.Logging;
using Traceability.OpenTelemetry;

namespace Traceability.NetStandard2.Compile
{
    /// <summary>
    /// Este projeto não roda testes. Ele existe para garantir que o assembly netstandard2.0
    /// compila e expõe o core portátil esperado.
    /// </summary>
    internal static class Smoke
    {
        public static void CompileOnly()
        {
            // Core context
            _ = CorrelationContext.GetOrCreate();
            CorrelationContext.Clear();

            // HttpClient integration (handler is portable)
            var handler = new CorrelationIdHandler();

            // Logging integration compiles
            var enricher = new CorrelationIdEnricher();

            // OpenTelemetry surface compiles (ActivitySource creates activities only when listeners exist)
            using var activity = TraceabilityActivitySource.StartActivity("Smoke");
            
            // Suppress unused warnings
            _ = handler;
            _ = enricher;
        }
    }
}

