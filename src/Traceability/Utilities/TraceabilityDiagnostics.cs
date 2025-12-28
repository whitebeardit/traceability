using System;
using System.Diagnostics;

namespace Traceability.Utilities
{
    /// <summary>
    /// Diagnostics opt-in (no-op por padrão). Consumidores podem habilitar via DiagnosticListener.
    /// </summary>
    internal static class TraceabilityDiagnostics
    {
        private static readonly DiagnosticListener Listener = new("Traceability");

        public static void TryWriteException(string eventName, Exception exception, object? context = null)
        {
            if (!Listener.IsEnabled(eventName))
            {
                return;
            }

            Listener.Write(eventName, new
            {
                Exception = exception,
                Context = context
            });
        }
    }
}


