#if NET48
using System;
using System.Diagnostics;
using System.Net.Http;
using Traceability;
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
        /// </summary>
        public void AddRequestTags(Activity activity, object request)
        {
            if (activity == null || request == null) return;

            // Adicionar correlation-ID como tag (já existe no contexto neste ponto)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag(Constants.ActivityTags.CorrelationId, correlationId);
            }

            // Suporta HttpRequest (System.Web) e HttpRequestMessage (System.Net.Http)
            if (request is System.Web.HttpRequest httpRequest)
            {
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequest.HttpMethod);
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequest.Url.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequest.Url.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequest.Url.Host);
                activity.SetTag(Constants.ActivityTags.HttpUserAgent, httpRequest.UserAgent);

                // Placeholder so controllers can assert presence during request handling
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, 0);

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
                activity.SetTag(Constants.ActivityTags.HttpMethod, httpRequestMessage.Method.ToString());
                activity.SetTag(Constants.ActivityTags.HttpUrl, httpRequestMessage.RequestUri?.ToString());
                activity.SetTag(Constants.ActivityTags.HttpScheme, httpRequestMessage.RequestUri?.Scheme);
                activity.SetTag(Constants.ActivityTags.HttpHost, httpRequestMessage.RequestUri?.Host);

                // Placeholder so controllers can assert presence during request handling
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, 0);

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
        /// </summary>
        public void AddResponseTags(Activity activity, object response)
        {
            if (activity == null || response == null) return;

            // Suporta HttpResponse (System.Web) e HttpResponseMessage (System.Net.Http)
            if (response is System.Web.HttpResponse httpResponse)
            {
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, (int)httpResponse.StatusCode);

                var contentLength = httpResponse.Headers["Content-Length"];
                if (!string.IsNullOrEmpty(contentLength))
                {
                    activity.SetTag(Constants.ActivityTags.HttpResponseContentLength, contentLength);
                }
            }
            else if (response is HttpResponseMessage httpResponseMessage)
            {
                activity.SetTag(Constants.ActivityTags.HttpStatusCode, (int)httpResponseMessage.StatusCode);

                if (httpResponseMessage.Content != null && httpResponseMessage.Content.Headers.ContentLength.HasValue)
                {
                    activity.SetTag(Constants.ActivityTags.HttpResponseContentLength, httpResponseMessage.Content.Headers.ContentLength.Value);
                }
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


