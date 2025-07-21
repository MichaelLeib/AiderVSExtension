using System;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing application state and lifecycle events
    /// </summary>
    public interface IApplicationStateService
    {
        /// <summary>
        /// Event fired when the extension is initializing
        /// </summary>
        event EventHandler<ExtensionInitializingEventArgs> ExtensionInitializing;

        /// <summary>
        /// Event fired when the extension has been initialized
        /// </summary>
        event EventHandler<ExtensionInitializedEventArgs> ExtensionInitialized;

        /// <summary>
        /// Event fired when the extension is shutting down
        /// </summary>
        event EventHandler<ExtensionShuttingDownEventArgs> ExtensionShuttingDown;

        /// <summary>
        /// Event fired when Visual Studio theme changes
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Event fired when solution is opened
        /// </summary>
        event EventHandler<SolutionOpenedEventArgs> SolutionOpened;

        /// <summary>
        /// Event fired when solution is closed
        /// </summary>
        event EventHandler<SolutionClosedEventArgs> SolutionClosed;

        /// <summary>
        /// Gets whether the extension is currently initializing
        /// </summary>
        bool IsInitializing { get; }

        /// <summary>
        /// Gets whether the extension has been initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether the extension is shutting down
        /// </summary>
        bool IsShuttingDown { get; }

        /// <summary>
        /// Gets the current Visual Studio theme
        /// </summary>
        string CurrentTheme { get; }

        /// <summary>
        /// Gets whether a solution is currently open
        /// </summary>
        bool IsSolutionOpen { get; }

        /// <summary>
        /// Gets the path of the currently open solution
        /// </summary>
        string? CurrentSolutionPath { get; }

        /// <summary>
        /// Initializes the application state service
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the application state service and performs cleanup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ShutdownAsync();

        /// <summary>
        /// Saves the current application state
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SaveStateAsync();

        /// <summary>
        /// Restores the application state from storage
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task RestoreStateAsync();

        /// <summary>
        /// Handles an unhandled exception in the extension
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="context">Context information about where the exception occurred</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleUnhandledExceptionAsync(Exception exception, string context);

        /// <summary>
        /// Performs graceful recovery from an error state
        /// </summary>
        /// <param name="errorContext">Context about the error</param>
        /// <returns>Task representing the async operation</returns>
        Task RecoverFromErrorAsync(string errorContext);

        /// <summary>
        /// Gets the current memory usage of the extension
        /// </summary>
        /// <returns>Memory usage in bytes</returns>
        long GetMemoryUsage();

        /// <summary>
        /// Performs garbage collection and cleanup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task PerformCleanupAsync();
    }

    /// <summary>
    /// Event arguments for extension initializing events
    /// </summary>
    public class ExtensionInitializingEventArgs : EventArgs
    {
        public DateTime StartTime { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for extension initialized events
    /// </summary>
    public class ExtensionInitializedEventArgs : EventArgs
    {
        public DateTime CompletedTime { get; set; }
        public TimeSpan InitializationDuration { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event arguments for extension shutting down events
    /// </summary>
    public class ExtensionShuttingDownEventArgs : EventArgs
    {
        public DateTime ShutdownTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsGraceful { get; set; }
    }


    /// <summary>
    /// Event arguments for solution opened events
    /// </summary>
    public class SolutionOpenedEventArgs : EventArgs
    {
        public string SolutionPath { get; set; } = string.Empty;
        public string SolutionName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for solution closed events
    /// </summary>
    public class SolutionClosedEventArgs : EventArgs
    {
        public string SolutionPath { get; set; } = string.Empty;
        public string SolutionName { get; set; } = string.Empty;
        public DateTime ClosedAt { get; set; }
    }
}