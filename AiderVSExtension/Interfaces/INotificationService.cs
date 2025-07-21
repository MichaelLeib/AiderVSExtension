using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for notification and user feedback services
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Event fired when a notification is shown
        /// </summary>
        event EventHandler<NotificationEventArgs> NotificationShown;

        /// <summary>
        /// Event fired when a notification is dismissed
        /// </summary>
        event EventHandler<NotificationEventArgs> NotificationDismissed;

        /// <summary>
        /// Shows an information notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="duration">Optional duration</param>
        Task ShowInfoAsync(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows a success notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="duration">Optional duration</param>
        Task ShowSuccessAsync(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="duration">Optional duration</param>
        Task ShowWarningAsync(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows an error notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="duration">Optional duration</param>
        Task ShowErrorAsync(string message, string title = null, TimeSpan? duration = null);

        /// <summary>
        /// Shows a progress notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="isIndeterminate">Whether progress is indeterminate</param>
        /// <returns>Progress notification interface</returns>
        Task<IProgressNotification> ShowProgressAsync(string message, string title = null, bool isIndeterminate = false);

        /// <summary>
        /// Shows a custom notification
        /// </summary>
        /// <param name="request">Notification request</param>
        Task ShowNotificationAsync(NotificationRequest request);

        /// <summary>
        /// Dismisses a notification by ID
        /// </summary>
        /// <param name="notificationId">Notification ID to dismiss</param>
        Task DismissNotificationAsync(string notificationId);

        /// <summary>
        /// Dismisses all notifications
        /// </summary>
        Task DismissAllNotificationsAsync();

        /// <summary>
        /// Gets all active notifications
        /// </summary>
        /// <returns>List of active notifications</returns>
        IEnumerable<NotificationItem> GetActiveNotifications();

        /// <summary>
        /// Shows a toast notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="type">Notification type</param>
        /// <param name="duration">Optional duration</param>
        Task ShowToastAsync(string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null);

        /// <summary>
        /// Shows a status bar notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="type">Notification type</param>
        /// <param name="duration">Optional duration</param>
        Task ShowStatusBarAsync(string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null);

        /// <summary>
        /// Shows a balloon notification
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="title">Optional title</param>
        /// <param name="type">Notification type</param>
        /// <param name="duration">Optional duration</param>
        Task ShowBalloonAsync(string message, string title = null, NotificationType type = NotificationType.Info, TimeSpan? duration = null);
    }

    /// <summary>
    /// Progress notification interface
    /// </summary>
    public interface IProgressNotification
    {
        /// <summary>
        /// Notification ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Notification title
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Notification message
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Current progress value (0-100)
        /// </summary>
        double Progress { get; set; }

        /// <summary>
        /// Whether progress is indeterminate
        /// </summary>
        bool IsIndeterminate { get; set; }

        /// <summary>
        /// Whether the operation is completed
        /// </summary>
        bool IsCompleted { get; set; }

        /// <summary>
        /// Whether the operation was cancelled
        /// </summary>
        bool IsCancelled { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        string ErrorMessage { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Updates the progress notification
        /// </summary>
        /// <param name="progress">Progress value (0-100)</param>
        /// <param name="message">Optional message update</param>
        Task UpdateProgressAsync(double progress, string message = null);

        /// <summary>
        /// Completes the progress notification
        /// </summary>
        /// <param name="message">Optional completion message</param>
        Task CompleteAsync(string message = null);

        /// <summary>
        /// Cancels the progress notification
        /// </summary>
        /// <param name="message">Optional cancellation message</param>
        Task CancelAsync(string message = null);

        /// <summary>
        /// Fails the progress notification
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        Task FailAsync(string errorMessage);
    }

    /// <summary>
    /// Notification event arguments
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        /// <summary>
        /// The notification item
        /// </summary>
        public NotificationItem Notification { get; set; }
    }
}