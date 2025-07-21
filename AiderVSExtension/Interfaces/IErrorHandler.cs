using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for handling errors and exceptions throughout the extension
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;

        /// <summary>
        /// Handles an exception and determines the appropriate response
        /// </summary>
        /// <param name="exception">The exception to handle</param>
        /// <param name="context">Additional context information</param>
        /// <returns>Error handling result</returns>
        Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string context = null);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="exception">Optional exception</param>
        /// <param name="context">Additional context</param>
        /// <returns>Task representing the async operation</returns>
        Task LogErrorAsync(string message, Exception exception = null, string context = null);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="context">Additional context</param>
        /// <returns>Task representing the async operation</returns>
        Task LogWarningAsync(string message, string context = null);

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The information message</param>
        /// <param name="context">Additional context</param>
        /// <returns>Task representing the async operation</returns>
        Task LogInfoAsync(string message, string context = null);

        /// <summary>
        /// Shows a user-friendly error notification
        /// </summary>
        /// <param name="message">The message to show</param>
        /// <param name="title">Optional title</param>
        /// <returns>Task representing the async operation</returns>
        Task ShowErrorNotificationAsync(string message, string title = null);

        /// <summary>
        /// Gets recent error entries
        /// </summary>
        /// <param name="count">Number of entries to retrieve</param>
        /// <returns>List of recent error entries</returns>
        Task<IEnumerable<ErrorEntry>> GetRecentErrorsAsync(int count = 50);

        /// <summary>
        /// Clears the error log
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ClearErrorLogAsync();
    }

    /// <summary>
    /// Event arguments for error occurred events
    /// </summary>
    public class ErrorOccurredEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string Context { get; set; }
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the result of error handling
    /// </summary>
    public class ErrorHandlingResult
    {
        public bool ShouldContinue { get; set; }
        public bool ShouldRetry { get; set; }
        public string UserMessage { get; set; }
        public TimeSpan? RetryDelay { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }

        public ErrorHandlingResult()
        {
            AdditionalData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Represents an error log entry
    /// </summary>
    public class ErrorEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
        public string Context { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }

        public ErrorEntry()
        {
            Id = Guid.NewGuid().ToString();
            AdditionalData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}