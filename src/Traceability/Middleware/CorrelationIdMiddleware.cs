#if NET8_0
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Traceability;
using Traceability.Configuration;
using Traceability.OpenTelemetry;

namespace Traceability.Middleware
{
    /// <summary>
    /// Middleware para ASP.NET Core que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry quando OpenTelemetry não está configurado.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TraceabilityOptions _options;

        /// <summary>
        /// Cria uma nova instância do CorrelationIdMiddleware.
        /// </summary>
        /// <param name="next">O próximo middleware no pipeline.</param>
        /// <param name="options">Opções de configuração (opcional, usa padrão se não fornecido).</param>
        public CorrelationIdMiddleware(RequestDelegate next, IOptions<TraceabilityOptions>? options = null)
        {
            _next = next;
            _options = options?.Value ?? new TraceabilityOptions();
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
            if (correlationId.Length > 128)
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
        /// Processa a requisição HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Criar Activity automaticamente se não existir (quando OpenTelemetry não está configurado)
            // Se OpenTelemetry estiver configurado, Activity já será criado automaticamente
            Activity? activity = null;
            if (Activity.Current == null)
            {
                activity = TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);
                
                if (activity != null)
                {
                    // Adicionar tags padrão (igual ao que OpenTelemetry faz automaticamente)
                    activity.SetTag("http.method", context.Request.Method);
                    activity.SetTag("http.url", context.Request.Path.ToString());
                    activity.SetTag("http.scheme", context.Request.Scheme);
                    activity.SetTag("http.host", context.Request.Host.ToString());
                    
                    if (context.Request.Headers.ContainsKey("User-Agent"))
                    {
                        activity.SetTag("http.user_agent", context.Request.Headers["User-Agent"].ToString());
                    }
                    
                    if (context.Request.ContentLength.HasValue)
                    {
                        activity.SetTag("http.request_content_length", context.Request.ContentLength.Value);
                    }
                    
                    if (!string.IsNullOrEmpty(context.Request.ContentType))
                    {
                        activity.SetTag("http.request_content_type", context.Request.ContentType);
                    }
                }
            }

            // Valida e obtém o nome do header (usa padrão se null/vazio)
            var headerName = string.IsNullOrWhiteSpace(_options.HeaderName) 
                ? "X-Correlation-Id" 
                : _options.HeaderName;
            
            // Tenta obter o correlation-id do header da requisição
            var correlationId = context.Request.Headers[headerName].FirstOrDefault();
            
            // Valida formato se habilitado
            if (!string.IsNullOrEmpty(correlationId) && !IsValidCorrelationId(correlationId))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }
            
            // Se não existir ou AlwaysGenerateNew estiver habilitado, gera um novo
            if (string.IsNullOrEmpty(correlationId) || _options.AlwaysGenerateNew)
            {
                // CorrelationContext.Current agora retorna trace-id se Activity existir
                correlationId = CorrelationContext.GetOrCreate();
            }
            else
            {
                // Se existir, usa o valor do header
                CorrelationContext.Current = correlationId;
            }

            // Adiciona o correlation-id no header da resposta (antes de chamar o próximo middleware)
            // Verifica se ainda é possível modificar headers
            if (!context.Response.HasStarted)
            {
                try
                {
                    context.Response.Headers[headerName] = correlationId;
                }
                catch
                {
                    // Ignora exceções ao adicionar header (pode ocorrer se headers já foram enviados)
                }
            }

            try
            {
                // Continua o pipeline
                await _next(context);
                
                // Adicionar status code ao Activity
                var currentActivity = Activity.Current ?? activity;
                if (currentActivity != null)
                {
                    currentActivity.SetTag("http.status_code", (int)context.Response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Adicionar exceção ao Activity
                var currentActivity = Activity.Current ?? activity;
                if (currentActivity != null)
                {
                    currentActivity.SetTag("error", true);
                    currentActivity.SetTag("error.type", ex.GetType().Name);
                    currentActivity.SetTag("error.message", ex.Message);
                    currentActivity.SetStatus(ActivityStatusCode.Error, ex.Message);
                }
                throw;
            }
            finally
            {
                // Parar Activity se foi criado por nós
                activity?.Stop();
            }
        }
    }
}
#endif

