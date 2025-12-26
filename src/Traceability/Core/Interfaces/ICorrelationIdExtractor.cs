namespace Traceability.Core.Interfaces
{
    /// <summary>
    /// Interface para extrair correlation-id de requisições HTTP.
    /// </summary>
    public interface ICorrelationIdExtractor
    {
        /// <summary>
        /// Tenta extrair o correlation-id do header da requisição.
        /// </summary>
        /// <param name="request">A requisição HTTP.</param>
        /// <param name="headerName">Nome do header a ser lido.</param>
        /// <returns>O correlation-id se encontrado, null caso contrário.</returns>
        string? ExtractCorrelationId(object request, string headerName);
    }
}

