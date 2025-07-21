using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Service for integrating with Visual Studio's Error List
    /// </summary>
    public interface IErrorListIntegrationService
    {
        /// <summary>
        /// Adds an error to the Error List
        /// </summary>
        /// <param name="error">The error to add</param>
        Task AddErrorAsync(ErrorListItem error);

        /// <summary>
        /// Adds multiple errors to the Error List
        /// </summary>
        /// <param name="errors">The errors to add</param>
        Task AddErrorsAsync(IEnumerable<ErrorListItem> errors);

        /// <summary>
        /// Removes an error from the Error List
        /// </summary>
        /// <param name="errorId">The ID of the error to remove</param>
        Task RemoveErrorAsync(string errorId);

        /// <summary>
        /// Removes all errors from a specific source
        /// </summary>
        /// <param name="source">The source to clear errors from</param>
        Task ClearErrorsFromSourceAsync(string source);

        /// <summary>
        /// Removes all errors managed by this service
        /// </summary>
        Task ClearAllErrorsAsync();

        /// <summary>
        /// Updates an existing error in the Error List
        /// </summary>
        /// <param name="errorId">The ID of the error to update</param>
        /// <param name="updatedError">The updated error information</param>
        Task UpdateErrorAsync(string errorId, ErrorListItem updatedError);

        /// <summary>
        /// Gets all errors from a specific source
        /// </summary>
        /// <param name="source">The source to get errors from</param>
        /// <returns>Collection of errors from the source</returns>
        Task<IEnumerable<ErrorListItem>> GetErrorsFromSourceAsync(string source);

        /// <summary>
        /// Gets all errors managed by this service
        /// </summary>
        /// <returns>Collection of all managed errors</returns>
        Task<IEnumerable<ErrorListItem>> GetAllErrorsAsync();

        /// <summary>
        /// Navigates to the location of an error
        /// </summary>
        /// <param name="errorId">The ID of the error to navigate to</param>
        Task NavigateToErrorAsync(string errorId);

        /// <summary>
        /// Filters errors by severity level
        /// </summary>
        /// <param name="severity">The severity level to filter by</param>
        /// <returns>Collection of errors with the specified severity</returns>
        Task<IEnumerable<ErrorListItem>> GetErrorsBySeverityAsync(ErrorSeverity severity);

        /// <summary>
        /// Event raised when an error is added to the Error List
        /// </summary>
        event EventHandler<ErrorListItem> ErrorAdded;

        /// <summary>
        /// Event raised when an error is removed from the Error List
        /// </summary>
        event EventHandler<string> ErrorRemoved;

        /// <summary>
        /// Event raised when an error is updated in the Error List
        /// </summary>
        event EventHandler<ErrorListItem> ErrorUpdated;
    }

    /// <summary>
    /// Represents an item in the Error List
    /// </summary>
    public class ErrorListItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for this error
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the error description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the severity level
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the source of the error
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the file path where the error occurred
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the line number where the error occurred
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the column number where the error occurred
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the category of the error
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the error
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether this error can be automatically fixed
        /// </summary>
        public bool CanAutoFix { get; set; }

        /// <summary>
        /// Gets or sets the help link for this error
        /// </summary>
        public string HelpLink { get; set; }
    }

}
