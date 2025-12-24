#if NET48
using System;
using System.Diagnostics;
using System.Web;
using Traceability;
using Traceability.Configuration;
using Traceability.OpenTelemetry;

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry, implementando funcionalidades que
    /// o OpenTelemetry faz automaticamente no .NET 8 mas não funcionam bem no .NET Framework 4.8.
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {
        private static volatile TraceabilityOptions _options = new TraceabilityOptions();
        private static readonly object _optionsLock = new object();
        
        private string CorrelationIdHeader
        {
            get
            {
                // Thread-safe: ler _options uma vez para evitar race condition
                var options = _options;
                var headerName = options.HeaderName;
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
            // Thread-safe: ler _options uma vez para evitar race condition
            var options = _options;
            if (!options.ValidateCorrelationIdFormat)
                return true;

            if (string.IsNullOrEmpty(correlationId))
                return false;

            // Valida tamanho máximo (128 caracteres)
            if (correlationId!.Length > 128)
                return false;

            // Valida que não contém caracteres inválidos (apenas alfanuméricos, hífens e underscores)
            // Permite GUIDs (com ou sem hífens), trace-ids W3C (32 hex chars), e outros formatos válidos
            for (int i = 0; i < correlationId.Length; i++)
            {
                var c = correlationId[i];
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inicializa o módulo.
        /// </summary>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
            context.Error += OnError;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var request = context.Request;

            // IMPORTANTE: Ler o header PRIMEIRO para garantir que o trace-id do header tenha prioridade
            // Isso garante que o trace-id seja propagado corretamente entre serviços
            var headerName = CorrelationIdHeader;
            var correlationId = request.Headers[headerName];

            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationId) && !IsValidCorrelationId(correlationId))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }

            // Thread-safe: ler _options uma vez para evitar race condition
            var options = _options;
            
            // Criar Activity automaticamente (OpenTelemetry) - igual ao que .NET 8 faz
            var activity = TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);
            
            // Armazenar Activity no HttpContext.Items para garantir que o mesmo Activity seja usado em OnEndRequest
            if (activity != null)
            {
                context.Items["TraceabilityActivity"] = activity;
                
                // Adicionar tags padrão (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("http.method", request.HttpMethod);
                activity.SetTag("http.url", request.Url.ToString());
                activity.SetTag("http.scheme", request.Url.Scheme);
                activity.SetTag("http.host", request.Url.Host);
                activity.SetTag("http.user_agent", request.UserAgent);
                
                if (request.ContentLength > 0)
                {
                    activity.SetTag("http.request_content_length", request.ContentLength);
                }
                
                if (!string.IsNullOrEmpty(request.ContentType))
                {
                    activity.SetTag("http.request_content_type", request.ContentType);
                }
                
                // Ler traceparent header (W3C Trace Context) se existir
                var traceParent = request.Headers["traceparent"];
                if (!string.IsNullOrEmpty(traceParent))
                {
                    // OpenTelemetry já processa traceparent automaticamente via DiagnosticSource
                    // Mas garantimos que Activity está criado
                }
            }
            
            // Se não existir ou AlwaysGenerateNew estiver habilitado, gera um novo
            if (string.IsNullOrEmpty(correlationId) || options.AlwaysGenerateNew)
            {
                // CorrelationContext.Current já retorna trace-id se Activity existir
                // Se não houver Activity válido, gera um novo GUID
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir no header, usa o valor do header (prioridade sobre Activity)
                // Isso garante que o trace-id seja propagado corretamente entre serviços
                CorrelationContext.Current = correlationId!;
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // Recuperar Activity do HttpContext.Items (garantido que é o mesmo criado em OnBeginRequest)
            var activity = context.Items["TraceabilityActivity"] as Activity;
            if (activity != null)
            {
                // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("http.status_code", (int)response.StatusCode);
                
                var contentLength = response.Headers["Content-Length"];
                if (!string.IsNullOrEmpty(contentLength))
                {
                    activity.SetTag("http.response_content_length", contentLength);
                }
                
                // Parar Activity
                activity.Stop();
                
                // Remover do HttpContext.Items para liberar memória
                context.Items.Remove("TraceabilityActivity");
            }
        }

        private void OnError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var exception = application.Server.GetLastError();

            // Recuperar Activity do HttpContext.Items (garantido que é o mesmo criado em OnBeginRequest)
            var activity = context.Items["TraceabilityActivity"] as Activity;
            if (activity != null && exception != null)
            {
                // Adicionar exceção ao Activity (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("error", true);
                activity.SetTag("error.type", exception.GetType().Name);
                activity.SetTag("error.message", exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
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

