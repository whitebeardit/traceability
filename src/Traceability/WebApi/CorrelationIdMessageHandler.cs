#if NET48
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Traceability.Utilities;
#if NET48
using StaticTraceabilityOptionsProvider = Traceability.Configuration.StaticTraceabilityOptionsProvider;
#endif

namespace Traceability.WebApi
{
    /// <summary>
    /// MessageHandler para ASP.NET Web API que gerencia correlation-id automaticamente
    /// (sem criar spans). O tracing deve ser instrumentado externamente (OpenTelemetry por fora).
    /// </summary>
    public class CorrelationIdMessageHandler : DelegatingHandler
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
        /// Cria uma nova instância do CorrelationIdMessageHandler.
        /// Construtor sem parâmetros para compatibilidade.
        /// </summary>
        public CorrelationIdMessageHandler()
            : this(null, null, null)
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
            ICorrelationIdExtractor? extractor)
        {
            _optionsProvider = optionsProvider ?? StaticTraceabilityOptionsProvider.Instance;
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpRequestMessageCorrelationIdExtractor();
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

            var headerName = CorrelationIdHeader;

            // Extrair correlation-id do header
            string? correlationIdFromHeader = _extractor.ExtractCorrelationId(request, headerName);

            // Preserve existing context when handler isn't the first component in the pipeline (rare, but safe).
            var existingContextCorrelationId =
                CorrelationContext.TryGetValue(out var existing) ? existing : null;

            var decision = CorrelationPolicy.DecideInbound(
                options,
                _validator,
                correlationIdFromHeader,
                existingContextCorrelationId);

            CorrelationContext.Current = decision.CorrelationId;

            try
            {
                // Continua o pipeline usando async/await para melhor propagação de exceções
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                
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
                    catch (Exception ex)
                    {
                        // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                        TraceabilityDiagnostics.TryWriteException(
                            "Traceability.CorrelationIdMessageHandler.SetResponseHeader.Exception",
                            ex,
                            new { HeaderName = headerName });
                    }
                }

                return response!;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
#endif

