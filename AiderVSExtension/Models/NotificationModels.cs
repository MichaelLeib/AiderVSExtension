using System;
using System.Threading.Tasks;
using System.Windows;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Progress
    }

    /// <summary>
    /// Notification display styles
    /// </summary>
    public enum NotificationStyle
    {
        Default,
        Toast,
        StatusBar,
        Balloon
    }

    /// <summary>
    /// Notification request
    /// </summary>
    public class NotificationRequest
    {
        /// <summary>
        /// Notification type
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Notification title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Notification message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Display duration (null for persistent)
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Display style
        /// </summary>
        public NotificationStyle? Style { get; set; }

        /// <summary>
        /// Additional data
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Notification item
    /// </summary>
    public class NotificationItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Notification type
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Notification title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Notification message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Display duration (null for persistent)
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Display style
        /// </summary>
        public NotificationStyle Style { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Expiration timestamp
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Additional data
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Associated UI element
        /// </summary>
        public UIElement UIElement { get; set; }

        /// <summary>
        /// Whether the notification is dismissed
        /// </summary>
        public bool IsDismissed { get; set; }
    }

    /// <summary>
    /// Progress notification implementation
    /// </summary>
    public class ProgressNotification : IProgressNotification
    {
        private readonly INotificationService _notificationService;

        /// <summary>
        /// Notification ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Notification title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Notification message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Current progress value (0-100)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Whether progress is indeterminate
        /// </summary>
        public bool IsIndeterminate { get; set; }

        /// <summary>
        /// Whether the operation is completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Whether the operation was cancelled
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Internal constructor for NotificationService
        /// </summary>
        internal ProgressNotification()
        {
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the progress notification
        /// </summary>
        /// <param name="progress">Progress value (0-100)</param>
        /// <param name="message">Optional message update</param>
        public async Task UpdateProgressAsync(double progress, string message = null)
        {
            Progress = Math.Max(0, Math.Min(100, progress));
            
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }

            // Update UI if notification service is available
            if (_notificationService != null)
            {
                await _notificationService.ShowNotificationAsync(new NotificationRequest
                {
                    Type = NotificationType.Progress,
                    Title = Title,
                    Message = Message,
                    Duration = null,
                    Data = this
                });
            }
        }

        /// <summary>
        /// Completes the progress notification
        /// </summary>
        /// <param name="message">Optional completion message</param>
        public async Task CompleteAsync(string message = null)
        {
            IsCompleted = true;
            Progress = 100;
            
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }

            // Show completion notification
            if (_notificationService != null)
            {
                await _notificationService.ShowSuccessAsync(Message ?? "Operation completed successfully", Title);
                await _notificationService.DismissNotificationAsync(Id);
            }
        }

        /// <summary>
        /// Cancels the progress notification
        /// </summary>
        /// <param name="message">Optional cancellation message</param>
        public async Task CancelAsync(string message = null)
        {
            IsCancelled = true;
            
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }

            // Show cancellation notification
            if (_notificationService != null)
            {
                await _notificationService.ShowWarningAsync(Message ?? "Operation cancelled", Title);
                await _notificationService.DismissNotificationAsync(Id);
            }
        }

        /// <summary>
        /// Fails the progress notification
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public async Task FailAsync(string errorMessage)
        {
            ErrorMessage = errorMessage;
            Message = errorMessage;

            // Show error notification
            if (_notificationService != null)
            {
                await _notificationService.ShowErrorAsync(errorMessage, Title);
                await _notificationService.DismissNotificationAsync(Id);
            }
        }
    }
}