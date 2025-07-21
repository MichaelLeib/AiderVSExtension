using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing application state and lifecycle events
    /// </summary>
    public class ApplicationStateService : IApplicationStateService, IDisposable
    {
        private readonly string _stateFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lockObject = new object();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        
        private DTE2? _dte;
        private IVsShell? _vsShell;
        private SolutionEvents? _solutionEvents;
        private bool _disposed = false;
        private DateTime _initializationStartTime;

        // State properties
        private bool _isInitializing = false;
        private bool _isInitialized = false;
        private bool _isShuttingDown = false;
        private string _currentTheme = "Unknown";
        private bool _isSolutionOpen = false;
        private string? _currentSolutionPath = null;

        // Events
        public event EventHandler<ExtensionInitializingEventArgs>? ExtensionInitializing;
        public event EventHandler<ExtensionInitializedEventArgs>? ExtensionInitialized;
        public event EventHandler<ExtensionShuttingDownEventArgs>? ExtensionShuttingDown;
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
        public event EventHandler<SolutionOpenedEventArgs>? SolutionOpened;
        public event EventHandler<SolutionClosedEventArgs>? SolutionClosed;

        // Properties
        public bool IsInitializing => _isInitializing;
        public bool IsInitialized => _isInitialized;
        public bool IsShuttingDown => _isShuttingDown;
        public string CurrentTheme => _currentTheme;
        public bool IsSolutionOpen => _isSolutionOpen;
        public string? CurrentSolutionPath => _currentSolutionPath;

        public ApplicationStateService()
        {
            // Get the user's local app data directory for Visual Studio extensions (lightweight)
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var stateDirectory = Path.Combine(localAppData, "AiderVSExtension", "State");
            _stateFilePath = Path.Combine(stateDirectory, "application-state.json");

            // Configure JSON serialization options (lightweight)
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // NOTE: Directory creation moved to InitializeAsync() to avoid blocking UI thread
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized || _isInitializing)
                return;

            _isInitializing = true;
            _initializationStartTime = DateTime.UtcNow;

            try
            {
                // Create directory on background thread
                await Task.Run(() =>
                {
                    var stateDirectory = Path.GetDirectoryName(_stateFilePath);
                    if (!string.IsNullOrEmpty(stateDirectory))
                    {
                        Directory.CreateDirectory(stateDirectory);
                    }
                });

                // Fire initializing event
                ExtensionInitializing?.Invoke(this, new ExtensionInitializingEventArgs
                {
                    StartTime = _initializationStartTime,
                    Version = GetExtensionVersion()
                });

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Initialize Visual Studio services
                await InitializeVisualStudioServicesAsync();

                // Restore previous state
                await RestoreStateAsync();

                // Setup event handlers
                SetupEventHandlers();

                // Detect current theme
                DetectCurrentTheme();

                // Check if solution is already open
                CheckCurrentSolution();

                _isInitialized = true;
                _isInitializing = false;

                // Fire initialized event
                ExtensionInitialized?.Invoke(this, new ExtensionInitializedEventArgs
                {
                    CompletedTime = DateTime.UtcNow,
                    InitializationDuration = DateTime.UtcNow - _initializationStartTime,
                    IsSuccessful = true
                });
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                
                // Fire initialized event with error
                ExtensionInitialized?.Invoke(this, new ExtensionInitializedEventArgs
                {
                    CompletedTime = DateTime.UtcNow,
                    InitializationDuration = DateTime.UtcNow - _initializationStartTime,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                });

                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;

            try
            {
                // Fire shutting down event
                ExtensionShuttingDown?.Invoke(this, new ExtensionShuttingDownEventArgs
                {
                    ShutdownTime = DateTime.UtcNow,
                    Reason = "Extension shutdown",
                    IsGraceful = true
                });

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Save current state
                await SaveStateAsync();

                // Cleanup resources
                await PerformCleanupAsync();

                // Dispose event handlers
                CleanupEventHandlers();
            }
            catch (Exception ex)
            {
                // Log error but don't throw during shutdown
                await HandleUnhandledExceptionAsync(ex, "ApplicationStateService.ShutdownAsync");
            }
        }

        public async Task SaveStateAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var state = new ApplicationState
                {
                    LastSavedAt = DateTime.UtcNow,
                    CurrentTheme = _currentTheme,
                    IsSolutionOpen = _isSolutionOpen,
                    CurrentSolutionPath = _currentSolutionPath,
                    ExtensionVersion = GetExtensionVersion(),
                    MemoryUsage = GetMemoryUsage()
                };

                lock (_lockObject)
                {
                    var json = JsonSerializer.Serialize(state, _jsonOptions);
                    File.WriteAllText(_stateFilePath, json);
                }
            }
            catch (Exception ex)
            {
                await HandleUnhandledExceptionAsync(ex, "ApplicationStateService.SaveStateAsync");
            }
        }

        public async Task RestoreStateAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                lock (_lockObject)
                {
                    if (!File.Exists(_stateFilePath))
                        return;

                    var json = File.ReadAllText(_stateFilePath);
                    var state = JsonSerializer.Deserialize<ApplicationState>(json, _jsonOptions);

                    if (state != null)
                    {
                        // Restore theme if it was saved
                        if (!string.IsNullOrEmpty(state.CurrentTheme))
                        {
                            _currentTheme = state.CurrentTheme;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't throw during restore - just log and continue
                await HandleUnhandledExceptionAsync(ex, "ApplicationStateService.RestoreStateAsync");
            }
        }

        public async Task HandleUnhandledExceptionAsync(Exception exception, string context)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Log to Visual Studio output window
                var outputWindow = await GetOutputWindowAsync();
                if (outputWindow != null)
                {
                    var message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in {context}: {exception.Message}\n{exception.StackTrace}\n";
                    outputWindow.OutputString(message);
                }

                // Save error state
                var errorState = new ErrorState
                {
                    Timestamp = DateTime.UtcNow,
                    Context = context,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace ?? string.Empty
                };

                await SaveErrorStateAsync(errorState);
            }
            catch
            {
                // If we can't handle the exception, there's not much we can do
            }
        }

        public async Task RecoverFromErrorAsync(string errorContext)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Perform cleanup
                await PerformCleanupAsync();

                // Try to reinitialize if needed
                if (!_isInitialized && !_isInitializing)
                {
                    await InitializeAsync();
                }

                // Log recovery attempt
                var outputWindow = await GetOutputWindowAsync();
                if (outputWindow != null)
                {
                    var message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Recovered from error in context: {errorContext}\n";
                    outputWindow.OutputString(message);
                }
            }
            catch (Exception ex)
            {
                await HandleUnhandledExceptionAsync(ex, $"ApplicationStateService.RecoverFromErrorAsync({errorContext})");
            }
        }

        public long GetMemoryUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return process.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        }

        public async Task PerformCleanupAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Cleanup temporary files if any
                await CleanupTemporaryFilesAsync();
            }
            catch (Exception ex)
            {
                await HandleUnhandledExceptionAsync(ex, "ApplicationStateService.PerformCleanupAsync");
            }
        }

        private async Task InitializeVisualStudioServicesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get DTE service
            _dte = await ServiceProvider.GetGlobalServiceAsync(typeof(DTE)) as DTE2;
            
            // Get VS Shell service
            _vsShell = await ServiceProvider.GetGlobalServiceAsync(typeof(SVsShell)) as IVsShell;
        }

        private void SetupEventHandlers()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte?.Events != null)
            {
                // Setup solution events
                _solutionEvents = _dte.Events.SolutionEvents;
                if (_solutionEvents != null)
                {
                    _solutionEvents.Opened += OnSolutionOpened;
                    _solutionEvents.AfterClosing += OnSolutionClosed;
                }
            }
        }

        private void CleanupEventHandlers()
        {
            if (_solutionEvents != null)
            {
                _solutionEvents.Opened -= OnSolutionOpened;
                _solutionEvents.AfterClosing -= OnSolutionClosed;
                _solutionEvents = null;
            }

            // Dispose all tracked disposables
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _disposables.Clear();
        }

        private void OnSolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _isSolutionOpen = true;
                _currentSolutionPath = _dte?.Solution?.FullName;

                var solutionName = string.IsNullOrEmpty(_currentSolutionPath) 
                    ? "Unknown" 
                    : Path.GetFileNameWithoutExtension(_currentSolutionPath);

                SolutionOpened?.Invoke(this, new SolutionOpenedEventArgs
                {
                    SolutionPath = _currentSolutionPath ?? string.Empty,
                    SolutionName = solutionName,
                    OpenedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _ = HandleUnhandledExceptionAsync(ex, "ApplicationStateService.OnSolutionOpened");
            }
        }

        private void OnSolutionClosed()
        {
            try
            {
                var oldSolutionPath = _currentSolutionPath;
                var oldSolutionName = string.IsNullOrEmpty(oldSolutionPath) 
                    ? "Unknown" 
                    : Path.GetFileNameWithoutExtension(oldSolutionPath);

                _isSolutionOpen = false;
                _currentSolutionPath = null;

                SolutionClosed?.Invoke(this, new SolutionClosedEventArgs
                {
                    SolutionPath = oldSolutionPath ?? string.Empty,
                    SolutionName = oldSolutionName,
                    ClosedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _ = HandleUnhandledExceptionAsync(ex, "ApplicationStateService.OnSolutionClosed");
            }
        }

        private void DetectCurrentTheme()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                // Try to detect Visual Studio theme
                // This is a simplified implementation - in practice, you might want to use
                // more sophisticated theme detection
                _currentTheme = "Default";
            }
            catch (Exception ex)
            {
                _ = HandleUnhandledExceptionAsync(ex, "ApplicationStateService.DetectCurrentTheme");
            }
        }

        private void CheckCurrentSolution()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_dte?.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName))
                {
                    _isSolutionOpen = true;
                    _currentSolutionPath = _dte.Solution.FullName;
                }
            }
            catch (Exception ex)
            {
                _ = HandleUnhandledExceptionAsync(ex, "ApplicationStateService.CheckCurrentSolution");
            }
        }

        private string GetExtensionVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<IVsOutputWindowPane?> GetOutputWindowAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var outputWindow = await ServiceProvider.GetGlobalServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (outputWindow != null)
                {
                    var guidGeneral = Microsoft.VisualStudio.VSConstants.GUID_OutWindowGeneralPane;
                    outputWindow.GetPane(ref guidGeneral, out var pane);
                    return pane;
                }
            }
            catch
            {
                // Ignore errors getting output window
            }

            return null;
        }

        private async Task SaveErrorStateAsync(ErrorState errorState)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var errorDirectory = Path.Combine(Path.GetDirectoryName(_stateFilePath)!, "Errors");
                Directory.CreateDirectory(errorDirectory);

                var errorFileName = $"error-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
                var errorFilePath = Path.Combine(errorDirectory, errorFileName);

                var json = JsonSerializer.Serialize(errorState, _jsonOptions);
                await File.WriteAllTextAsync(errorFilePath, json);
            }
            catch
            {
                // If we can't save error state, there's not much we can do
            }
        }

        private async Task CleanupTemporaryFilesAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var tempDirectory = Path.Combine(Path.GetDirectoryName(_stateFilePath)!, "Temp");
                if (Directory.Exists(tempDirectory))
                {
                    var files = Directory.GetFiles(tempDirectory);
                    var cutoffTime = DateTime.UtcNow.AddHours(-24); // Clean files older than 24 hours

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.LastWriteTime < cutoffTime)
                            {
                                File.Delete(file);
                            }
                        }
                        catch
                        {
                            // Ignore individual file cleanup errors
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CleanupEventHandlers();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the application state for persistence
    /// </summary>
    internal class ApplicationState
    {
        public DateTime LastSavedAt { get; set; }
        public string CurrentTheme { get; set; } = string.Empty;
        public bool IsSolutionOpen { get; set; }
        public string? CurrentSolutionPath { get; set; }
        public string ExtensionVersion { get; set; } = string.Empty;
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// Represents an error state for logging
    /// </summary>
    internal class ErrorState
    {
        public DateTime Timestamp { get; set; }
        public string Context { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }
}