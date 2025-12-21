#if NET8_0
using System;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
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
            var options = new TraceabilityOptions();
            configureOptions?.Invoke(options);
            
            services.AddSingleton(options);
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
    }
}
#endif

