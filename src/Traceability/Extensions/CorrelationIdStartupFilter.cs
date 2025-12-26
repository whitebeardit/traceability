#if NET8_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Traceability.Extensions
{
    /// <summary>
    /// StartupFilter que registra automaticamente o CorrelationIdMiddleware no pipeline HTTP.
    /// Usado internamente quando AutoRegisterMiddleware está habilitado.
    /// </summary>
    internal class CorrelationIdStartupFilter : IStartupFilter
    {
        /// <summary>
        /// Configura o pipeline adicionando o CorrelationIdMiddleware antes de qualquer outro middleware.
        /// </summary>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseCorrelationId();
                next(app);
            };
        }
    }
}
#endif



