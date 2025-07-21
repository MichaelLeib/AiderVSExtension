using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing message queuing and processing
    /// </summary>
    public interface IMessageQueue : IDisposable
    {
        /// <summary>
        /// Event fired when a message is processed
        /// </summary>
        event EventHandler<MessageProcessedEventArgs> MessageProcessed;

        /// <summary>
        /// Event fired when the queue state changes
        /// </summary>
        event EventHandler<QueueStateChangedEventArgs> QueueStateChanged;

        /// <summary>
        /// Enqueues a message for processing
        /// </summary>
        /// <param name="message">The message to enqueue</param>
        /// <param name="priority">The priority of the message</param>
        /// <returns>Unique message ID</returns>
        string EnqueueMessage(QueuedMessage message, MessagePriority priority = MessagePriority.Normal);

        /// <summary>
        /// Dequeues the next message for processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next message or null if queue is empty</returns>
        Task<QueuedMessage> DequeueMessageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current queue size
        /// </summary>
        int QueueSize { get; }

        /// <summary>
        /// Gets whether the queue is processing messages
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Starts message processing
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops message processing
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Clears all messages from the queue
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets all pending messages
        /// </summary>
        /// <returns>Collection of pending messages</returns>
        IEnumerable<QueuedMessage> GetPendingMessages();

        /// <summary>
        /// Removes a specific message from the queue
        /// </summary>
        /// <param name="messageId">The message ID to remove</param>
        /// <returns>True if message was removed</returns>
        bool RemoveMessage(string messageId);

        /// <summary>
        /// Gets message processing statistics
        /// </summary>
        QueueStatistics GetStatistics();

        /// <summary>
        /// Enqueues a message for processing (async version)
        /// </summary>
        Task<bool> EnqueueAsync(ChatMessage message, MessagePriority priority = MessagePriority.Normal);

        /// <summary>
        /// Dequeues a message for processing (async version)
        /// </summary>
        Task<QueuedMessage> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as completed
        /// </summary>
        Task<bool> MarkAsCompletedAsync(string messageId);

        /// <summary>
        /// Marks a message as failed with error message
        /// </summary>
        Task<bool> MarkAsFailedAsync(string messageId, string errorMessage = null);

        /// <summary>
        /// Gets the status of a specific message
        /// </summary>
        MessageStatus GetMessageStatus(string messageId);

        /// <summary>
        /// Gets all messages in the queue
        /// </summary>
        IEnumerable<QueuedMessage> GetAllMessages();

        /// <summary>
        /// Clears all messages from the queue (async version)
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Events for message lifecycle
        /// </summary>
        event EventHandler<MessageQueueEventArgs> MessageEnqueued;
        event EventHandler<MessageQueueEventArgs> MessageDequeued;
        event EventHandler<MessageQueueEventArgs> MessageExpired;
        event EventHandler<MessageQueueEventArgs> MessageRetryExceeded;
    }

    /// <summary>
    /// Represents a message in the queue
    /// </summary>
    public class QueuedMessage
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Message priority
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Timestamp when message was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when message was queued
        /// </summary>
        public DateTime QueuedAt { get; set; }

        /// <summary>
        /// Number of processing attempts
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Additional message metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timeout for message processing
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Whether the message has been completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Timestamp when message was completed
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Whether the message has failed
        /// </summary>
        public bool IsFailed { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when message failed
        /// </summary>
        public DateTime FailedAt { get; set; }
    }

    /// <summary>
    /// Message priority levels
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Event arguments for message processed events
    /// </summary>
    public class MessageProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// The processed message
        /// </summary>
        public QueuedMessage Message { get; set; }

        /// <summary>
        /// Whether processing was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Processing duration
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Event arguments for queue state changes
    /// </summary>
    public class QueueStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Previous queue state
        /// </summary>
        public QueueState PreviousState { get; set; }

        /// <summary>
        /// Current queue state
        /// </summary>
        public QueueState CurrentState { get; set; }

        /// <summary>
        /// Current queue size
        /// </summary>
        public int QueueSize { get; set; }
    }

    /// <summary>
    /// Queue processing states
    /// </summary>
    public enum QueueState
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    /// <summary>
    /// Queue processing statistics
    /// </summary>
    public class QueueStatistics
    {
        /// <summary>
        /// Total messages processed
        /// </summary>
        public long TotalProcessed { get; set; }

        /// <summary>
        /// Total successful messages
        /// </summary>
        public long TotalSuccessful { get; set; }

        /// <summary>
        /// Total failed messages
        /// </summary>
        public long TotalFailed { get; set; }

        /// <summary>
        /// Current queue size
        /// </summary>
        public int CurrentQueueSize { get; set; }

        /// <summary>
        /// Average processing time
        /// </summary>
        public TimeSpan AverageProcessingTime { get; set; }

        /// <summary>
        /// Messages processed per minute
        /// </summary>
        public double MessagesPerMinute { get; set; }

        /// <summary>
        /// Statistics collection start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for message queue operations
    /// </summary>
    public class MessageQueueEventArgs : EventArgs
    {
        /// <summary>
        /// The queued message involved in the operation
        /// </summary>
        public QueuedMessage QueuedMessage { get; set; }
    }

    /// <summary>
    /// Message processing status
    /// </summary>
    public enum MessageStatus
    {
        Unknown = 0,
        Queued = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Expired = 5
    }
}