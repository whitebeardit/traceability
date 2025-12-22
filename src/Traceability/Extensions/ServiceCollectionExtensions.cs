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
using Traceability.Utilities;
using Microsoft.Extensions.Logging;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para IServiceCollection (apenas .NET 8).
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        private static void AddOrDecorateExternalScopeProvider(
            IServiceCollection services,
            Func<IServiceProvider, IExternalScopeProvider?, IExternalScopeProvider> decorator)
        {
            // Evita recursão/ciclo de DI: não resolvemos IExternalScopeProvider via GetService<IExternalScopeProvider>()
            // dentro do próprio registration. Em vez disso, capturamos o ServiceDescriptor existente e o materializamos.
            var existingDescriptor = services.LastOrDefault(s => s.ServiceType == typeof(IExternalScopeProvider));

            if (existingDescriptor == null)
            {
                services.AddSingleton<IExternalScopeProvider>(sp => decorator(sp, null));
                return;
            }

            services.Remove(existingDescriptor);

            services.AddSingleton<IExternalScopeProvider>(sp =>
            {
                IExternalScopeProvider? inner = null;

                if (existingDescriptor.ImplementationInstance is IExternalScopeProvider instance)
                {
                    inner = instance;
                }
                else if (existingDescriptor.ImplementationFactory != null)
                {
                    inner = existingDescriptor.ImplementationFactory(sp) as IExternalScopeProvider;
                }
                else if (existingDescriptor.ImplementationType != null)
                {
                    inner = (IExternalScopeProvider)ActivatorUtilities.CreateInstance(sp, existingDescriptor.ImplementationType);
                }

                return decorator(sp, inner);
            });
        }

        /// <summary>
        /// Adiciona os serviços de traceability ao container de DI.
        /// Configura automaticamente todos os componentes (logging, HttpClient, etc.) por padrão.
        /// O Source (ServiceName) pode ser fornecido via parâmetro, configureOptions ou variável de ambiente TRACEABILITY_SERVICENAME.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (opcional - pode vir de variável de ambiente TRACEABILITY_SERVICENAME).</param>
        /// <param name="configureOptions">Ação para configurar as opções adicionais (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançado quando services é null.</exception>
        /// <exception cref="InvalidOperationException">Lançado quando source não está disponível (nem parâmetro, nem options, nem env var).</exception>
        /// <remarks>
        /// Este método configura automaticamente:
        /// - TraceabilityOptions com valores padrão (ou customizados via configureOptions)
        /// - CorrelationIdHandler para propagação de correlation-id em HttpClient
        /// - Microsoft.Extensions.Logging com scope providers (Source e CorrelationId, se Source estiver definido)
        /// - IHttpClientFactory (se não estiver registrado)
        /// 
        /// O ServiceName (Source) é determinado na seguinte ordem de prioridade:
        /// 1. Parâmetro source (se fornecido e não vazio) - prioridade máxima
        /// 2. TraceabilityOptions.Source (se especificado em configureOptions)
        /// 3. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// // Configuração com Source explícito (prioridade sobre env var)
        /// builder.Services.AddTraceability("UserService");
        /// 
        /// // Configuração com variável de ambiente TRACEABILITY_SERVICENAME
        /// // export TRACEABILITY_SERVICENAME="UserService"
        /// builder.Services.AddTraceability(); // source opcional
        /// 
        /// // Configuração com Source em options
        /// builder.Services.AddTraceability(configureOptions: options => {
        ///     options.Source = "UserService";
        /// });
        /// 
        /// // Configuração completa
        /// builder.Services.AddTraceability("UserService", options => {
        ///     options.HeaderName = "X-Custom-Id";
        ///     options.ValidateCorrelationIdFormat = true;
        /// });
        /// </code>
        /// </remarks>
        public static IServiceCollection AddTraceability(
            this IServiceCollection services,
            string? source = null,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Cria instância temporária para obter Source (se definido em options)
            var tempOptions = new TraceabilityOptions();
            configureOptions?.Invoke(tempOptions);

            // Obtém source seguindo a ordem de prioridade
            var serviceName = TraceabilityUtilities.GetServiceName(source, tempOptions);
            tempOptions.Source = serviceName; // Garante que está definido

            // Registra TraceabilityOptions usando o padrão Options
            services.Configure<TraceabilityOptions>(options =>
            {
                // Aplica configuração padrão primeiro
                configureOptions?.Invoke(options);
                // Garante que Source está definido (pode ter vindo da env var)
                if (string.IsNullOrWhiteSpace(options.Source))
                {
                    options.Source = serviceName;
                }
            });
            
            // Registra CorrelationIdHandler
            services.AddTransient<CorrelationIdHandler>();

            // Configura Microsoft.Extensions.Logging com scope providers (decorando o provider existente quando houver)
            AddOrDecorateExternalScopeProvider(services, (_, inner) =>
            {
                return new SourceScopeProvider(
                    serviceName,
                    new CorrelationIdScopeProvider(inner));
            });

            // Garante que IHttpClientFactory está disponível
            if (!services.Any(s => s.ServiceType == typeof(IHttpClientFactory)))
            {
                services.AddHttpClient(); // Registra IHttpClientFactory
            }

            // Auto-registra middleware via IStartupFilter se habilitado
            if (tempOptions.AutoRegisterMiddleware)
            {
                services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, CorrelationIdStartupFilter>();
            }

            // Auto-configura todos os HttpClients com CorrelationIdHandler se habilitado
            if (tempOptions.AutoConfigureHttpClient)
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(builder =>
                    {
                        var handler = builder.Services.GetRequiredService<CorrelationIdHandler>();
                        builder.AdditionalHandlers.Add(handler);
                    });
                });
            }
            
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
            where TClient : class
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
        /// O Source fornecido como parâmetro tem prioridade sobre a variável de ambiente TRACEABILITY_SERVICENAME.
        /// </summary>
        /// <param name="services">A coleção de serviços.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (opcional - pode vir de variável de ambiente TRACEABILITY_SERVICENAME).</param>
        /// <param name="configureOptions">Ação para configurar as opções adicionais (opcional).</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançado quando services é null.</exception>
        /// <exception cref="InvalidOperationException">Lançado quando source não está disponível (nem parâmetro, nem options, nem env var).</exception>
        /// <remarks>
        /// Este método configura o Source nas opções de traceability e registra o SourceScopeProvider
        /// para Microsoft.Extensions.Logging. Para Serilog, use SourceEnricher diretamente no LoggerConfiguration.
        /// 
        /// O ServiceName (Source) é determinado na seguinte ordem de prioridade:
        /// 1. Parâmetro source (se fornecido e não vazio) - prioridade máxima
        /// 2. TraceabilityOptions.Source (se especificado em configureOptions)
        /// 3. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// // Configurar traceability com source explícito (prioridade sobre env var)
        /// builder.Services.AddTraceabilityLogging("UserService", options =>
        /// {
        ///     options.HeaderName = "X-Correlation-Id";
        /// });
        /// 
        /// // Configurar com variável de ambiente TRACEABILITY_SERVICENAME
        /// // export TRACEABILITY_SERVICENAME="UserService"
        /// builder.Services.AddTraceabilityLogging(); // source opcional
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
            string? source = null,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Cria instância temporária para obter Source (se definido em options)
            var tempOptions = new TraceabilityOptions();
            configureOptions?.Invoke(tempOptions);

            // Obtém source seguindo a ordem de prioridade
            var serviceName = TraceabilityUtilities.GetServiceName(source, tempOptions);

            // Configura TraceabilityOptions com Source
            services.Configure<TraceabilityOptions>(options =>
            {
                options.Source = serviceName;
                configureOptions?.Invoke(options);
            });

            // Registra CorrelationIdHandler
            services.AddTransient<CorrelationIdHandler>();

            // Registra SourceScopeProvider para Microsoft.Extensions.Logging
            // O usuário ainda precisa configurar o logger para usar este provider
            AddOrDecorateExternalScopeProvider(services, (_, inner) =>
                new SourceScopeProvider(serviceName, new CorrelationIdScopeProvider(inner)));

            return services;
        }
    }
}
#endif

