#if NET48
using System;
using System.Collections;

namespace Traceability.WebApi
{
    /// <summary>
    /// Classe para fazer match de paths com route templates.
    /// Extrai a lógica de matching presente em RouteTemplateHelper.
    /// </summary>
    internal static class RouteMatcher
    {
        /// <summary>
        /// Obtém o route template de um objeto route via reflection.
        /// </summary>
        public static string? GetRouteTemplate(object? route)
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

        /// <summary>
        /// Verifica se um path corresponde a um route template.
        /// </summary>
        public static bool MatchesRouteTemplate(string path, string template)
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

