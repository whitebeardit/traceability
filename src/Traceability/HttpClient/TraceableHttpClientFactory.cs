using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
#if NET8_0
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Traceability.HttpClient
{
    /// <summary>
    /// Factory para criar HttpClient com correlation-id e políticas Polly.
    /// </summary>
    public class TraceableHttpClientFactory
    {
        /// <summary>
        /// Cria um HttpClient configurado com CorrelationIdHandler.
        /// ⚠️ OBSOLETO: Este método cria um novo HttpClient a cada chamada, podendo causar socket exhaustion.
        /// Para aplicações de produção, use IHttpClientFactory com AddHttpMessageHandler&lt;CorrelationIdHandler&gt;()
        /// ou o método CreateFromFactory().
        /// </summary>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional).</param>
        /// <returns>HttpClient configurado com correlation-id.</returns>
        /// <remarks>
        /// Este método é adequado apenas para:
        /// - Aplicações console de uso único
        /// - Testes unitários
        /// - Prototipação rápida
        /// 
        /// Para aplicações web ou de alta carga, sempre use IHttpClientFactory.
        /// </remarks>
        [Obsolete("Este método pode causar socket exhaustion. Use IHttpClientFactory com AddHttpMessageHandler<CorrelationIdHandler>() ou CreateFromFactory() para aplicações de produção.")]
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
        /// ⚠️ OBSOLETO: Este método cria um novo HttpClient a cada chamada, podendo causar socket exhaustion.
        /// Para aplicações de produção, use IHttpClientFactory com AddHttpMessageHandler&lt;CorrelationIdHandler&gt;()
        /// ou o método CreateFromFactory().
        /// </summary>
        /// <param name="policy">Política Polly para aplicar (opcional).</param>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional).</param>
        /// <returns>HttpClient configurado.</returns>
        /// <remarks>
        /// Este método é adequado apenas para:
        /// - Aplicações console de uso único
        /// - Testes unitários
        /// - Prototipação rápida
        /// 
        /// Para aplicações web ou de alta carga, sempre use IHttpClientFactory.
        /// </remarks>
        [Obsolete("Este método pode causar socket exhaustion. Use IHttpClientFactory com AddHttpMessageHandler<CorrelationIdHandler>() ou CreateFromFactory() para aplicações de produção.")]
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

#if NET8_0
        /// <summary>
        /// Cria um HttpClient usando IHttpClientFactory (RECOMENDADO - previne socket exhaustion).
        /// O IHttpClientFactory gerencia o pool de HttpClient e reutiliza conexões HTTP.
        /// </summary>
        /// <param name="factory">A factory de HttpClient (normalmente injetada via DI).</param>
        /// <param name="clientName">Nome do cliente HTTP configurado (opcional, usa "DefaultTraceableClient" se não fornecido).</param>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional, sobrescreve configuração do cliente nomeado).</param>
        /// <returns>HttpClient configurado com correlation-id via CorrelationIdHandler.</returns>
        /// <exception cref="ArgumentNullException">Lançado quando factory é null.</exception>
        /// <remarks>
        /// Para usar este método, primeiro configure o HttpClient no DI:
        /// <code>
        /// services.AddHttpClient("MyClient", client => {
        ///     client.BaseAddress = new Uri("https://api.example.com/");
        /// })
        /// .AddHttpMessageHandler&lt;CorrelationIdHandler&gt;();
        /// </code>
        /// 
        /// Ou use o método de extensão AddTraceableHttpClient() para simplificar.
        /// </remarks>
        public static System.Net.Http.HttpClient CreateFromFactory(
            IHttpClientFactory factory,
            string? clientName = null,
            string? baseAddress = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var client = factory.CreateClient(clientName ?? "DefaultTraceableClient");
            
            if (!string.IsNullOrEmpty(baseAddress))
            {
                client.BaseAddress = new Uri(baseAddress);
            }
            
            return client;
        }
#endif
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

        // Nota: Método Send() sincrono removido para evitar deadlock potencial.
        // Em .NET 8, o framework raramente chama Send() diretamente, preferindo SendAsync().
        // Se necessário, o framework pode chamar SendAsync().GetAwaiter().GetResult() internamente.
    }
}

