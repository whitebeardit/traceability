using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Traceability;

namespace Traceability.HttpClient
{
    /// <summary>
    /// DelegatingHandler que adiciona automaticamente o correlation-id nos headers das requisições HTTP.
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";

        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = CorrelationContext.Current;
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Remove(CorrelationIdHeader);
                request.Headers.Add(CorrelationIdHeader, correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }

#if NET8_0
        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header.
        /// Versão assíncrona para .NET 8+.
        /// </summary>
        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = CorrelationContext.Current;
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Remove(CorrelationIdHeader);
                request.Headers.Add(CorrelationIdHeader, correlationId);
            }

            return base.Send(request, cancellationToken);
        }
#endif
    }
}

