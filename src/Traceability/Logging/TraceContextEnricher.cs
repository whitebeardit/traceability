using System.Diagnostics;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que adiciona informações de trace context do Activity.Current:
    /// TraceId, SpanId e ParentSpanId (OpenTelemetry).
    /// </summary>
    public class TraceContextEnricher : ILogEventEnricher
    {
        private const string TraceIdPropertyName = "TraceId";
        private const string SpanIdPropertyName = "SpanId";
        private const string ParentSpanIdPropertyName = "ParentSpanId";

        private static readonly AsyncLocal<(string? TraceId, string? SpanId, string? ParentSpanId, LogEventProperty? TraceIdProp, LogEventProperty? SpanIdProp, LogEventProperty? ParentSpanIdProp)> _cache = new();

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null || propertyFactory == null)
                return;

            var activity = Activity.Current;
            if (activity == null)
                return;

            var traceId = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();

            // Para spans root, ParentSpanId pode ser default (000...); preferimos string vazia para manter compatibilidade com template textual.
            var parentSpanId = activity.ParentSpanId == default ? string.Empty : activity.ParentSpanId.ToString();

            var cached = _cache.Value;

            if (cached.TraceId == traceId && cached.SpanId == spanId && cached.ParentSpanId == parentSpanId &&
                cached.TraceIdProp != null && cached.SpanIdProp != null && cached.ParentSpanIdProp != null)
            {
                logEvent.AddPropertyIfAbsent(cached.TraceIdProp);
                logEvent.AddPropertyIfAbsent(cached.SpanIdProp);
                logEvent.AddPropertyIfAbsent(cached.ParentSpanIdProp);
                return;
            }

            var traceIdProp = propertyFactory.CreateProperty(TraceIdPropertyName, traceId);
            var spanIdProp = propertyFactory.CreateProperty(SpanIdPropertyName, spanId);
            var parentSpanIdProp = propertyFactory.CreateProperty(ParentSpanIdPropertyName, parentSpanId);

            _cache.Value = (traceId, spanId, parentSpanId, traceIdProp, spanIdProp, parentSpanIdProp);

            logEvent.AddPropertyIfAbsent(traceIdProp);
            logEvent.AddPropertyIfAbsent(spanIdProp);
            logEvent.AddPropertyIfAbsent(parentSpanIdProp);
        }
    }
}



