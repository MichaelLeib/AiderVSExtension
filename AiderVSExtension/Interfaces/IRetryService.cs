using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for implementing retry logic and error recovery strategies
    /// </summary>
    public interface IRetryService
    {
        /// <summary>
        /// Executes an operation with retry logic
        /// </summary>
        /// <typeparam name="T">The type of data returned by the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        Task<ServiceResult<T>> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<ServiceResult<T>>> operation,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with retry logic (non-generic version)
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the operation</returns>
        Task<ServiceResult> ExecuteWithRetryAsync(
            Func<CancellationToken, Task<ServiceResult>> operation,
            RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default);
    }
}