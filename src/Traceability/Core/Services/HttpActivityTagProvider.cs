#if NET8_0
using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Traceability.Core;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de IActivityTagProvider para ASP.NET Core (NET8).
    /// Extrai a lógica de adicionar tags HTTP em Activities presente em CorrelationIdMiddleware e CorrelationIdHandler.
    /// </summary>
    internal class HttpActivityTagProvider : Core.Interfaces.IActivityTagProvider
    {
        /// <summary>
        /// Adiciona tags de requisição HTTP ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdMiddleware.InvokeAsync e CorrelationIdHandler.SendAsync.
        /// </summary>
        public void AddRequestTags(Activity activity, object request)
        {
            if (activity == null || request == null) return;

            // Suporta HttpContext (ASP.NET Core) e HttpRequestMessage (HttpClient)
            if (request is HttpContext httpContext)
            {
                // Adicionar tags padrão (igual ao que OpenTelemetry faz automaticamente)
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpContext.Request.Method);
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpContext.Request.Path.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpContext.Request.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpContext.Request.Host.ToString());

                if (httpContext.Request.Headers.ContainsKey("User-Agent"))
                {
                    activity.SetTag(Constants.ActivityTags.HttpUserAgent, httpContext.Request.Headers["User-Agent"].ToString());
                }

                if (httpContext.Request.ContentLength.HasValue)
                {
                    activity.SetTag(Constants.ActivityTags.HttpRequestContentLength, httpContext.Request.ContentLength.Value);
                }

                if (!string.IsNullOrEmpty(httpContext.Request.ContentType))
                {
                    activity.SetTag(Constants.ActivityTags.HttpRequestContentType, httpContext.Request.ContentType);
                }
            }
            else if (request is HttpRequestMessage httpRequestMessage)
            {
                // Add standard tags if we created a span (de CorrelationIdHandler.SendAsync)
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequestMessage.Method.ToString());
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequestMessage.RequestUri?.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequestMessage.RequestUri?.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequestMessage.RequestUri?.Host);
            }
        }

        /// <summary>
        /// Adiciona tags de resposta HTTP ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdMiddleware.InvokeAsync e CorrelationIdHandler.SendAsync.
        /// </summary>
        public void AddResponseTags(Activity activity, object response)
        {
            if (activity == null || response == null) return;

            // Suporta HttpContext (ASP.NET Core) e HttpResponseMessage (HttpClient)
            if (response is HttpContext httpContext)
            {
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, (int)httpContext.Response.StatusCode);
            }
            else if (response is HttpResponseMessage httpResponseMessage)
            {
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, (int)httpResponseMessage.StatusCode);
            }
        }

        /// <summary>
        /// Adiciona tags de erro ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdMiddleware.InvokeAsync e CorrelationIdHandler.SendAsync.
        /// </summary>
        public void AddErrorTags(Activity activity, Exception exception)
        {
            if (activity == null || exception == null) return;

            activity.SetTag(Constants.ActivityTags.Error, true);
            activity.SetTag(Constants.ActivityTags.ErrorType, exception.GetType().Name);
            activity.SetTag(Constants.ActivityTags.ErrorMessage, exception.Message);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
    }
}
#endif

#if NET48
using System;
using System.Diagnostics;
using System.Net.Http;
using Traceability.Core;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de IActivityTagProvider para ASP.NET Framework (NET48).
    /// Extrai a lógica de adicionar tags HTTP em Activities presente em CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// </summary>
    internal class HttpActivityTagProvider : Core.Interfaces.IActivityTagProvider
    {
        /// <summary>
        /// Adiciona tags de requisição HTTP ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnBeginRequest e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        public void AddRequestTags(Activity activity, object request)
        {
            if (activity == null || request == null) return;

            // Suporta HttpRequest (System.Web) e HttpRequestMessage (System.Net.Http)
            if (request is System.Web.HttpRequest httpRequest)
            {
                // Base HTTP tags (de CorrelationIdHttpModule.OnBeginRequest)
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequest.HttpMethod);
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequest.Url.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequest.Url.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequest.Url.Host);
                activity.SetTag(Constants.ActivityTags.HttpUserAgent, httpRequest.UserAgent);

                // Placeholder so controllers can assert presence during request handling
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, "0");

                if (httpRequest.ContentLength > 0)
                {
                    activity.SetTag(Constants.ActivityTags.HttpRequestContentLength, httpRequest.ContentLength);
                }

                if (!string.IsNullOrEmpty(httpRequest.ContentType))
                {
                    activity.SetTag(Constants.ActivityTags.HttpRequestContentType, httpRequest.ContentType);
                }
            }
            else if (request is HttpRequestMessage httpRequestMessage)
            {
                // Adicionar tags padrão (igual ao que OpenTelemetry faz no .NET 8)
                // (de CorrelationIdMessageHandler.SendAsync)
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequestMessage.Method.ToString());
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequestMessage.RequestUri?.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequestMessage.RequestUri?.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequestMessage.RequestUri?.Host);
                // Setar placeholder para garantir que o controller enxergue a tag durante o processamento.
                // (O valor real será atualizado após base.SendAsync retornar.)
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, "0");

                if (httpRequestMessage.Content != null && httpRequestMessage.Content.Headers.ContentLength.HasValue)
                {
                    activity.SetTag(Constants.ActivityTags.HttpRequestContentLength, httpRequestMessage.Content.Headers.ContentLength.Value);
                }

                if (httpRequestMessage.Content != null && httpRequestMessage.Content.Headers.ContentType != null)
                {
                    var contentType = httpRequestMessage.Content.Headers.ContentType.ToString();
                    if (!string.IsNullOrEmpty(contentType))
                    {
                        activity.SetTag(Constants.ActivityTags.HttpRequestContentType, contentType);
                    }
                }
            }
        }

        /// <summary>
        /// Adiciona tags de resposta HTTP ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnEndRequest e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        public void AddResponseTags(Activity activity, object response)
        {
            if (activity == null || response == null) return;

            // Suporta HttpResponse (System.Web) e HttpResponseMessage (System.Net.Http)
            if (response is System.Web.HttpResponse httpResponse)
            {
                // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, ((int)httpResponse.StatusCode).ToString());

                var contentLength = httpResponse.Headers["Content-Length"];
                if (!string.IsNullOrEmpty(contentLength))
                {
                    activity.SetTag(Constants.ActivityTags.HttpResponseContentLength, contentLength);
                }
            }
            else if (response is HttpResponseMessage httpResponseMessage)
            {
                // Adicionar status code (igual ao que OpenTelemetry faz no .NET 8)
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, ((int)httpResponseMessage.StatusCode).ToString());

                if (httpResponseMessage.Content != null && httpResponseMessage.Content.Headers.ContentLength.HasValue)
                {
                    activity.SetTag(Constants.ActivityTags.HttpResponseContentLength, httpResponseMessage.Content.Headers.ContentLength.Value);
                }
            }
        }

        /// <summary>
        /// Adiciona tags de erro ao Activity.
        /// Lógica copiada EXATAMENTE de CorrelationIdHttpModule.OnError e CorrelationIdMessageHandler.SendAsync.
        /// </summary>
        public void AddErrorTags(Activity activity, Exception exception)
        {
            if (activity == null || exception == null) return;

            // Adicionar exceção ao Activity (igual ao que OpenTelemetry faz no .NET 8)
            activity.SetTag(Constants.ActivityTags.Error, "true");
            activity.SetTag(Constants.ActivityTags.ErrorType, exception.GetType().Name);
            activity.SetTag(Constants.ActivityTags.ErrorMessage, exception.Message);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
    }
}
#endif

