using System;
using System.Diagnostics;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Serviço para extrair ActivityContext do header traceparent.
    /// Extrai a lógica duplicada presente em CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// </summary>
    internal static class TraceParentExtractor
    {
        /// <summary>
        /// Tenta extrair o ActivityContext do header traceparent.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnBeginRequest e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        /// <param name="traceparent">Valor do header traceparent (pode ser null). Usa Constants.HttpHeaders.TraceParent como nome do header.</param>
        /// <param name="tracestate">Valor do header tracestate (pode ser null). Usa Constants.HttpHeaders.TraceState como nome do header.</param>
        /// <returns>O ActivityContext extraído, ou default se não conseguir fazer parse.</returns>
        public static ActivityContext Extract(string? traceparent, string? tracestate)
        {
            ActivityContext parentFromTraceparent = default;
            if (!string.IsNullOrWhiteSpace(traceparent))
            {
                ActivityContext.TryParse(traceparent, tracestate, out parentFromTraceparent);
            }
            return parentFromTraceparent;
        }
    }
}

