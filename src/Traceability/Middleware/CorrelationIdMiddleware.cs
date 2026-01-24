#if NET8_0
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Traceability.Utilities;

namespace Traceability.Middleware
{
    /// <summary>
    /// Middleware para ASP.NET Core que gerencia correlation-id automaticamente
    /// (sem criar spans). O tracing deve ser instrumentado externamente (OpenTelemetry por fora).
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TraceabilityOptions _options;
        private readonly ICorrelationIdValidator _validator;
        private readonly ICorrelationIdExtractor _extractor;

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMiddleware.
        /// </summary>
        /// <param name="next">O próximo middleware no pipeline.</param>
        /// <param name="options">Opções de configuração (opcional, usa padrão se não fornecido).</param>
        /// <param name="validator">Validador de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="extractor">Extrator de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="activityFactory">Factory de Activities (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="tagProvider">Provider de tags HTTP (opcional, cria instância padrão se não fornecido).</param>
        public CorrelationIdMiddleware(
            RequestDelegate next,
            IOptions<TraceabilityOptions>? options = null,
            ICorrelationIdValidator? validator = null,
            ICorrelationIdExtractor? extractor = null)
        {
            _next = next;
            _options = options?.Value ?? new TraceabilityOptions();
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpContextCorrelationIdExtractor();
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var options = _options;

            var headerName = CorrelationPolicy.GetCorrelationIdHeaderName(options);
            var correlationIdFromHeader = _extractor.ExtractCorrelationId(context, headerName);

            // Preserve existing context when middleware isn't the first component in the pipeline.
            var existingContextCorrelationId =
                CorrelationContext.TryGetValue(out var existing) ? existing : null;

            var decision = CorrelationPolicy.DecideInbound(
                options,
                _validator,
                correlationIdFromHeader,
                existingContextCorrelationId);

            // Always set fallback context; Activity.TraceId may not be overridable when OTel is configured.
            CorrelationContext.Current = decision.CorrelationId;

            // Adiciona o correlation-id no header da resposta (antes de chamar o próximo middleware)
            // Verifica se ainda é possível modificar headers
            if (!context.Response.HasStarted)
            {
                try
                {
                    context.Response.Headers[decision.HeaderName] = decision.CorrelationId;
                }
                catch (Exception ex)
                {
                    // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                    TraceabilityDiagnostics.TryWriteException(
                        "Traceability.CorrelationIdMiddleware.SetResponseHeader.Exception",
                        ex,
                        new { HeaderName = decision.HeaderName });
                }
            }

            try
            {
                // Continua o pipeline
                await _next(context);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
#endif

