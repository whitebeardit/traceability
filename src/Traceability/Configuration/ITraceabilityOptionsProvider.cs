namespace Traceability.Configuration
{
    /// <summary>
    /// Interface para acesso a opções de traceability.
    /// Permite abstrair a forma como as opções são acessadas (estático no NET48, DI no NET8).
    /// </summary>
    public interface ITraceabilityOptionsProvider
    {
        /// <summary>
        /// Obtém as opções de traceability.
        /// </summary>
        /// <returns>As opções de traceability configuradas.</returns>
        TraceabilityOptions GetOptions();
    }
}

