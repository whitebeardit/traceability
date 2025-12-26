#if NET48
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web;
using Traceability;
using Traceability.Configuration;
using Traceability.OpenTelemetry;
using Traceability.WebApi;

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry, implementando funcionalidades que
    /// o OpenTelemetry faz automaticamente no .NET 8 mas não funcionam bem no .NET Framework 4.8.
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {
        private const string ActivityItemKey = "TraceabilityActivity";
        private const string ActivityOwnedKey = "TraceabilityActivityOwned";
        private const string ActivityRenamedKey = "TraceabilityActivityRenamed";
        private const string PreviousActivityKey = "TraceabilityPreviousActivity";

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
            context.PreRequestHandlerExecute += OnPreRequestHandlerExecute;
            context.PostRequestHandlerExecute += OnPostRequestHandlerExecute;
            context.EndRequest += OnEndRequest;
            context.PreSendRequestHeaders += OnPreSendRequestHeaders;
            context.Error += OnError;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var request = context.Request;

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

            // Determine trace context inputs
            var traceparent = request.Headers["traceparent"];
            var tracestate = request.Headers["tracestate"];

            ActivityContext parentFromTraceparent = default;
            if (!string.IsNullOrWhiteSpace(traceparent))
            {
                ActivityContext.TryParse(traceparent, tracestate, out parentFromTraceparent);
            }

            // Decide effective correlation-id for this request:
            // Priority: AlwaysGenerateNew > correlation header > traceparent > generate
            string effectiveCorrelationId;
            if (options.AlwaysGenerateNew)
            {
                effectiveCorrelationId = Guid.NewGuid().ToString("N");
            }
            else if (!string.IsNullOrEmpty(correlationId))
            {
                effectiveCorrelationId = correlationId!;
            }
            else if (parentFromTraceparent != default)
            {
                effectiveCorrelationId = parentFromTraceparent.TraceId.ToString();
            }
            else
            {
                effectiveCorrelationId = Guid.NewGuid().ToString("N");
            }

            // Always set fallback context (so invalid formats can still work when validation disabled)
            CorrelationContext.Current = effectiveCorrelationId;

            // Create server span for every request (NET48 does not reliably provide an ambient Activity for controllers).
            // This keeps behavior consistent and ensures Activity.Current is available for WebApi actions.
            Activity activity = null;

            // Build parent context:
            // - If traceparent exists and no correlation header override, keep real parent
            // - Else, create an artificial parent if correlation id is a W3C trace-id (32 hex)
            ActivityContext parentContext = default;
            if (!options.AlwaysGenerateNew && string.IsNullOrEmpty(correlationId) && parentFromTraceparent != default)
            {
                parentContext = parentFromTraceparent;
            }
            else if (effectiveCorrelationId.Length == 32)
            {
                try
                {
                    var traceId = ActivityTraceId.CreateFromString(effectiveCorrelationId.AsSpan());
                    parentContext = new ActivityContext(traceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
                }
                catch
                {
                    parentContext = default;
                }
            }

            activity = parentContext != default
                ? TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server, parentContext)
                : TraceabilityActivitySource.StartActivity("HTTP Request", ActivityKind.Server);

            if (activity != null)
            {
                context.Items[ActivityItemKey] = activity;
                context.Items[ActivityOwnedKey] = true;

                // Base HTTP tags
                activity.SetTag("http.method", request.HttpMethod);
                activity.SetTag("http.url", request.Url.ToString());
                activity.SetTag("http.scheme", request.Url.Scheme);
                activity.SetTag("http.host", request.Url.Host);
                activity.SetTag("http.user_agent", request.UserAgent);

                // Placeholder so controllers can assert presence during request handling
                activity.SetTag("http.status_code", "0");

                if (request.ContentLength > 0)
                {
                    activity.SetTag("http.request_content_length", request.ContentLength);
                }

                if (!string.IsNullOrEmpty(request.ContentType))
                {
                    activity.SetTag("http.request_content_type", request.ContentType);
                }
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // Retrieve Activity created by us (if any)
            var activity = context.Items[ActivityItemKey] as Activity;
            var owned = context.Items[ActivityOwnedKey] as bool?;
            if (activity != null && owned == true)
            {
                // Try rename to route template (WebApi hosting) once route is resolved
                TryRenameToRouteTemplate(context, activity);

                // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("http.status_code", ((int)response.StatusCode).ToString());

                var contentLength = response.Headers["Content-Length"];
                if (!string.IsNullOrEmpty(contentLength))
                {
                    activity.SetTag("http.response_content_length", contentLength);
                }

                // Parar Activity
                activity.Stop();

                // Remover do HttpContext.Items para liberar memória
                context.Items.Remove(ActivityItemKey);
                context.Items.Remove(ActivityOwnedKey);
                context.Items.Remove(ActivityRenamedKey);
            }
        }

        private void OnPreRequestHandlerExecute(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

            var activity = context.Items[ActivityItemKey] as Activity;
            var owned = context.Items[ActivityOwnedKey] as bool?;
            if (activity != null && owned == true)
            {
                // Ensure Activity.Current is set on the request handler thread so controllers can see it.
                context.Items[PreviousActivityKey] = Activity.Current;
                Activity.Current = activity;

                // Best-effort: tentar obter route template cedo (pode não estar disponível ainda)
                // Se não conseguir, será tentado novamente em OnPostRequestHandlerExecute, OnPreSendRequestHeaders e OnEndRequest
                TryRenameToRouteTemplate(context, activity);
            }
        }

        private void OnPostRequestHandlerExecute(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

            var activity = context.Items[ActivityItemKey] as Activity;
            var owned = context.Items[ActivityOwnedKey] as bool?;
            if (activity != null && owned == true)
            {
                try
                {
                    // Try to rename to route template now that handler has executed (route may be resolved)
                    TryRenameToRouteTemplate(context, activity);

                    // Restore previous activity for safety
                    var previous = context.Items[PreviousActivityKey] as Activity;
                    if (ReferenceEquals(Activity.Current, activity))
                    {
                        Activity.Current = previous;
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    context.Items.Remove(PreviousActivityKey);
                }
            }
        }

        private void OnError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var exception = application.Server.GetLastError();

            // Recuperar Activity do HttpContext.Items (garantido que é o mesmo criado em OnBeginRequest)
            var activity = context.Items[ActivityItemKey] as Activity;
            if (activity != null && exception != null)
            {
                // Adicionar exceção ao Activity (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag("error", "true");
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

            // If we created the activity, attempt to rename it to the route template before headers are sent.
            var activity = context.Items[ActivityItemKey] as Activity;
            var owned = context.Items[ActivityOwnedKey] as bool?;
            if (activity != null && owned == true)
            {
                TryRenameToRouteTemplate(context, activity);

                // Optional debug headers (opt-in) to allow sample tests to assert span naming without exporter.
                var debug = context.Request.Headers["X-Traceability-Debug"];
                if (string.IsNullOrEmpty(debug))
                {
                    debug = context.Request.QueryString["traceabilityDebug"];
                }
                if (!string.IsNullOrEmpty(debug))
                {
                    try
                    {
                        response.Headers["X-Traceability-SpanName"] = activity.DisplayName;
                        response.Headers["X-Traceability-OperationName"] = activity.OperationName;
                        response.Headers["X-Traceability-TraceId"] = activity.TraceId.ToString();
                        response.Headers["X-Traceability-SpanId"] = activity.SpanId.ToString();
                    }
                    catch
                    {
                        // Ignore header failures
                    }
                }
            }

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

        private static void TryRenameToRouteTemplate(HttpContext context, Activity activity)
        {
            if (context == null || activity == null) return;
            
            // Se já renomeamos com sucesso, não tentar novamente
            if (context.Items[ActivityRenamedKey] is bool already && already) return;

            try
            {
                var method = context.Request.HttpMethod;
                string template = null;

                // Tentativa 1: Extrair template do HttpContext (via HttpRequestMessage)
                template = RouteTemplateHelper.TryGetRouteTemplate(context);
                
                // Tentativa 2: Se não encontrou, tentar inferir do path via HttpConfiguration
                if (string.IsNullOrEmpty(template))
                {
                    var path = context.Request.Url != null ? context.Request.Url.AbsolutePath : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        template = RouteTemplateHelper.TryInferRouteTemplateFromPath(path, method);
                    }
                }

                // Se encontrou template, normalizar e aplicar
                if (!string.IsNullOrEmpty(template))
                {
                    var displayName = RouteTemplateHelper.NormalizeDisplayName(method, template);
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        activity.DisplayName = displayName;
                        context.Items[ActivityRenamedKey] = true;
                        return;
                    }
                }

                // Fallback extremo: usar path (mas logar que não encontramos template)
                // Isso só deve acontecer em casos muito raros
                var fallbackPath = context.Request.Url != null ? context.Request.Url.AbsolutePath.TrimStart('/') : "/";
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    activity.DisplayName = RouteTemplateHelper.NormalizeDisplayName(method, fallbackPath) ?? $"{method.ToUpperInvariant()} {fallbackPath}";
                    context.Items[ActivityRenamedKey] = true;
                }
            }
            catch
            {
                // Ignore route naming failures
            }
        }
    }
}
#endif

