using System;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Traceability.Configuration;
using Traceability.Logging;

namespace Traceability.Extensions
{
    /// <summary>
    /// Extensões para LoggerConfiguration do Serilog.
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        private const string LogLevelEnvironmentVariable = "LOG_LEVEL";
        private const string ServiceNameEnvironmentVariable = "TRACEABILITY_SERVICENAME";

        /// <summary>
        /// Obtém o ServiceName (Source) seguindo a ordem de prioridade:
        /// 1) Parâmetro source (se fornecido e não vazio)
        /// 2) options.Source (se definido)
        /// 3) Variável de ambiente TRACEABILITY_SERVICENAME
        /// 4) Assembly name (se UseAssemblyNameAsFallback = true)
        /// Se nenhum estiver disponível, lança InvalidOperationException.
        /// </summary>
        /// <param name="source">Source fornecido como parâmetro (opcional).</param>
        /// <param name="options">Opções de traceability (opcional).</param>
        /// <returns>O ServiceName configurado.</returns>
        /// <exception cref="InvalidOperationException">Lançado quando nenhum source está disponível.</exception>
        private static string GetServiceName(string? source, TraceabilityOptions? options = null)
        {
            // Prioridade 1: Parâmetro source (se fornecido e não vazio)
            if (!string.IsNullOrWhiteSpace(source))
            {
                return source!; // Já validado acima
            }

            // Prioridade 2: options.Source (se definido)
            if (options != null && !string.IsNullOrWhiteSpace(options.Source))
            {
                return options.Source!; // Já validado acima - não pode ser null após IsNullOrWhiteSpace
            }

            // Prioridade 3: Variável de ambiente TRACEABILITY_SERVICENAME
            var envValue = Environment.GetEnvironmentVariable(ServiceNameEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                return envValue;
            }

            // Prioridade 4: Assembly name (se UseAssemblyNameAsFallback = true)
            if (options == null || options.UseAssemblyNameAsFallback)
            {
                var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    return assemblyName!; // Null-forgiving: já validado com IsNullOrWhiteSpace
                }
            }

