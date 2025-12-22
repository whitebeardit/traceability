namespace Traceability.Configuration
{
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
    }
}

