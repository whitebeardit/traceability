#if NET48
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Traceability.WebApi
{
    /// <summary>
    /// Extrai templates de rota de ASP.NET MVC 5 Attribute Routing.
    /// Suporta RoutePrefix, rotas absolutas, áreas, e HTTP verb filtering.
    /// </summary>
    internal static class MvcRouteExtractor
    {
        /// <summary>
        /// Cache estático de tipos de controllers para melhorar performance.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Type?> _controllerTypeCache = new();

        /// <summary>
        /// Mapa de HTTP verbs para seus atributos correspondentes.
        /// </summary>
        private static readonly Dictionary<string, Type> _httpVerbAttributeMap = new()
        {
            { "GET", typeof(HttpGetAttribute) },
            { "POST", typeof(HttpPostAttribute) },
            { "PUT", typeof(HttpPutAttribute) },
            { "DELETE", typeof(HttpDeleteAttribute) },
            { "PATCH", typeof(HttpPatchAttribute) }
        };

        /// <summary>
        /// Tenta extrair o template de rota de um controller MVC com Attribute Routing.
        /// </summary>
        /// <param name="httpContext">Contexto HTTP da requisição</param>
        /// <param name="routeTemplate">Template extraído (se encontrado)</param>
        /// <returns>True se conseguiu extrair, False caso contrário</returns>
        public static bool TryExtractMvcRouteTemplate(HttpContext httpContext, out string? routeTemplate)
        {
            routeTemplate = null;

            try
            {
                // 1. Acessar RouteData diretamente (não via HttpRequestMessage)
                var routeData = httpContext.Request.RequestContext?.RouteData;
                if (routeData == null)
                    return false;

                // 2. Extrair controller, action e area do RouteData
                var controllerName = routeData.Values["controller"]?.ToString();
                var actionName = routeData.Values["action"]?.ToString();
                var area = routeData.DataTokens["area"]?.ToString();

                if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
                {
                    return false;
                }

                // 3. Encontrar o tipo do controller (com cache)
                var controllerType = FindControllerTypeWithCache(controllerName!);
                if (controllerType == null)
                {
                    // Fallback: rota convencional
                    routeTemplate = BuildConventionalRoute(controllerName!, actionName!, area);
                    return true;
                }

                // 4. Encontrar o método da action (com HTTP verb matching)
                var httpMethod = httpContext.Request.HttpMethod;
                var actionMethod = FindActionMethod(controllerType, actionName!, httpMethod);
                if (actionMethod == null)
                {
                    // Fallback: rota convencional
                    routeTemplate = BuildConventionalRoute(controllerName!, actionName!, area);
                    return true;
                }

                // 5. Extrair RouteAttribute e fazer match com rota resolvida
                var routeAttribute = MatchResolvedRoute(actionMethod, routeData);
                if (routeAttribute == null)
                {
                    // Se não tem RouteAttribute, usar rota convencional
                    routeTemplate = BuildConventionalRoute(controllerName!, actionName!, area);
                    return true;
                }

                // 6. Construir template completo (com suporte a área)
                routeTemplate = BuildRouteTemplate(controllerType, routeAttribute, area);
                return routeTemplate != null;
            }
            catch
            {
                // Em caso de erro, retornar false para usar fallback
                return false;
            }
        }

        /// <summary>
        /// Constrói uma rota convencional (sem attribute routing).
        /// </summary>
        private static string BuildConventionalRoute(string controllerName, string actionName, string? area)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(area))
                parts.Add(area!);
            
            parts.Add(controllerName);
            parts.Add(actionName);
            
            return "/" + string.Join("/", parts);
        }

        /// <summary>
        /// Constrói o template de rota completo combinando RoutePrefix, RouteAttribute e área.
        /// </summary>
        private static string? BuildRouteTemplate(Type controllerType, RouteAttribute routeAttribute, string? area)
        {
            var routeTemplate = routeAttribute.Template;

            // Rota absoluta (inicia com ~/): ignora prefix e área
            if (routeTemplate?.StartsWith("~/") == true)
            {
                return routeTemplate.Substring(2); // Remove "~/"
            }

            var parts = new List<string>();

            // Adicionar área se presente
            if (!string.IsNullOrEmpty(area))
            {
                parts.Add(area!);
            }

            // Verificar se há RoutePrefix no controller
            var routePrefix = controllerType
                .GetCustomAttributes(typeof(RoutePrefixAttribute), false)
                .OfType<RoutePrefixAttribute>()
                .FirstOrDefault();

            if (routePrefix != null && !string.IsNullOrEmpty(routePrefix.Prefix))
            {
                parts.Add(routePrefix.Prefix!);
            }

            // Se a rota estiver vazia, usar apenas o que temos até agora
            if (string.IsNullOrEmpty(routeTemplate))
            {
                return parts.Count > 0 ? "/" + string.Join("/", parts) : null;
            }

            // Adicionar a rota do atributo
            // Remover leading slash se presente
            var normalizedRoute = routeTemplate!.TrimStart('/');
            if (!string.IsNullOrEmpty(normalizedRoute))
            {
                parts.Add(normalizedRoute);
            }

            return "/" + string.Join("/", parts);
        }

        /// <summary>
        /// Encontra o tipo do controller usando cache para melhorar performance.
        /// </summary>
        private static Type? FindControllerTypeWithCache(string controllerName)
        {
            return _controllerTypeCache.GetOrAdd(controllerName, name =>
            {
                // Buscar apenas se não estiver em cache
                return FindControllerTypeInAssemblies(name);
            });
        }

        /// <summary>
        /// Encontra o tipo do controller baseado no nome, procurando em todos os assemblies carregados.
        /// </summary>
        private static Type? FindControllerTypeInAssemblies(string controllerName)
        {
            try
            {
                var controllerTypeName = $"{controllerName}Controller";
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // Procurar tipo que termina com "Controller" e herda de Controller
                        var controllerType = assembly.GetTypes()
                            .FirstOrDefault(t =>
                                t.Name.Equals(controllerTypeName, StringComparison.OrdinalIgnoreCase) &&
                                typeof(Controller).IsAssignableFrom(t) &&
                                !t.IsAbstract);

                        if (controllerType != null)
                            return controllerType;
                    }
                    catch
                    {
                        // Ignorar erros ao iterar assemblies (alguns podem não ser carregáveis)
                        continue;
                    }
                }
            }
            catch
            {
                // Ignorar erros gerais
            }

            return null;
        }

        /// <summary>
        /// Encontra o método da action no controller, com suporte a HTTP verb filtering.
        /// </summary>
        private static MethodInfo? FindActionMethod(Type controllerType, string actionName, string httpMethod)
        {
            try
            {
                // Procurar métodos públicos que retornam ActionResult ou Task<ActionResult>
                var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m =>
                        m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase) &&
                        (typeof(ActionResult).IsAssignableFrom(m.ReturnType) ||
                         (m.ReturnType.IsGenericType && 
                          m.ReturnType.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>) &&
                          typeof(ActionResult).IsAssignableFrom(m.ReturnType.GetGenericArguments()[0]))))
                    .ToList();

                if (methods.Count == 0)
                    return null;

                // Se houver múltiplos métodos (overload), tentar filtrar por HTTP verb
                if (methods.Count > 1)
                {
                    var methodWithMatchingVerb = methods.FirstOrDefault(m => MatchesHttpVerb(m, httpMethod));
                    if (methodWithMatchingVerb != null)
                        return methodWithMatchingVerb;
                }

                // Retornar o primeiro método encontrado
                return methods[0];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verifica se o método corresponde ao HTTP verb especificado.
        /// </summary>
        private static bool MatchesHttpVerb(MethodInfo method, string httpMethod)
        {
            var upperMethod = httpMethod.ToUpperInvariant();
            
            if (_httpVerbAttributeMap.TryGetValue(upperMethod, out var verbAttributeType))
            {
                return method.GetCustomAttributes(verbAttributeType, false).Length > 0;
            }

            return false;
        }

        /// <summary>
        /// Faz match do RouteAttribute com a rota resolvida no RouteData.
        /// Se houver múltiplas rotas, tenta identificar qual foi usada.
        /// </summary>
        private static RouteAttribute? MatchResolvedRoute(MethodInfo actionMethod, RouteData routeData)
        {
            var routeAttributes = actionMethod
                .GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>()
                .ToList();

            if (routeAttributes.Count == 0)
                return null;

            if (routeAttributes.Count == 1)
                return routeAttributes[0];

            // Múltiplas rotas: tentar identificar qual foi usada
            // Por enquanto, retornar a primeira (pode ser melhorado no futuro)
            // TODO: Melhorar matching usando RouteData.Route para identificar rota exata
            return routeAttributes[0];
        }
    }
}
#endif

