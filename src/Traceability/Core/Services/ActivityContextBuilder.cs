using System;
using System.Diagnostics;
using Traceability.Configuration;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Serviço para construir ActivityContext parent.
    /// Extrai a lógica duplicada de construção presente em CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// </summary>
    internal static class ActivityContextBuilder
    {
        /// <summary>
        /// Constrói o ActivityContext parent seguindo a lógica de prioridade.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnBeginRequest e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        /// <param name="correlationId">Correlation-id efetivo.</param>
        /// <param name="correlationIdFromHeader">Correlation-id do header (pode ser null).</param>
        /// <param name="parentFromTraceparent">Contexto pai extraído do traceparent (pode ser default).</param>
        /// <returns>O ActivityContext parent a ser usado, ou default se não houver parent.</returns>
        public static ActivityContext BuildParentContext(
            TraceabilityOptions options,
            string correlationId,
            string? correlationIdFromHeader,
            ActivityContext parentFromTraceparent)
        {
            // Build parent context:
            // - If traceparent exists and no correlation header override, keep real parent
            // - Correlation-ID is independent from OpenTelemetry trace ID, so we don't create artificial ActivityContext
            if (!options.AlwaysGenerateNew && string.IsNullOrEmpty(correlationIdFromHeader) && parentFromTraceparent != default)
            {
                return parentFromTraceparent;
            }

            return default;
        }
    }
}

