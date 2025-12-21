#if NET8_0
using Microsoft.AspNetCore.Builder;
using Traceability.Middleware;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para IApplicationBuilder.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adiciona o middleware de correlation-id ao pipeline da aplicação.
        /// </summary>
        /// <param name="app">O construtor da aplicação.</param>
        /// <returns>O construtor da aplicação para encadeamento.</returns>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
#endif

