#if NET48
using System;
using System.Web;
using Traceability;
using Traceability.Configuration;

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente.
    /// Lê o correlation-id do header da requisição ou cria um novo se não existir.
    /// Adiciona o correlation-id no header da resposta.
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {
        private static volatile TraceabilityOptions _options = new TraceabilityOptions();
        private static readonly object _optionsLock = new object();
        
        private string CorrelationIdHeader
        {
            get
            {
                var headerName = _options.HeaderName;
                return string.IsNullOrWhiteSpace(headerName) ? "X-Correlation-Id" : headerName;
            }
        }

        /// <summary>
        /// Configura as opções do módulo (deve ser chamado antes do módulo ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            lock (_optionsLock)
            {
                _options = options;
            }
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

            var headerName = CorrelationIdHeader;
            // Tenta obter o correlation-id do header da requisição
            var correlationId = request.Headers[headerName];

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
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            var headerName = CorrelationIdHeader;
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                try
                {
                    // PreSendRequestHeaders é chamado antes de enviar headers, então ainda podemos modificá-los
                    response.Headers[headerName] = correlationId;
                }
                catch
                {
                    // Ignora exceções ao adicionar header (pode ocorrer em casos raros)
                }
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

