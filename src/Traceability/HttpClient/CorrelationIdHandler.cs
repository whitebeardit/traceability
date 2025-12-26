using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if NET8_0
using Microsoft.Extensions.Options;
using Traceability.Configuration;
#endif
using Traceability;
#if NET48 || NET8_0
using Traceability.OpenTelemetry;
#endif

namespace Traceability.HttpClient
{
    /// <summary>
    /// DelegatingHandler que adiciona automaticamente o correlation-id nos headers das requisições HTTP
    /// e cria Activities (spans) filhas do OpenTelemetry para propagação de trace context.
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
#if NET8_0
        private readonly TraceabilityOptions? _options;
        private string CorrelationIdHeader
        {
            get
            {
                var headerName = _options?.HeaderName;
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    return "X-Correlation-Id";
                }
                return headerName;
            }
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHandler.
        /// </summary>
        /// <param name="options">Opções de configuração (opcional, injetado via DI).</param>
        public CorrelationIdHandler(IOptions<TraceabilityOptions>? options = null)
        {
            _options = options?.Value;
        }

        private bool ShouldCreateHttpClientSpan()
        {
            // Default false on NET8 to avoid duplication with System.Net.Http/OpenTelemetry instrumentation.
            // Opt-in via options or env var.
            if (_options?.Net8HttpClientSpansEnabled == true)
            {
                return true;
            }

            var env = Environment.GetEnvironmentVariable("TRACEABILITY_NET8_HTTPCLIENT_SPANS_ENABLED");
            if (bool.TryParse(env, out var enabled))
            {
                return enabled;
            }

            return false;
        }
#else
        private const string CorrelationIdHeader = "X-Correlation-Id";
#endif

        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header
        /// e criando Activity filho (span hierárquico) para propagação de trace context.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var parentActivity = Activity.Current;

            // Only propagate/create spans when we already have a correlation context (no implicit GUID creation).
            var hasCorrelationContext = CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId);

#if NET8_0
            var createSpan = ShouldCreateHttpClientSpan();
#else
            var createSpan = true;
#endif

            // If there is no context at all, we must not create a root span (would implicitly create correlation id via Activity).
            if (!hasCorrelationContext)
            {
                createSpan = false;
            }

            // Create Activity child span only when enabled (to avoid duplication on NET8)
            using var activity = createSpan
                ? TraceabilityActivitySource.StartActivity("HTTP Client", ActivityKind.Client, parentActivity)
                : null;

            // Propagate trace context even when not creating span
            var traceParent = activity?.Id ?? parentActivity?.Id;
            if (!string.IsNullOrEmpty(traceParent) && !request.Headers.Contains("traceparent"))
            {
                request.Headers.Add("traceparent", traceParent);
            }

            // Add standard tags if we created a span
            if (activity != null)
            {
                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());
                activity.SetTag("http.scheme", request.RequestUri?.Scheme);
                activity.SetTag("http.host", request.RequestUri?.Host);
            }
            
            // Adicionar X-Correlation-Id para compatibilidade
            if (hasCorrelationContext)
            {
                var headerName = CorrelationIdHeader;
                // Verifica se o header existe antes de remover (evita operação desnecessária)
                if (request.Headers.Contains(headerName))
                {
                    request.Headers.Remove(headerName);
                }
                request.Headers.Add(headerName, correlationId);
            }

            try
            {
                // Capturar Activity antes da continuação para garantir que está disponível
                var capturedActivity = activity;
                var responseTask = base.SendAsync(request, cancellationToken);
                
                // Adicionar status code quando resposta estiver disponível
                if (capturedActivity != null)
                {
                    // Usar ContinueWith com opções apropriadas e capturar Activity
                    responseTask.ContinueWith(task =>
                    {
                        try
                        {
                            if (task.Status == TaskStatus.RanToCompletion && task.Result != null)
                            {
                                // Activity foi capturado antes da continuação, então está disponível
                                capturedActivity.SetTag("http.status_code", (int)task.Result.StatusCode);
                            }
                        }
                        catch
                        {
                            // Ignora erros ao adicionar tag
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }
                
                return responseTask;
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetTag("error", true);
                    activity.SetTag("error.type", ex.GetType().Name);
                    activity.SetTag("error.message", ex.Message);
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                }
                throw;
            }
        }

#if NET8_0
        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header.
        /// Versão síncrona para .NET 8+.
        /// </summary>
        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var parentActivity = Activity.Current;

            var createSpan = ShouldCreateHttpClientSpan();
            using var activity = createSpan
                ? TraceabilityActivitySource.StartActivity("HTTP Client", ActivityKind.Client, parentActivity)
                : null;

            var traceParent = activity?.Id ?? parentActivity?.Id;
            if (!string.IsNullOrEmpty(traceParent) && !request.Headers.Contains("traceparent"))
            {
                request.Headers.Add("traceparent", traceParent);
            }

            if (activity != null)
            {
                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());
                activity.SetTag("http.scheme", request.RequestUri?.Scheme);
                activity.SetTag("http.host", request.RequestUri?.Host);
            }
            
            // Adicionar X-Correlation-Id para compatibilidade
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var headerName = CorrelationIdHeader;
                // Verifica se o header existe antes de remover (evita operação desnecessária)
                if (request.Headers.Contains(headerName))
                {
                    request.Headers.Remove(headerName);
                }
                request.Headers.Add(headerName, correlationId);
            }

            try
            {
                var response = base.Send(request, cancellationToken);
                
                if (activity != null && response != null)
                {
                    activity.SetTag("http.status_code", (int)response.StatusCode);
                }
                
                return response!;
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetTag("error", true);
                    activity.SetTag("error.type", ex.GetType().Name);
                    activity.SetTag("error.message", ex.Message);
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                }
                throw;
            }
        }
#endif
    }
}

