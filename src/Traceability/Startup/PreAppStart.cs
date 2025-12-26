#if NET48
using System.Threading;
using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Traceability.Middleware;
using Traceability.OpenTelemetry;

[assembly: PreApplicationStartMethod(typeof(Traceability.Startup.PreAppStart), "Start")]

namespace Traceability.Startup
{
    /// <summary>
    /// Auto-start entrypoint for classic ASP.NET (NET48).
    /// Registers the HttpModule and enables span creation with near zero configuration.
    /// </summary>
    public static class PreAppStart
    {
        private static int _started;

        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
            {
                return;
            }

            // Enable ActivitySource listener (unless disabled by config)
            TraceabilityAutoInstrumentationNet48.EnsureInitialized();

            // Register HttpModule for inbound instrumentation (zero code in consumer app)
            DynamicModuleUtility.RegisterModule(typeof(CorrelationIdHttpModule));
        }
    }
}
#endif
