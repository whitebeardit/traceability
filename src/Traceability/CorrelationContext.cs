using System;
using System.Diagnostics;
using System.Threading;
using Traceability.Utilities;

namespace Traceability
{
    /// <summary>
    /// Gerencia trace-id/correlation-id no contexto assíncrono da thread atual.
    /// Usa Activity.TraceId (OpenTelemetry) quando disponível, fallback para AsyncLocal quando necessário.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _fallbackId = new AsyncLocal<string?>();

        /// <summary>
        /// Tenta obter o trace-id de um Activity, suportando tanto W3C quanto Hierarchical format.
        /// </summary>
        /// <param name="activity">O Activity do qual extrair o trace-id.</param>
        /// <param name="traceId">O trace-id extraído, ou null se não disponível.</param>
        /// <returns>true se o trace-id foi extraído com sucesso, false caso contrário.</returns>
        private static bool TryGetTraceIdFromActivity(Activity activity, out string? traceId)
        {
            traceId = null;
            if (activity == null) return false;
            
            try
            {
                // Prefer TraceId whenever it's available (some runtimes may not reflect IdFormat as W3C reliably).
                var traceIdString = activity.TraceId.ToString();
                if (!string.IsNullOrEmpty(traceIdString) && traceIdString != "00000000000000000000000000000000")
                {
                    traceId = traceIdString;
                    return true;
                }

                // Prioridade 1: W3C format (preferencial)
                if (activity.IdFormat == ActivityIdFormat.W3C)
                {
                    var w3cTraceId = activity.TraceId.ToString();
                    if (!string.IsNullOrEmpty(w3cTraceId))
                    {
                        traceId = w3cTraceId; // Já está em formato correto (32 hex chars sem hífens)
                        return true;
                    }
                }
                
                // Prioridade 2: Hierarchical format (fallback para .NET 4.8)
                var activityId = activity.Id;
                if (!string.IsNullOrEmpty(activityId))
                {
                    // Formato: |{trace-id}.{span-id}.{parent-id}|
                    var id = activityId!.Trim('|');
                    var firstDot = id.IndexOf('.');
                    if (firstDot > 0)
                    {
                        var hierarchicalTraceId = id.Substring(0, firstDot);
                        // Remove hífens se existirem e garante formato consistente
                        traceId = hierarchicalTraceId.Replace("-", "");
                        // Verificar se tem tamanho válido (GUID sem hífens tem 32 chars)
                        if (traceId != null && traceId.Length == 32)
                        {
                            return true;
                        }
                        // Se não tem 32 chars, ainda pode ser válido (aceitar qualquer tamanho razoável)
                        if (traceId != null && traceId.Length > 0 && traceId.Length <= 128)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceabilityDiagnostics.TryWriteException(
                    "Traceability.CorrelationContext.TryGetTraceIdFromActivity.Exception",
                    ex);
            }
            
            return false;
        }

        /// <summary>
        /// Obtém o trace-id atual (Activity.TraceId se disponível, senão correlation-id customizado).
        /// Se não existir, cria automaticamente um novo.
        /// Thread-safe: Activity e AsyncLocal são thread-safe por design.
        /// </summary>
        public static string Current
        {
            get
            {
                // Prioridade 1: OpenTelemetry Activity.TraceId (padrão da indústria)
                // Cachear Activity.Current para evitar múltiplas chamadas
                var activity = Activity.Current;
                if (activity != null && TryGetTraceIdFromActivity(activity, out var traceId) && traceId != null)
                {
                    // Sincronizar com fallback para compatibilidade
                    _fallbackId.Value = traceId;
                    return traceId;
                }
                
                // Prioridade 2: Fallback (apenas se Activity não disponível)
                var value = _fallbackId.Value;
                if (value == null)
                {
                    value = GenerateNew();
                    _fallbackId.Value = value;
                }
                return value;
            }
            set
            {
                // Sempre atualizar fallback para compatibilidade
                _fallbackId.Value = value;
                
                // Se Activity existir, não podemos modificar TraceId diretamente
                // Mas podemos garantir sincronização via fallback
            }
        }

        /// <summary>
        /// Verifica se existe trace-id/correlation-id no contexto atual.
        /// </summary>
        public static bool HasValue
        {
            get
            {
                // Cachear Activity.Current para evitar múltiplas chamadas
                var activity = Activity.Current;
                if (activity != null && TryGetTraceIdFromActivity(activity, out _))
                {
                    return true;
                }
                return _fallbackId.Value != null;
            }
        }

        /// <summary>
        /// Tenta obter o trace-id/correlation-id existente sem criar um novo.
        /// </summary>
        /// <param name="value">O trace-id/correlation-id se existir, caso contrário null.</param>
        /// <returns>true se um trace-id/correlation-id existe, false caso contrário.</returns>
        public static bool TryGetValue(out string? value)
        {
            // Cachear Activity.Current para evitar múltiplas chamadas
            var activity = Activity.Current;
            if (activity != null && TryGetTraceIdFromActivity(activity, out value))
            {
                return true;
            }
            
            value = _fallbackId.Value;
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
        /// Limpa o correlation-id do contexto (apenas fallback, Activity não pode ser limpo).
        /// </summary>
        public static void Clear()
        {
            _fallbackId.Value = null;
        }

        /// <summary>
        /// Gera um novo correlation-id usando GUID formatado sem hífens.
        /// Usado como fallback quando OpenTelemetry não está disponível.
        /// </summary>
        /// <returns>Um novo correlation-id.</returns>
        private static string GenerateNew()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}

