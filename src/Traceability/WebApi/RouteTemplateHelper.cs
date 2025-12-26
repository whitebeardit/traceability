#if NET48
using System;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Traceability.WebApi
{
    /// <summary>
    /// Helper para extrair route templates de requisições HTTP em ASP.NET Web API.
    /// Suporta múltiplas fontes e inferência via HttpConfiguration quando necessário.
    /// </summary>
    internal class RouteTemplateHelper : IRouteTemplateResolver
    {
        /// <summary>
        /// Instância padrão do resolver para uso quando não há injeção de dependência.
        /// </summary>
        public static readonly IRouteTemplateResolver Default = new RouteTemplateHelper();

        // Métodos estáticos para compatibilidade (mantidos para não quebrar código existente)
        // Estes são os métodos públicos que o código existente usa
        /// <summary>
        /// Tenta extrair o route template de um HttpRequestMessage.
        /// </summary>
        public static string? TryGetRouteTemplate(HttpRequestMessage? request)
        {
            return HttpRouteDataExtractor.ExtractFromHttpRequestMessage(request);
        }

        /// <summary>
        /// Tenta extrair o route template de um HttpContext.
        /// </summary>
        public static string? TryGetRouteTemplate(HttpContext? context)
        {
            return HttpRouteDataExtractor.ExtractFromHttpContext(context);
        }

        /// <summary>
        /// Tenta inferir o route template fazendo match do path com rotas registradas no HttpConfiguration.
        /// </summary>
        public static string? TryInferRouteTemplateFromPath(string? path, string? method)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(method))
                return null;

            try
            {
                // Acessar GlobalConfiguration.Configuration via reflection (pode não estar disponível diretamente)
                var globalConfigType = Type.GetType("System.Web.Http.GlobalConfiguration, System.Web.Http", false);
                if (globalConfigType == null)
                    return null;

                var configProperty = globalConfigType.GetProperty("Configuration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (configProperty == null)
                    return null;

                var config = configProperty.GetValue(null);
                if (config == null)
                    return null;

                var routesProperty = config.GetType().GetProperty("Routes");
                if (routesProperty == null)
                    return null;

                var routes = routesProperty.GetValue(config);
                if (routes == null)
                    return null;

                // Normalizar path (remover leading slash)
                var normalizedPath = path!.TrimStart('/');

                // Tentar fazer match com cada rota registrada
                var enumerable = routes as System.Collections.IEnumerable;
                if (enumerable == null)
                    return null;

                foreach (var route in enumerable)
                {
                    try
                    {
                        var routeTemplate = RouteMatcher.GetRouteTemplate(route);
                        if (string.IsNullOrEmpty(routeTemplate))
                            continue;

                        // Verificar se o path corresponde ao template
                        if (RouteMatcher.MatchesRouteTemplate(normalizedPath, routeTemplate!))
                        {
                            return routeTemplate;
                        }
                    }
                    catch
                    {
                        // Ignore individual route failures
                        continue;
                    }
                }
            }
            catch
            {
                // Ignore inference failures
            }

            return null;
        }

        /// <summary>
        /// Normaliza o formato do display name: {METHOD} {template}
        /// </summary>
        public static string? NormalizeDisplayName(string? method, string? template)
        {
            if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(template))
                return null;

            // Method sempre uppercase
            var normalizedMethod = method!.ToUpperInvariant();

            // Template sem leading slash
            var normalizedTemplate = template!.TrimStart('/');

            return $"{normalizedMethod} {normalizedTemplate}";
        }

        // Implementação explícita da interface (para permitir injeção de dependência se necessário)
        /// <summary>
        /// Tenta extrair o route template de um HttpRequestMessage (implementação da interface).
        /// </summary>
        string? IRouteTemplateResolver.TryGetRouteTemplate(HttpRequestMessage? request)
        {
            return TryGetRouteTemplate(request);
        }

        /// <summary>
        /// Tenta extrair o route template de um HttpContext (implementação da interface).
        /// </summary>
        string? IRouteTemplateResolver.TryGetRouteTemplate(HttpContext? context)
        {
            return TryGetRouteTemplate(context);
        }

        /// <summary>
        /// Tenta inferir o route template fazendo match do path (implementação da interface).
        /// </summary>
        string? IRouteTemplateResolver.TryInferRouteTemplateFromPath(string? path, string? method)
        {
            return TryInferRouteTemplateFromPath(path, method);
        }

        /// <summary>
        /// Normaliza o formato do display name (implementação da interface).
        /// </summary>
        string? IRouteTemplateResolver.NormalizeDisplayName(string? method, string? template)
        {
            return NormalizeDisplayName(method, template);
        }
    }
}
#endif
