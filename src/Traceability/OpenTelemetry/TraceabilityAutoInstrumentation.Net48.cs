#if NET48
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace Traceability.OpenTelemetry
{
    internal static class TraceabilityAutoInstrumentationNet48
    {
        private static int _initialized;

        public static void EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                return;
            }

            if (IsSpansDisabled())
            {
                return;
            }

            try
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;
            }
            catch
            {
                // Best-effort only
            }

            // ActivitySource only creates Activity when an ActivityListener exists.
            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = s => s != null && s.Name == "Traceability",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded
            });
        }

        private static bool IsSpansDisabled()
        {
            // Opt-out key: Traceability:SpansEnabled (default true)
            var v = ConfigurationManager.AppSettings["Traceability:SpansEnabled"];
            if (bool.TryParse(v, out var enabled))
            {
                return !enabled;
            }

            v = Environment.GetEnvironmentVariable("TRACEABILITY_SPANS_ENABLED");
            if (bool.TryParse(v, out enabled))
            {
                return !enabled;
            }

            return false;
        }
    }
}
#endif
