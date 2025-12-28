using System.Diagnostics;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que adiciona automaticamente o route name (DisplayName do Activity) aos logs.
    /// Usa cache para reduzir alocações quando o mesmo route name é usado em múltiplos logs.
    /// </summary>
    public class RouteNameEnricher : ILogEventEnricher
    {
        private const string RouteNamePropertyName = "RouteName";
        private static readonly AsyncLocal<(string? RouteName, LogEventProperty? Property)> _cache = new();

        /// <summary>
        /// Enriquece o log event com o route name do Activity atual.
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Tenta obter o DisplayName do Activity atual
            var activity = Activity.Current;
            if (activity == null || string.IsNullOrEmpty(activity.DisplayName))
            {
                // Se não houver Activity ou DisplayName, não adiciona nada ao log
                return;
            }

            var routeName = activity.DisplayName;
            var cached = _cache.Value;
            
            // Reutiliza propriedade se o route name não mudou
            if (cached.RouteName == routeName && cached.Property != null)
            {
                logEvent.AddPropertyIfAbsent(cached.Property);
                return;
            }
            
            // Cria nova propriedade e atualiza cache
            var property = propertyFactory.CreateProperty(RouteNamePropertyName, routeName);
            _cache.Value = (routeName, property);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}

