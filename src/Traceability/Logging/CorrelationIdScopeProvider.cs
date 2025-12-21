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
        /// Cria um Dictionary de scope para o correlation-id especificado.
        /// </summary>
        private static Dictionary<string, object> CreateScope(string correlationId)
        {
            return new Dictionary<string, object>
            {
                { "CorrelationId", correlationId }
            };
        }

        /// <summary>
        /// Executa um callback com um scope que inclui o correlation-id.
        /// </summary>
        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var correlationIdScope = CreateScope(correlationId);
                callback(correlationIdScope, state);
            }

            // Chama o provider interno se existir
            _innerProvider?.ForEachScope(callback, state);
        }

        /// <summary>
        /// Adiciona um novo scope ao contexto de logging.
        /// </summary>
        public IDisposable Push(object? state)
        {
            // Tenta obter correlation-id sem criar um novo (evita criar indesejadamente)
            if (CorrelationContext.TryGetValue(out var correlationId) && !string.IsNullOrEmpty(correlationId))
            {
                var correlationIdScope = CreateScope(correlationId);
                var innerScope = _innerProvider?.Push(correlationIdScope);
                return new CorrelationIdScope(innerScope, correlationIdScope);
            }

            // Se não houver correlation-id, apenas retorna o scope interno
            return _innerProvider?.Push(state) ?? new NullScope();
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

    /// <summary>
    /// Scope nulo que não faz nada (usado quando não há correlation-id).
    /// </summary>
    internal class NullScope : IDisposable
    {
        public void Dispose()
        {
            // Nada a fazer
        }
    }
}

