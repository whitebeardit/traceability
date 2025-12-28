using System.Diagnostics;
using Traceability.Configuration;
using Traceability.Core.Interfaces;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Centraliza a política de resolução do correlation-id e construção do parent ActivityContext.
    /// Mantém a regra de prioridade:
    /// AlwaysGenerateNew > CorrelationId (header/override) > traceparent > generate
    /// </summary>
    internal static class CorrelationPolicy
    {
        internal readonly struct CorrelationDecision
        {
            public CorrelationDecision(
                string headerName,
                string correlationId,
                ActivityContext parentFromTraceparent,
                ActivityContext parentContext)
            {
                HeaderName = headerName;
                CorrelationId = correlationId;
                ParentFromTraceparent = parentFromTraceparent;
                ParentContext = parentContext;
            }

            public string HeaderName { get; }
            public string CorrelationId { get; }
            public ActivityContext ParentFromTraceparent { get; }
            public ActivityContext ParentContext { get; }
        }

        public static string GetCorrelationIdHeaderName(TraceabilityOptions options)
        {
            var headerName = options.HeaderName;
            return string.IsNullOrWhiteSpace(headerName) ? Constants.HttpHeaders.CorrelationId : headerName;
        }

        public static string? NormalizeCorrelationIdFromHeader(
            TraceabilityOptions options,
            ICorrelationIdValidator validator,
            string? correlationIdFromHeader)
        {
            if (string.IsNullOrEmpty(correlationIdFromHeader))
            {
                return null;
            }

            return validator.Validate(correlationIdFromHeader, options) ? correlationIdFromHeader : null;
        }

        /// <summary>
        /// Resolve o correlation-id efetivo e o parentContext para criação de Activity.
        /// </summary>
        public static CorrelationDecision DecideInbound(
            TraceabilityOptions options,
            ICorrelationIdValidator validator,
            string? correlationIdFromHeader,
            string? existingContextCorrelationId,
            string? traceparent,
            string? tracestate)
        {
            var headerName = GetCorrelationIdHeaderName(options);

            var normalizedHeader = NormalizeCorrelationIdFromHeader(options, validator, correlationIdFromHeader);

            // Never overwrite an existing correlation context unless AlwaysGenerateNew is enabled.
            // (This matches existing package behavior and helps when middleware is not first in pipeline.)
            var overrideCorrelationId = normalizedHeader;
            if (!options.AlwaysGenerateNew && string.IsNullOrEmpty(overrideCorrelationId) && !string.IsNullOrEmpty(existingContextCorrelationId))
            {
                overrideCorrelationId = existingContextCorrelationId;
            }

            var parentFromTraceparent = TraceParentExtractor.Extract(traceparent, tracestate);

            var effectiveCorrelationId = CorrelationIdResolver.Resolve(options, overrideCorrelationId, parentFromTraceparent);

            // If we used an override value (header or existing context), we must NOT keep the real parentFromTraceparent.
            // Treat it as an explicit override for ActivityContextBuilder's purpose.
            var parentContext = ActivityContextBuilder.BuildParentContext(
                options,
                effectiveCorrelationId,
                overrideCorrelationId,
                parentFromTraceparent);

            return new CorrelationDecision(headerName, effectiveCorrelationId, parentFromTraceparent, parentContext);
        }
    }
}