            // Se nenhum estiver disponível, lançar erro
            throw new InvalidOperationException(
                $"Source (ServiceName) must be provided either as a parameter, in TraceabilityOptions.Source, via the {ServiceNameEnvironmentVariable} environment variable, or (if UseAssemblyNameAsFallback = true) it will use the assembly name. " +
                "At least one of these must be specified to ensure uniform logging across all applications and services.");
        }

        /// <summary>
        /// Obtém o nível mínimo de log da variável de ambiente, opções ou usa o padrão Information.
        /// Prioridade: 1) Variável de ambiente LOG_LEVEL, 2) Options.MinimumLogLevel, 3) Information (padrão)
        /// </summary>
        /// <param name="options">Opções de traceability (opcional).</param>
        /// <returns>O nível mínimo de log configurado.</returns>
        private static LogEventLevel GetMinimumLogLevel(TraceabilityOptions? options = null)
        {
            // Prioridade 1: Variável de ambiente (prioridade máxima)
            var envValue = Environment.GetEnvironmentVariable(LogLevelEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                // Tenta fazer parse do valor (case-insensitive)
                if (Enum.TryParse<LogEventLevel>(envValue, ignoreCase: true, out var parsedLevel))
                {
                    return parsedLevel;
                }
            }

            // Prioridade 2: Options.MinimumLogLevel (se especificado)
            if (options?.MinimumLogLevel.HasValue == true)
            {
                return options.MinimumLogLevel.Value;
            }

            // Prioridade 3: Padrão Information
            return LogEventLevel.Information;
        }

        /// <summary>
        /// Adiciona automaticamente os enrichers de traceability (Source e CorrelationId) ao Serilog.
        /// Configura automaticamente o nível mínimo de log a partir da variável de ambiente LOG_LEVEL
        /// ou usa Information como padrão.
        /// O output é sempre em formato JSON para garantir uniformização de logs.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (opcional - pode vir de variável de ambiente TRACEABILITY_SERVICENAME).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config é null.</exception>
        /// <exception cref="System.InvalidOperationException">Lançado quando source não está disponível (nem parâmetro, nem options, nem env var).</exception>
        /// <remarks>
        /// <para>
        /// Este método adiciona automaticamente:
        /// - SourceEnricher: Adiciona o campo Source aos logs
        /// - CorrelationIdEnricher: Adiciona o campo CorrelationId aos logs (quando disponível no contexto)
        /// - MinimumLevel: Configura o nível mínimo de log
        /// - JsonFormatter: Output sempre em formato JSON para uniformização
        /// </para>
        /// <para>
        /// O ServiceName (Source) é determinado na seguinte ordem de prioridade:
        /// 1. Parâmetro source (se fornecido e não vazio) - prioridade máxima
        /// 2. TraceabilityOptions.Source (se especificado)
        /// 3. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único.
        /// </para>
        /// <para>
        /// O nível mínimo de log é determinado na seguinte ordem de prioridade:
        /// 1. Variável de ambiente LOG_LEVEL (prioridade máxima - permite alteração fácil em produção)
        /// 2. TraceabilityOptions.MinimumLogLevel (se especificado - usado apenas se env var não estiver presente)
        /// 3. Information (padrão)
        /// </para>
        /// <para>
        /// Valores aceitos para LOG_LEVEL (case-insensitive): Verbose, Debug, Information, Warning, Error, Fatal
        /// </para>
        /// <para>
        /// A variável de ambiente tem prioridade máxima para facilitar alteração do log level em produção
        /// sem necessidade de recompilar ou reimplantar a aplicação. Por exemplo, para habilitar debug em produção:
        /// <code>export LOG_LEVEL=Debug</code> (Linux/Mac) ou <code>$env:LOG_LEVEL="Debug"</code> (PowerShell)
        /// </para>
        /// <para>
        /// Exemplo de uso:
        /// </para>
        /// <code>
        /// // Com source explícito (prioridade sobre env var)
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceability("UserService")
        ///     .WriteTo.Console(new JsonFormatter())
        ///     .CreateLogger();
        /// 
        /// // Com variável de ambiente TRACEABILITY_SERVICENAME definida
        /// // export TRACEABILITY_SERVICENAME="UserService"
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceability() // source opcional
        ///     .WriteTo.Console(new JsonFormatter())
        ///     .CreateLogger();
        /// </code>
        /// </remarks>
        public static LoggerConfiguration WithTraceability(
            this LoggerConfiguration config,
            string? source = null)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            var serviceName = GetServiceName(source);
            var minimumLevel = GetMinimumLogLevel();

            return config
                .MinimumLevel.Is(minimumLevel)
                .Enrich.With(new SourceEnricher(serviceName))
                .Enrich.With<CorrelationIdEnricher>();
        }

        /// <summary>
        /// Configura Serilog com template JSON padrão e enrichers de traceability.
        /// Adiciona automaticamente Source, CorrelationId e DataEnricher para serialização de objetos.
        /// Configura automaticamente o nível mínimo de log a partir da variável de ambiente LOG_LEVEL
        /// ou usa Information como padrão.
        /// O output é sempre em formato JSON para garantir uniformização de logs.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (opcional - pode vir de variável de ambiente TRACEABILITY_SERVICENAME).</param>
        /// <param name="configureOptions">Ação para configurar as opções de traceability (opcional).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config é null.</exception>
        /// <exception cref="System.InvalidOperationException">Lançado quando source não está disponível (nem parâmetro, nem options, nem env var).</exception>
        /// <remarks>
        /// <para>
        /// Este método configura automaticamente:
        /// - SourceEnricher: Adiciona o campo Source aos logs
        /// - CorrelationIdEnricher: Adiciona o campo CorrelationId aos logs (quando disponível no contexto)
        /// - DataEnricher: Detecta e serializa objetos complexos no campo "data"
        /// - MinimumLevel: Configura o nível mínimo de log
        /// - Output sempre em formato JSON para uniformização
        /// </para>
        /// <para>
        /// O ServiceName (Source) é determinado na seguinte ordem de prioridade:
        /// 1. Parâmetro source (se fornecido e não vazio) - prioridade máxima
        /// 2. TraceabilityOptions.Source (se especificado nas opções)
        /// 3. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único.
        /// </para>
        /// <para>
        /// O nível mínimo de log é determinado na seguinte ordem de prioridade:
        /// 1. Variável de ambiente LOG_LEVEL (prioridade máxima - permite alteração fácil em produção)
        /// 2. TraceabilityOptions.MinimumLogLevel (se especificado - usado apenas se env var não estiver presente)
        /// 3. Information (padrão)
        /// </para>
        /// <para>
        /// Valores aceitos para LOG_LEVEL (case-insensitive): Verbose, Debug, Information, Warning, Error, Fatal
        /// </para>
        /// <para>
        /// A variável de ambiente tem prioridade máxima para facilitar alteração do log level em produção
        /// sem necessidade de recompilar ou reimplantar a aplicação.
        /// </para>
        /// <para>
        /// Exemplo de uso:
        /// </para>
        /// <code>
        /// // Com source explícito (prioridade sobre env var)
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceabilityJson("UserService")
        ///     .WriteTo.Console(new JsonFormatter())
        ///     .CreateLogger();
        /// 
        /// // Com variável de ambiente TRACEABILITY_SERVICENAME definida
        /// // export TRACEABILITY_SERVICENAME="UserService"
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceabilityJson() // source opcional
        ///     .WriteTo.Console(new JsonFormatter())
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
            string? source = null,
            Action<TraceabilityOptions>? configureOptions = null)
        {
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            var options = new TraceabilityOptions();
            configureOptions?.Invoke(options);

            // Obtém source seguindo a ordem de prioridade
            var serviceName = GetServiceName(source, options);
            options.Source = serviceName; // Garante que options.Source está definido

            var minimumLevel = GetMinimumLogLevel(options);

            // Adiciona enrichers
            config = config
                .MinimumLevel.Is(minimumLevel)
                .Enrich.With(new SourceEnricher(serviceName))
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
        /// Configura automaticamente o nível mínimo de log a partir da variável de ambiente LOG_LEVEL
        /// ou usa Information como padrão.
        /// O output é sempre em formato JSON para garantir uniformização de logs.
        /// </summary>
        /// <param name="config">A configuração do logger.</param>
        /// <param name="options">Opções de configuração de traceability (Source pode vir de options.Source ou variável de ambiente TRACEABILITY_SERVICENAME).</param>
        /// <returns>A configuração do logger para encadeamento.</returns>
        /// <exception cref="System.ArgumentNullException">Lançado quando config ou options é null.</exception>
        /// <exception cref="System.InvalidOperationException">Lançado quando Source não está disponível (nem options.Source, nem env var).</exception>
        /// <remarks>
        /// <para>
        /// Este método é útil quando você já tem uma instância de TraceabilityOptions configurada.
        /// </para>
        /// <para>
        /// O ServiceName (Source) é determinado na seguinte ordem de prioridade:
        /// 1. TraceabilityOptions.Source (se especificado)
        /// 2. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para forçar o padrão único.
        /// </para>
        /// <para>
        /// O nível mínimo de log é determinado na seguinte ordem de prioridade:
        /// 1. Variável de ambiente LOG_LEVEL (prioridade máxima - permite alteração fácil em produção)
        /// 2. TraceabilityOptions.MinimumLogLevel (se especificado - usado apenas se env var não estiver presente)
        /// 3. Information (padrão)
        /// </para>
        /// <para>
        /// Valores aceitos para LOG_LEVEL (case-insensitive): Verbose, Debug, Information, Warning, Error, Fatal
        /// </para>
        /// <para>
        /// Exemplo de uso:
        /// </para>
        /// <code>
        /// // Com Source em options
        /// var options = new TraceabilityOptions { Source = "UserService" };
        /// Log.Logger = new LoggerConfiguration()
        ///     .WithTraceabilityJson(options)
        ///     .WriteTo.Console(new JsonFormatter(options))
        ///     .CreateLogger();
        /// 
        /// // Com variável de ambiente TRACEABILITY_SERVICENAME definida
        /// // export TRACEABILITY_SERVICENAME="UserService"
        /// var options = new TraceabilityOptions(); // Source será obtido da env var
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

            // Obtém source seguindo a ordem de prioridade (sem parâmetro source)
            var serviceName = GetServiceName(null, options);
            options.Source = serviceName; // Garante que options.Source está definido

            var minimumLevel = GetMinimumLogLevel(options);

            // Adiciona enrichers
            config = config
                .MinimumLevel.Is(minimumLevel)
                .Enrich.With(new SourceEnricher(serviceName))
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


