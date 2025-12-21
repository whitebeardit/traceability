#if NET48
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Traceability;

namespace Traceability.WebApi
{
    /// <summary>
    /// MessageHandler para ASP.NET Web API que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdMessageHandler : DelegatingHandler
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";

        /// <summary>
        /// Processa a requisição HTTP.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Tenta obter o correlation-id do header da requisição
            string? correlationId = null;
            if (request.Headers.Contains(CorrelationIdHeader))
            {
                var values = request.Headers.GetValues(CorrelationIdHeader);
                if (values != null)
                {
                    correlationId = values.FirstOrDefault();
                }
            }

            // Se não existir, gera um novo
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir, usa o valor do header
                CorrelationContext.Current = correlationId;
            }

            // Continua o pipeline
            return base.SendAsync(request, cancellationToken).ContinueWith(task =>
            {
                var response = task.Result;
                
                // Adiciona o correlation-id no header da resposta
                if (response != null && !string.IsNullOrEmpty(correlationId))
                {
                    response.Headers.Add(CorrelationIdHeader, correlationId);
                }

                return response;
            }, cancellationToken);
        }
    }
}
#endif

