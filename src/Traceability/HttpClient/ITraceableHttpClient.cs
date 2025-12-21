using System.Net.Http;

namespace Traceability.HttpClient
{
    /// <summary>
    /// Interface para HttpClient com correlation-id automático.
    /// </summary>
    public interface ITraceableHttpClient
    {
        /// <summary>
        /// Obtém a instância do HttpClient configurada com correlation-id.
        /// </summary>
        System.Net.Http.HttpClient Client { get; }
    }
}

