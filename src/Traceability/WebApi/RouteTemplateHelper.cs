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
    internal static class RouteTemplateHelper
    {
        /// <summary>
        /// Tenta extrair o route template de um HttpRequestMessage.
        /// Tenta múltiplas fontes em ordem de prioridade.
        /// </summary>
        public static string? TryGetRouteTemplate(HttpRequestMessage? request)
        {
            if (request == null) return null;

            // Fonte 1: MS_HttpRouteData (mais comum no WebApi v2)
            try
            {
                if (request.Properties != null && request.Properties.TryGetValue("MS_HttpRouteData", out var routeData) && routeData != null)
                {
                    var routeProp = routeData.GetType().GetProperty("Route");
                    var route = routeProp != null ? routeProp.GetValue(routeData) : null;
                    if (route != null)
                    {
                        var templateProp = route.GetType().GetProperty("RouteTemplate");
                        var template = templateProp != null ? templateProp.GetValue(route) as string : null;
                        if (!string.IsNullOrEmpty(template))
                        {
                            return template;
                        }
                    }
                }
            }
            catch
            {
                // Ignore reflection failures
            }

            // Fonte 2: RequestContext.RouteData (via reflection para compatibilidade)
            try
            {
                // Tentar acessar RequestContext via Properties
                if (request.Properties != null && request.Properties.TryGetValue("MS_RequestContext", out var requestContext) && requestContext != null)
                {
                    var routeDataProp = requestContext.GetType().GetProperty("RouteData");
                    var routeData = routeDataProp != null ? routeDataProp.GetValue(requestContext) : null;
                    if (routeData != null)
                    {
                        var routeProp = routeData.GetType().GetProperty("Route");
                        var route = routeProp != null ? routeProp.GetValue(routeData) : null;
                        if (route != null)
                        {
                            var templateProp = route.GetType().GetProperty("RouteTemplate");
                            var template = templateProp != null ? templateProp.GetValue(route) as string : null;
                            if (!string.IsNullOrEmpty(template))
                            {
                                return template;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore if reflection fails
            }

            return null;
        }

        /// <summary>
        /// Tenta extrair o route template de um HttpContext.
        /// </summary>
        public static string? TryGetRouteTemplate(HttpContext? context)
        {
            if (context == null) return null;

            // Fonte 1: HttpRequestMessage armazenado no HttpContext.Items
            try
            {
                var httpRequestMessage = context.Items["MS_HttpRequestMessage"] as HttpRequestMessage;
                if (httpRequestMessage != null)
                {
                    var template = TryGetRouteTemplate(httpRequestMessage);
                    if (!string.IsNullOrEmpty(template))
                    {
                        return template;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return null;
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
                // path já foi verificado como não-null na linha 111, mas o compilador não sabe
                var normalizedPath = path!.TrimStart('/');

                // Tentar fazer match com cada rota registrada
                var enumerable = routes as System.Collections.IEnumerable;
                if (enumerable == null)
                    return null;

                foreach (var route in enumerable)
                {
                    try
                    {
                        var routeTemplate = GetRouteTemplate(route);
                        if (string.IsNullOrEmpty(routeTemplate))
                            continue;

                        // Verificar se o path corresponde ao template
                        // routeTemplate já foi verificado como não-null acima
                        if (MatchesRouteTemplate(normalizedPath, routeTemplate!))
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
            // method já foi verificado como não-null acima
            var normalizedMethod = method!.ToUpperInvariant();

            // Template sem leading slash
            // template já foi verificado como não-null acima
            var normalizedTemplate = template!.TrimStart('/');

            return $"{normalizedMethod} {normalizedTemplate}";
        }

        private static string? GetRouteTemplate(object? route)
        {
            if (route == null) return null;

            try
            {
                // Tentar obter RouteTemplate via reflection
                var templateProp = route.GetType().GetProperty("RouteTemplate");
                if (templateProp != null)
                {
                    return templateProp.GetValue(route) as string;
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        private static bool MatchesRouteTemplate(string path, string template)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(template))
                return false;

            // Algoritmo simples de match: converter template para regex pattern básico
            // Ex: "api/{controller}/{id}" -> "api/[^/]+/[^/]+"
            try
            {
                var pattern = template;
                
                // Substituir placeholders por padrões de match
                // {controller}, {id}, etc. -> [^/]+
                var placeholderPattern = @"\{[^}]+\}";
                pattern = System.Text.RegularExpressions.Regex.Replace(pattern, placeholderPattern, "[^/]+");

                // Escapar caracteres especiais do regex (exceto os que já adicionamos)
                pattern = System.Text.RegularExpressions.Regex.Escape(pattern);
                // Reverter escape dos placeholders que adicionamos
                pattern = pattern.Replace(@"\[^/\]\+", "[^/]+");

                // Fazer match
                var regex = new System.Text.RegularExpressions.Regex($"^{pattern}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return regex.IsMatch(path);
            }
            catch
            {
                // Se falhar, fazer match simples por prefixo
                // Ex: path "api/test-all/run-all" vs template "api/{controller}/{id}"
                var templateParts = template.Split('/');
                var pathParts = path.Split('/');

                if (templateParts.Length != pathParts.Length)
                    return false;

                for (int i = 0; i < templateParts.Length; i++)
                {
                    var templatePart = templateParts[i];
                    var pathPart = pathParts[i];

                    // Se não for placeholder, deve ser exato match
                    if (!templatePart.StartsWith("{") || !templatePart.EndsWith("}"))
                    {
                        if (!string.Equals(templatePart, pathPart, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }

                return true;
            }
        }
    }
}
#endif

