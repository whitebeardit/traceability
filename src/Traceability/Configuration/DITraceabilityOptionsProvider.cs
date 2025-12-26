#if NET8_0
using Microsoft.Extensions.Options;

namespace Traceability.Configuration
{
    /// <summary>
    /// Implementação de ITraceabilityOptionsProvider para NET8.
    /// Usa IOptions<TraceabilityOptions> via Dependency Injection.
    /// </summary>
    internal class DITraceabilityOptionsProvider : ITraceabilityOptionsProvider
    {
        private readonly IOptions<TraceabilityOptions> _options;

        /// <summary>
        /// Cria uma nova instância do DITraceabilityOptionsProvider.
        /// </summary>
        /// <param name="options">Opções de traceability via DI.</param>
        public DITraceabilityOptionsProvider(IOptions<TraceabilityOptions> options)
        {
            _options = options ?? throw new System.ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Obtém as opções de traceability via DI.
        /// </summary>
        public TraceabilityOptions GetOptions()
        {
            return _options.Value;
        }
    }
}
#endif

