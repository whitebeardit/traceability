using System;
using System.Reflection;
using System.Text;
using Traceability.Configuration;

namespace Traceability.Utilities
{
    /// <summary>
    /// Utilitários compartilhados para o pacote Traceability.
    /// </summary>
    internal static class TraceabilityUtilities
    {
        private const string ServiceNameEnvironmentVariable = "TRACEABILITY_SERVICENAME";
        private const int MaxSourceLength = 100;

        /// <summary>
        /// Obtém o ServiceName (Source) seguindo a ordem de prioridade:
        /// 1) Parâmetro source (se fornecido e não vazio)
        /// 2) options.Source (se definido)
        /// 3) Variável de ambiente TRACEABILITY_SERVICENAME
        /// 4) Assembly name (se UseAssemblyNameAsFallback = true)
        /// Se nenhum estiver disponível, lança InvalidOperationException.
        /// O Source retornado é automaticamente sanitizado para garantir segurança.
        /// </summary>
        /// <param name="source">Source fornecido como parâmetro (opcional).</param>
        /// <param name="options">Opções de traceability (opcional).</param>
        /// <returns>O ServiceName configurado e sanitizado.</returns>
        /// <exception cref="InvalidOperationException">Lançado quando nenhum source está disponível.</exception>
        public static string GetServiceName(string? source, TraceabilityOptions? options = null)
        {
            string? rawSource = null;

            // Prioridade 1: Parâmetro source (se fornecido e não vazio)
            if (!string.IsNullOrWhiteSpace(source))
            {
                rawSource = source;
            }
            // Prioridade 2: options.Source (se definido)
            else if (options != null && !string.IsNullOrWhiteSpace(options.Source))
            {
                rawSource = options.Source;
            }
            // Prioridade 3: Variável de ambiente TRACEABILITY_SERVICENAME
            else
            {
                var envValue = Environment.GetEnvironmentVariable(ServiceNameEnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    rawSource = envValue;
                }
                // Prioridade 4: Assembly name (se UseAssemblyNameAsFallback = true)
                else if (options == null || options.UseAssemblyNameAsFallback)
                {
                    var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        rawSource = assemblyName;
                    }
                }
            }

            // Se encontrou um source, sanitiza e retorna
            if (!string.IsNullOrWhiteSpace(rawSource))
            {
                return SanitizeSource(rawSource!); // rawSource não é null aqui devido à verificação acima
            }

            // Se nenhum estiver disponível, lançar erro
            throw new InvalidOperationException(
                $"Source (ServiceName) must be provided either as a parameter, in TraceabilityOptions.Source, via the {ServiceNameEnvironmentVariable} environment variable, or (if UseAssemblyNameAsFallback = true) it will use the assembly name. " +
                "At least one of these must be specified to ensure uniform logging across all applications and services.");
        }

        /// <summary>
        /// Sanitiza o Source removendo ou substituindo caracteres inválidos.
        /// Garante que o Source seja seguro para uso em logs e headers HTTP.
        /// </summary>
        /// <param name="source">Source a ser sanitizado.</param>
        /// <returns>Source sanitizado e seguro para uso.</returns>
        public static string SanitizeSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return source ?? string.Empty;

            // Remove caracteres de controle e espaços em branco
            var sanitized = new StringBuilder(source.Length);
            foreach (var c in source)
            {
                // Permite apenas caracteres alfanuméricos, hífen, underscore e ponto
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                {
                    sanitized.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    // Substitui espaços por underscore
                    sanitized.Append('_');
                }
                // Ignora outros caracteres (incluindo caracteres de controle)
            }

            var result = sanitized.ToString();

            // Limita o tamanho máximo
            if (result.Length > MaxSourceLength)
            {
                result = result.Substring(0, MaxSourceLength);
            }

            // Se após sanitização ficou vazio, usa fallback
            if (string.IsNullOrWhiteSpace(result))
            {
                result = "Unknown";
            }

            return result;
        }
    }
}

