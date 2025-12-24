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
            // IMPORTANTE: Ler o header PRIMEIRO para garantir que o trace-id do header tenha prioridade
            // Isso garante que o trace-id seja propagado corretamente entre serviços
            var headerName = CorrelationIdHeader;
            
            // Tenta obter o correlation-id do header da requisição (compatibilidade)
            string? correlationId = null;
            if (request.Headers.Contains(headerName))
            {
                var values = request.Headers.GetValues(headerName);
                if (values != null)
                {
                    correlationId = values.FirstOrDefault();
                }
            }

            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationId) && !IsValidCorrelationId(correlationId))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }

            // Thread-safe: ler _options uma vez para evitar race condition
            var options = _options;
            
            // Criar Activity automaticamente (OpenTelemetry) - igual ao que .NET 8 faz
            using var activity = TraceabilityActivitySource.StartActivity("Web API Request", ActivityKind.Server);
            
            if (activity != null)
            {
                // Adicionar tags padrão (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());
                activity.SetTag("http.scheme", request.RequestUri?.Scheme);
                activity.SetTag("http.host", request.RequestUri?.Host);
                
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
                
                // Ler traceparent se existir (W3C Trace Context)
                if (request.Headers.Contains("traceparent"))
                {
                    var traceParent = request.Headers.GetValues("traceparent").FirstOrDefault();
                    // OpenTelemetry já processa traceparent automaticamente via DiagnosticSource
                }
            }
            
            // Se não existir ou AlwaysGenerateNew estiver habilitado, gera um novo
            if (string.IsNullOrEmpty(correlationId) || options.AlwaysGenerateNew)
            {
                // CorrelationContext.Current já retorna trace-id se Activity existir
                // Se não houver Activity válido, gera um novo GUID
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir no header, usa o valor do header (prioridade sobre Activity)
                // Isso garante que o trace-id seja propagado corretamente entre serviços
                CorrelationContext.Current = correlationId!;
            }

            try
            {
                // Continua o pipeline usando async/await para melhor propagação de exceções
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                
                if (activity != null)
                {
                    // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                    activity.SetTag("http.status_code", (int)response.StatusCode);
                    
                    if (response.Content != null && response.Content.Headers.ContentLength.HasValue)
                    {
                        activity.SetTag("http.response_content_length", response.Content.Headers.ContentLength.Value);
                    }
                }
                
                // Adiciona o correlation-id no header da resposta
                if (response != null && !string.IsNullOrEmpty(correlationId))
                {
                    try
                    {
                        // Verifica se o header já existe antes de adicionar
                        if (response.Headers.Contains(headerName))
                        {
                            response.Headers.Remove(headerName);
                        }
                        response.Headers.Add(headerName, correlationId);
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
                    activity.SetTag("error", true);
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

