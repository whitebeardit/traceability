#if NET8_0
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;

namespace Traceability.Middleware
{
    /// <summary>
    /// Middleware para ASP.NET Core que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry quando OpenTelemetry não está configurado.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TraceabilityOptions _options;
        private readonly ICorrelationIdValidator _validator;
        private readonly ICorrelationIdExtractor _extractor;
        private readonly IActivityFactory _activityFactory;
        private readonly IActivityTagProvider _tagProvider;

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
            ICorrelationIdExtractor? extractor = null,
            IActivityFactory? activityFactory = null,
            IActivityTagProvider? tagProvider = null)
        {
            _next = next;
            _options = options?.Value ?? new TraceabilityOptions();
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpContextCorrelationIdExtractor();
            _activityFactory = activityFactory ?? new TraceabilityActivityFactory();
            _tagProvider = tagProvider ?? new HttpActivityTagProvider();
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

            // If OpenTelemetry isn't configured (no Activity.Current), respect inbound traceparent/tracestate as well.
            var traceparent = context.Request.Headers[Constants.HttpHeaders.TraceParent].FirstOrDefault();
            var tracestate = context.Request.Headers[Constants.HttpHeaders.TraceState].FirstOrDefault();

            var decision = CorrelationPolicy.DecideInbound(
                options,
                _validator,
                correlationIdFromHeader,
                existingContextCorrelationId,
                traceparent,
                tracestate);

            // Always set fallback context; Activity.TraceId may not be overridable when OTel is configured.
            CorrelationContext.Current = decision.CorrelationId;

            // Create Activity only when none exists (avoid duplication with OpenTelemetry automatic instrumentation)
            Activity? activity = null;
            if (Activity.Current == null)
            {
                activity = decision.ParentContext != default
                    ? _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server, decision.ParentContext)
                    : _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server);

                if (activity != null)
                {
                    _tagProvider.AddRequestTags(activity, context);
                }
            }

            // Adiciona o correlation-id no header da resposta (antes de chamar o próximo middleware)
            // Verifica se ainda é possível modificar headers
            if (!context.Response.HasStarted)
            {
                try
                {
                    context.Response.Headers[decision.HeaderName] = decision.CorrelationId;
                }
                catch
                {
                    // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                }
            }

            try
            {
                // Continua o pipeline
                await _next(context);

                // Adicionar status code ao Activity
                var currentActivity = Activity.Current ?? activity;
                if (currentActivity != null)
                {
                    _tagProvider.AddResponseTags(currentActivity, context);
                }
            }
            catch (Exception ex)
            {
                // Adicionar exceção ao Activity
                var currentActivity = Activity.Current ?? activity;
                if (currentActivity != null)
                {
                    _tagProvider.AddErrorTags(currentActivity, ex);
                }
                throw;
            }
            finally
            {
                // Parar Activity se foi criado por nós
                activity?.Stop();
            }
        }
    }
}
#endif

