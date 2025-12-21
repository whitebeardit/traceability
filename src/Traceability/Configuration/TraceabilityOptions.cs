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
    }
}

