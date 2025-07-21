using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
// Use fully qualified names to avoid ambiguity

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Implementation of message queue service for processing messages asynchronously
    /// </summary>
    public class MessageQueueService : Interfaces.IMessageQueue
    {
        private readonly ConcurrentQueue<AiderVSExtension.Interfaces.QueuedMessage> _queue;
        private readonly ConcurrentDictionary<string, AiderVSExtension.Interfaces.QueuedMessage> _messageIndex;
        private readonly AiderVSExtension.Interfaces.QueueStatistics _statistics;
        private readonly Timer _processingTimer;
        private QueueState _state = QueueState.Stopped;
        private bool _disposed = false;
        private readonly object _stateLock = new object();

        public event EventHandler<AiderVSExtension.Interfaces.MessageProcessedEventArgs> MessageProcessed;
        public event EventHandler<AiderVSExtension.Interfaces.QueueStateChangedEventArgs> QueueStateChanged;
        
        // Additional events required by interface
        public event EventHandler<AiderVSExtension.Interfaces.MessageQueueEventArgs> MessageEnqueued;
        public event EventHandler<AiderVSExtension.Interfaces.MessageQueueEventArgs> MessageDequeued;
        public event EventHandler<AiderVSExtension.Interfaces.MessageQueueEventArgs> MessageExpired;
        public event EventHandler<AiderVSExtension.Interfaces.MessageQueueEventArgs> MessageRetryExceeded;

        public MessageQueueService()
        {
            _queue = new ConcurrentQueue<QueuedMessage>();
            _messageIndex = new ConcurrentDictionary<string, QueuedMessage>();
            _statistics = new AiderVSExtension.Interfaces.QueueStatistics
            {
                StartTime = DateTime.UtcNow
            };

            // Create a timer for periodic processing (every 100ms)
            _processingTimer = new Timer(ProcessMessages, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <inheritdoc/>
        public int QueueSize => _queue.Count;

        /// <inheritdoc/>
        public bool IsProcessing => _state == QueueState.Running;

        // Additional properties expected by interface
        public int Count => _queue.Count;
        public bool IsEmpty => _queue.IsEmpty;

        /// <inheritdoc/>
        public string EnqueueMessage(AiderVSExtension.Interfaces.QueuedMessage message, AiderVSExtension.Interfaces.MessagePriority priority = AiderVSExtension.Interfaces.MessagePriority.Normal)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageQueueService));

            message.Priority = priority;
            message.QueuedAt = DateTime.UtcNow;

            _queue.Enqueue(message);
            _messageIndex.TryAdd(message.Id, message);

            _statistics.CurrentQueueSize = _queue.Count;
            _statistics.LastUpdated = DateTime.UtcNow;

            return message.Id;
        }

        /// <inheritdoc/>
        public async Task<AiderVSExtension.Interfaces.QueuedMessage> DequeueMessageAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return null;

            // Try to dequeue with priority ordering
            var messages = GetPendingMessages().OrderByDescending(m => m.Priority).ToList();
            
            foreach (var message in messages)
            {
                if (_queue.TryDequeue(out var dequeuedMessage) && dequeuedMessage.Id == message.Id)
                {
                    _messageIndex.TryRemove(message.Id, out _);
                    _statistics.CurrentQueueSize = _queue.Count;
                    return dequeuedMessage;
                }
            }

            return await Task.FromResult<QueuedMessage>(null);
        }

        /// <inheritdoc/>
        public Task StartAsync()
        {
            lock (_stateLock)
            {
                if (_state == QueueState.Running)
                    return Task.CompletedTask;

                var previousState = _state;
                _state = QueueState.Starting;

                OnQueueStateChanged(previousState, _state);

                // Start the processing timer
                _processingTimer.Change(0, 100); // Process every 100ms

                _state = QueueState.Running;
                OnQueueStateChanged(QueueState.Starting, _state);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            lock (_stateLock)
            {
                if (_state == QueueState.Stopped)
                    return Task.CompletedTask;

                var previousState = _state;
                _state = QueueState.Stopping;

                OnQueueStateChanged(previousState, _state);

                // Stop the processing timer
                _processingTimer.Change(Timeout.Infinite, Timeout.Infinite);

                _state = QueueState.Stopped;
                OnQueueStateChanged(QueueState.Stopping, _state);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
            _messageIndex.Clear();
            _statistics.CurrentQueueSize = 0;
            _statistics.LastUpdated = DateTime.UtcNow;
        }

        /// <inheritdoc/>
        public IEnumerable<AiderVSExtension.Interfaces.QueuedMessage> GetPendingMessages()
        {
            return _messageIndex.Values.ToList();
        }

        /// <inheritdoc/>
        public bool RemoveMessage(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
                return false;

            var removed = _messageIndex.TryRemove(messageId, out _);
            if (removed)
            {
                _statistics.CurrentQueueSize = _queue.Count;
                _statistics.LastUpdated = DateTime.UtcNow;
            }

            return removed;
        }

        /// <inheritdoc/>
        public AiderVSExtension.Interfaces.QueueStatistics GetStatistics()
        {
            _statistics.LastUpdated = DateTime.UtcNow;
            _statistics.CurrentQueueSize = _queue.Count;

            // Calculate messages per minute
            var elapsed = DateTime.UtcNow - _statistics.StartTime;
            if (elapsed.TotalMinutes > 0)
            {
                _statistics.MessagesPerMinute = _statistics.TotalProcessed / elapsed.TotalMinutes;
            }

            // Calculate average processing time
            if (_statistics.TotalProcessed > 0)
            {
                // This is a simplified calculation - in a real implementation, 
                // you'd track actual processing times
                _statistics.AverageProcessingTime = TimeSpan.FromMilliseconds(100);
            }

            return _statistics;
        }

        /// <summary>
        /// Timer callback for processing messages
        /// </summary>
        private async void ProcessMessages(object state)
        {
            if (_state != QueueState.Running || _disposed)
                return;

            try
            {
                var message = await DequeueMessageAsync();
                if (message != null)
                {
                    await ProcessMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a single message
        /// </summary>
        private async Task ProcessMessageAsync(AiderVSExtension.Interfaces.QueuedMessage message)
        {
            var startTime = DateTime.UtcNow;
            bool success = false;
            string errorMessage = null;

            try
            {
                // Increment attempt count
                message.AttemptCount++;

                // Simulate message processing
                // In a real implementation, this would delegate to appropriate handlers
                await Task.Delay(10); // Simulate processing time

                success = true;
                _statistics.TotalSuccessful++;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                success = false;
                _statistics.TotalFailed++;

                // Re-queue message if retries available
                if (message.AttemptCount < message.MaxRetries)
                {
                    EnqueueMessage(message, message.Priority);
                }
            }
            finally
            {
                _statistics.TotalProcessed++;
                _statistics.LastUpdated = DateTime.UtcNow;

                var processingTime = DateTime.UtcNow - startTime;

                // Fire processed event
                MessageProcessed?.Invoke(this, new AiderVSExtension.Interfaces.MessageProcessedEventArgs
                {
                    Message = message,
                    Success = success,
                    ErrorMessage = errorMessage,
                    ProcessingTime = processingTime
                });
            }
        }

        /// <summary>
        /// Raises the queue state changed event
        /// </summary>
        private void OnQueueStateChanged(QueueState previousState, QueueState currentState)
        {
            QueueStateChanged?.Invoke(this, new QueueStateChangedEventArgs
            {
                PreviousState = previousState,
                CurrentState = currentState,
                QueueSize = _queue.Count
            });
        }

        // Additional methods required by interface
        public async Task<bool> EnqueueAsync(AiderVSExtension.Models.ChatMessage message, AiderVSExtension.Interfaces.MessagePriority priority = AiderVSExtension.Interfaces.MessagePriority.Normal)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var queuedMessage = new AiderVSExtension.Interfaces.QueuedMessage
            {
                Content = message.Content ?? string.Empty,
                Priority = priority,
                QueuedAt = DateTime.UtcNow
            };

            var messageId = EnqueueMessage(queuedMessage, priority);
            MessageEnqueued?.Invoke(this, new AiderVSExtension.Interfaces.MessageQueueEventArgs { QueuedMessage = queuedMessage });
            return await Task.FromResult(true);
        }

        public async Task<AiderVSExtension.Interfaces.QueuedMessage> DequeueAsync(CancellationToken cancellationToken = default)
        {
            var message = await DequeueMessageAsync(cancellationToken);
            if (message != null)
            {
                MessageDequeued?.Invoke(this, new AiderVSExtension.Interfaces.MessageQueueEventArgs { QueuedMessage = message });
            }
            return message;
        }

        public async Task<bool> MarkAsCompletedAsync(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
                return false;

            if (_messageIndex.TryGetValue(messageId, out var message))
            {
                message.IsCompleted = true;
                message.CompletedAt = DateTime.UtcNow;
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        public async Task<bool> MarkAsFailedAsync(string messageId, string errorMessage = null)
        {
            if (string.IsNullOrEmpty(messageId))
                return false;

            if (_messageIndex.TryGetValue(messageId, out var message))
            {
                message.IsFailed = true;
                message.ErrorMessage = errorMessage;
                message.FailedAt = DateTime.UtcNow;
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        public AiderVSExtension.Interfaces.MessageStatus GetMessageStatus(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
                return AiderVSExtension.Interfaces.MessageStatus.Unknown;

            if (_messageIndex.TryGetValue(messageId, out var message))
            {
                if (message.IsCompleted)
                    return AiderVSExtension.Interfaces.MessageStatus.Completed;
                if (message.IsFailed)
                    return AiderVSExtension.Interfaces.MessageStatus.Failed;
                return AiderVSExtension.Interfaces.MessageStatus.Queued;
            }
            return AiderVSExtension.Interfaces.MessageStatus.Unknown;
        }

        public IEnumerable<AiderVSExtension.Interfaces.QueuedMessage> GetAllMessages()
        {
            return GetPendingMessages();
        }

        public async Task ClearAsync()
        {
            Clear();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _processingTimer?.Dispose();
                Clear();
                MessageProcessed = null;
                QueueStateChanged = null;
                _disposed = true;
            }
        }
    }
}