using System;

namespace AiderVSExtension.Exceptions
{
    /// <summary>
    /// Exception thrown when there are connection issues with the Aider backend
    /// </summary>
    public class AiderConnectionException : AiderServiceException
    {
        /// <summary>
        /// Initializes a new instance of the AiderConnectionException class
        /// </summary>
        public AiderConnectionException() : base("Connection to Aider backend failed")
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderConnectionException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public AiderConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderConnectionException class with a specified error message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public AiderConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the connection endpoint that failed
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets the connection type that failed (WebSocket, HTTP, etc.)
        /// </summary>
        public string ConnectionType { get; set; }

        /// <summary>
        /// Gets the number of retry attempts made
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// Gets the timeout duration if applicable
        /// </summary>
        public TimeSpan Timeout { get; set; }
    }
}