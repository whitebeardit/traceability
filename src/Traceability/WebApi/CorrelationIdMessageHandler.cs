#if NET48
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Traceability.OpenTelemetry;
#if NET48
using StaticTraceabilityOptionsProvider = Traceability.Configuration.StaticTraceabilityOptionsProvider;
#endif

namespace Traceability.WebApi
{
    /// <summary>
    /// MessageHandler para ASP.NET Web API que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry, implementando funcionalidades que
    /// o OpenTelemetry faz automaticamente no .NET 8 mas não funcionam bem no .NET Framework 4.8.
    /// </summary>
    public class CorrelationIdMessageHandler : DelegatingHandler
    {
        private readonly ITraceabilityOptionsProvider _optionsProvider;
        private readonly ICorrelationIdValidator _validator;
        private readonly ICorrelationIdExtractor _extractor;
        private readonly IActivityFactory _activityFactory;
        private readonly IActivityTagProvider _tagProvider;
        
        private string CorrelationIdHeader
        {
            get
            {
                // Thread-safe: ler options uma vez para evitar race condition
                var options = _optionsProvider.GetOptions();
                return CorrelationPolicy.GetCorrelationIdHeaderName(options);
            }
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMessageHandler.
        /// Construtor sem parâmetros para compatibilidade.
        /// </summary>
        public CorrelationIdMessageHandler()
            : this(null, null, null, null, null)
        {
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMessageHandler.
        /// </summary>
        /// <param name="optionsProvider">Provider de opções (opcional, usa StaticTraceabilityOptionsProvider como padrão).</param>
        /// <param name="validator">Validador de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="extractor">Extrator de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="activityFactory">Factory de Activities (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="tagProvider">Provider de tags HTTP (opcional, cria instância padrão se não fornecido).</param>
        public CorrelationIdMessageHandler(
            ITraceabilityOptionsProvider? optionsProvider,
            ICorrelationIdValidator? validator,
            ICorrelationIdExtractor? extractor,
            IActivityFactory? activityFactory,
            IActivityTagProvider? tagProvider)
        {
            _optionsProvider = optionsProvider ?? StaticTraceabilityOptionsProvider.Instance;
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpRequestMessageCorrelationIdExtractor();
            _activityFactory = activityFactory ?? new TraceabilityActivityFactory();
            _tagProvider = tagProvider ?? new HttpActivityTagProvider();
        }

        /// <summary>
        /// Configura as opções do handler (deve ser chamado antes do handler ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            StaticTraceabilityOptionsProvider.Configure(options);
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Thread-safe: ler options uma vez para evitar race condition
            var options = _optionsProvider.GetOptions();

            // IMPORTANTE: Ler headers PRIMEIRO para decidir o trace-id e a hierarquia do span.
            // - X-Correlation-Id tem prioridade sobre traceparent
            // - traceparent define o parent para criar hierarquia real
            var headerName = CorrelationIdHeader;

            // Extrair correlation-id do header
            string? correlationIdFromHeader = _extractor.ExtractCorrelationId(request, headerName);

            // Tentar extrair parent context W3C do traceparent/tracestate (se existir)
            var traceparent = request.Headers.Contains(Constants.HttpHeaders.TraceParent)
                ? request.Headers.GetValues(Constants.HttpHeaders.TraceParent).FirstOrDefault()
                : null;
            var tracestate = request.Headers.Contains(Constants.HttpHeaders.TraceState)
                ? request.Headers.GetValues(Constants.HttpHeaders.TraceState).FirstOrDefault()
                : null;
            
            // Preserve existing context when handler isn't the first component in the pipeline (rare, but safe).
            var existingContextCorrelationId =
                CorrelationContext.TryGetValue(out var existing) ? existing : null;

            var decision = CorrelationPolicy.DecideInbound(
                options,
                _validator,
                correlationIdFromHeader,
                existingContextCorrelationId,
                traceparent,
                tracestate);

            // Garantir que o CorrelationContext esteja sempre setado (fallback),
            // mesmo quando o trace-id não puder ser aplicado ao Activity.
            CorrelationContext.Current = decision.CorrelationId;

            // Criar Activity (span) do request.
            // Observação: ActivitySource só cria Activity se houver ActivityListener.
            // Nome inicial será atualizado após base.SendAsync quando route template estiver disponível
            using var activity = decision.ParentContext != default
                ? _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server, decision.ParentContext)
                : _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server);

            if (activity != null)
            {
                _tagProvider.AddRequestTags(activity, request);
            }

            try
            {
                // Continua o pipeline usando async/await para melhor propagação de exceções
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                
                if (activity != null)
                {
                    // Tentar obter route template e atualizar DisplayName
                    var template = RouteTemplateHelper.TryGetRouteTemplate(request);
                    if (!string.IsNullOrEmpty(template))
                    {
                        var method = request.Method != null ? request.Method.Method : "GET";
                        var displayName = RouteTemplateHelper.NormalizeDisplayName(method, template);
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            activity.DisplayName = displayName!;
                        }
                    }
                    else
                    {
                        // Se não encontrou template, tentar inferir do path
                        var path = request.RequestUri != null ? request.RequestUri.AbsolutePath : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            var method = request.Method != null ? request.Method.Method : "GET";
                            template = RouteTemplateHelper.TryInferRouteTemplateFromPath(path, method);
                            if (!string.IsNullOrEmpty(template))
                            {
                                var displayName = RouteTemplateHelper.NormalizeDisplayName(method, template);
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    activity.DisplayName = displayName!;
                                }
                            }
                        }
                    }

                    _tagProvider.AddResponseTags(activity, response);
                }
                
                // Adiciona o correlation-id no header da resposta
                if (response != null && !string.IsNullOrEmpty(decision.CorrelationId))
                {
                    try
                    {
                        // Verifica se o header já existe antes de adicionar
                        if (response.Headers.Contains(headerName))
                        {
                            response.Headers.Remove(headerName);
                        }
                        response.Headers.Add(headerName, decision.CorrelationId);
                    }
                    catch
                    {
                        // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                    }
                }

                return response!;
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    _tagProvider.AddErrorTags(activity, ex);
                }
                throw;
            }
        }
    }
}
#endif

