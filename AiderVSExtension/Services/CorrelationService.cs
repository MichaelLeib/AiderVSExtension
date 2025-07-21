using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing correlation IDs across async operations
    /// </summary>
    public class CorrelationService : ICorrelationService
    {
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
        private static readonly AsyncLocal<string> _operationId = new AsyncLocal<string>();
        private static readonly AsyncLocal<string> _userId = new AsyncLocal<string>();

        /// <summary>
        /// Gets or sets the current correlation ID
        /// </summary>
        public string CorrelationId 
        { 
            get => _correlationId.Value ?? GenerateNewCorrelationId();
            set => _correlationId.Value = value;
        }

        /// <summary>
        /// Gets or sets the current operation ID
        /// </summary>
        public string OperationId 
        { 
            get => _operationId.Value;
            set => _operationId.Value = value;
        }

        /// <summary>
        /// Gets or sets the current user ID
        /// </summary>
        public string UserId 
        { 
            get => _userId.Value;
            set => _userId.Value = value;
        }

        /// <summary>
        /// Starts a new operation with a fresh correlation ID
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Correlation context for the operation</returns>
        public ICorrelationContext StartOperation(string operationName)
        {
            var correlationId = GenerateNewCorrelationId();
            var operationId = GenerateNewOperationId();
            
            return new CorrelationContext(correlationId, operationId, operationName);
        }

        /// <summary>
        /// Starts a child operation with the same correlation ID
        /// </summary>
        /// <param name="operationName">Name of the child operation</param>
        /// <returns>Correlation context for the child operation</returns>
        public ICorrelationContext StartChildOperation(string operationName)
        {
            var currentCorrelationId = CorrelationId;
            var childOperationId = GenerateNewOperationId();
            
            return new CorrelationContext(currentCorrelationId, childOperationId, operationName);
        }

        /// <summary>
        /// Gets the current correlation context
        /// </summary>
        /// <returns>Current correlation context</returns>
        public ICorrelationContext GetCurrentContext()
        {
            return new CorrelationContext(CorrelationId, OperationId, "Current");
        }

        /// <summary>
        /// Sets the correlation context from another context
        /// </summary>
        /// <param name="context">Context to set</param>
        public void SetContext(ICorrelationContext context)
        {
            if (context != null)
            {
                CorrelationId = context.CorrelationId;
                OperationId = context.OperationId;
            }
        }

        /// <summary>
        /// Clears the current correlation context
        /// </summary>
        public void Clear()
        {
            _correlationId.Value = null;
            _operationId.Value = null;
        }

        /// <summary>
        /// Gets the current correlation ID for the active request
        /// </summary>
        /// <returns>Current correlation ID</returns>
        public string GetCurrentCorrelationId()
        {
            return CorrelationId;
        }

        /// <summary>
        /// Sets the correlation ID for the current request context
        /// </summary>
        /// <param name="correlationId">The correlation ID to set</param>
        public void SetCorrelationId(string correlationId)
        {
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Generates a new correlation ID
        /// </summary>
        /// <returns>A new unique correlation ID</returns>
        public string GenerateCorrelationId()
        {
            return GenerateNewCorrelationId();
        }

        /// <summary>
        /// Executes an action within a correlation context
        /// </summary>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="action">The action to execute</param>
        public async Task ExecuteWithCorrelationAsync(string correlationId, Func<Task> action)
        {
            var previousCorrelationId = CorrelationId;
            try
            {
                CorrelationId = correlationId;
                await action();
            }
            finally
            {
                CorrelationId = previousCorrelationId;
            }
        }

        /// <summary>
        /// Executes a function within a correlation context
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="correlationId">The correlation ID to use</param>
        /// <param name="function">The function to execute</param>
        /// <returns>The result of the function</returns>
        public async Task<T> ExecuteWithCorrelationAsync<T>(string correlationId, Func<Task<T>> function)
        {
            var previousCorrelationId = CorrelationId;
            try
            {
                CorrelationId = correlationId;
                return await function();
            }
            finally
            {
                CorrelationId = previousCorrelationId;
            }
        }

        private static string GenerateNewCorrelationId()
        {
            return $"corr-{Guid.NewGuid():N}";
        }

        private static string GenerateNewOperationId()
        {
            return $"op-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// Correlation context that tracks IDs and manages scope
    /// </summary>
    public class CorrelationContext : ICorrelationContext
    {
        private readonly string _previousCorrelationId;
        private readonly string _previousOperationId;
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private bool _disposed = false;

        public string CorrelationId { get; }
        public string OperationId { get; }
        public string OperationName { get; }
        public DateTime StartTime { get; }
        public DateTime CreatedAt { get; }
        public IReadOnlyDictionary<string, object> Properties => _properties;

        public CorrelationContext(string correlationId, string operationId, string operationName)
        {
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            StartTime = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;

            // Save previous context
            _previousCorrelationId = CorrelationService._correlationId.Value;
            _previousOperationId = CorrelationService._operationId.Value;

            // Set new context
            CorrelationService._correlationId.Value = CorrelationId;
            CorrelationService._operationId.Value = OperationId;
        }

        /// <summary>
        /// Gets the elapsed time since the operation started
        /// </summary>
        public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;

        /// <summary>
        /// Creates a formatted log prefix with correlation information
        /// </summary>
        /// <returns>Log prefix string</returns>
        public string ToLogPrefix()
        {
            return $"[{CorrelationId}:{OperationId}:{OperationName}]";
        }

        /// <summary>
        /// Creates a dictionary with correlation properties for telemetry
        /// </summary>
        /// <returns>Dictionary with correlation properties</returns>
        public System.Collections.Generic.Dictionary<string, string> ToTelemetryProperties()
        {
            return new System.Collections.Generic.Dictionary<string, string>
            {
                ["CorrelationId"] = CorrelationId,
                ["OperationId"] = OperationId,
                ["OperationName"] = OperationName,
                ["StartTime"] = StartTime.ToString("O"),
                ["ElapsedMs"] = ElapsedTime.TotalMilliseconds.ToString("F2")
            };
        }

        /// <summary>
        /// Sets a property in the correlation context
        /// </summary>
        public void SetProperty(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            
            _properties[key] = value;
        }

        /// <summary>
        /// Gets a property from the correlation context
        /// </summary>
        public T GetProperty<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);
            
            if (_properties.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            
            return default(T);
        }

        /// <summary>
        /// Checks if a property exists in the context
        /// </summary>
        public bool HasProperty(string key)
        {
            return !string.IsNullOrEmpty(key) && _properties.ContainsKey(key);
        }

        /// <summary>
        /// Removes a property from the context
        /// </summary>
        public bool RemoveProperty(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            
            return _properties.Remove(key);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Restore previous context
                CorrelationService._correlationId.Value = _previousCorrelationId;
                CorrelationService._operationId.Value = _previousOperationId;
                
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for enhanced logging with correlation IDs
    /// </summary>
    public static class CorrelationExtensions
    {
        /// <summary>
        /// Logs a message with correlation information
        /// </summary>
        public static async System.Threading.Tasks.Task LogWithCorrelationAsync(
            this IErrorHandler errorHandler, 
            string message, 
            ICorrelationContext context, 
            string source = null)
        {
            var correlatedMessage = $"{context.ToLogPrefix()} {message}";
            await errorHandler.LogInfoAsync(correlatedMessage, source);
        }

        /// <summary>
        /// Logs an error with correlation information
        /// </summary>
        public static async System.Threading.Tasks.Task LogErrorWithCorrelationAsync(
            this IErrorHandler errorHandler, 
            string message, 
            Exception exception, 
            ICorrelationContext context, 
            string source = null)
        {
            var correlatedMessage = $"{context.ToLogPrefix()} {message}";
            await errorHandler.LogErrorAsync(correlatedMessage, exception, source);
        }

        /// <summary>
        /// Tracks an event with correlation properties
        /// </summary>
        public static void TrackEventWithCorrelation(
            this ITelemetryService telemetryService,
            string eventName,
            ICorrelationContext context,
            System.Collections.Generic.Dictionary<string, string> additionalProperties = null,
            System.Collections.Generic.Dictionary<string, double> metrics = null)
        {
            var properties = context.ToTelemetryProperties();
            
            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    properties[prop.Key] = prop.Value;
                }
            }

            telemetryService.TrackEvent(eventName, properties, metrics);
        }
    }
}