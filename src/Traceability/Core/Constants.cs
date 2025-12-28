namespace Traceability.Core
{
    /// <summary>
    /// Constantes compartilhadas do projeto Traceability.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Chaves usadas no HttpContext.Items para armazenar informações do Activity (NET48).
        /// </summary>
        public static class HttpContextKeys
        {
            public const string ActivityItem = "TraceabilityActivity";
            public const string ActivityOwned = "TraceabilityActivityOwned";
            public const string ActivityRenamed = "TraceabilityActivityRenamed";
            public const string PreviousActivity = "TraceabilityPreviousActivity";
        }

        /// <summary>
        /// Nomes de headers HTTP usados pelo Traceability.
        /// </summary>
        public static class HttpHeaders
        {
            public const string TraceParent = "traceparent";
            public const string TraceState = "tracestate";
            public const string CorrelationId = "X-Correlation-Id";
            public const string TraceabilityDebug = "X-Traceability-Debug";
            public const string TraceabilitySpanName = "X-Traceability-SpanName";
            public const string TraceabilityOperationName = "X-Traceability-OperationName";
            public const string TraceabilityTraceId = "X-Traceability-TraceId";
            public const string TraceabilitySpanId = "X-Traceability-SpanId";
        }

        /// <summary>
        /// Nomes de tags HTTP usadas em Activities (OpenTelemetry).
        /// </summary>
        public static class ActivityTags
        {
            public const string HttpMethod = "http.method";
            public const string HttpUrl = "http.url";
            public const string HttpScheme = "http.scheme";
            public const string HttpHost = "http.host";
            public const string HttpUserAgent = "http.user_agent";
            public const string HttpStatusCode = "http.status_code";
            public const string HttpRequestContentLength = "http.request_content_length";
            public const string HttpRequestContentType = "http.request_content_type";
            public const string HttpResponseContentLength = "http.response_content_length";
            public const string Error = "error";
            public const string ErrorType = "error.type";
            public const string ErrorMessage = "error.message";
        }

        /// <summary>
        /// Nomes de Activities (spans) criados pelo Traceability.
        /// </summary>
        public static class ActivityNames
        {
            public const string HttpRequest = "HTTP Request";
            public const string HttpClient = "HTTP Client";
        }
    }
}

