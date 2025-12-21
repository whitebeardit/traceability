using System;
using System.Threading;

namespace Traceability
{
    /// <summary>
    /// Gerencia o correlation-id no contexto assíncrono da thread atual.
    /// Usa AsyncLocal para garantir isolamento entre diferentes contextos assíncronos.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();

        /// <summary>
        /// Obtém ou define o correlation-id atual no contexto assíncrono.
        /// Se não existir, cria automaticamente um novo GUID.
        /// Thread-safe: AsyncLocal já é thread-safe por design.
        /// </summary>
        public static string Current
        {
            get
            {
                if (_correlationId.Value == null)
                {
                    _correlationId.Value = GenerateNew();
                }
                return _correlationId.Value;
            }
            set => _correlationId.Value = value;
        }

        /// <summary>
        /// Verifica se existe um correlation-id no contexto atual.
        /// </summary>
        public static bool HasValue => _correlationId.Value != null;

        /// <summary>
        /// Obtém o correlation-id existente ou cria um novo se não existir.
        /// </summary>
        /// <returns>O correlation-id atual ou um novo se não existir.</returns>
        public static string GetOrCreate()
        {
            if (!HasValue)
            {
                Current = GenerateNew();
            }
            return Current;
        }

        /// <summary>
        /// Limpa o correlation-id do contexto atual.
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

