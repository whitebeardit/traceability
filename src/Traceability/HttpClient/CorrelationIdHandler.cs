using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if NET8_0
using Microsoft.Extensions.Options;
using Traceability.Configuration;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
#endif
using Traceability;
using Traceability.Core;
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
        private readonly IActivityFactory _activityFactory;
        private readonly IActivityTagProvider _tagProvider;
        private string CorrelationIdHeader
        {
            get
            {
                var headerName = _options?.HeaderName;
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    return Constants.HttpHeaders.CorrelationId;
                }
                return headerName;
            }
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHandler.
        /// </summary>
        /// <param name="options">Opções de configuração (opcional, injetado via DI).</param>
        /// <param name="activityFactory">Factory de Activities (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="tagProvider">Provider de tags HTTP (opcional, cria instância padrão se não fornecido).</param>
        public CorrelationIdHandler(
            IOptions<TraceabilityOptions>? options = null,
            IActivityFactory? activityFactory = null,
            IActivityTagProvider? tagProvider = null)
        {
            _options = options?.Value;
            _activityFactory = activityFactory ?? new TraceabilityActivityFactory();
            _tagProvider = tagProvider ?? new HttpActivityTagProvider();
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
        private const string CorrelationIdHeader = Constants.HttpHeaders.CorrelationId;
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
#if NET8_0
            using var activity = createSpan
                ? _activityFactory.StartActivity(Constants.ActivityNames.HttpClient, ActivityKind.Client, parentActivity)
                : null;
#else
            using var activity = createSpan
                ? TraceabilityActivitySource.StartActivity(Constants.ActivityNames.HttpClient, ActivityKind.Client, parentActivity)
                : null;
#endif

            // Propagate trace context even when not creating span
            var traceParent = activity?.Id ?? parentActivity?.Id;
            if (!string.IsNullOrEmpty(traceParent) && !request.Headers.Contains(Constants.HttpHeaders.TraceParent))
            {
                request.Headers.Add(Constants.HttpHeaders.TraceParent, traceParent);
            }

            // Add standard tags if we created a span
            if (activity != null)
            {
#if NET8_0
                _tagProvider.AddRequestTags(activity, request);
#else
                activity.SetTag(Constants.ActivityTags.HttpMethod, request.Method.ToString());
                activity.SetTag(Constants.ActivityTags.HttpUrl, request.RequestUri?.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, request.RequestUri?.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, request.RequestUri?.Host);
#endif
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
                return SendAsyncCore(request, cancellationToken, activity);
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
#if NET8_0
                    _tagProvider.AddErrorTags(activity, ex);
#else
                    activity.SetTag(Constants.ActivityTags.Error, true);
                    activity.SetTag(Constants.ActivityTags.ErrorType, ex.GetType().Name);
                    activity.SetTag(Constants.ActivityTags.ErrorMessage, ex.Message);
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
#endif
                }
                throw;
            }
        }

#if NET8_0
        private async Task<HttpResponseMessage> SendAsyncCore(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            Activity? activity)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (activity != null && response != null)
            {
                try
                {
                    _tagProvider.AddResponseTags(activity, response);
                }
                catch
                {
                    // ignore
                }
            }

            return response!;
        }
#else
        private async Task<HttpResponseMessage> SendAsyncCore(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            Activity? activity)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (activity != null && response != null)
            {
                try
                {
                    activity.SetTag(Constants.ActivityTags.HttpStatusCode, (int)response.StatusCode);
                }
                catch
                {
                    // ignore
                }
            }

            return response!;
        }
#endif

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

            // Only propagate/create spans when we already have a correlation context (no implicit GUID creation).
            var hasCorrelationContext = CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId);

            var createSpan = ShouldCreateHttpClientSpan();
            if (!hasCorrelationContext)
            {
                createSpan = false;
            }

            using var activity = createSpan
                ? _activityFactory.StartActivity(Constants.ActivityNames.HttpClient, ActivityKind.Client, parentActivity)
                : null;

            var traceParent = activity?.Id ?? parentActivity?.Id;
            if (!string.IsNullOrEmpty(traceParent) && !request.Headers.Contains(Constants.HttpHeaders.TraceParent))
            {
                request.Headers.Add(Constants.HttpHeaders.TraceParent, traceParent);
            }

            if (activity != null)
            {
                _tagProvider.AddRequestTags(activity, request);
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
                var response = base.Send(request, cancellationToken);

                if (activity != null && response != null)
                {
                    _tagProvider.AddResponseTags(activity, response);
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
#endif
    }
}
