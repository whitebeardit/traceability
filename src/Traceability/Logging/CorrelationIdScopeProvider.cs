using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Traceability;

namespace Traceability.Logging
{
    /// <summary>
    /// Provider de scope para Microsoft.Extensions.Logging que adiciona correlation-id.
    /// </summary>
    public class CorrelationIdScopeProvider : IExternalScopeProvider
    {
        private readonly IExternalScopeProvider? _innerProvider;

        /// <summary>
        /// Cria uma nova instância do CorrelationIdScopeProvider.
        /// </summary>
        /// <param name="innerProvider">Provider interno (opcional).</param>
        public CorrelationIdScopeProvider(IExternalScopeProvider? innerProvider = null)
        {
            _innerProvider = innerProvider;
        }

        /// <summary>
        /// Executa um callback com um scope que inclui o correlation-id.
        /// </summary>
        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            // Adiciona correlation-id ao scope
            var correlationId = CorrelationContext.Current;
            var correlationIdScope = new Dictionary<string, object>
            {
                { "CorrelationId", correlationId }
            };

            callback(correlationIdScope, state);

            // Chama o provider interno se existir
            _innerProvider?.ForEachScope(callback, state);
        }

        /// <summary>
        /// Adiciona um novo scope ao contexto de logging.
        /// </summary>
        public IDisposable Push(object? state)
        {
            var correlationId = CorrelationContext.Current;
            var correlationIdScope = new Dictionary<string, object>
            {
                { "CorrelationId", correlationId }
            };

            var innerScope = _innerProvider?.Push(correlationIdScope);
            
            return new CorrelationIdScope(innerScope, correlationIdScope);
        }
    }

    /// <summary>
    /// Scope que inclui correlation-id.
    /// </summary>
    internal class CorrelationIdScope : IDisposable
    {
        private readonly IDisposable? _innerScope;
        private readonly Dictionary<string, object> _scope;

        public CorrelationIdScope(IDisposable? innerScope, Dictionary<string, object> scope)
        {
            _innerScope = innerScope;
            _scope = scope;
        }

        public void Dispose()
        {
            _innerScope?.Dispose();
        }
    }
}

