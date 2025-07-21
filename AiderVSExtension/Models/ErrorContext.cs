using System;
using System.Collections.Generic;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the context of an error for quick fix operations
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error code or identifier
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// File path where the error occurred
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Line number where the error occurred
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number where the error occurred
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Error severity level
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Type of error
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// Additional context information
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Source of the error (compiler, analyzer, etc.)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// When the error was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of errors that can be handled
    /// </summary>
    public enum ErrorType
    {
        CompileError,
        Warning,
        SyntaxError,
        SemanticError,
        RuntimeError,
        ConfigurationError,
        NetworkError,
        FileSystemError,
        ValidationError,
        PerformanceIssue,
        SecurityIssue,
        AccessibilityIssue
    }
}