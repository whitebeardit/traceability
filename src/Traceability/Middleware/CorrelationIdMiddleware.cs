#if NET8_0
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Traceability;

namespace Traceability.Middleware
{
    /// <summary>
    /// Middleware para ASP.NET Core que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMiddleware.
        /// </summary>
        /// <param name="next">O próximo middleware no pipeline.</param>
        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Tenta obter o correlation-id do header da requisição
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
            
            // Se não existir, gera um novo
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir, usa o valor do header
                CorrelationContext.Current = correlationId;
            }

            // Adiciona o correlation-id no header da resposta
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            // Continua o pipeline
            await _next(context);
        }
    }
}
#endif

