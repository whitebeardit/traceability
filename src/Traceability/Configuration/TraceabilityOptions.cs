using Serilog.Events;

namespace Traceability.Configuration
{
    /// <summary>
    /// Formato de saída para logs.
    /// </summary>
    public enum LogOutputFormat
    {
        /// <summary>
        /// JSON compacto (formato padrão do Serilog CompactJsonFormatter).
        /// </summary>
        JsonCompact,

        /// <summary>
        /// JSON indentado (formato legível).
        /// </summary>
        JsonIndented,

        /// <summary>
        /// Texto estruturado (template customizado).
        /// </summary>
        Text
    }

    /// <summary>
    /// Opções de configuração para o pacote de traceability.
    /// </summary>
    public class TraceabilityOptions
    {
        /// <summary>
        /// Nome do header HTTP para correlation-id (padrão: "X-Correlation-Id").
        /// </summary>
        public string HeaderName { get; set; } = "X-Correlation-Id";

        /// <summary>
        /// Se true, gera um novo correlation-id mesmo se já existir um no contexto (padrão: false).
        /// </summary>
        public bool AlwaysGenerateNew { get; set; } = false;

        /// <summary>
        /// Se true, valida o formato do correlation-id recebido no header (padrão: false).
        /// Quando habilitado, valida que o correlation-id não seja vazio e tenha tamanho máximo de 128 caracteres.
        /// </summary>
        public bool ValidateCorrelationIdFormat { get; set; } = false;

        /// <summary>
        /// Nome da origem/serviço que está gerando os logs (opcional, mas recomendado).
        /// Este valor será adicionado a todos os logs para identificar a origem em ambientes distribuídos.
        /// Se não especificado, será lido da variável de ambiente TRACEABILITY_SERVICENAME.
        /// Se nem esta propriedade nem a variável de ambiente estiverem definidas, uma exceção será lançada
        /// para forçar o padrão único de logs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A prioridade de configuração para Source é:
        /// 1. Parâmetro source (se fornecido explicitamente nos métodos de extensão) - prioridade máxima
        /// 2. Esta propriedade Source (se definida)
        /// 3. Variável de ambiente TRACEABILITY_SERVICENAME
        /// Se nenhum estiver disponível, uma exceção será lançada para garantir uniformização de logs.
        /// </para>
        /// <para>
        /// Exemplo de uso da variável de ambiente:
        /// <code>
        /// // Linux/Mac
        /// export TRACEABILITY_SERVICENAME="UserService"
        /// 
        /// // Windows PowerShell
        /// $env:TRACEABILITY_SERVICENAME="UserService"
        /// </code>
        /// </para>
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Formato de saída para logs (padrão: JsonCompact).
        /// </summary>
        public LogOutputFormat LogOutputFormat { get; set; } = LogOutputFormat.JsonCompact;

        /// <summary>
        /// Se deve incluir timestamp nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeTimestamp { get; set; } = true;

        /// <summary>
        /// Se deve incluir level nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeLevel { get; set; } = true;

        /// <summary>
        /// Se deve incluir Source nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeSource { get; set; } = true;

        /// <summary>
        /// Se deve incluir CorrelationId nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeCorrelationId { get; set; } = true;

        /// <summary>
        /// Se deve incluir Message nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeMessage { get; set; } = true;

        /// <summary>
        /// Se deve incluir campo Data para objetos serializados nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeData { get; set; } = true;

        /// <summary>
        /// Se deve incluir Exception nos logs (padrão: true).
        /// </summary>
        public bool LogIncludeException { get; set; } = true;

        /// <summary>
        /// Nível mínimo de log para filtrar eventos.
        /// Se não especificado, será lido da variável de ambiente LOG_LEVEL (prioridade máxima)
        /// ou usado Information como padrão.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A prioridade de configuração é:
        /// 1. Variável de ambiente LOG_LEVEL (prioridade máxima - permite alteração fácil em produção)
        /// 2. Esta propriedade MinimumLogLevel (usado apenas se env var não estiver presente)
        /// 3. Information (padrão)
        /// </para>
        /// <para>
        /// Valores aceitos para LOG_LEVEL (case-insensitive): Verbose, Debug, Information, Warning, Error, Fatal
        /// </para>
        /// </remarks>
        public LogEventLevel? MinimumLogLevel { get; set; }

        /// <summary>
        /// Se false, desabilita o registro automático do middleware CorrelationIdMiddleware.
        /// Padrão: true (middleware é registrado automaticamente via IStartupFilter).
        /// Defina como false apenas se precisar de controle manual sobre a ordem do middleware.
        /// </summary>
        public bool AutoRegisterMiddleware { get; set; } = true;

        /// <summary>
        /// Se false, desabilita a configuração automática de todos os HttpClients com CorrelationIdHandler.
        /// Padrão: true (todos os HttpClients criados via IHttpClientFactory terão o handler automaticamente).
        /// Defina como false apenas se precisar configurar HttpClients manualmente.
        /// </summary>
        public bool AutoConfigureHttpClient { get; set; } = true;

        /// <summary>
        /// Se false, desabilita o uso do assembly name como fallback para Source quando nenhum Source for fornecido.
        /// Padrão: true (usa assembly name se nenhum Source estiver disponível).
        /// </summary>
        public bool UseAssemblyNameAsFallback { get; set; } = true;
    }
}

