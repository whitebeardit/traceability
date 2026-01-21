using System;
using System.Diagnostics;
using System.Threading;
using Traceability.Utilities;

namespace Traceability
{
    /// <summary>
    /// Gerencia correlation-id no contexto assíncrono da thread atual.
    /// Correlation-ID é independente do trace ID do OpenTelemetry.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _correlationId = new AsyncLocal<string?>();

        /// <summary>
        /// Obtém o correlation-id atual. Se não existir, cria automaticamente um novo.
        /// Thread-safe: AsyncLocal é thread-safe por design.
        /// </summary>
        public static string Current
        {
            get
            {
                var value = _correlationId.Value;
                if (value == null)
                {
                    value = GenerateNew();
                    _correlationId.Value = value;
                }
                return value;
            }
            set
            {
                _correlationId.Value = value;
            }
        }

        /// <summary>
        /// Verifica se existe correlation-id no contexto atual.
        /// </summary>
        public static bool HasValue
        {
            get => _correlationId.Value != null;
        }

        /// <summary>
        /// Tenta obter o correlation-id existente sem criar um novo.
        /// </summary>
        /// <param name="value">O correlation-id se existir, caso contrário null.</param>
        /// <returns>true se um correlation-id existe, false caso contrário.</returns>
        public static bool TryGetValue(out string? value)
        {
            value = _correlationId.Value;
            return value != null;
        }

        /// <summary>
        /// Obtém o trace-id/correlation-id existente ou cria um novo se não existir.
        /// Thread-safe: usa Current property que já é thread-safe.
        /// </summary>
        /// <returns>O trace-id/correlation-id atual ou um novo se não existir.</returns>
        public static string GetOrCreate()
        {
            return Current;
        }

        /// <summary>
        /// Limpa o correlation-id do contexto.
        /// </summary>
        public static void Clear()
        {
            _correlationId.Value = null;
        }

        /// <summary>
        /// Gera um novo correlation-id usando GUID formatado sem hífens.
        /// </summary>
        /// <returns>Um novo correlation-id.</returns>
        private static string GenerateNew()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}

