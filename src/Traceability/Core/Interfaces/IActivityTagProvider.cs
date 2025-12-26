using System;
using System.Diagnostics;

namespace Traceability.Core.Interfaces
{
    /// <summary>
    /// Interface para adicionar tags HTTP em Activities.
    /// </summary>
    public interface IActivityTagProvider
    {
        /// <summary>
        /// Adiciona tags de requisição HTTP ao Activity.
        /// </summary>
        void AddRequestTags(Activity activity, object request);

        /// <summary>
        /// Adiciona tags de resposta HTTP ao Activity.
        /// </summary>
        void AddResponseTags(Activity activity, object response);

        /// <summary>
        /// Adiciona tags de erro ao Activity.
        /// </summary>
        void AddErrorTags(Activity activity, Exception exception);
    }
}

