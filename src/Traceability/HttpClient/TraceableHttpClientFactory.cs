using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Traceability.HttpClient
{
    /// <summary>
    /// Factory para criar HttpClient com correlation-id e políticas Polly.
    /// </summary>
    public class TraceableHttpClientFactory
    {
        /// <summary>
        /// Cria um HttpClient configurado com CorrelationIdHandler.
        /// </summary>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional).</param>
        /// <returns>HttpClient configurado com correlation-id.</returns>
        public static System.Net.Http.HttpClient Create(string? baseAddress = null)
        {
            var handler = new CorrelationIdHandler();
            var httpClient = new System.Net.Http.HttpClient(handler);
            
            if (!string.IsNullOrEmpty(baseAddress))
            {
                httpClient.BaseAddress = new Uri(baseAddress);
            }

            return httpClient;
        }

        /// <summary>
        /// Cria um HttpClient configurado com CorrelationIdHandler e política Polly.
        /// A política será aplicada usando PolicyHttpMessageHandler se disponível.
        /// </summary>
        /// <param name="policy">Política Polly para aplicar (opcional).</param>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional).</param>
        /// <returns>HttpClient configurado.</returns>
        public static System.Net.Http.HttpClient CreateWithPolicy(
            IAsyncPolicy<HttpResponseMessage> policy,
            string? baseAddress = null)
        {
            var handler = new CorrelationIdHandler();
            
            if (policy != null)
            {
#if NET8_0
                // Para .NET 8, podemos usar PolicyHttpMessageHandler se Polly.Extensions.Http estiver disponível
                // Por enquanto, vamos usar uma abordagem mais simples
                var policyHandler = new PolicyDelegatingHandler(policy)
                {
                    InnerHandler = handler
                };
                handler = new CorrelationIdHandler
                {
                    InnerHandler = policyHandler
                };
#else
                // Para .NET Framework, aplicamos a política manualmente no handler
                var policyHandler = new PolicyDelegatingHandler(policy)
                {
                    InnerHandler = handler
                };
                handler = new CorrelationIdHandler
                {
                    InnerHandler = policyHandler
                };
#endif
            }

            var httpClient = new System.Net.Http.HttpClient(handler);
            
            if (!string.IsNullOrEmpty(baseAddress))
            {
                httpClient.BaseAddress = new Uri(baseAddress);
            }

            return httpClient;
        }
    }

    /// <summary>
    /// DelegatingHandler que aplica uma política Polly.
    /// </summary>
    internal class PolicyDelegatingHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public PolicyDelegatingHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
        }

#if NET8_0
        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Para .NET 8, Execute pode não estar disponível dependendo da versão do Polly
            // Usamos ExecuteAsync e aguardamos o resultado
            return _policy.ExecuteAsync(ct => Task.FromResult(base.Send(request, ct)), cancellationToken).GetAwaiter().GetResult();
        }
#endif
    }
}

