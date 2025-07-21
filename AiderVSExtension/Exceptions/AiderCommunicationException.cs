using System;

namespace AiderVSExtension.Exceptions
{
    /// <summary>
    /// Exception thrown when there are communication issues with the Aider backend
    /// </summary>
    public class AiderCommunicationException : AiderServiceException
    {
        /// <summary>
        /// Initializes a new instance of the AiderCommunicationException class
        /// </summary>
        public AiderCommunicationException() : base("Communication with Aider backend failed")
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderCommunicationException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public AiderCommunicationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AiderCommunicationException class with a specified error message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public AiderCommunicationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the message that failed to be sent or received
        /// </summary>
        public string FailedMessage { get; set; }

        /// <summary>
        /// Gets the communication method that failed (WebSocket, HTTP, etc.)
        /// </summary>
        public string CommunicationMethod { get; set; }

        /// <summary>
        /// Gets the HTTP status code if applicable
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Gets the WebSocket close status if applicable
        /// </summary>
        public System.Net.WebSockets.WebSocketCloseStatus WebSocketCloseStatus { get; set; }

        /// <summary>
        /// Gets the response content if available
        /// </summary>
        public string ResponseContent { get; set; }
    }
}