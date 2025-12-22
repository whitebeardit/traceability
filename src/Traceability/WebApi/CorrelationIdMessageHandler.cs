#if NET48
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Traceability;
using Traceability.Configuration;

namespace Traceability.WebApi
{
    /// <summary>
    /// MessageHandler para ASP.NET Web API que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdMessageHandler : DelegatingHandler
    {
        private static TraceabilityOptions _options = new TraceabilityOptions();
        private string CorrelationIdHeader => _options.HeaderName;

        /// <summary>
        /// Configura as opções do handler (deve ser chamado antes do handler ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _options = options;
        }

        /// <summary>
        /// Valida o formato do correlation-id se a validação estiver habilitada.
        /// </summary>
        private bool IsValidCorrelationId(string? correlationId)
        {
            if (!_options.ValidateCorrelationIdFormat)
                return true;

            if (string.IsNullOrEmpty(correlationId))
                return false;

            // Valida tamanho máximo (128 caracteres)
            if (correlationId!.Length > 128)
                return false;

            return true;
        }

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var headerName = CorrelationIdHeader;
            
            // Tenta obter o correlation-id do header da requisição
            string? correlationId = null;
            if (request.Headers.Contains(headerName))
            {
                var values = request.Headers.GetValues(headerName);
                if (values != null)
                {
                    correlationId = values.FirstOrDefault();
                }
            }

            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationId) && !IsValidCorrelationId(correlationId))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }

            // Se não existir ou AlwaysGenerateNew estiver habilitado, gera um novo
            if (string.IsNullOrEmpty(correlationId) || _options.AlwaysGenerateNew)
            {
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir, usa o valor do header
                CorrelationContext.Current = correlationId!;
            }

            // Continua o pipeline usando async/await para melhor propagação de exceções
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            
            // Adiciona o correlation-id no header da resposta
            if (response != null && !string.IsNullOrEmpty(correlationId))
            {
                try
                {
                    // Verifica se o header já existe antes de adicionar
                    if (response.Headers.Contains(headerName))
                    {
                        response.Headers.Remove(headerName);
                    }
                    response.Headers.Add(headerName, correlationId);
                }
                catch
                {
                    // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                }
            }

            return response!;
        }
    }
}
#endif

