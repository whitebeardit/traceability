using System.Threading;
using Serilog.Core;
using Serilog.Events;
using Traceability;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que adiciona automaticamente o correlation-id aos logs.
    /// Usa cache para reduzir alocações quando o mesmo correlation-id é usado em múltiplos logs.
    /// </summary>
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private const string CorrelationIdPropertyName = "CorrelationId";
        private static readonly AsyncLocal<(string? CorrelationId, LogEventProperty? Property)> _cache = new();

        /// <summary>
        /// Enriquece o log event com o correlation-id do contexto atual.
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (!CorrelationContext.TryGetValue(out var correlationId) || string.IsNullOrEmpty(correlationId))
            {
                // Se não houver correlation-id, não adiciona nada ao log
                return;
            }

            var cached = _cache.Value;
            
            // Reutiliza propriedade se o correlation-id não mudou
            if (cached.CorrelationId == correlationId && cached.Property != null)
            {
                logEvent.AddPropertyIfAbsent(cached.Property);
                return;
            }
            
            // Cria nova propriedade e atualiza cache
            var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
            _cache.Value = (correlationId, property);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}

