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
using Traceability.Core;
#if NET48 || NET8_0 || NETSTANDARD2_0
using Traceability.Utilities;
#endif

namespace Traceability.HttpClient
{
    /// <summary>
    /// DelegatingHandler que adiciona automaticamente o correlation-id nos headers das requisições HTTP
    /// e propaga trace context (traceparent) quando existir Activity.Current (instrumentação externa).
    /// </summary>
    public class CorrelationIdHandler : DelegatingHandler
    {
#if NET48 || NET8_0 || NETSTANDARD2_0
        private static bool TryGetValidW3CTraceParent(Activity? activity, out string? traceParent)
        {
            traceParent = null;
            if (activity == null)
            {
                return false;
            }

            var id = activity.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            // Hardening: Activity.Id can be hierarchical (legacy) which is NOT a valid traceparent header.
            // Only propagate when it parses as W3C traceparent.
            // Use positional args for broad framework compatibility (parameter name differs across TFMs/versions).
            if (!ActivityContext.TryParse(id, null, out var ctx) || ctx == default)
            {
                return false;
            }

            traceParent = id;
            return true;
        }
#endif

#if NET8_0
        private readonly TraceabilityOptions? _options;
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
        public CorrelationIdHandler(
            IOptions<TraceabilityOptions>? options = null)
        {
            _options = options?.Value;
        }
#else
        private const string CorrelationIdHeader = Constants.HttpHeaders.CorrelationId;
#endif

        /// <summary>
        /// Envia a requisição HTTP adicionando o correlation-id do contexto atual no header
        /// e propagando trace context (traceparent) quando existir Activity.Current.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var parentActivity = Activity.Current;

            // Only propagate/create spans when we already have a correlation context (no implicit GUID creation).
            var hasCorrelationContext = CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId);

            // Propagate trace context from the current Activity (created by external instrumentation)
            if (!request.Headers.Contains(Constants.HttpHeaders.TraceParent))
            {
#if NET48 || NET8_0 || NETSTANDARD2_0
                if (TryGetValidW3CTraceParent(parentActivity, out var traceParent))
                {
                    request.Headers.Add(Constants.HttpHeaders.TraceParent, traceParent!);
                }
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
                return SendAsyncCore(request, cancellationToken);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<HttpResponseMessage> SendAsyncCore(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            return response!;
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

            // Only propagate/create spans when we already have a correlation context (no implicit GUID creation).
            var hasCorrelationContext = CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId);

            if (!request.Headers.Contains(Constants.HttpHeaders.TraceParent))
            {
                if (TryGetValidW3CTraceParent(parentActivity, out var traceParent))
                {
                    request.Headers.Add(Constants.HttpHeaders.TraceParent, traceParent!);
                }
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

                return response!;
            }
            catch (Exception)
            {
                throw;
            }
        }
#endif
    }
}
