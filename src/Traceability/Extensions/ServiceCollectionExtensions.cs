#if NET8_0
using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Traceability.HttpClient;
using Traceability.Configuration;
using Traceability.Logging;
using Microsoft.Extensions.Logging;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para IServiceCollection (apenas .NET 8).
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adiciona os serviços de traceability ao container de DI.
        /// Configura automaticamente todos os componentes (logging, HttpClient, etc.) por padrão.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="configureOptions">Ação para configurar as opções (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <remarks>
        /// Este método configura automaticamente:
        /// - TraceabilityOptions com valores padrão (ou customizados via configureOptions)
        /// - CorrelationIdHandler para propagação de correlation-id em HttpClient
        /// - Microsoft.Extensions.Logging com scope providers (Source e CorrelationId, se Source estiver definido)
        /// - IHttpClientFactory (se não estiver registrado)
        /// 
        /// Exemplo de uso:
        /// <code>
        /// // Configuração simples com defaults
        /// builder.Services.AddTraceability();
        /// 
        /// // Configuração com Source
        /// builder.Services.AddTraceability(options => {
        ///     options.Source = "UserService";
        /// });
        /// 
        /// // Configuração completa
        /// builder.Services.AddTraceability(options => {
        ///     options.Source = "UserService";
        ///     options.HeaderName = "X-Custom-Id";
        ///     options.ValidateCorrelationIdFormat = true;
        /// });
        /// </code>
        /// </remarks>
        public static IServiceCollection AddTraceability(
            this IServiceCollection services,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Cria instância temporária para obter Source (se definido)
            var tempOptions = new TraceabilityOptions();
            configureOptions?.Invoke(tempOptions);

            // Registra TraceabilityOptions usando o padrão Options
            services.Configure<TraceabilityOptions>(options =>
            {
                // Aplica configuração padrão primeiro
                configureOptions?.Invoke(options);
            });
            
            // Registra CorrelationIdHandler
            services.AddTransient<CorrelationIdHandler>();

            // Configura Microsoft.Extensions.Logging com scope providers
            // IMPORTANTE: não podemos resolver IExternalScopeProvider dentro do próprio factory,
            // senão criamos recursão/ciclo de DI (causa hang/StackOverflow no testhost).
            var hasExternalScopeProvider = services.Any(s => s.ServiceType == typeof(IExternalScopeProvider));
            if (!hasExternalScopeProvider)
            {
                if (!string.IsNullOrWhiteSpace(tempOptions.Source))
                {
                    // Se Source está definido, registra SourceScopeProvider com CorrelationIdScopeProvider como inner
                    services.AddSingleton<IExternalScopeProvider>(_ =>
                        new SourceScopeProvider(tempOptions.Source!, new CorrelationIdScopeProvider()));
                }
                else
                {
                    // Se Source não está definido, registra apenas CorrelationIdScopeProvider
                    services.AddSingleton<IExternalScopeProvider>(_ => new CorrelationIdScopeProvider());
                }
            }

            // Garante que IHttpClientFactory está disponível
            if (!services.Any(s => s.ServiceType == typeof(IHttpClientFactory)))
            {
                services.AddHttpClient(); // Registra IHttpClientFactory
            }
            
            return services;
        }

        /// <summary>
        /// Adiciona os serviços de traceability ao container de DI com Source especificado.
        /// Configura automaticamente todos os componentes (logging, HttpClient, etc.) por padrão.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <param name="configureOptions">Ação para configurar as opções adicionais (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançado quando services ou source é null.</exception>
        /// <exception cref="ArgumentException">Lançado quando source é vazio.</exception>
        /// <remarks>
        /// Este método é uma sobrecarga conveniente que define Source diretamente.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// // Configuração simples com Source
        /// builder.Services.AddTraceability("UserService");
        /// 
        /// // Configuração com Source e opções adicionais
        /// builder.Services.AddTraceability("UserService", options => {
        ///     options.HeaderName = "X-Custom-Id";
        ///     options.ValidateCorrelationIdFormat = true;
        /// });
        /// </code>
        /// </remarks>
        public static IServiceCollection AddTraceability(
            this IServiceCollection services,
            string source,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            return services.AddTraceability(options =>
            {
                options.Source = source;
                configureOptions?.Invoke(options);
            });
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

        /// <summary>
        /// Adiciona os serviços de traceability com logging configurado.
        /// Configura o Source para identificação da origem dos logs em ambientes distribuídos.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <param name="configureOptions">Ação para configurar as opções adicionais (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançado quando services ou source é null.</exception>
        /// <exception cref="ArgumentException">Lançado quando source é vazio.</exception>
        /// <remarks>
        /// Este método configura o Source nas opções de traceability e registra o SourceScopeProvider
        /// para Microsoft.Extensions.Logging. Para Serilog, use SourceEnricher diretamente no LoggerConfiguration.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// // Configurar traceability com source
        /// builder.Services.AddTraceabilityLogging("UserService", options =>
        /// {
        ///     options.HeaderName = "X-Correlation-Id";
        /// });
        /// 
        /// // Configurar Serilog (se estiver usando)
        /// Log.Logger = new LoggerConfiguration()
        ///     .Enrich.With(new SourceEnricher("UserService"))
        ///     .Enrich.With&lt;CorrelationIdEnricher&gt;()
        ///     .WriteTo.Console()
        ///     .CreateLogger();
        /// </code>
        /// </remarks>
        public static IServiceCollection AddTraceabilityLogging(
            this IServiceCollection services,
            string source,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source cannot be null or empty", nameof(source));

            // Configura TraceabilityOptions com Source
            services.Configure<TraceabilityOptions>(options =>
            {
                options.Source = source;
                configureOptions?.Invoke(options);
            });

            // Registra CorrelationIdHandler
            services.AddTransient<CorrelationIdHandler>();

            // Registra SourceScopeProvider para Microsoft.Extensions.Logging
            // O usuário ainda precisa configurar o logger para usar este provider
            // IMPORTANTE: evitar resolver IExternalScopeProvider dentro do próprio factory (recursão/ciclo)
            var hasExternalScopeProvider = services.Any(s => s.ServiceType == typeof(IExternalScopeProvider));
            if (!hasExternalScopeProvider)
            {
                services.AddSingleton<IExternalScopeProvider>(_ =>
                    new SourceScopeProvider(source, new CorrelationIdScopeProvider()));
            }

            return services;
        }
    }
}
#endif

