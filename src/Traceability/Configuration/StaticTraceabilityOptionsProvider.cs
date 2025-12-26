#if NET48
using System;

namespace Traceability.Configuration
{
    /// <summary>
    /// Implementação de ITraceabilityOptionsProvider para NET48.
    /// Wrapper thread-safe sobre o campo estático _options usado em CorrelationIdHttpModule e CorrelationIdMessageHandler.
    /// </summary>
    internal class StaticTraceabilityOptionsProvider : ITraceabilityOptionsProvider
    {
        private static volatile TraceabilityOptions _options = new TraceabilityOptions();
        private static readonly object _optionsLock = new object();

        /// <summary>
        /// Obtém as opções de traceability do campo estático.
        /// Thread-safe: lê o campo uma vez para evitar race conditions.
        /// </summary>
        public TraceabilityOptions GetOptions()
        {
            // Thread-safe: ler _options uma vez para evitar race condition
            return _options;
        }

        /// <summary>
        /// Configura as opções do provider (deve ser chamado antes do provider ser usado).
        /// Thread-safe: usa lock para garantir consistência em cenários multi-threaded.
        /// </summary>
        /// <param name="options">Opções de configuração.</param>
        public static void Configure(TraceabilityOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (_optionsLock)
            {
                _options = options;
            }
        }

        /// <summary>
        /// Obtém a instância singleton do provider estático.
        /// </summary>
        public static readonly StaticTraceabilityOptionsProvider Instance = new StaticTraceabilityOptionsProvider();
    }
}
#endif

