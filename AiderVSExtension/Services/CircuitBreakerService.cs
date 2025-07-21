using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Circuit breaker pattern implementation for AI service calls
    /// </summary>
    public class CircuitBreakerService : ICircuitBreakerService
    {
        private readonly IErrorHandler _errorHandler;
        private readonly TimeSpan _timeout;
        private readonly int _failureThreshold;
        private readonly TimeSpan _recoveryTimeout;

        private volatile CircuitBreakerState _state = CircuitBreakerState.Closed;
        private volatile int _failureCount = 0;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private readonly object _lock = new object();

        public CircuitBreakerService(
            IErrorHandler errorHandler,
            TimeSpan timeout = default,
            int failureThreshold = 5,
            TimeSpan recoveryTimeout = default)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
            _failureThreshold = failureThreshold;
            _recoveryTimeout = recoveryTimeout == default ? TimeSpan.FromMinutes(1) : recoveryTimeout;
        }

        public CircuitBreakerState State => _state;
        public int FailureCount => _failureCount;
        public DateTime LastFailureTime => _lastFailureTime;

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Check if circuit is open
            if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime < _recoveryTimeout)
                {
                    throw new CircuitBreakerOpenException("Circuit breaker is open. Service temporarily unavailable.");
                }
                else
                {
                    // Transition to half-open state
                    lock (_lock)
                    {
                        if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _lastFailureTime >= _recoveryTimeout)
                        {
                            _state = CircuitBreakerState.HalfOpen;
                            await _errorHandler.LogInfoAsync("Circuit breaker transitioning to half-open state", "CircuitBreakerService");
                        }
                    }
                }
            }

            // Execute the operation with timeout
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_timeout);

                var result = await operation(timeoutCts.Token);

                // Operation succeeded - reset failure count
                OnOperationSuccess();
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // User cancelled - don't count as failure
                throw;
            }
            catch (Exception ex)
            {
                // Operation failed - record failure
                OnOperationFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation without return value
        /// </summary>
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async ct => 
            {
                await operation(ct);
                return true; // Dummy return value
            }, cancellationToken);
        }

        /// <summary>
        /// Checks if the circuit breaker allows operations
        /// </summary>
        public bool CanExecute()
        {
            if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
                return true;

            if (_state == CircuitBreakerState.Open && DateTime.UtcNow - _lastFailureTime >= _recoveryTimeout)
                return true;

            return false;
        }

        /// <summary>
        /// Manually resets the circuit breaker
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _lastFailureTime = DateTime.MinValue;
            }
            
            _ = _errorHandler.LogInfoAsync("Circuit breaker manually reset", "CircuitBreakerService");
        }

        private void OnOperationSuccess()
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Successful operation in half-open state - close the circuit
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _ = _errorHandler.LogInfoAsync("Circuit breaker closed after successful operation", "CircuitBreakerService");
                }
                else if (_state == CircuitBreakerState.Closed)
                {
                    // Reset failure count on success
                    _failureCount = 0;
                }
            }
        }

        private void OnOperationFailure(Exception exception)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Failure in half-open state - open the circuit immediately
                    _state = CircuitBreakerState.Open;
                    _ = _errorHandler.LogWarningAsync($"Circuit breaker opened due to failure in half-open state: {exception.Message}", "CircuitBreakerService");
                }
                else if (_state == CircuitBreakerState.Closed && _failureCount >= _failureThreshold)
                {
                    // Too many failures - open the circuit
                    _state = CircuitBreakerState.Open;
                    _ = _errorHandler.LogWarningAsync($"Circuit breaker opened due to {_failureCount} consecutive failures. Last error: {exception.Message}", "CircuitBreakerService");
                }
            }
        }
    }

    /// <summary>
    /// Circuit breaker states
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is closed - operations are allowed
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open - operations are blocked
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open - testing if service has recovered
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }
}