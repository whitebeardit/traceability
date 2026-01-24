#if NET48
using System;
using System.Web;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Traceability.Utilities;
#if NET48
using StaticTraceabilityOptionsProvider = Traceability.Configuration.StaticTraceabilityOptionsProvider;
#endif

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente
    /// (sem criar spans). O tracing deve ser instrumentado externamente (OpenTelemetry por fora).
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {
        private readonly ITraceabilityOptionsProvider _optionsProvider;
        private readonly ICorrelationIdValidator _validator;
        private readonly ICorrelationIdExtractor _extractor;

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
        /// Cria uma nova instância do CorrelationIdHttpModule.
        /// Construtor sem parâmetros para compatibilidade com registro automático via DynamicModuleUtility.
        /// </summary>
        public CorrelationIdHttpModule()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHttpModule.
        /// </summary>
        /// <param name="optionsProvider">Provider de opções (opcional, usa StaticTraceabilityOptionsProvider como padrão).</param>
        /// <param name="validator">Validador de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="extractor">Extrator de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="activityFactory">Factory de Activities (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="tagProvider">Provider de tags HTTP (opcional, cria instância padrão se não fornecido).</param>
        public CorrelationIdHttpModule(
            ITraceabilityOptionsProvider? optionsProvider,
            ICorrelationIdValidator? validator,
            ICorrelationIdExtractor? extractor)
        {
            _optionsProvider = optionsProvider ?? StaticTraceabilityOptionsProvider.Instance;
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpRequestMessageCorrelationIdExtractor();
        }

        /// <summary>
        /// Configura as opções do módulo (deve ser chamado antes do módulo ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            StaticTraceabilityOptionsProvider.Configure(options);
        }


        /// <summary>
        /// Inicializa o módulo.
        /// </summary>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var request = context.Request;

            var headerName = CorrelationIdHeader;

            // Extrair correlation-id do header
            var correlationIdFromHeader = _extractor.ExtractCorrelationId(request, headerName);

            // Thread-safe: ler options uma vez para evitar race condition
            var options = _optionsProvider.GetOptions();

            // Preserve existing context when module isn't the first component in the pipeline (rare, but safe).
            var existingContextCorrelationId =
                CorrelationContext.TryGetValue(out var existing) ? existing : null;

            var decision = CorrelationPolicy.DecideInbound(
                options,
                _validator,
                correlationIdFromHeader,
                existingContextCorrelationId);

            // Always set fallback context (so invalid formats can still work when validation disabled)
            CorrelationContext.Current = decision.CorrelationId;
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            var headerName = CorrelationIdHeader;
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                try
                {
                    // PreSendRequestHeaders é chamado antes de enviar headers, então ainda podemos modificá-los
                    response.Headers[headerName] = correlationId;
                }
                catch (Exception ex)
                {
                    // Ignora exceções ao adicionar header (pode ocorrer em casos raros)
                    TraceabilityDiagnostics.TryWriteException(
                        "Traceability.CorrelationIdHttpModule.SetResponseHeader.Exception",
                        ex,
                        new { HeaderName = headerName });
                }
            }
        }

        /// <summary>
        /// Libera recursos.
        /// </summary>
        public void Dispose()
        {
            // Nada a fazer
        }
    }
}
#endif

