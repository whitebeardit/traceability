#if NET48
using System;
using System.Net.Http;

namespace Traceability.WebApi
{
    /// <summary>
    /// Classe para extrair route data de requisições HTTP usando reflection.
    /// Extrai a lógica de reflection presente em RouteTemplateHelper.
    /// </summary>
    internal static class HttpRouteDataExtractor
    {
        /// <summary>
        /// Extrai route data de um HttpRequestMessage via MS_HttpRouteData.
        /// </summary>
        public static string? ExtractFromHttpRequestMessage(HttpRequestMessage? request)
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
        /// Extrai route data de um HttpContext via MS_HttpRequestMessage.
        /// </summary>
        public static string? ExtractFromHttpContext(System.Web.HttpContext? context)
        {
            if (context == null) return null;

            try
            {
                var httpRequestMessage = context.Items["MS_HttpRequestMessage"] as HttpRequestMessage;
                if (httpRequestMessage != null)
                {
                    return ExtractFromHttpRequestMessage(httpRequestMessage);
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }
    }
}
#endif

