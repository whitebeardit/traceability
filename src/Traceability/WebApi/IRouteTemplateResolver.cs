#if NET48
using System.Net.Http;
using System.Web;

namespace Traceability.WebApi
{
    /// <summary>
    /// Interface para resolução de route templates de requisições HTTP.
    /// </summary>
    public interface IRouteTemplateResolver
    {
        /// <summary>
        /// Tenta extrair o route template de um HttpRequestMessage.
        /// </summary>
        string? TryGetRouteTemplate(HttpRequestMessage? request);

        /// <summary>
        /// Tenta extrair o route template de um HttpContext.
        /// </summary>
        string? TryGetRouteTemplate(HttpContext? context);

        /// <summary>
        /// Tenta inferir o route template fazendo match do path com rotas registradas no HttpConfiguration.
        /// </summary>
        string? TryInferRouteTemplateFromPath(string? path, string? method);

        /// <summary>
        /// Normaliza o formato do display name: {METHOD} {template}
        /// </summary>
        string? NormalizeDisplayName(string? method, string? template);
    }
}
#endif

