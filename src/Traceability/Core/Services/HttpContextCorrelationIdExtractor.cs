#if NET8_0
using Microsoft.AspNetCore.Http;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de ICorrelationIdExtractor para ASP.NET Core (NET8).
    /// Extrai correlation-id de HttpContext.
    /// </summary>
    internal class HttpContextCorrelationIdExtractor : Core.Interfaces.ICorrelationIdExtractor
    {
        /// <summary>
        /// Tenta extrair o correlation-id do header da requisição.
        /// Lógica copiada EXATAMENTE de CorrelationIdMiddleware.InvokeAsync.
        /// </summary>
        public string? ExtractCorrelationId(object request, string headerName)
        {
            if (request == null) return null;

            var httpContext = request as HttpContext;
            if (httpContext == null) return null;

            return httpContext.Request.Headers[headerName].FirstOrDefault();
        }
    }
}
#endif

