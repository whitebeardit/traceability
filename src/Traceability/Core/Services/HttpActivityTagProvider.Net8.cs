#if NET8_0
using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Traceability;
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
        /// </summary>
        public void AddRequestTags(Activity activity, object request)
        {
            if (activity == null || request == null) return;

            // Adicionar correlation-ID como tag (já existe no contexto neste ponto)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag(Constants.ActivityTags.CorrelationId, correlationId);
            }

            // Suporta HttpContext (ASP.NET Core) e HttpRequestMessage (HttpClient)
            if (request is HttpContext httpContext)
            {
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
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequestMessage.Method.ToString());
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequestMessage.RequestUri?.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequestMessage.RequestUri?.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequestMessage.RequestUri?.Host);
            }
        }

        /// <summary>
        /// Adiciona tags de resposta HTTP ao Activity.
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



