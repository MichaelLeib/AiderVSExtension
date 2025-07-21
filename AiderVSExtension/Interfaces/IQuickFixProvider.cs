using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for providing quick fixes for errors and warnings
    /// </summary>
    public interface IQuickFixProvider
    {
        /// <summary>
        /// Gets available quick fixes for the specified error context
        /// </summary>
        /// <param name="context">The error context to provide fixes for</param>
        /// <returns>Collection of available quick fix actions</returns>
        Task<IEnumerable<QuickFixAction>> GetQuickFixesAsync(ErrorContext context);

        /// <summary>
        /// Determines if the provider can provide quick fixes for the specified error type
        /// </summary>
        /// <param name="errorType">The type of error to check</param>
        /// <returns>True if quick fixes can be provided, false otherwise</returns>
        bool CanProvideQuickFix(ErrorType errorType);

        /// <summary>
        /// Executes the specified quick fix action
        /// </summary>
        /// <param name="action">The quick fix action to execute</param>
        /// <returns>True if the fix was applied successfully, false otherwise</returns>
        Task<bool> ExecuteQuickFixAsync(QuickFixAction action);

        /// <summary>
        /// Gets the priority of this provider (higher numbers indicate higher priority)
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// Represents an error context for quick fix providers
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The error or warning message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The type of error
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// The file path where the error occurred
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The line number where the error occurred
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The column number where the error occurred
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// The error code (if available)
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// The project containing the error
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The text span containing the error
        /// </summary>
        public string ErrorSpan { get; set; }

        /// <summary>
        /// Additional context information
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; }

        public ErrorContext()
        {
            AdditionalContext = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Represents a quick fix action
    /// </summary>
    public class QuickFixAction
    {
        /// <summary>
        /// The display name of the quick fix
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A description of what the quick fix does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The type of quick fix action
        /// </summary>
        public QuickFixActionType ActionType { get; set; }

        /// <summary>
        /// The priority of this quick fix (higher numbers indicate higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The error context this quick fix applies to
        /// </summary>
        public ErrorContext Context { get; set; }

        /// <summary>
        /// Custom data for the quick fix implementation
        /// </summary>
        public Dictionary<string, object> ActionData { get; set; }

        /// <summary>
        /// Indicates if this quick fix will modify files
        /// </summary>
        public bool WillModifyFiles { get; set; }

        /// <summary>
        /// The files that will be modified by this quick fix
        /// </summary>
        public List<string> FilesToModify { get; set; }

        public QuickFixAction()
        {
            ActionData = new Dictionary<string, object>();
            FilesToModify = new List<string>();
        }
    }

    /// <summary>
    /// Types of errors that can be handled by quick fix providers
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Compilation error
        /// </summary>
        CompilationError,

        /// <summary>
        /// Runtime error
        /// </summary>
        RuntimeError,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning,

        /// <summary>
        /// Code analysis warning
        /// </summary>
        CodeAnalysisWarning,

        /// <summary>
        /// Build error
        /// </summary>
        BuildError,

        /// <summary>
        /// Syntax error
        /// </summary>
        SyntaxError,

        /// <summary>
        /// Type error
        /// </summary>
        TypeError,

        /// <summary>
        /// Reference error
        /// </summary>
        ReferenceError,

        /// <summary>
        /// Unknown error type
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Types of quick fix actions
    /// </summary>
    public enum QuickFixActionType
    {
        /// <summary>
        /// Fix with AI assistance
        /// </summary>
        FixWithAI,

        /// <summary>
        /// Add to Aider chat for discussion
        /// </summary>
        AddToChat,

        /// <summary>
        /// Apply automatic fix
        /// </summary>
        AutomaticFix,

        /// <summary>
        /// Provide suggestion
        /// </summary>
        Suggestion,

        /// <summary>
        /// Refactor code
        /// </summary>
        Refactor,

        /// <summary>
        /// Add missing using/import
        /// </summary>
        AddUsing,

        /// <summary>
        /// Create missing member
        /// </summary>
        CreateMember,

        /// <summary>
        /// Custom action
        /// </summary>
        Custom
    }
}