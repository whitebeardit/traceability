using System.Diagnostics;

namespace Traceability.Core.Interfaces
{
    /// <summary>
    /// Interface para criação de Activities (spans) do OpenTelemetry.
    /// </summary>
    public interface IActivityFactory
    {
        /// <summary>
        /// Cria um novo Activity (span) com o nome e tipo especificados.
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity.</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Server);

        /// <summary>
        /// Cria um novo Activity (span) com um ActivityContext pai explícito.
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity.</param>
        /// <param name="parentContext">Contexto pai (W3C) a ser usado como parent.</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext);

        /// <summary>
        /// Cria um novo Activity (span) filho do Activity pai especificado.
        /// </summary>
        /// <param name="name">Nome do Activity (span).</param>
        /// <param name="kind">Tipo do Activity.</param>
        /// <param name="parent">Activity pai (opcional). Se fornecido, cria um span filho.</param>
        /// <returns>O Activity criado ou null se não houver listeners.</returns>
        Activity? StartActivity(string name, ActivityKind kind, Activity? parent);
    }
}

