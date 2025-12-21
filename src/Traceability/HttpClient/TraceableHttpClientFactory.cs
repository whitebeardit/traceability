using System;
using System.Net.Http;
#if NET8_0
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Traceability.HttpClient
{
    /// <summary>
    /// Factory para criar HttpClient com correlation-id usando IHttpClientFactory.
    /// Previne socket exhaustion ao reutilizar conexões HTTP gerenciadas pelo IHttpClientFactory.
    /// </summary>
    public class TraceableHttpClientFactory
    {
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
}

