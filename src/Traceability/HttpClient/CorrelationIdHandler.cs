using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if NET8_0
using Microsoft.Extensions.Options;
using Traceability.Configuration;
#endif
using Traceability;

namespace Traceability.HttpClient
{
    /// <summary>
    /// DelegatingHandler que adiciona automaticamente o correlation-id nos headers das requisições HTTP.
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
#if NET8_0
        private readonly TraceabilityOptions? _options;
        private string CorrelationIdHeader
        {
            get
            {
                var headerName = _options?.HeaderName;
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    return "X-Correlation-Id";
                }
                return headerName;
            }
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHandler.
        /// </summary>
        /// <param name="options">Opções de configuração (opcional, injetado via DI).</param>
        public CorrelationIdHandler(IOptions<TraceabilityOptions>? options = null)
        {
            _options = options?.Value;
        }
#else
        private const string CorrelationIdHeader = "X-Correlation-Id";
#endif

        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var headerName = CorrelationIdHeader;
                // Verifica se o header existe antes de remover (evita operação desnecessária)
                if (request.Headers.Contains(headerName))
                {
                    request.Headers.Remove(headerName);
                }
                request.Headers.Add(headerName, correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }

#if NET8_0
        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header.
        /// Versão síncrona para .NET 8+.
        /// </summary>
        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var headerName = CorrelationIdHeader;
                // Verifica se o header existe antes de remover (evita operação desnecessária)
                if (request.Headers.Contains(headerName))
                {
                    request.Headers.Remove(headerName);
                }
                request.Headers.Add(headerName, correlationId);
            }

            return base.Send(request, cancellationToken);
        }
#endif
    }
}

