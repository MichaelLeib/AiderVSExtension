using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Advanced message queue system with priority, persistence, and retry capabilities
    /// </summary>
    public class MessageQueue : IDisposable
    {
        private readonly ConcurrentPriorityQueue<QueuedMessage> _messageQueue;
        private readonly ConcurrentDictionary<string, QueuedMessage> _messageTracker;
        private readonly IErrorHandler _errorHandler;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _processingSemaphore;
        
        private bool _isDisposed;
        private readonly object _lockObject = new object();
        
        // Configuration
        private readonly TimeSpan _messageExpiration = TimeSpan.FromMinutes(30);
        private readonly int _maxRetryAttempts = 3;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);
        private readonly int _maxQueueSize = 1000;

        public event EventHandler<MessageQueueEventArgs> MessageEnqueued;
        public event EventHandler<MessageQueueEventArgs> MessageDequeued;
        public event EventHandler<MessageQueueEventArgs> MessageExpired;
        public event EventHandler<MessageQueueEventArgs> MessageRetryExceeded;

        public int Count => _messageQueue.Count;
        public bool IsEmpty => _messageQueue.IsEmpty;

        public MessageQueue(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _messageQueue = new ConcurrentPriorityQueue<QueuedMessage>();
            _messageTracker = new ConcurrentDictionary<string, QueuedMessage>();
            _processingSemaphore = new SemaphoreSlim(1, 1);
            
            // Start cleanup timer to remove expired messages
            _cleanupTimer = new Timer(CleanupExpiredMessages, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Enqueues a message with specified priority
        /// </summary>
        public async Task<bool> EnqueueAsync(ChatMessage message, MessagePriority priority = MessagePriority.Normal)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageQueue));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Check queue size limit
                if (_messageQueue.Count >= _maxQueueSize)
                {
                    await _errorHandler.LogWarningAsync($"Message queue is full ({_maxQueueSize} messages)", "MessageQueue.EnqueueAsync");
                    return false;
                }

                var queuedMessage = new QueuedMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = message,
                    Priority = priority,
                    EnqueuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(_messageExpiration),
                    RetryCount = 0,
                    Status = MessageStatus.Queued
                };

                _messageQueue.Enqueue(queuedMessage, (int)priority);
                _messageTracker.TryAdd(queuedMessage.Id, queuedMessage);

                MessageEnqueued?.Invoke(this, new MessageQueueEventArgs { QueuedMessage = queuedMessage });

                await _errorHandler.LogInfoAsync($"Message enqueued with priority {priority}", "MessageQueue.EnqueueAsync");
                return true;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "MessageQueue.EnqueueAsync");
                return false;
            }
        }

        /// <summary>
        /// Dequeues the highest priority message
        /// </summary>
        public async Task<QueuedMessage> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageQueue));

            await _processingSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_messageQueue.TryDequeue(out var queuedMessage))
                {
                    // Check if message has expired
                    if (queuedMessage.ExpiresAt < DateTime.UtcNow)
                    {
                        queuedMessage.Status = MessageStatus.Expired;
                        _messageTracker.TryRemove(queuedMessage.Id, out _);
                        
                        MessageExpired?.Invoke(this, new MessageQueueEventArgs { QueuedMessage = queuedMessage });
                        await _errorHandler.LogWarningAsync($"Message expired: {queuedMessage.Id}", "MessageQueue.DequeueAsync");
                        
                        // Try to get next message
                        return await DequeueAsync(cancellationToken);
                    }

                    queuedMessage.Status = MessageStatus.Processing;
                    queuedMessage.DequeuedAt = DateTime.UtcNow;
                    
                    MessageDequeued?.Invoke(this, new MessageQueueEventArgs { QueuedMessage = queuedMessage });
                    
                    await _errorHandler.LogInfoAsync($"Message dequeued: {queuedMessage.Id}", "MessageQueue.DequeueAsync");
                    return queuedMessage;
                }

                return null;
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        /// <summary>
        /// Marks a message as successfully processed
        /// </summary>
        public async Task<bool> MarkAsCompletedAsync(string messageId)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageQueue));

            try
            {
                if (_messageTracker.TryGetValue(messageId, out var message))
                {
                    message.Status = MessageStatus.Completed;
                    message.CompletedAt = DateTime.UtcNow;
                    _messageTracker.TryRemove(messageId, out _);
                    
                    await _errorHandler.LogInfoAsync($"Message marked as completed: {messageId}", "MessageQueue.MarkAsCompletedAsync");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "MessageQueue.MarkAsCompletedAsync");
                return false;
            }
        }

        /// <summary>
        /// Marks a message as failed and re-queues it if retry limit not exceeded
        /// </summary>
        public async Task<bool> MarkAsFailedAsync(string messageId, string errorMessage = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageQueue));

            try
            {
                if (_messageTracker.TryGetValue(messageId, out var message))
                {
                    message.RetryCount++;
                    message.LastError = errorMessage;
                    message.LastRetryAt = DateTime.UtcNow;

                    if (message.RetryCount < _maxRetryAttempts)
                    {
                        // Re-queue for retry
                        message.Status = MessageStatus.Queued;
                        message.NextRetryAt = DateTime.UtcNow.Add(_retryDelay);
                        
                        _messageQueue.Enqueue(message, (int)message.Priority);
                        
                        await _errorHandler.LogWarningAsync($"Message re-queued for retry ({message.RetryCount}/{_maxRetryAttempts}): {messageId}", "MessageQueue.MarkAsFailedAsync");
                        return true;
                    }
                    else
                    {
                        // Max retries exceeded
                        message.Status = MessageStatus.Failed;
                        _messageTracker.TryRemove(messageId, out _);
                        
                        MessageRetryExceeded?.Invoke(this, new MessageQueueEventArgs { QueuedMessage = message });
                        await _errorHandler.LogErrorAsync($"Message failed after {_maxRetryAttempts} attempts: {messageId} - {errorMessage}", "MessageQueue.MarkAsFailedAsync");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "MessageQueue.MarkAsFailedAsync");
                return false;
            }
        }

        /// <summary>
        /// Gets the status of a specific message
        /// </summary>
        public MessageStatus GetMessageStatus(string messageId)
        {
            if (_messageTracker.TryGetValue(messageId, out var message))
            {
                return message.Status;
            }

            return MessageStatus.Unknown;
        }

        /// <summary>
        /// Gets all messages in the queue
        /// </summary>
        public IEnumerable<QueuedMessage> GetAllMessages()
        {
            return _messageTracker.Values.ToList();
        }

        /// <summary>
        /// Gets queue statistics
        /// </summary>
        public QueueStatistics GetStatistics()
        {
            var messages = _messageTracker.Values.ToList();
            
            return new QueueStatistics
            {
                TotalMessages = messages.Count,
                QueuedMessages = messages.Count(m => m.Status == MessageStatus.Queued),
                ProcessingMessages = messages.Count(m => m.Status == MessageStatus.Processing),
                CompletedMessages = messages.Count(m => m.Status == MessageStatus.Completed),
                FailedMessages = messages.Count(m => m.Status == MessageStatus.Failed),
                ExpiredMessages = messages.Count(m => m.Status == MessageStatus.Expired),
                HighPriorityMessages = messages.Count(m => m.Priority == MessagePriority.High),
                NormalPriorityMessages = messages.Count(m => m.Priority == MessagePriority.Normal),
                LowPriorityMessages = messages.Count(m => m.Priority == MessagePriority.Low),
                OldestMessage = messages.OrderBy(m => m.EnqueuedAt).FirstOrDefault()?.EnqueuedAt,
                NewestMessage = messages.OrderByDescending(m => m.EnqueuedAt).FirstOrDefault()?.EnqueuedAt
            };
        }

        /// <summary>
        /// Clears all messages from the queue
        /// </summary>
        public async Task ClearAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageQueue));

            try
            {
                await _processingSemaphore.WaitAsync();
                
                _messageQueue.Clear();
                _messageTracker.Clear();
                
                await _errorHandler.LogInfoAsync("Message queue cleared", "MessageQueue.ClearAsync");
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        /// <summary>
        /// Removes expired messages from the queue
        /// </summary>
        private async void CleanupExpiredMessages(object state)
        {
            if (_isDisposed)
                return;

            try
            {
                var expiredMessages = _messageTracker.Values
                    .Where(m => m.ExpiresAt < DateTime.UtcNow && m.Status == MessageStatus.Queued)
                    .ToList();

                foreach (var message in expiredMessages)
                {
                    message.Status = MessageStatus.Expired;
                    _messageTracker.TryRemove(message.Id, out _);
                    
                    MessageExpired?.Invoke(this, new MessageQueueEventArgs { QueuedMessage = message });
                }

                if (expiredMessages.Any())
                {
                    await _errorHandler.LogInfoAsync($"Cleaned up {expiredMessages.Count} expired messages", "MessageQueue.CleanupExpiredMessages");
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "MessageQueue.CleanupExpiredMessages");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            
            _cleanupTimer?.Dispose();
            _processingSemaphore?.Dispose();
            
            // Clear all messages
            _messageQueue.Clear();
            _messageTracker.Clear();
        }
    }

    // Interface moved to Interfaces/IMessageQueue.cs to avoid duplication

    /// <summary>
    /// Represents a message in the queue with metadata
    /// </summary>
    public class QueuedMessage
    {
        public string Id { get; set; }
        public ChatMessage Message { get; set; }
        public MessagePriority Priority { get; set; }
        public MessageStatus Status { get; set; }
        public DateTime EnqueuedAt { get; set; }
        public DateTime? DequeuedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastRetryAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string LastError { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
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
    /// Message status in the queue
    /// </summary>
    public enum MessageStatus
    {
        Unknown,
        Queued,
        Processing,
        Completed,
        Failed,
        Expired
    }

    /// <summary>
    /// Event arguments for message queue events
    /// </summary>
    public class MessageQueueEventArgs : EventArgs
    {
        public QueuedMessage QueuedMessage { get; set; }
        public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Queue statistics
    /// </summary>
    public class QueueStatistics
    {
        public int TotalMessages { get; set; }
        public int QueuedMessages { get; set; }
        public int ProcessingMessages { get; set; }
        public int CompletedMessages { get; set; }
        public int FailedMessages { get; set; }
        public int ExpiredMessages { get; set; }
        public int HighPriorityMessages { get; set; }
        public int NormalPriorityMessages { get; set; }
        public int LowPriorityMessages { get; set; }
        public DateTime? OldestMessage { get; set; }
        public DateTime? NewestMessage { get; set; }
    }
}