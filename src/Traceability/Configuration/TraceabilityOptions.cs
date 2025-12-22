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
        /// </summary>
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
    }
}

