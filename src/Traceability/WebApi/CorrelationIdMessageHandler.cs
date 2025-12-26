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
using Traceability.OpenTelemetry;

namespace Traceability.WebApi
{
    /// <summary>
    /// MessageHandler para ASP.NET Web API que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry, implementando funcionalidades que
    /// o OpenTelemetry faz automaticamente no .NET 8 mas não funcionam bem no .NET Framework 4.8.
    /// </summary>
    public class CorrelationIdMessageHandler : DelegatingHandler
    {
        private static volatile TraceabilityOptions _options = new TraceabilityOptions();
        private static readonly object _optionsLock = new object();
        
        private string CorrelationIdHeader
        {
            get
            {
                // Thread-safe: ler _options uma vez para evitar race condition
                var options = _options;
                var headerName = options.HeaderName;
                return string.IsNullOrWhiteSpace(headerName) ? "X-Correlation-Id" : headerName;
            }
        }

        /// <summary>
        /// Configura as opções do handler (deve ser chamado antes do handler ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            lock (_optionsLock)
            {
                _options = options;
            }
        }

        /// <summary>
        /// Valida o formato do correlation-id se a validação estiver habilitada.
        /// </summary>
        private bool IsValidCorrelationId(string? correlationId)
        {
            // Thread-safe: ler _options uma vez para evitar race condition
            var options = _options;
            if (!options.ValidateCorrelationIdFormat)
                return true;

            if (string.IsNullOrEmpty(correlationId))
                return false;

            // Valida tamanho máximo (128 caracteres)
            if (correlationId!.Length > 128)
                return false;

            // Valida que não contém caracteres inválidos (apenas alfanuméricos, hífens e underscores)
            // Permite GUIDs (com ou sem hífens), trace-ids W3C (32 hex chars), e outros formatos válidos
            for (int i = 0; i < correlationId.Length; i++)
            {
                var c = correlationId[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Thread-safe: ler _options uma vez para evitar race condition
            var options = _options;

            // IMPORTANTE: Ler headers PRIMEIRO para decidir o trace-id e a hierarquia do span.
            // - X-Correlation-Id tem prioridade sobre traceparent
            // - traceparent define o parent para criar hierarquia real
            var headerName = CorrelationIdHeader;

            string? correlationIdFromHeader = null;
            if (request.Headers.Contains(headerName))
            {
                var values = request.Headers.GetValues(headerName);
                if (values != null)
                {
                    correlationIdFromHeader = values.FirstOrDefault();
                }
            }

            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationIdFromHeader) && !IsValidCorrelationId(correlationIdFromHeader))
            {
                // Se inválido, ignora o header e gera novo
                correlationIdFromHeader = null;
            }

            // Tentar extrair parent context W3C do traceparent/tracestate (se existir)
            ActivityContext parentFromTraceparent = default;
            var hasTraceparentHeader = request.Headers.Contains("traceparent");
            if (hasTraceparentHeader)
            {
                var traceparent = request.Headers.GetValues("traceparent").FirstOrDefault();
                var tracestate = request.Headers.Contains("tracestate")
                    ? request.Headers.GetValues("tracestate").FirstOrDefault()
                    : null;

                if (!string.IsNullOrWhiteSpace(traceparent))
                {
                    ActivityContext.TryParse(traceparent, tracestate, out parentFromTraceparent);
                }
            }

            // Decidir qual trace-id/correlation-id vamos usar neste request
            // Prioridade: AlwaysGenerateNew > X-Correlation-Id > traceparent > gerar novo
            string effectiveCorrelationId;
            if (options.AlwaysGenerateNew)
            {
                effectiveCorrelationId = Guid.NewGuid().ToString("N");
            }
            else if (!string.IsNullOrEmpty(correlationIdFromHeader))
            {
                effectiveCorrelationId = correlationIdFromHeader!;
            }
            else if (parentFromTraceparent != default)
            {
                effectiveCorrelationId = parentFromTraceparent.TraceId.ToString();
            }
            else
            {
                effectiveCorrelationId = Guid.NewGuid().ToString("N");
            }

            // Construir o parent context para o Activity:
            // - Se houver traceparent (e não houver X-Correlation-Id/AlwaysGenerateNew), respeitar o parent real
            // - Caso contrário, criar um parent artificial para garantir que o Activity.TraceId seja o correlation-id efetivo
            ActivityContext parentContext;
            if (!options.AlwaysGenerateNew && string.IsNullOrEmpty(correlationIdFromHeader) && parentFromTraceparent != default)
            {
                parentContext = parentFromTraceparent;
            }
            else
            {
                // Se o correlation-id não for um trace-id W3C (32 hex), não é possível setar no TraceId.
                // Nesse caso, vamos cair no fallback AsyncLocal via CorrelationContext.
                if (effectiveCorrelationId.Length == 32)
                {
                    var traceId = ActivityTraceId.CreateFromString(effectiveCorrelationId.AsSpan());
                    parentContext = new ActivityContext(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                }
                else
                {
                    parentContext = default;
                }
            }

            // Garantir que o CorrelationContext esteja sempre setado (fallback),
            // mesmo quando o trace-id não puder ser aplicado ao Activity.
            CorrelationContext.Current = effectiveCorrelationId;

            // Criar Activity (span) do request.
            // Observação: ActivitySource só cria Activity se houver ActivityListener.
            // Nome inicial será atualizado após base.SendAsync quando route template estiver disponível
            using var activity = parentContext != default
                ? TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server, parentContext)
                : TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);

            if (activity != null)
            {
                // Adicionar tags padrão (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());
                activity.SetTag("http.scheme", request.RequestUri?.Scheme);
                activity.SetTag("http.host", request.RequestUri?.Host);
                // Setar placeholder para garantir que o controller enxergue a tag durante o processamento.
                // (O valor real será atualizado após base.SendAsync retornar.)
                activity.SetTag("http.status_code", "0");

                if (request.Content != null && request.Content.Headers.ContentLength.HasValue)
                {
                    activity.SetTag("http.request_content_length", request.Content.Headers.ContentLength.Value);
                }

                if (request.Content != null && request.Content.Headers.ContentType != null)
                {
                    var contentType = request.Content.Headers.ContentType.ToString();
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        activity.SetTag("http.request_content_type", contentType);
                    }
                }
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

                    // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                    activity.SetTag("http.status_code", ((int)response.StatusCode).ToString());
                    
                    if (response.Content != null && response.Content.Headers.ContentLength.HasValue)
                    {
                        activity.SetTag("http.response_content_length", response.Content.Headers.ContentLength.Value);
                    }
                }
                
                // Adiciona o correlation-id no header da resposta
                if (response != null && !string.IsNullOrEmpty(effectiveCorrelationId))
                {
                    try
                    {
                        // Verifica se o header já existe antes de adicionar
                        if (response.Headers.Contains(headerName))
                        {
                            response.Headers.Remove(headerName);
                        }
                        response.Headers.Add(headerName, effectiveCorrelationId);
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
                    // Adicionar exceção ao Activity (igual ao que OpenTelemetry faz no .NET 8)
                    activity.SetTag("error", "true");
                    activity.SetTag("error.type", ex.GetType().Name);
                    activity.SetTag("error.message", ex.Message);
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                }
                throw;
            }
        }
    }
}
#endif

