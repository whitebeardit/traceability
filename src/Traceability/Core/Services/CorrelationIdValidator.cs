using Traceability.Configuration;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de validação de correlation-id.
    /// Extrai a lógica duplicada de validação presente em CorrelationIdMiddleware, CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// </summary>
    internal class CorrelationIdValidator : Core.Interfaces.ICorrelationIdValidator
    {
        /// <summary>
        /// Valida o formato do correlation-id se a validação estiver habilitada.
        /// Lógica copiada EXATAMENTE de CorrelationIdMiddleware.IsValidCorrelationId.
        /// </summary>
        public bool Validate(string? correlationId, TraceabilityOptions options)
        {
            if (!options.ValidateCorrelationIdFormat)
                return true;

            if (string.IsNullOrEmpty(correlationId))
                return false;

            // Valida tamanho máximo (128 caracteres)
            // correlationId não é null aqui devido à verificação acima
            if (correlationId!.Length > 128)
                return false;

            // Valida que não contém caracteres inválidos (apenas alfanuméricos, hífens e underscores)
            // Permite GUIDs (com ou sem hífens), trace-ids W3C (32 hex chars), e outros formatos válidos
            for (int i = 0; i < correlationId.Length; i++)
            {
                var c = correlationId[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }
    }
}

