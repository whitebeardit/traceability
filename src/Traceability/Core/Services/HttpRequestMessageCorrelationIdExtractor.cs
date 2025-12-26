#if NET48
using System.Linq;
using System.Net.Http;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de ICorrelationIdExtractor para ASP.NET Framework (NET48).
    /// Extrai correlation-id de HttpRequestMessage e HttpRequest.
    /// </summary>
    internal class HttpRequestMessageCorrelationIdExtractor : Core.Interfaces.ICorrelationIdExtractor
    {
        /// <summary>
        /// Tenta extrair o correlation-id do header da requisição.
        /// Lógica copiada EXATAMENTE de CorrelationIdMessageHandler.SendAsync e CorrelationIdHttpModule.OnBeginRequest.
        /// </summary>
        public string? ExtractCorrelationId(object request, string headerName)
        {
            if (request == null) return null;

            // Suporta HttpRequestMessage (System.Net.Http) e HttpRequest (System.Web)
            if (request is HttpRequestMessage httpRequestMessage)
            {
                if (httpRequestMessage.Headers.Contains(headerName))
                {
                    var values = httpRequestMessage.Headers.GetValues(headerName);
                    if (values != null)
                    {
                        return values.FirstOrDefault();
                    }
                }
            }
            else if (request is System.Web.HttpRequest httpRequest)
            {
                return httpRequest.Headers[headerName];
            }

            return null;
        }
    }
}
#endif

