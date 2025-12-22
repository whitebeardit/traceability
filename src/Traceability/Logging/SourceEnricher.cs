using System;
using Serilog.Core;
using Serilog.Events;
using Traceability.Utilities;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que adiciona automaticamente o campo Source aos logs.
    /// Usa cache para reduzir alocações quando o mesmo source é usado em múltiplos logs.
    /// </summary>
    public class SourceEnricher : ILogEventEnricher
    {
        private const string SourcePropertyName = "Source";
        private readonly string _source;
        private LogEventProperty? _cachedProperty;

        /// <summary>
        /// Cria uma nova instância do SourceEnricher.
        /// </summary>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <exception cref="ArgumentNullException">Lançado quando source é null ou vazio.</exception>
        public SourceEnricher(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source), "Source cannot be null or empty");
            }

            // Sanitiza o source para garantir segurança em logs
            _source = TraceabilityUtilities.SanitizeSource(source);
        }

        /// <summary>
        /// Enriquece o log event com o source.
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Cria propriedade apenas uma vez e reutiliza (cache)
            if (_cachedProperty == null)
            {
                _cachedProperty = propertyFactory.CreateProperty(SourcePropertyName, _source);
            }

            logEvent.AddPropertyIfAbsent(_cachedProperty);
        }
    }
}



