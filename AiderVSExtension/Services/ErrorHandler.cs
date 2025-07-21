using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Comprehensive error handler that integrates with Visual Studio's output window and notification system
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        private readonly IVsOutputWindow _outputWindow;
        private readonly IVsStatusbar _statusBar;
        private readonly ConcurrentQueue<ErrorEntry> _errorLog;
        private readonly object _outputPaneLock = new object();
        
        private IVsOutputWindowPane _outputPane;
        private const string OutputPaneName = "Aider VS Extension";
        private const int MaxErrorLogSize = 1000;

        public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;

        public ErrorHandler()
        {
            _errorLog = new ConcurrentQueue<ErrorEntry>();
            
            // Get Visual Studio services
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            _statusBar = ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            
            InitializeOutputPane();
        }

        public async Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string context = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var result = new ErrorHandlingResult();
            var severity = DetermineErrorSeverity(exception);
            
            // Log the error
            await LogErrorAsync($"Exception occurred: {exception.Message}", exception, context);
            
            // Fire error occurred event
            ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs
            {
                Exception = exception,
                Context = context,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            });

            // Determine handling strategy based on exception type
            switch (exception)
            {
                case AiderVSExtension.Exceptions.AiderConnectionException connectionEx:
                    result.ShouldRetry = true;
                    result.RetryDelay = TimeSpan.FromSeconds(5);
                    result.UserMessage = "Connection to Aider backend failed. Retrying...";
                    result.ShouldContinue = true;
                    break;

                case AiderVSExtension.Exceptions.AiderCommunicationException commEx:
                    result.ShouldRetry = true;
                    result.RetryDelay = TimeSpan.FromSeconds(2);
                    result.UserMessage = "Communication error with Aider. Retrying...";
                    result.ShouldContinue = true;
                    break;

                case AiderVSExtension.Exceptions.AiderSessionException sessionEx:
                    result.ShouldRetry = false;
                    result.UserMessage = "Session error occurred. Please restart the extension.";
                    result.ShouldContinue = false;
                    await ShowErrorNotificationAsync(result.UserMessage, "Aider Extension Error");
                    break;

                case ArgumentException argEx:
                    result.ShouldRetry = false;
                    result.UserMessage = "Invalid input provided.";
                    result.ShouldContinue = true;
                    break;

                case UnauthorizedAccessException authEx:
                    result.ShouldRetry = false;
                    result.UserMessage = "Access denied. Please check your permissions.";
                    result.ShouldContinue = false;
                    await ShowErrorNotificationAsync(result.UserMessage, "Access Denied");
                    break;

                case System.Net.Http.HttpRequestException httpEx:
                    result.ShouldRetry = true;
                    result.RetryDelay = TimeSpan.FromSeconds(3);
                    result.UserMessage = "Network error occurred. Retrying...";
                    result.ShouldContinue = true;
                    break;

                case TaskCanceledException timeoutEx:
                    result.ShouldRetry = true;
                    result.RetryDelay = TimeSpan.FromSeconds(1);
                    result.UserMessage = "Operation timed out. Retrying...";
                    result.ShouldContinue = true;
                    break;

                default:
                    result.ShouldRetry = false;
                    result.UserMessage = "An unexpected error occurred.";
                    result.ShouldContinue = true;
                    
                    // For critical errors, show notification
                    if (severity == ErrorSeverity.Critical)
                    {
                        await ShowErrorNotificationAsync($"Critical error: {exception.Message}", "Aider Extension Error");
                        result.ShouldContinue = false;
                    }
                    break;
            }

            return result;
        }

        public async Task LogErrorAsync(string message, Exception exception = null, string context = null)
        {
            var errorEntry = new ErrorEntry
            {
                Timestamp = DateTime.UtcNow,
                Severity = ErrorSeverity.Error,
                Message = message,
                Context = context,
                ExceptionType = exception?.GetType().Name,
                StackTrace = exception?.StackTrace
            };

            if (exception != null)
            {
                errorEntry.AdditionalData["ExceptionMessage"] = exception.Message;
                errorEntry.AdditionalData["InnerException"] = exception.InnerException?.Message;
            }

            await AddErrorEntry(errorEntry);
            await WriteToOutputPaneAsync($"[ERROR] {FormatLogMessage(errorEntry)}");
        }

        public async Task LogWarningAsync(string message, string context = null)
        {
            var errorEntry = new ErrorEntry
            {
                Timestamp = DateTime.UtcNow,
                Severity = ErrorSeverity.Warning,
                Message = message,
                Context = context
            };

            await AddErrorEntry(errorEntry);
            await WriteToOutputPaneAsync($"[WARNING] {FormatLogMessage(errorEntry)}");
        }

        public async Task LogInfoAsync(string message, string context = null)
        {
            var errorEntry = new ErrorEntry
            {
                Timestamp = DateTime.UtcNow,
                Severity = ErrorSeverity.Info,
                Message = message,
                Context = context
            };

            await AddErrorEntry(errorEntry);
            await WriteToOutputPaneAsync($"[INFO] {FormatLogMessage(errorEntry)}");
        }

        public async Task ShowErrorNotificationAsync(string message, string title = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                // Show in status bar
                if (_statusBar != null)
                {
                    _statusBar.SetText($"Aider Extension: {message}");
                }

                // Show notification using Visual Studio's notification system
                var notificationService = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
                if (notificationService != null)
                {
                    // Use Visual Studio's info bar or notification system
                    // This is a simplified implementation - in a real extension, you'd use IVsInfoBarUIFactory
                    System.Windows.MessageBox.Show(message, title ?? "Aider Extension", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Fallback logging if notification fails
                await WriteToOutputPaneAsync($"[ERROR] Failed to show notification: {ex.Message}");
                await WriteToOutputPaneAsync($"[ERROR] Original message: {message}");
            }
        }

        public async Task<IEnumerable<ErrorEntry>> GetRecentErrorsAsync(int count = 50)
        {
            await Task.CompletedTask; // Make method async
            return _errorLog.TakeLast(Math.Min(count, _errorLog.Count)).ToList();
        }

        public async Task ClearErrorLogAsync()
        {
            await Task.CompletedTask; // Make method async
            
            // Clear the queue
            while (_errorLog.TryDequeue(out _)) { }
            
            await WriteToOutputPaneAsync("[INFO] Error log cleared");
        }

        private void InitializeOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                if (_outputWindow == null)
                    return;

                // Create or get the output pane
                var paneGuid = new Guid("12345678-1234-1234-1234-123456789012"); // Unique GUID for our pane
                
                _outputWindow.CreatePane(ref paneGuid, OutputPaneName, 1, 1);
                _outputWindow.GetPane(ref paneGuid, out _outputPane);
                
                if (_outputPane != null)
                {
                    _outputPane.Activate();
                    _outputPane.OutputString($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Aider VS Extension logging initialized\n");
                }
            }
            catch (Exception ex)
            {
                // If we can't initialize the output pane, we'll just continue without it
                System.Diagnostics.Debug.WriteLine($"Failed to initialize output pane: {ex.Message}");
            }
        }

        private async Task WriteToOutputPaneAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            try
            {
                lock (_outputPaneLock)
                {
                    if (_outputPane != null)
                    {
                        var timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                        _outputPane.OutputString(timestampedMessage);
                    }
                    else
                    {
                        // Fallback to debug output
                        System.Diagnostics.Debug.WriteLine($"Aider Extension: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Last resort fallback
                System.Diagnostics.Debug.WriteLine($"Failed to write to output pane: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Original message: {message}");
            }
        }

        private async Task AddErrorEntry(ErrorEntry entry)
        {
            await Task.CompletedTask; // Make method async
            
            _errorLog.Enqueue(entry);
            
            // Maintain maximum log size
            while (_errorLog.Count > MaxErrorLogSize)
            {
                _errorLog.TryDequeue(out _);
            }
        }

        private ErrorSeverity DetermineErrorSeverity(Exception exception)
        {
            switch (exception)
            {
                case AiderVSExtension.Exceptions.AiderSessionException:
                case UnauthorizedAccessException:
                case System.Security.SecurityException:
                    return ErrorSeverity.Critical;
                
                case AiderVSExtension.Exceptions.AiderConnectionException:
                case AiderVSExtension.Exceptions.AiderCommunicationException:
                case System.Net.Http.HttpRequestException:
                case TaskCanceledException:
                    return ErrorSeverity.Warning;
                
                case ArgumentException:
                case ArgumentNullException:
                case InvalidOperationException:
                    return ErrorSeverity.Error;
                
                default:
                    return ErrorSeverity.Error;
            }
        }

        private string FormatLogMessage(ErrorEntry entry)
        {
            var message = $"{entry.Message}";
            
            if (!string.IsNullOrEmpty(entry.Context))
            {
                message += $" [Context: {entry.Context}]";
            }
            
            if (!string.IsNullOrEmpty(entry.ExceptionType))
            {
                message += $" [Exception: {entry.ExceptionType}]";
            }
            
            return message;
        }
    }
}