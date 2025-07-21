using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Provides retry policies for handling transient failures
    /// </summary>
    public class RetryPolicy
    {
        private readonly IErrorHandler _errorHandler;
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly TimeSpan _maxDelay;

        public RetryPolicy(IErrorHandler errorHandler = null, int maxAttempts = 3, TimeSpan baseDelay = default, TimeSpan maxDelay = default)
        {
            _errorHandler = errorHandler;
            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
            _maxDelay = maxDelay == default ? TimeSpan.FromMinutes(1) : maxDelay;
        }

        /// <summary>
        /// Executes an operation with the configured retry policy
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <returns>Result of the operation</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await ExecuteWithRetryAsync(operation, _maxAttempts, _baseDelay, _maxDelay);
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry policy
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="baseDelay">Base delay between retries</param>
        /// <param name="maxDelay">Maximum delay between retries</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxAttempts = 3,
            TimeSpan baseDelay = default,
            TimeSpan maxDelay = default,
            CancellationToken cancellationToken = default)
        {
            var actualBaseDelay = baseDelay == default ? TimeSpan.FromSeconds(1) : baseDelay;
            var actualMaxDelay = maxDelay == default ? TimeSpan.FromMinutes(1) : maxDelay;
            
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsTransientException(ex) && attempt < maxAttempts)
                {
                    lastException = ex;
                    
                    var delay = CalculateDelay(attempt, actualBaseDelay, actualMaxDelay);
                    
                    await (_errorHandler?.LogWarningAsync(
                        $"Transient error on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying in {delay.TotalSeconds} seconds.",
                        $"RetryPolicy.ExecuteWithRetryAsync") ?? Task.CompletedTask);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Non-transient exception or final attempt
                    await (_errorHandler?.LogErrorAsync(
                        $"Operation failed on attempt {attempt}/{maxAttempts}: {ex.Message}",
                        ex,
                        "RetryPolicy.ExecuteWithRetryAsync") ?? Task.CompletedTask);
                    throw;
                }
            }

            // If we get here, all retries failed
            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry policy (void return)
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="baseDelay">Base delay between retries</param>
        /// <param name="maxDelay">Maximum delay between retries</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            int maxAttempts = 3,
            TimeSpan baseDelay = default,
            TimeSpan maxDelay = default,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true; // Dummy return value
            }, maxAttempts, baseDelay, maxDelay, cancellationToken);
        }

        /// <summary>
        /// Executes an operation with linear backoff retry policy
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxAttempts">Maximum number of retry attempts</param>
        /// <param name="delay">Fixed delay between retries</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<T> ExecuteWithLinearRetryAsync<T>(
            Func<Task<T>> operation,
            int maxAttempts = 3,
            TimeSpan delay = default,
            CancellationToken cancellationToken = default)
        {
            var actualDelay = delay == default ? TimeSpan.FromSeconds(2) : delay;
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsTransientException(ex) && attempt < maxAttempts)
                {
                    lastException = ex;
                    
                    await (_errorHandler?.LogWarningAsync(
                        $"Transient error on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying in {actualDelay.TotalSeconds} seconds.",
                        "RetryPolicy.ExecuteWithLinearRetryAsync") ?? Task.CompletedTask);

                    await Task.Delay(actualDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    await (_errorHandler?.LogErrorAsync(
                        $"Operation failed on attempt {attempt}/{maxAttempts}: {ex.Message}",
                        ex,
                        "RetryPolicy.ExecuteWithLinearRetryAsync") ?? Task.CompletedTask);
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        /// <summary>
        /// Determines if an exception is transient and should be retried
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception is transient</returns>
        private bool IsTransientException(Exception exception)
        {
            return exception switch
            {
                AiderVSExtension.Exceptions.AiderConnectionException => true,
                AiderVSExtension.Exceptions.AiderCommunicationException => true,
                System.Net.Http.HttpRequestException => true,
                TaskCanceledException => true,
                TimeoutException => true,
                System.Net.Sockets.SocketException => true,
                System.Net.WebSockets.WebSocketException => true,
                _ => false
            };
        }

        /// <summary>
        /// Calculates the delay for exponential backoff
        /// </summary>
        /// <param name="attempt">Current attempt number (1-based)</param>
        /// <param name="baseDelay">Base delay</param>
        /// <param name="maxDelay">Maximum delay</param>
        /// <returns>Calculated delay</returns>
        private TimeSpan CalculateDelay(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            // Exponential backoff: baseDelay * 2^(attempt-1)
            var exponentialDelay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

            // Add jitter to prevent thundering herd
            var jitter = TimeSpan.FromMilliseconds(
                new Random().Next(0, (int)(exponentialDelay.TotalMilliseconds * 0.1)));

            var totalDelay = exponentialDelay.Add(jitter);

            // Cap at maximum delay
            return totalDelay > maxDelay ? maxDelay : totalDelay;
        }
    }
}