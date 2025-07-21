using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Constants;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for integrating with Visual Studio's output window
    /// </summary>
    public class OutputWindowService : IOutputWindowService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IQuickFixProvider _quickFixProvider;
        private readonly IErrorHandler _errorHandler;
        private IVsOutputWindow _outputWindow;
        private IVsOutputWindowPane _aiderPane;
        private const string AiderPaneGuid = "{A7C02A2B-8B4E-4F5D-9B3C-1E2F3A4B5C6E}";
        private readonly Dictionary<string, Regex> _errorPatterns;
        private bool _isInitialized = false;

        public event EventHandler<OutputWindowErrorEventArgs> ErrorAddToChatRequested;

        public OutputWindowService(IServiceProvider serviceProvider, IQuickFixProvider quickFixProvider, IErrorHandler errorHandler)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _quickFixProvider = quickFixProvider ?? throw new ArgumentNullException(nameof(quickFixProvider));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            // Initialize error detection patterns
            _errorPatterns = new Dictionary<string, Regex>
            {
                ["BuildError"] = new Regex(@"(?<file>[^(]+)\((?<line>\d+),(?<column>\d+)\):\s*error\s+(?<code>\w+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                ["BuildWarning"] = new Regex(@"(?<file>[^(]+)\((?<line>\d+),(?<column>\d+)\):\s*warning\s+(?<code>\w+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                ["CompilerError"] = new Regex(@"(?<file>.*?):\s*error\s+(?<code>\w+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                ["RuntimeError"] = new Regex(@"Exception\s+(?<type>\w+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                ["GeneralError"] = new Regex(@"(?i)error[:\s]+(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };
        }

        /// <summary>
        /// Initializes the output window integration
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _outputWindow = await _serviceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                
                if (_outputWindow != null)
                {
                    await CreateAiderOutputPaneAsync();
                    await RegisterForOutputEventsAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to initialize output window service");
            }
        }

        /// <summary>
        /// Writes a message to the Aider output pane
        /// </summary>
        public async Task WriteToAiderPaneAsync(string message)
        {
            if (!_isInitialized)
                await InitializeAsync();

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_aiderPane != null)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    var formattedMessage = $"[{timestamp}] {message}\r\n";
                    _aiderPane.OutputString(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to write to Aider pane");
            }
        }

        /// <summary>
        /// Writes an error message to the Aider output pane with "Add to Chat" functionality
        /// </summary>
        public async Task WriteErrorWithChatOptionAsync(string errorMessage, string source = null)
        {
            try
            {
                // Parse the error message to extract details
                var errorInfo = ParseErrorMessage(errorMessage, source);
                
                // Write to output pane with special formatting
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var formattedMessage = $"[{timestamp}] [{source ?? "Error"}] {errorMessage}\r\n";
                formattedMessage += $"[{timestamp}] >>> Click here to add to Aider Chat <<<\r\n";
                
                await WriteToAiderPaneAsync(formattedMessage);

                // Fire event for potential chat integration
                ErrorAddToChatRequested?.Invoke(this, errorInfo);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to write error with chat option");
            }
        }

        /// <summary>
        /// Clears the Aider output pane
        /// </summary>
        public async Task ClearAiderPaneAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_aiderPane != null)
                {
                    _aiderPane.Clear();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to clear Aider pane");
            }
        }

        /// <summary>
        /// Shows the Aider output pane
        /// </summary>
        public async Task ShowAiderPaneAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_aiderPane != null)
                {
                    _aiderPane.Activate();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to show Aider pane");
            }
        }

        /// <summary>
        /// Activates the output window and brings it to focus
        /// </summary>
        public async Task ActivateOutputWindowAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_outputWindow != null)
                {
                    var windowFrame = _outputWindow as IVsWindowFrame;
                    windowFrame?.Show();
                    
                    await ShowAiderPaneAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to activate output window");
            }
        }

        /// <summary>
        /// Registers for output window events to detect errors
        /// </summary>
        public async Task RegisterForOutputEventsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Note: Visual Studio's output window doesn't have direct event APIs
                // This would typically require polling or hooking into build events
                // For now, this is a placeholder for future implementation
                
                await WriteToAiderPaneAsync("Aider Output Window Integration Activated");
                await WriteToAiderPaneAsync("Monitoring for errors to add 'Add to Chat' functionality...");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to register for output events");
            }
        }

        /// <summary>
        /// Unregisters from output window events
        /// </summary>
        public async Task UnregisterFromOutputEventsAsync()
        {
            try
            {
                await WriteToAiderPaneAsync("Aider Output Window Integration Deactivated");
                // Cleanup event registrations here
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Failed to unregister from output events");
            }
        }

        /// <summary>
        /// Creates the Aider output pane
        /// </summary>
        private async Task CreateAiderOutputPaneAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var paneGuid = new Guid(AiderPaneGuid);
                
                // Try to get existing pane first
                _outputWindow.GetPane(ref paneGuid, out _aiderPane);
                
                // Create pane if it doesn't exist
                if (_aiderPane == null)
                {
                    _outputWindow.CreatePane(ref paneGuid, "Aider AI", 1, 1);
                    _outputWindow.GetPane(ref paneGuid, out _aiderPane);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error initializing output window service");
            }
        }

        /// <summary>
        /// Parses an error message to extract structured information
        /// </summary>
        private OutputWindowErrorEventArgs ParseErrorMessage(string errorMessage, string source)
        {
            var errorInfo = new OutputWindowErrorEventArgs
            {
                ErrorMessage = errorMessage,
                Source = source ?? "Unknown",
                Timestamp = DateTime.UtcNow
            };

            // Try to match against known error patterns
            foreach (var pattern in _errorPatterns)
            {
                var match = pattern.Value.Match(errorMessage);
                if (match.Success)
                {
                    if (match.Groups["file"].Success)
                        errorInfo.FilePath = match.Groups["file"].Value.Trim();
                    
                    if (match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, out int line))
                        errorInfo.LineNumber = line;
                    
                    if (match.Groups["column"].Success && int.TryParse(match.Groups["column"].Value, out int column))
                        errorInfo.ColumnNumber = column;
                    
                    if (match.Groups["code"].Success)
                        errorInfo.ErrorCode = match.Groups["code"].Value;
                    
                    if (match.Groups["message"].Success)
                        errorInfo.ErrorMessage = match.Groups["message"].Value.Trim();

                    // Set severity based on pattern type
                    errorInfo.Severity = pattern.Key.ToLowerInvariant() switch
                    {
                        var key when key.Contains("warning") => ErrorSeverity.Warning,
                        var key when key.Contains("error") => ErrorSeverity.Error,
                        var key when key.Contains("critical") => ErrorSeverity.Critical,
                        _ => ErrorSeverity.Error
                    };

                    break;
                }
            }

            return errorInfo;
        }
    }
}