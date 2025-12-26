#if NET48 || NET8_0
using System.Diagnostics;

namespace Traceability.OpenTelemetry
{
    /// <summary>
    /// ActivitySource centralizado para criar Activities (spans) do OpenTelemetry.
    /// Fornece funcionalidades que o OpenTelemetry faz automaticamente no .NET 8,
    /// mas que precisam ser implementadas manualmente no .NET Framework 4.8.
    /// </summary>
    public static class TraceabilityActivitySource
    {
        private static readonly ActivitySource _activitySource = new("Traceability");
        
        /// <summary>
        /// Obtém o ActivitySource da biblioteca.
        /// </summary>
        public static ActivitySource Source => _activitySource;
        
        /// <summary>
        /// Cria um novo Activity (span) com o nome e tipo especificados.
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity (Server para requisições HTTP recebidas, Client para chamadas HTTP enviadas).</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Server)
        {
#if NET48
            TraceabilityAutoInstrumentationNet48.EnsureInitialized();
#endif
            return _activitySource.StartActivity(name, kind);
        }

        /// <summary>
        /// Cria um novo Activity (span) com um ActivityContext pai explícito (ex: extraído de traceparent).
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity.</param>
        /// <param name="parentContext">Contexto pai (W3C) a ser usado como parent.</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        public static Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext)
        {
#if NET48
            TraceabilityAutoInstrumentationNet48.EnsureInitialized();
#endif
            return _activitySource.StartActivity(name, kind, parentContext);
        }
        
        /// <summary>
        /// Cria um novo Activity (span) filho do Activity pai especificado.
        /// Usado para criar spans hierárquicos (parent/child).
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity.</param>
        /// <param name="parent">Activity pai (opcional). Se fornecido, cria um span filho.</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        public static Activity? StartActivity(string name, ActivityKind kind, Activity? parent)
        {
#if NET48
            TraceabilityAutoInstrumentationNet48.EnsureInitialized();
#endif
            if (parent != null)
            {
                return _activitySource.StartActivity(name, kind, parent.Context);
            }
            return _activitySource.StartActivity(name, kind);
        }
    }
}
#endif

