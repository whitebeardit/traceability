namespace Traceability.Core.Interfaces
{
    /// <summary>
    /// Interface para validação de correlation-id.
    /// </summary>
    public interface ICorrelationIdValidator
    {
        /// <summary>
        /// Valida o formato do correlation-id se a validação estiver habilitada.
        /// </summary>
        /// <param name="correlationId">O correlation-id a ser validado.</param>
        /// <param name="options">Opções de configuração.</param>
        /// <returns>true se o correlation-id é válido ou se a validação está desabilitada, false caso contrário.</returns>
        bool Validate(string? correlationId, Configuration.TraceabilityOptions options);
    }
}

