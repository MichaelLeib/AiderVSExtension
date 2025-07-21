using System;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for integrating with Visual Studio's output window
    /// </summary>
    public interface IOutputWindowService
    {
        /// <summary>
        /// Event fired when an error in the output window is clicked for adding to chat
        /// </summary>
        event EventHandler<OutputWindowErrorEventArgs> ErrorAddToChatRequested;

        /// <summary>
        /// Initializes the output window integration
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Writes a message to the Aider output pane
        /// </summary>
        /// <param name="message">The message to write</param>
        Task WriteToAiderPaneAsync(string message);

        /// <summary>
        /// Writes an error message to the Aider output pane with "Add to Chat" functionality
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="source">The source of the error (e.g., "Build", "Debug")</param>
        Task WriteErrorWithChatOptionAsync(string errorMessage, string source = null);

        /// <summary>
        /// Clears the Aider output pane
        /// </summary>
        Task ClearAiderPaneAsync();

        /// <summary>
        /// Shows the Aider output pane
        /// </summary>
        Task ShowAiderPaneAsync();

        /// <summary>
        /// Activates the output window and brings it to focus
        /// </summary>
        Task ActivateOutputWindowAsync();

        /// <summary>
        /// Registers for output window events to detect errors
        /// </summary>
        Task RegisterForOutputEventsAsync();

        /// <summary>
        /// Unregisters from output window events
        /// </summary>
        Task UnregisterFromOutputEventsAsync();
    }

    /// <summary>
    /// Event arguments for output window error events
    /// </summary>
    public class OutputWindowErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The error message text
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The source of the error (Build, Debug, etc.)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// File path associated with the error, if available
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Line number associated with the error, if available
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number associated with the error, if available
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Error code, if available
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Project name associated with the error, if available
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Severity level of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    }

}