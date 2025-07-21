using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for showing notifications and user feedback
    /// </summary>
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly IVSThemingService _themingService;
        private readonly IErrorHandler _errorHandler;
        private readonly List<NotificationItem> _activeNotifications = new List<NotificationItem>();
        private readonly DispatcherTimer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public event EventHandler<NotificationEventArgs> NotificationShown;
        public event EventHandler<NotificationEventArgs> NotificationDismissed;

        public NotificationService(IVSThemingService themingService, IErrorHandler errorHandler)
        {
            _themingService = themingService ?? throw new ArgumentNullException(nameof(themingService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            // Start cleanup timer
            _cleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _cleanupTimer.Tick += CleanupExpiredNotifications;
            _cleanupTimer.Start();
        }

        /// <summary>
        /// Shows an information notification
        /// </summary>
        public async Task ShowInfoAsync(string message, string title = null, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = NotificationType.Info,
                Title = title ?? "Information",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(5)
            });
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        public async Task ShowSuccessAsync(string message, string title = null, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = NotificationType.Success,
                Title = title ?? "Success",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(4)
            });
        }

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        public async Task ShowWarningAsync(string message, string title = null, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = NotificationType.Warning,
                Title = title ?? "Warning",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(6)
            });
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        public async Task ShowErrorAsync(string message, string title = null, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = NotificationType.Error,
                Title = title ?? "Error",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(8)
            });
        }

        /// <summary>
        /// Shows a progress notification
        /// </summary>
        public async Task<IProgressNotification> ShowProgressAsync(string message, string title = null, bool isIndeterminate = false)
        {
            var notification = new ProgressNotification
            {
                Id = Guid.NewGuid().ToString(),
                Title = title ?? "Progress",
                Message = message,
                IsIndeterminate = isIndeterminate,
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };

            await ShowNotificationAsync(new NotificationRequest
            {
                Type = NotificationType.Progress,
                Title = notification.Title,
                Message = notification.Message,
                Duration = null, // Progress notifications don't auto-expire
                Data = notification
            });

            return notification;
        }

        /// <summary>
        /// Shows a custom notification
        /// </summary>
        public async Task ShowNotificationAsync(NotificationRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var notification = CreateNotificationItem(request);
                    DisplayNotification(notification);
                    
                    lock (_lockObject)
                    {
                        _activeNotifications.Add(notification);
                    }

                    NotificationShown?.Invoke(this, new NotificationEventArgs { Notification = notification });
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "NotificationService.ShowNotificationAsync");
            }
        }

        /// <summary>
        /// Dismisses a notification
        /// </summary>
        public async Task DismissNotificationAsync(string notificationId)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lockObject)
                    {
                        var notification = _activeNotifications.FirstOrDefault(n => n.Id == notificationId);
                        if (notification != null)
                        {
                            DismissNotification(notification);
                            _activeNotifications.Remove(notification);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "NotificationService.DismissNotificationAsync");
            }
        }

        /// <summary>
        /// Dismisses all notifications
        /// </summary>
        public async Task DismissAllNotificationsAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lockObject)
                    {
                        var notifications = _activeNotifications.ToList();
                        foreach (var notification in notifications)
                        {
                            DismissNotification(notification);
                        }
                        _activeNotifications.Clear();
                    }
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "NotificationService.DismissAllNotificationsAsync");
            }
        }

        /// <summary>
        /// Gets all active notifications
        /// </summary>
        public IEnumerable<NotificationItem> GetActiveNotifications()
        {
            lock (_lockObject)
            {
                return _activeNotifications.ToList();
            }
        }

        /// <summary>
        /// Shows a toast notification
        /// </summary>
        public async Task ShowToastAsync(string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = type,
                Title = null,
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(3),
                Style = NotificationStyle.Toast
            });
        }

        /// <summary>
        /// Shows a status bar notification
        /// </summary>
        public async Task ShowStatusBarAsync(string message, NotificationType type = NotificationType.Info, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = type,
                Title = null,
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(5),
                Style = NotificationStyle.StatusBar
            });
        }

        /// <summary>
        /// Shows a balloon notification
        /// </summary>
        public async Task ShowBalloonAsync(string message, string title = null, NotificationType type = NotificationType.Info, TimeSpan? duration = null)
        {
            await ShowNotificationAsync(new NotificationRequest
            {
                Type = type,
                Title = title ?? "Notification",
                Message = message,
                Duration = duration ?? TimeSpan.FromSeconds(6),
                Style = NotificationStyle.Balloon
            });
        }

        #region Private Methods

        private NotificationItem CreateNotificationItem(NotificationRequest request)
        {
            var notification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Duration = request.Duration,
                Style = request.Style ?? NotificationStyle.Default,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.Duration.HasValue ? DateTime.UtcNow.Add(request.Duration.Value) : null,
                Data = request.Data
            };

            return notification;
        }

        private void DisplayNotification(NotificationItem notification)
        {
            try
            {
                switch (notification.Style)
                {
                    case NotificationStyle.Toast:
                        ShowToastNotification(notification);
                        break;
                    case NotificationStyle.StatusBar:
                        ShowStatusBarNotification(notification);
                        break;
                    case NotificationStyle.Balloon:
                        ShowBalloonNotification(notification);
                        break;
                    default:
                        ShowDefaultNotification(notification);
                        break;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "NotificationService.DisplayNotification");
            }
        }

        private void ShowToastNotification(NotificationItem notification)
        {
            var toastWindow = new ToastNotificationWindow(notification, _themingService);
            toastWindow.Show();
            notification.UIElement = toastWindow;

            // Position at top-right of screen
            var workingArea = SystemParameters.WorkArea;
            toastWindow.Left = workingArea.Right - toastWindow.Width - 20;
            toastWindow.Top = 20;

            // Animate in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            toastWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void ShowStatusBarNotification(NotificationItem notification)
        {
            // This would integrate with VS status bar
            // For now, show as toast
            ShowToastNotification(notification);
        }

        private void ShowBalloonNotification(NotificationItem notification)
        {
            var balloonWindow = new BalloonNotificationWindow(notification, _themingService);
            balloonWindow.Show();
            notification.UIElement = balloonWindow;

            // Position at bottom-right of screen
            var workingArea = SystemParameters.WorkArea;
            balloonWindow.Left = workingArea.Right - balloonWindow.Width - 20;
            balloonWindow.Top = workingArea.Bottom - balloonWindow.Height - 20;

            // Animate in
            var slideIn = new ThicknessAnimation(new Thickness(balloonWindow.Left + 300, balloonWindow.Top, 0, 0), 
                                                new Thickness(balloonWindow.Left, balloonWindow.Top, 0, 0), 
                                                TimeSpan.FromMilliseconds(400));
            balloonWindow.BeginAnimation(FrameworkElement.MarginProperty, slideIn);
        }

        private void ShowDefaultNotification(NotificationItem notification)
        {
            var defaultWindow = new DefaultNotificationWindow(notification, _themingService);
            defaultWindow.Show();
            notification.UIElement = defaultWindow;

            // Position at center of screen
            defaultWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void DismissNotification(NotificationItem notification)
        {
            try
            {
                if (notification.UIElement is Window window)
                {
                    // Animate out
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (s, e) => window.Close();
                    window.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }

                NotificationDismissed?.Invoke(this, new NotificationEventArgs { Notification = notification });
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "NotificationService.DismissNotification");
            }
        }

        private void CleanupExpiredNotifications(object sender, EventArgs e)
        {
            try
            {
                var now = DateTime.UtcNow;
                lock (_lockObject)
                {
                    var expiredNotifications = _activeNotifications
                        .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now)
                        .ToList();

                    foreach (var notification in expiredNotifications)
                    {
                        DismissNotification(notification);
                        _activeNotifications.Remove(notification);
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "NotificationService.CleanupExpiredNotifications");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Stop();
                _cleanupTimer?.Dispose();
                DismissAllNotificationsAsync().Wait(TimeSpan.FromSeconds(1));
                _disposed = true;
            }
        }
    }

    #region Notification Windows

    /// <summary>
    /// Toast notification window
    /// </summary>
    public class ToastNotificationWindow : Window
    {
        public ToastNotificationWindow(NotificationItem notification, IVSThemingService themingService)
        {
            InitializeComponent(notification, themingService);
        }

        private void InitializeComponent(NotificationItem notification, IVSThemingService themingService)
        {
            Title = "Toast Notification";
            Width = 300;
            Height = 80;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Transparent;

            var border = new Border
            {
                Background = themingService.GetThemedBrush(ThemeResourceKey.WindowBackground),
                BorderBrush = themingService.GetThemedBrush(ThemeResourceKey.ActiveBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10)
            };

            var stackPanel = new StackPanel();
            
            if (!string.IsNullOrEmpty(notification.Title))
            {
                var titleText = new TextBlock
                {
                    Text = notification.Title,
                    FontWeight = FontWeights.Bold,
                    Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText)
                };
                stackPanel.Children.Add(titleText);
            }

            var messageText = new TextBlock
            {
                Text = notification.Message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText)
            };
            stackPanel.Children.Add(messageText);

            border.Child = stackPanel;
            Content = border;

            // Apply theme
            themingService.ApplyTheme(this);
        }
    }

    /// <summary>
    /// Balloon notification window
    /// </summary>
    public class BalloonNotificationWindow : Window
    {
        public BalloonNotificationWindow(NotificationItem notification, IVSThemingService themingService)
        {
            InitializeComponent(notification, themingService);
        }

        private void InitializeComponent(NotificationItem notification, IVSThemingService themingService)
        {
            Title = "Balloon Notification";
            Width = 350;
            Height = 120;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Transparent;

            var border = new Border
            {
                Background = themingService.GetThemedBrush(ThemeResourceKey.WindowBackground),
                BorderBrush = themingService.GetThemedBrush(ThemeResourceKey.ActiveBorder),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon
            var icon = new TextBlock
            {
                Text = GetNotificationIcon(notification.Type),
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = GetNotificationColor(notification.Type, themingService)
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // Content
            var stackPanel = new StackPanel();
            Grid.SetColumn(stackPanel, 1);

            if (!string.IsNullOrEmpty(notification.Title))
            {
                var titleText = new TextBlock
                {
                    Text = notification.Title,
                    FontWeight = FontWeights.Bold,
                    Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(titleText);
            }

            var messageText = new TextBlock
            {
                Text = notification.Message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText)
            };
            stackPanel.Children.Add(messageText);

            grid.Children.Add(stackPanel);
            border.Child = grid;
            Content = border;

            // Apply theme
            themingService.ApplyTheme(this);
        }

        private string GetNotificationIcon(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Info:
                    return "ℹ";
                case NotificationType.Success:
                    return "✓";
                case NotificationType.Warning:
                    return "⚠";
                case NotificationType.Error:
                    return "✗";
                default:
                    return "●";
            }
        }

        private Brush GetNotificationColor(NotificationType type, IVSThemingService themingService)
        {
            switch (type)
            {
                case NotificationType.Info:
                    return themingService.GetThemedBrush(ThemeResourceKey.Information);
                case NotificationType.Success:
                    return new SolidColorBrush(Colors.Green);
                case NotificationType.Warning:
                    return themingService.GetThemedBrush(ThemeResourceKey.Warning);
                case NotificationType.Error:
                    return themingService.GetThemedBrush(ThemeResourceKey.Error);
                default:
                    return themingService.GetThemedBrush(ThemeResourceKey.WindowText);
            }
        }
    }

    /// <summary>
    /// Default notification window
    /// </summary>
    public class DefaultNotificationWindow : Window
    {
        public DefaultNotificationWindow(NotificationItem notification, IVSThemingService themingService)
        {
            InitializeComponent(notification, themingService);
        }

        private void InitializeComponent(NotificationItem notification, IVSThemingService themingService)
        {
            Title = notification.Title ?? "Notification";
            Width = 400;
            Height = 200;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Background = themingService.GetThemedBrush(ThemeResourceKey.WindowBackground);

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            if (!string.IsNullOrEmpty(notification.Title))
            {
                var titleText = new TextBlock
                {
                    Text = notification.Title,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(titleText);
            }

            var messageText = new TextBlock
            {
                Text = notification.Message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = themingService.GetThemedBrush(ThemeResourceKey.WindowText),
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(messageText);

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeButton.Click += (s, e) => Close();
            stackPanel.Children.Add(closeButton);

            Content = stackPanel;

            // Apply theme
            themingService.ApplyTheme(this);
        }
    }

    #endregion
}