using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Services;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for circuit breaker pattern implementation
    /// </summary>
    public interface ICircuitBreakerService
    {
        /// <summary>
        /// Gets the current state of the circuit breaker
        /// </summary>
        CircuitBreakerState State { get; }

        /// <summary>
        /// Gets the current failure count
        /// </summary>
        int FailureCount { get; }

        /// <summary>
        /// Gets the timestamp of the last failure
        /// </summary>
        DateTime LastFailureTime { get; }

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation without return value with circuit breaker protection
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the circuit breaker allows operations
        /// </summary>
        /// <returns>True if operations are allowed</returns>
        bool CanExecute();

        /// <summary>
        /// Manually resets the circuit breaker to closed state
        /// </summary>
        void Reset();
    }
}