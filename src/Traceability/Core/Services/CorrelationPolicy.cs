using Traceability.Configuration;
using Traceability.Core.Interfaces;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Centraliza a política de resolução do correlation-id.
    /// Regra de prioridade:
    /// AlwaysGenerateNew > CorrelationId (header/override) > generate
    /// </summary>
    internal static class CorrelationPolicy
    {
        internal readonly struct CorrelationDecision
        {
            public CorrelationDecision(
                string headerName,
                string correlationId)
            {
                HeaderName = headerName;
                CorrelationId = correlationId;
            }

            public string HeaderName { get; }
            public string CorrelationId { get; }
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
        /// Resolve o correlation-id efetivo.
        /// </summary>
        public static CorrelationDecision DecideInbound(
            TraceabilityOptions options,
            ICorrelationIdValidator validator,
            string? correlationIdFromHeader,
            string? existingContextCorrelationId)
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

            var effectiveCorrelationId = CorrelationIdResolver.Resolve(options, overrideCorrelationId);

            return new CorrelationDecision(headerName, effectiveCorrelationId);
        }
    }
}



