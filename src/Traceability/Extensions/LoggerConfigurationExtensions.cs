using System;
using Serilog;
using Traceability.Configuration;
using Traceability.Logging;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para LoggerConfiguration do Serilog.
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// Adiciona automaticamente os enrichers de traceability (Source e CorrelationId) ao Serilog.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config ou source é null.</exception>
        /// <exception cref="System.ArgumentException">Lançado quando source é vazio.</exception>
        /// <remarks>
        /// Este método adiciona automaticamente:
        /// - SourceEnricher: Adiciona o campo Source aos logs
        /// - CorrelationIdEnricher: Adiciona o campo CorrelationId aos logs (quando disponível no contexto)
        /// 
        /// Exemplo de uso:
        /// <code>
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceability("UserService")
        ///     .WriteTo.Console()
        ///     .CreateLogger();
        /// </code>
        /// </remarks>
        public static LoggerConfiguration WithTraceability(
            this LoggerConfiguration config,
            string source)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source))
                throw new System.ArgumentException("Source cannot be null or empty", nameof(source));

            return config
                .Enrich.With(new SourceEnricher(source))
                .Enrich.With<CorrelationIdEnricher>();
        }

        /// <summary>
        /// Configura Serilog com template JSON padrão e enrichers de traceability.
        /// Adiciona automaticamente Source, CorrelationId e DataEnricher para serialização de objetos.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <param name="configureOptions">Ação para configurar as opções de traceability (opcional).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config ou source é null.</exception>
        /// <exception cref="System.ArgumentException">Lançado quando source é vazio.</exception>
        /// <remarks>
        /// Este método configura automaticamente:
        /// - SourceEnricher: Adiciona o campo Source aos logs
        /// - CorrelationIdEnricher: Adiciona o campo CorrelationId aos logs (quando disponível no contexto)
        /// - DataEnricher: Detecta e serializa objetos complexos no campo "data"
        /// 
        /// Exemplo de uso:
        /// <code>
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceabilityJson("UserService")
        ///     .WriteTo.Console()
        ///     .CreateLogger();
        /// 
        /// // Uso no código
        /// var user = new { UserId = 123, UserName = "john.doe" };
        /// Log.Information("Processando requisição {@User}", user);
        /// // Output: JSON com campo "data" contendo o objeto serializado
        /// </code>
        /// </remarks>
        public static LoggerConfiguration WithTraceabilityJson(
            this LoggerConfiguration config,
            string source,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source))
                throw new System.ArgumentException("Source cannot be null or empty", nameof(source));

            var options = new TraceabilityOptions
            {
                Source = source
            };
            configureOptions?.Invoke(options);

            // Adiciona enrichers
            config = config
                .Enrich.With(new SourceEnricher(source))
                .Enrich.With<CorrelationIdEnricher>();

            // Adiciona DataEnricher se configurado para incluir dados
            if (options.LogIncludeData)
            {
                config = config.Enrich.With<DataEnricher>();
            }

            return config;
        }

        /// <summary>
        /// Configura Serilog com template JSON padrão usando as opções especificadas.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="options">Opções de configuração de traceability (deve ter Source definido).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config ou options é null.</exception>
        /// <exception cref="System.ArgumentException">Lançado quando Source não está definido nas opções.</exception>
        /// <remarks>
        /// Este método é útil quando você já tem uma instância de TraceabilityOptions configurada.
        /// 
        /// Exemplo de uso:
        /// <code>
        /// var options = new TraceabilityOptions { Source = "UserService" };
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceabilityJson(options)
        ///     .WriteTo.Console(new JsonFormatter(options))
        ///     .CreateLogger();
        /// </code>
        /// </remarks>
        public static LoggerConfiguration WithTraceabilityJson(
            this LoggerConfiguration config,
            TraceabilityOptions options)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));
            if (options == null)
                throw new System.ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.Source))
                throw new System.ArgumentException("Source must be defined in TraceabilityOptions", nameof(options));

            var source = options.Source!; // Já validado acima

            // Adiciona enrichers
            config = config
                .Enrich.With(new SourceEnricher(source))
                .Enrich.With<CorrelationIdEnricher>();

            // Adiciona DataEnricher se configurado para incluir dados
            if (options.LogIncludeData)
            {
                config = config.Enrich.With<DataEnricher>();
            }

            return config;
        }
    }
}


