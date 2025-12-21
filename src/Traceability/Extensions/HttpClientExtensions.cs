using System.Net.Http;
using Traceability;

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
            var correlationId = CorrelationContext.Current;
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Remove("X-Correlation-Id");
                request.Headers.Add("X-Correlation-Id", correlationId);
            }
            return client;
        }
    }
}

