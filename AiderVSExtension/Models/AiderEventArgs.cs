using System;
using System.Collections.Generic;
using AiderVSExtension.Models;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Event arguments for message received events
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The message that was received
        /// </summary>
        public ChatMessage Message { get; set; }

        /// <summary>
        /// The timestamp when the message was received
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The source of the message (WebSocket, HTTP, etc.)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Additional metadata about the message
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Event arguments for connection status changed events
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the connection is currently established
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// The previous connection state
        /// </summary>
        public bool PreviousState { get; set; }

        /// <summary>
        /// Error message if connection failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The timestamp when the status changed
        /// </summary>
        public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The type of connection (WebSocket, HTTP)
        /// </summary>
        public string ConnectionType { get; set; }

        /// <summary>
        /// The endpoint that was connected to
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// The reason for the connection status change
        /// </summary>
        public ConnectionChangeReason Reason { get; set; }
    }

    /// <summary>
    /// Event arguments for message sent events
    /// </summary>
    public class MessageSentEventArgs : EventArgs
    {
        /// <summary>
        /// The message that was sent
        /// </summary>
        public ChatMessage Message { get; set; }

        /// <summary>
        /// Whether the message was sent successfully
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if sending failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The timestamp when the message was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The number of retry attempts made
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// The method used to send the message (WebSocket, HTTP)
        /// </summary>
        public string SendMethod { get; set; }
    }

    /// <summary>
    /// Event arguments for session events
    /// </summary>
    public class SessionEventArgs : EventArgs
    {
        /// <summary>
        /// The session identifier
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The type of session event
        /// </summary>
        public SessionEventType EventType { get; set; }

        /// <summary>
        /// The timestamp when the event occurred
        /// </summary>
        public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional data about the session event
        /// </summary>
        public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Error message if the event represents an error
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents the reason for a connection status change
    /// </summary>
    public enum ConnectionChangeReason
    {
        /// <summary>
        /// Initial connection established
        /// </summary>
        InitialConnection,

        /// <summary>
        /// Connection lost unexpectedly
        /// </summary>
        ConnectionLost,

        /// <summary>
        /// Connection closed normally
        /// </summary>
        NormalClosure,

        /// <summary>
        /// Connection failed to establish
        /// </summary>
        ConnectionFailed,

        /// <summary>
        /// Reconnection attempt succeeded
        /// </summary>
        Reconnected,

        /// <summary>
        /// Reconnection attempt failed
        /// </summary>
        ReconnectionFailed,

        /// <summary>
        /// Connection manually closed
        /// </summary>
        ManuallyDisconnected,

        /// <summary>
        /// Connection timeout
        /// </summary>
        Timeout,

        /// <summary>
        /// Server shutdown
        /// </summary>
        ServerShutdown,

        /// <summary>
        /// Authentication failed
        /// </summary>
        AuthenticationFailed,

        /// <summary>
        /// Configuration changed
        /// </summary>
        ConfigurationChanged
    }

    /// <summary>
    /// Types of session events
    /// </summary>
    public enum SessionEventType
    {
        /// <summary>
        /// New session started
        /// </summary>
        SessionStarted,

        /// <summary>
        /// Session ended
        /// </summary>
        SessionEnded,

        /// <summary>
        /// Session saved
        /// </summary>
        SessionSaved,

        /// <summary>
        /// Session loaded
        /// </summary>
        SessionLoaded,

        /// <summary>
        /// Session archived
        /// </summary>
        SessionArchived,

        /// <summary>
        /// Session restored from archive
        /// </summary>
        SessionRestored,

        /// <summary>
        /// Session error occurred
        /// </summary>
        SessionError,

        /// <summary>
        /// Session configuration changed
        /// </summary>
        SessionConfigurationChanged,

        /// <summary>
        /// Session data synchronized
        /// </summary>
        SessionSynchronized
    }
}