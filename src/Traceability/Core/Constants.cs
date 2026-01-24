namespace Traceability.Core
{
    /// <summary>
    /// Constantes compartilhadas do projeto Traceability.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Nomes de headers HTTP usados pelo Traceability.
        /// </summary>
        public static class HttpHeaders
        {
            public const string TraceParent = "traceparent";
            public const string CorrelationId = "X-Correlation-Id";
        }
    }
}

