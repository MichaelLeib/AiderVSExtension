using System;

namespace AiderVSExtension.Exceptions
{
    /// <summary>
    /// Exception thrown when there are session management issues with the Aider service
    /// </summary>
    public class AiderSessionException : AiderServiceException
    {
        /// <summary>
        /// Initializes a new instance of the AiderSessionException class
        /// </summary>
        public AiderSessionException() : base("Aider session management failed")
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderSessionException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public AiderSessionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderSessionException class with a specified error message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public AiderSessionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the session ID that encountered the error
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets the session operation that failed
        /// </summary>
        public string FailedOperation { get; set; }

        /// <summary>
        /// Gets the conversation ID if applicable
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Gets the session state when the error occurred
        /// </summary>
        public string SessionState { get; set; }
    }
}