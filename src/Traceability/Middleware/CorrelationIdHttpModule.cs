#if NET48
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web;
using Traceability;
using Traceability.Configuration;
using Traceability.Core;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Traceability.OpenTelemetry;
using Traceability.WebApi;
#if NET48
using StaticTraceabilityOptionsProvider = Traceability.Configuration.StaticTraceabilityOptionsProvider;
#endif

namespace Traceability.Middleware
{
    /// <summary>
    /// HttpModule para ASP.NET tradicional que gerencia correlation-id automaticamente
    /// e cria Activities (spans) do OpenTelemetry, implementando funcionalidades que
    /// o OpenTelemetry faz automaticamente no .NET 8 mas não funcionam bem no .NET Framework 4.8.
    /// </summary>
    public class CorrelationIdHttpModule : IHttpModule
    {

        private readonly ITraceabilityOptionsProvider _optionsProvider;
        private readonly ICorrelationIdValidator _validator;
        private readonly ICorrelationIdExtractor _extractor;
        private readonly IActivityFactory _activityFactory;
        private readonly IActivityTagProvider _tagProvider;

        private string CorrelationIdHeader
        {
            get
            {
                // Thread-safe: ler options uma vez para evitar race condition
                var options = _optionsProvider.GetOptions();
                var headerName = options.HeaderName;
                return string.IsNullOrWhiteSpace(headerName) ? Constants.HttpHeaders.CorrelationId : headerName;
            }
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHttpModule.
        /// Construtor sem parâmetros para compatibilidade com registro automático via DynamicModuleUtility.
        /// </summary>
        public CorrelationIdHttpModule()
            : this(null, null, null, null, null)
        {
        }

        /// <summary>
        /// Cria uma nova instância do CorrelationIdHttpModule.
        /// </summary>
        /// <param name="optionsProvider">Provider de opções (opcional, usa StaticTraceabilityOptionsProvider como padrão).</param>
        /// <param name="validator">Validador de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="extractor">Extrator de correlation-id (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="activityFactory">Factory de Activities (opcional, cria instância padrão se não fornecido).</param>
        /// <param name="tagProvider">Provider de tags HTTP (opcional, cria instância padrão se não fornecido).</param>
        public CorrelationIdHttpModule(
            ITraceabilityOptionsProvider? optionsProvider,
            ICorrelationIdValidator? validator,
            ICorrelationIdExtractor? extractor,
            IActivityFactory? activityFactory,
            IActivityTagProvider? tagProvider)
        {
            _optionsProvider = optionsProvider ?? StaticTraceabilityOptionsProvider.Instance;
            _validator = validator ?? new CorrelationIdValidator();
            _extractor = extractor ?? new HttpRequestMessageCorrelationIdExtractor();
            _activityFactory = activityFactory ?? new TraceabilityActivityFactory();
            _tagProvider = tagProvider ?? new HttpActivityTagProvider();
        }

        /// <summary>
        /// Configura as opções do módulo (deve ser chamado antes do módulo ser usado).
        /// Como .NET Framework não tem DI nativo, usamos configuração estática.
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            StaticTraceabilityOptionsProvider.Configure(options);
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
            
            // Extrair correlation-id do header
            var correlationId = _extractor.ExtractCorrelationId(request, headerName);

            // Valida formato se habilitado
            // Thread-safe: ler options uma vez para evitar race condition
            var options = _optionsProvider.GetOptions();
            if (!string.IsNullOrEmpty(correlationId) && !_validator.Validate(correlationId, options))
            {
                // Se inválido, ignora o header e gera novo
                correlationId = null;
            }

            // Determine trace context inputs
            var traceparent = request.Headers[Constants.HttpHeaders.TraceParent];
            var tracestate = request.Headers[Constants.HttpHeaders.TraceState];
            var parentFromTraceparent = TraceParentExtractor.Extract(traceparent, tracestate);

            // Decide effective correlation-id for this request:
            // Priority: AlwaysGenerateNew > correlation header > traceparent > generate
            var effectiveCorrelationId = CorrelationIdResolver.Resolve(options, correlationId, parentFromTraceparent);

            // Always set fallback context (so invalid formats can still work when validation disabled)
            CorrelationContext.Current = effectiveCorrelationId;

            // Create server span for every request (NET48 does not reliably provide an ambient Activity for controllers).
            // This keeps behavior consistent and ensures Activity.Current is available for WebApi actions.
            // Build parent context:
            // - If traceparent exists and no correlation header override, keep real parent
            // - Else, create an artificial parent if correlation id is a W3C trace-id (32 hex)
            var parentContext = ActivityContextBuilder.BuildParentContext(options, effectiveCorrelationId, correlationId, parentFromTraceparent);

            var activity = parentContext != default
                ? _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server, parentContext)
                : _activityFactory.StartActivity(Constants.ActivityNames.HttpRequest, ActivityKind.Server);

