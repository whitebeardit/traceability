#if NET48
using System.Threading;
using System.Web;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Traceability.Middleware;

[assembly: PreApplicationStartMethod(typeof(Traceability.Startup.PreAppStart), "Start")]

namespace Traceability.Startup
{
    /// <summary>
    /// Auto-start entrypoint for classic ASP.NET (NET48).
    /// Registers the HttpModule for correlation-id management with near zero configuration.
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

            // Register HttpModule for inbound correlation-id management (zero code in consumer app)
            DynamicModuleUtility.RegisterModule(typeof(CorrelationIdHttpModule));
        }
    }
}
#endif
