using System.Net.Http;
using Traceability;
using Traceability.Core;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para HttpClient.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Adiciona o correlation-id do contexto atual ao header da requisição HTTP.
        /// </summary>
        /// <param name="client">O cliente HTTP.</param>
        /// <param name="request">A mensagem de requisição HTTP.</param>
        /// <returns>O cliente HTTP para encadeamento.</returns>
        public static System.Net.Http.HttpClient AddCorrelationIdHeader(
            this System.Net.Http.HttpClient client,
            HttpRequestMessage request)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var headerName = Constants.HttpHeaders.CorrelationId;
                if (request.Headers.Contains(headerName))
                {
                    request.Headers.Remove(headerName);
                }
                request.Headers.Add(headerName, correlationId);
            }
            return client;
        }
    }
}
