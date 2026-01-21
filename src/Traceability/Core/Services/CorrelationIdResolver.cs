using System;
using System.Diagnostics;
using Traceability;
using Traceability.Configuration;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Serviço para resolver correlation-id seguindo a ordem de prioridade.
    /// Extrai a lógica duplicada de resolução presente em CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// Prioridade: AlwaysGenerateNew > correlation header > traceparent > generate
    /// </summary>
    internal static class CorrelationIdResolver
    {
        /// <summary>
        /// Resolve o correlation-id efetivo seguindo a ordem de prioridade.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnBeginRequest e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        /// <param name="headerValue">Valor do header de correlation-id (pode ser null).</param>
        /// <param name="parentFromTraceparent">Contexto pai extraído do traceparent (pode ser default).</param>
        /// <returns>O correlation-id efetivo a ser usado.</returns>
        public static string Resolve(TraceabilityOptions options, string? headerValue, ActivityContext parentFromTraceparent)
        {
            // Decide effective correlation-id for this request:
            // Priority: AlwaysGenerateNew > correlation header > generate
            // Correlation-ID is independent from OpenTelemetry trace ID
            if (options.AlwaysGenerateNew)
            {
                return Guid.NewGuid().ToString("N");
            }
            else if (!string.IsNullOrEmpty(headerValue))
            {
                return headerValue!;
            }
            else
            {
                return Guid.NewGuid().ToString("N");
            }
        }
    }
}

