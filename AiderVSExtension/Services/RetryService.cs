using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for implementing retry logic and circuit breaker patterns
    /// </summary>
    public class RetryService : IRetryService
    {
        private readonly IErrorHandler _errorHandler;

        public RetryService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Executes an operation with retry logic
        /// </summary>
        public async Task<ServiceResult<T>> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<ServiceResult<T>>> operation,
            AiderVSExtension.Models.RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            
            if (retryPolicy == null)
                retryPolicy = AiderVSExtension.Models.RetryPolicy.NoRetry;

            var currentAttempt = 0;
            ServiceResult<T> lastResult = null;

            while (currentAttempt <= retryPolicy.MaxRetryAttempts)
            {
                try
                {
                    retryPolicy.CurrentAttempt = currentAttempt;
                    var result = await operation(cancellationToken);

                    if (result.Success)
                    {
                        // Log successful retry if it wasn't the first attempt
                        if (currentAttempt > 0)
                        {
                            await _errorHandler.LogInfoAsync(
                                $"Operation succeeded after {currentAttempt} retries",
                                "RetryService.ExecuteWithRetryAsync");
                        }
                        return result;
                    }

                    lastResult = result;

                    // Check if we should retry based on error type
                    if (!ShouldRetryForErrorType(result.ErrorType) || !retryPolicy.CanRetry())
                    {
                        await _errorHandler.LogWarningAsync(
                            $"Operation failed and retry not applicable. Error: {result.ErrorMessage}",
                            "RetryService.ExecuteWithRetryAsync");
                        return result;
                    }

                    // Calculate delay for next retry
                    var delay = retryPolicy.GetNextRetryDelay();
                    
                    await _errorHandler.LogWarningAsync(
                        $"Operation failed (attempt {currentAttempt + 1}/{retryPolicy.MaxRetryAttempts + 1}). " +
                        $"Retrying in {delay.TotalSeconds:F1} seconds. Error: {result.ErrorMessage}",
                        "RetryService.ExecuteWithRetryAsync");

                    // Wait before retrying
                    await Task.Delay(delay, cancellationToken);
                    currentAttempt++;
                }
                catch (OperationCanceledException)
                {
                    // Don't retry if operation was cancelled
                    return ServiceResult<T>.Failed(ServiceErrorType.Timeout, "Operation was cancelled", new OperationCanceledException());
                }
                catch (Exception ex)
                {
                    // Convert exception to ServiceResult and check if we should retry
                    var exceptionResult = ServiceResult<T>.FromException(ex, "RetryService.ExecuteWithRetryAsync");
                    lastResult = exceptionResult;

                    if (!ShouldRetryForErrorType(exceptionResult.ErrorType) || !retryPolicy.CanRetry())
                    {
                        await _errorHandler.HandleExceptionAsync(ex, "RetryService.ExecuteWithRetryAsync");
                        return exceptionResult;
                    }

                    // Calculate delay for next retry
                    var delay = retryPolicy.GetNextRetryDelay();
                    
                    await _errorHandler.LogWarningAsync(
                        $"Operation threw exception (attempt {currentAttempt + 1}/{retryPolicy.MaxRetryAttempts + 1}). " +
                        $"Retrying in {delay.TotalSeconds:F1} seconds. Exception: {ex.Message}",
                        "RetryService.ExecuteWithRetryAsync");

                    // Wait before retrying
                    await Task.Delay(delay, cancellationToken);
                    currentAttempt++;
                }
            }

            // All retries exhausted
            await _errorHandler.LogErrorAsync(
                $"Operation failed after {retryPolicy.MaxRetryAttempts + 1} attempts. Final error: {lastResult?.ErrorMessage}",
                lastResult?.Exception,
                "RetryService.ExecuteWithRetryAsync");

            return lastResult ?? ServiceResult<T>.Failed(ServiceErrorType.Unknown, "All retry attempts exhausted");
        }

        /// <summary>
        /// Executes an operation with retry logic (non-generic version)
        /// </summary>
        public async Task<ServiceResult> ExecuteWithRetryAsync(
            Func<CancellationToken, Task<ServiceResult>> operation,
            AiderVSExtension.Models.RetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            var genericResult = await ExecuteWithRetryAsync<object>(
                async ct => 
                {
                    var result = await operation(ct);
                    return new ServiceResult<object>
                    {
                        Success = result.Success,
                        ErrorType = result.ErrorType,
                        ErrorMessage = result.ErrorMessage,
                        Exception = result.Exception,
                        AdditionalData = result.AdditionalData
                    };
                },
                retryPolicy,
                cancellationToken);

            return new ServiceResult
            {
                Success = genericResult.Success,
                ErrorType = genericResult.ErrorType,
                ErrorMessage = genericResult.ErrorMessage,
                Exception = genericResult.Exception,
                AdditionalData = genericResult.AdditionalData
            };
        }

        /// <summary>
        /// Determines if an operation should be retried based on the error type
        /// </summary>
        private bool ShouldRetryForErrorType(ServiceErrorType errorType)
        {
            return errorType switch
            {
                // Retryable errors
                ServiceErrorType.Network => true,
                ServiceErrorType.Timeout => true,
                ServiceErrorType.ServiceUnavailable => true,
                ServiceErrorType.Throttling => true,
                ServiceErrorType.Communication => true,

                // Non-retryable errors
                ServiceErrorType.Authentication => false,
                ServiceErrorType.Authorization => false,
                ServiceErrorType.InvalidInput => false,
                ServiceErrorType.Validation => false,
                ServiceErrorType.NotFound => false,
                ServiceErrorType.Security => false,
                ServiceErrorType.NotSupported => false,
                ServiceErrorType.Configuration => false,
                ServiceErrorType.InvalidOperation => false,

                // Default to non-retryable for safety
                _ => false
            };
        }
    }

    /// <summary>
    /// Circuit breaker implementation for preventing cascading failures
    /// </summary>
    public class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _timeout;
        private readonly IErrorHandler _errorHandler;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private int _consecutiveFailures = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly object _lock = new object();

        public CircuitBreaker(int failureThreshold, TimeSpan timeout, IErrorHandler errorHandler)
        {
            _failureThreshold = failureThreshold;
            _timeout = timeout;
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitBreakerState.HalfOpen)
                    {
                        return _state;
                    }

                    if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _lastFailureTime >= _timeout)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _ = _errorHandler.LogInfoAsync("Circuit breaker state changed to HalfOpen", "CircuitBreaker");
                    }

                    return _state;
                }
            }
        }

        public async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation)
        {
            if (State == CircuitBreakerState.Open)
            {
                await _errorHandler.LogWarningAsync("Circuit breaker is open, operation blocked", "CircuitBreaker");
                return ServiceResult<T>.Failed(ServiceErrorType.ServiceUnavailable, "Circuit breaker is open");
            }

            try
            {
                var result = await operation();

                if (result.Success)
                {
                    OnSuccess();
                }
                else
                {
                    OnFailure();
                }

                return result;
            }
            catch (Exception ex)
            {
                OnFailure();
                await _errorHandler.HandleExceptionAsync(ex, "CircuitBreaker.ExecuteAsync");
                return ServiceResult<T>.FromException(ex);
            }
        }

        private void OnSuccess()
        {
            lock (_lock)
            {
                _consecutiveFailures = 0;
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _state = CircuitBreakerState.Closed;
                    _ = _errorHandler.LogInfoAsync("Circuit breaker state changed to Closed", "CircuitBreaker");
                }
            }
        }

        private void OnFailure()
        {
            lock (_lock)
            {
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;

                if (_consecutiveFailures >= _failureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    _ = _errorHandler.LogWarningAsync(
                        $"Circuit breaker tripped after {_consecutiveFailures} consecutive failures",
                        "CircuitBreaker");
                }
            }
        }
    }

}