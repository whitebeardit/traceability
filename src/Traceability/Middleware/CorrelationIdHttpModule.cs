#if NET48
using System;
using System.Web;
using Traceability;

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";

        /// <summary>
        /// Inicializa o módulo.
        /// </summary>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var request = context.Request;

            // Tenta obter o correlation-id do header da requisição
            var correlationId = request.Headers[CorrelationIdHeader];

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
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // Adiciona o correlation-id no header da resposta
            var correlationId = CorrelationContext.Current;
            if (!string.IsNullOrEmpty(correlationId))
            {
                response.Headers[CorrelationIdHeader] = correlationId;
            }
        }

        /// <summary>
        /// Libera recursos.
        /// </summary>
        public void Dispose()
        {
            // Nada a fazer
        }
    }
}
#endif

