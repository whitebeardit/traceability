using Serilog;
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
    }
}


