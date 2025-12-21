#if NET8_0
using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Traceability.HttpClient;
using Traceability.Configuration;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para IServiceCollection (apenas .NET 8).
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adiciona os serviços de traceability ao container de DI.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="configureOptions">Ação para configurar as opções (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        public static IServiceCollection AddTraceability(
            this IServiceCollection services,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            // Registra TraceabilityOptions usando o padrão Options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                // Registra com valores padrão se não houver configuração
                services.Configure<TraceabilityOptions>(_ => { });
            }
            
            services.AddTransient<CorrelationIdHandler>();
            
            return services;
        }

        /// <summary>
        /// Adiciona um HttpClient traceable ao container de DI.
        /// </summary>
        /// <typeparam name="TClient">O tipo do cliente HTTP.</typeparam>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="baseAddress">Endereço base do HttpClient (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        public static IServiceCollection AddTraceableHttpClient<TClient>(
            this IServiceCollection services,
            string? baseAddress = null)
            where TClient : class, ITraceableHttpClient
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddHttpClient<TClient>(client =>
            {
                if (!string.IsNullOrEmpty(baseAddress))
                {
                    client.BaseAddress = new Uri(baseAddress);
                }
            })
            .AddHttpMessageHandler<CorrelationIdHandler>();
            
            return services;
        }

        /// <summary>
        /// Adiciona um HttpClient traceable nomeado ao container de DI.
        /// Previne socket exhaustion ao usar IHttpClientFactory que gerencia o pool de HttpClient.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="clientName">Nome do cliente HTTP (obrigatório).</param>
        /// <param name="configureClient">Ação para configurar o HttpClient (opcional).</param>
        /// <returns>IHttpClientBuilder para configuração adicional (ex: políticas Polly).</returns>
        /// <exception cref="ArgumentNullException">Lançado quando services é null.</exception>
        /// <exception cref="ArgumentException">Lançado quando clientName é null ou vazio.</exception>
        /// <remarks>
        /// Este método configura um HttpClient nomeado com CorrelationIdHandler automaticamente.
        /// O IHttpClientFactory gerencia o ciclo de vida do HttpClient, prevenindo socket exhaustion.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// services.AddTraceableHttpClient("ExternalApi", client => {
        ///     client.BaseAddress = new Uri("https://api.example.com/");
        ///     client.Timeout = TimeSpan.FromSeconds(30);
        /// });
        /// 
        /// // No controller ou serviço:
        /// var client = _httpClientFactory.CreateClient("ExternalApi");
        /// </code>
        /// </remarks>
        public static IHttpClientBuilder AddTraceableHttpClient(
            this IServiceCollection services,
            string clientName,
            Action<System.Net.Http.HttpClient>? configureClient = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(clientName))
                throw new ArgumentException("Client name cannot be null or empty", nameof(clientName));

            return services.AddHttpClient(clientName, client =>
            {
                configureClient?.Invoke(client);
            })
            .AddHttpMessageHandler<CorrelationIdHandler>();
        }
    }
}
#endif

