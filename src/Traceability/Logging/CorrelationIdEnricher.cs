using Serilog.Core;
using Serilog.Events;
using Traceability;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que adiciona automaticamente o correlation-id aos logs.
    /// </summary>
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private const string CorrelationIdPropertyName = "CorrelationId";

        /// <summary>
        /// Enriquece o log event com o correlation-id do contexto atual.
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var correlationId = CorrelationContext.Current;
            var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}

