using System;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Service for tracking and correlating requests across the application
    /// </summary>
    public interface ICorrelationService
    {
        /// <summary>
        /// Gets the current correlation ID for the active request
        /// </summary>
        string GetCurrentCorrelationId();

        /// <summary>
        /// Sets the correlation ID for the current request context
        /// </summary>
        /// <param name="correlationId">The correlation ID to set</param>
        void SetCorrelationId(string correlationId);

        /// <summary>
        /// Generates a new correlation ID
        /// </summary>
        /// <returns>A new unique correlation ID</returns>
        string GenerateCorrelationId();

        /// <summary>
        /// Executes an action within a correlation context
        /// </summary>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="action">The action to execute</param>
        Task ExecuteWithCorrelationAsync(string correlationId, Func<Task> action);

        /// <summary>
        /// Executes a function within a correlation context
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="function">The function to execute</param>
        /// <returns>The result of the function</returns>
        Task<T> ExecuteWithCorrelationAsync<T>(string correlationId, Func<Task<T>> function);
    }
}