            if (activity != null)
            {
                context.Items[Constants.HttpContextKeys.ActivityItem] = activity;
                context.Items[Constants.HttpContextKeys.ActivityOwned] = true;

                _tagProvider.AddRequestTags(activity, request);
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // Retrieve Activity created by us (if any)
            var activity = context.Items[Constants.HttpContextKeys.ActivityItem] as Activity;
            var owned = context.Items[Constants.HttpContextKeys.ActivityOwned] as bool?;
            if (activity != null && owned == true)
            {
                // Try rename to route template (WebApi hosting) once route is resolved
                TryRenameToRouteTemplate(context, activity);

                _tagProvider.AddResponseTags(activity, response);

                // Parar Activity
                activity.Stop();

                // Remover do HttpContext.Items para liberar memória
                context.Items.Remove(Constants.HttpContextKeys.ActivityItem);
                context.Items.Remove(Constants.HttpContextKeys.ActivityOwned);
                context.Items.Remove(Constants.HttpContextKeys.ActivityRenamed);
            }
        }

        private void OnPreRequestHandlerExecute(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

            var activity = context.Items[Constants.HttpContextKeys.ActivityItem] as Activity;
            var owned = context.Items[Constants.HttpContextKeys.ActivityOwned] as bool?;
            if (activity != null && owned == true)
            {
                // Ensure Activity.Current is set on the request handler thread so controllers can see it.
                context.Items[Constants.HttpContextKeys.PreviousActivity] = Activity.Current;
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

            var activity = context.Items[Constants.HttpContextKeys.ActivityItem] as Activity;
            var owned = context.Items[Constants.HttpContextKeys.ActivityOwned] as bool?;
            if (activity != null && owned == true)
            {
                try
                {
                    // Try to rename to route template now that handler has executed (route may be resolved)
                    TryRenameToRouteTemplate(context, activity);

                    // Restore previous activity for safety
                    var previous = context.Items[Constants.HttpContextKeys.PreviousActivity] as Activity;
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
                    context.Items.Remove(Constants.HttpContextKeys.PreviousActivity);
                }
            }
        }

        private void OnError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var exception = application.Server.GetLastError();

            // Recuperar Activity do HttpContext.Items (garantido que é o mesmo criado em OnBeginRequest)
            var activity = context.Items[Constants.HttpContextKeys.ActivityItem] as Activity;
            if (activity != null && exception != null)
            {
                _tagProvider.AddErrorTags(activity, exception);
            }
        }

        private void OnPreSendRequestHeaders(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var response = context.Response;

            // If we created the activity, attempt to rename it to the route template before headers are sent.
            var activity = context.Items[Constants.HttpContextKeys.ActivityItem] as Activity;
            var owned = context.Items[Constants.HttpContextKeys.ActivityOwned] as bool?;
            if (activity != null && owned == true)
            {
                TryRenameToRouteTemplate(context, activity);

                // Optional debug headers (opt-in) to allow sample tests to assert span naming without exporter.
                var debug = context.Request.Headers[Constants.HttpHeaders.TraceabilityDebug];
                if (string.IsNullOrEmpty(debug))
                {
                    debug = context.Request.QueryString["traceabilityDebug"];
                }
                if (!string.IsNullOrEmpty(debug))
                {
                    try
                    {
                        response.Headers[Constants.HttpHeaders.TraceabilitySpanName] = activity.DisplayName;
                        response.Headers[Constants.HttpHeaders.TraceabilityOperationName] = activity.OperationName;
                        response.Headers[Constants.HttpHeaders.TraceabilityTraceId] = activity.TraceId.ToString();
                        response.Headers[Constants.HttpHeaders.TraceabilitySpanId] = activity.SpanId.ToString();
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
            if (context.Items[Constants.HttpContextKeys.ActivityRenamed] is bool already && already) return;

            try
            {
                var method = context.Request.HttpMethod;
                string? template = null;

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
                        activity.DisplayName = displayName!;
                        context.Items[Constants.HttpContextKeys.ActivityRenamed] = true;
                        return;
                    }
                }

                // Fallback extremo: usar path (mas logar que não encontramos template)
                // Isso só deve acontecer em casos muito raros
                var fallbackPath = context.Request.Url != null ? context.Request.Url.AbsolutePath.TrimStart('/') : "/";
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    var fallbackDisplayName = RouteTemplateHelper.NormalizeDisplayName(method, fallbackPath);
                    activity.DisplayName = fallbackDisplayName ?? $"{method.ToUpperInvariant()} {fallbackPath}";
                    context.Items[Constants.HttpContextKeys.ActivityRenamed] = true;
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

