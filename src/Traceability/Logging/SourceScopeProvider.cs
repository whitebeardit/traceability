using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Traceability.Logging
{
    /// <summary>
    /// Provider de scope para Microsoft.Extensions.Logging que adiciona Source.
    /// </summary>
    public class SourceScopeProvider : IExternalScopeProvider
    {
        private readonly string _source;
        private readonly IExternalScopeProvider? _innerProvider;

        /// <summary>
        /// Cria uma nova instância do SourceScopeProvider.
        /// </summary>
        /// <param name="source">Nome da origem/serviço que está gerando os logs (obrigatório).</param>
        /// <param name="innerProvider">Provider interno (opcional).</param>
        /// <exception cref="ArgumentNullException">Lançado quando source é null ou vazio.</exception>
        public SourceScopeProvider(string source, IExternalScopeProvider? innerProvider = null)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source), "Source cannot be null or empty");
            }

            _source = source;
            _innerProvider = innerProvider;
        }

        /// <summary>
        /// Cria um Dictionary de scope para o source especificado.
        /// </summary>
        private Dictionary<string, object> CreateScope()
        {
            return new Dictionary<string, object>
            {
                { "Source", _source }
            };
        }

        /// <summary>
        /// Executa um callback com um scope que inclui o source.
        /// </summary>
        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            // Sempre adiciona source ao scope
            var sourceScope = CreateScope();
            callback(sourceScope, state);

            // Chama o provider interno se existir
            _innerProvider?.ForEachScope(callback, state);
        }

        /// <summary>
        /// Adiciona um novo scope ao contexto de logging.
        /// </summary>
        public IDisposable Push(object? state)
        {
            // Sempre adiciona source ao scope
            var sourceScope = CreateScope();
            var innerScope = _innerProvider?.Push(sourceScope);
            return new SourceScope(innerScope, sourceScope);
        }
    }

    /// <summary>
    /// Scope que inclui source.
    /// </summary>
    internal class SourceScope : IDisposable
    {
        private readonly IDisposable? _innerScope;
        private readonly Dictionary<string, object> _scope;

        public SourceScope(IDisposable? innerScope, Dictionary<string, object> scope)
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


