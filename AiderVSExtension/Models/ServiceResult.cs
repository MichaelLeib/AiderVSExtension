using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the result of a service operation with detailed error information
    /// </summary>
    /// <typeparam name="T">The type of data returned on success</typeparam>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public ServiceErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static ServiceResult<T> Successful(T data)
        {
            return new ServiceResult<T>
            {
                Success = true,
                Data = data,
                ErrorType = ServiceErrorType.None
            };
        }

        /// <summary>
        /// Creates a failed result with error details
        /// </summary>
        public static ServiceResult<T> Failed(ServiceErrorType errorType, string errorMessage, Exception exception = null)
        {
            return new ServiceResult<T>
            {
                Success = false,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static ServiceResult<T> FromException(Exception exception, string context = null)
        {
            var errorType = DetermineErrorTypeFromException(exception);
            var errorMessage = string.IsNullOrEmpty(context) 
                ? exception.Message 
                : $"{context}: {exception.Message}";

            return Failed(errorType, errorMessage, exception);
        }

        private static ServiceErrorType DetermineErrorTypeFromException(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException => ServiceErrorType.InvalidInput,
                UnauthorizedAccessException => ServiceErrorType.Authentication,
                System.Net.Http.HttpRequestException => ServiceErrorType.Network,
                TaskCanceledException => ServiceErrorType.Timeout,
                System.IO.FileNotFoundException => ServiceErrorType.NotFound,
                System.IO.DirectoryNotFoundException => ServiceErrorType.NotFound,
                System.IO.IOException => ServiceErrorType.FileSystem,
                System.Security.SecurityException => ServiceErrorType.Security,
                NotSupportedException => ServiceErrorType.NotSupported,
                InvalidOperationException => ServiceErrorType.InvalidOperation,
                System.Net.WebException => ServiceErrorType.Network,
                AiderVSExtension.Exceptions.AiderConnectionException => ServiceErrorType.Network,
                AiderVSExtension.Exceptions.AiderCommunicationException => ServiceErrorType.Communication,
                AiderVSExtension.Exceptions.AiderSessionException => ServiceErrorType.Session,
                _ => ServiceErrorType.Unknown
            };
        }
    }

    /// <summary>
    /// Non-generic version for operations that don't return data
    /// </summary>
    public class ServiceResult : ServiceResult<object>
    {
        /// <summary>
        /// Creates a successful result without data
        /// </summary>
        public static ServiceResult Successful()
        {
            return new ServiceResult
            {
                Success = true,
                ErrorType = ServiceErrorType.None
            };
        }

        /// <summary>
        /// Creates a failed result with error details
        /// </summary>
        public static new ServiceResult Failed(ServiceErrorType errorType, string errorMessage, Exception exception = null)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static new ServiceResult FromException(Exception exception, string context = null)
        {
            var errorType = DetermineErrorTypeFromException(exception);
            var errorMessage = string.IsNullOrEmpty(context) 
                ? exception.Message 
                : $"{context}: {exception.Message}";

            return Failed(errorType, errorMessage, exception);
        }

        private static ServiceErrorType DetermineErrorTypeFromException(Exception exception)
        {
            return exception switch
            {
                ArgumentException or ArgumentNullException => ServiceErrorType.InvalidInput,
                UnauthorizedAccessException => ServiceErrorType.Authentication,
                System.Net.Http.HttpRequestException => ServiceErrorType.Network,
                TaskCanceledException => ServiceErrorType.Timeout,
                System.IO.FileNotFoundException => ServiceErrorType.NotFound,
                System.IO.DirectoryNotFoundException => ServiceErrorType.NotFound,
                System.IO.IOException => ServiceErrorType.FileSystem,
                System.Security.SecurityException => ServiceErrorType.Security,
                NotSupportedException => ServiceErrorType.NotSupported,
                InvalidOperationException => ServiceErrorType.InvalidOperation,
                System.Net.WebException => ServiceErrorType.Network,
                AiderVSExtension.Exceptions.AiderConnectionException => ServiceErrorType.Network,
                AiderVSExtension.Exceptions.AiderCommunicationException => ServiceErrorType.Communication,
                AiderVSExtension.Exceptions.AiderSessionException => ServiceErrorType.Session,
                _ => ServiceErrorType.Unknown
            };
        }
    }

    /// <summary>
    /// Types of service errors for categorization and handling
    /// </summary>
    public enum ServiceErrorType
    {
        None,
        Unknown,
        InvalidInput,
        Authentication,
        Authorization,
        Network,
        Timeout,
        NotFound,
        FileSystem,
        Security,
        NotSupported,
        InvalidOperation,
        Communication,
        Session,
        Configuration,
        Validation,
        Throttling,
        ServiceUnavailable
    }

    /// <summary>
    /// Retry policy information for failed operations
    /// </summary>
    public class RetryPolicy
    {
        public bool ShouldRetry { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public int MaxRetryAttempts { get; set; } = 3;
        public int CurrentAttempt { get; set; }
        public bool UseExponentialBackoff { get; set; }

        public static RetryPolicy NoRetry => new RetryPolicy { ShouldRetry = false };

        public static RetryPolicy LinearRetry(TimeSpan delay, int maxAttempts = 3)
        {
            return new RetryPolicy
            {
                ShouldRetry = true,
                RetryDelay = delay,
                MaxRetryAttempts = maxAttempts,
                UseExponentialBackoff = false
            };
        }

        public static RetryPolicy ExponentialRetry(TimeSpan baseDelay, int maxAttempts = 3)
        {
            return new RetryPolicy
            {
                ShouldRetry = true,
                RetryDelay = baseDelay,
                MaxRetryAttempts = maxAttempts,
                UseExponentialBackoff = true
            };
        }

        public TimeSpan GetNextRetryDelay()
        {
            if (!UseExponentialBackoff)
                return RetryDelay;

            // Exponential backoff with jitter
            var exponentialDelay = TimeSpan.FromMilliseconds(
                RetryDelay.TotalMilliseconds * Math.Pow(2, CurrentAttempt));
            
            // Add jitter (±25% random variation)
            var jitter = new Random().NextDouble() * 0.5 - 0.25; // -0.25 to +0.25
            var jitteredDelay = TimeSpan.FromMilliseconds(
                exponentialDelay.TotalMilliseconds * (1 + jitter));

            // Cap at reasonable maximum (e.g., 30 seconds)
            var maxDelay = TimeSpan.FromSeconds(30);
            return jitteredDelay > maxDelay ? maxDelay : jitteredDelay;
        }

        public bool CanRetry()
        {
            return ShouldRetry && CurrentAttempt < MaxRetryAttempts;
        }
    }
}