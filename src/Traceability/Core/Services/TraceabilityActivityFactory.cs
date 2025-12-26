using System.Diagnostics;
using Traceability.OpenTelemetry;

namespace Traceability.Core.Services
{
    /// <summary>
    /// Implementação de IActivityFactory que usa TraceabilityActivitySource.
    /// Wrapper sobre TraceabilityActivitySource para permitir injeção de dependências.
    /// </summary>
    internal class TraceabilityActivityFactory : Core.Interfaces.IActivityFactory
    {
        /// <summary>
        /// Cria um novo Activity (span) com o nome e tipo especificados.
        /// Delega para TraceabilityActivitySource.StartActivity.
        /// </summary>
        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Server)
        {
            return TraceabilityActivitySource.StartActivity(name, kind);
        }

        /// <summary>
        /// Cria um novo Activity (span) com um ActivityContext pai explícito.
        /// Delega para TraceabilityActivitySource.StartActivity.
        /// </summary>
        public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext)
        {
            return TraceabilityActivitySource.StartActivity(name, kind, parentContext);
        }

        /// <summary>
        /// Cria um novo Activity (span) filho do Activity pai especificado.
        /// Delega para TraceabilityActivitySource.StartActivity.
        /// </summary>
        public Activity? StartActivity(string name, ActivityKind kind, Activity? parent)
        {
            return TraceabilityActivitySource.StartActivity(name, kind, parent);
        }
    }
}

