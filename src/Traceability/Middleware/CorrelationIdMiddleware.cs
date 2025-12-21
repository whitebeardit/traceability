#if NET8_0
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Traceability;
using Traceability.Configuration;

namespace Traceability.Middleware
{
    /// <summary>
    /// Middleware para ASP.NET Core que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TraceabilityOptions _options;

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMiddleware.
        /// </summary>
        /// <param name="next">O próximo middleware no pipeline.</param>
        /// <param name="options">Opções de configuração (opcional, usa padrão se não fornecido).</param>
        public CorrelationIdMiddleware(RequestDelegate next, IOptions<TraceabilityOptions>? options = null)
        {
            _next = next;
            _options = options?.Value ?? new TraceabilityOptions();
        }

        /// <summary>
        /// Valida o formato do correlation-id se a validação estiver habilitada.
        /// </summary>
        private bool IsValidCorrelationId(string? correlationId)
        {
            if (!_options.ValidateCorrelationIdFormat)
                return true;

            if (string.IsNullOrEmpty(correlationId))
                return false;

            // Valida tamanho máximo (128 caracteres)
            if (correlationId.Length > 128)
                return false;

            return true;
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var headerName = _options.HeaderName;
            
            // Tenta obter o correlation-id do header da requisição
            var correlationId = context.Request.Headers[headerName].FirstOrDefault();
            
            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationId) && !IsValidCorrelationId(correlationId))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }
            
            // Se não existir ou AlwaysGenerateNew estiver habilitado, gera um novo
            if (string.IsNullOrEmpty(correlationId) || _options.AlwaysGenerateNew)
            {
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir, usa o valor do header
                CorrelationContext.Current = correlationId;
            }

            // Adiciona o correlation-id no header da resposta
            context.Response.Headers[headerName] = correlationId;

            // Continua o pipeline
            await _next(context);
        }
    }
}
#endif

