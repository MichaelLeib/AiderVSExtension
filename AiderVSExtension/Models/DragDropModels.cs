using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Drag-and-drop options
    /// </summary>
    public class DragDropOptions
    {
        /// <summary>
        /// Whether to accept file drops
        /// </summary>
        public bool AcceptFiles { get; set; } = true;

        /// <summary>
        /// Whether to accept text drops
        /// </summary>
        public bool AcceptText { get; set; } = true;

        /// <summary>
        /// Whether to accept Visual Studio item drops
        /// </summary>
        public bool AcceptVSItems { get; set; } = true;

        /// <summary>
        /// Whether to allow dragging from this element
        /// </summary>
        public bool AllowDrag { get; set; } = false;

        /// <summary>
        /// Maximum number of files to accept
        /// </summary>
        public int MaxFiles { get; set; } = 100;

        /// <summary>
        /// Maximum file size in bytes
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// File extensions to accept (null for all)
        /// </summary>
        public HashSet<string> AcceptedExtensions { get; set; }

        /// <summary>
        /// Whether to show visual feedback during drag
        /// </summary>
        public bool ShowVisualFeedback { get; set; } = true;

        /// <summary>
        /// Visual feedback style
        /// </summary>
        public DragDropFeedbackStyle FeedbackStyle { get; set; } = DragDropFeedbackStyle.Highlight;

        /// <summary>
        /// Custom feedback template
        /// </summary>
        public DataTemplate CustomFeedbackTemplate { get; set; }

        /// <summary>
        /// Whether to validate files before processing
        /// </summary>
        public bool ValidateFiles { get; set; } = true;

        /// <summary>
        /// Whether to process directories recursively
        /// </summary>
        public bool ProcessDirectories { get; set; } = true;

        /// <summary>
        /// Custom validation function
        /// </summary>
        public Func<string, bool> CustomFileValidator { get; set; }
    }

    /// <summary>
    /// Drag-and-drop event arguments
    /// </summary>
    public class DragDropEventArgs : EventArgs
    {
        /// <summary>
        /// Target element
        /// </summary>
        public FrameworkElement Element { get; set; }

        /// <summary>
        /// Drag data
        /// </summary>
        public IDataObject Data { get; set; }

        /// <summary>
        /// Drag effects
        /// </summary>
        public DragDropEffects Effects { get; set; }

        /// <summary>
        /// Drop point relative to element
        /// </summary>
        public Point DropPoint { get; set; }

        /// <summary>
        /// Dropped files
        /// </summary>
        public IEnumerable<FileReference> Files { get; set; }

        /// <summary>
        /// Dropped text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the event was handled
        /// </summary>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Drag-and-drop result
    /// </summary>
    public class DragDropResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Processed files
        /// </summary>
        public IEnumerable<FileReference> ProcessedFiles { get; set; } = new List<FileReference>();

        /// <summary>
        /// Invalid files that couldn't be processed
        /// </summary>
        public IEnumerable<string> InvalidFiles { get; set; } = new List<string>();

        /// <summary>
        /// Processed text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Processing duration
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Visual feedback styles for drag-and-drop
    /// </summary>
    public enum DragDropFeedbackStyle
    {
        /// <summary>
        /// No visual feedback
        /// </summary>
        None,

        /// <summary>
        /// Highlight the drop target
        /// </summary>
        Highlight,

        /// <summary>
        /// Show border around drop target
        /// </summary>
        Border,

        /// <summary>
        /// Show overlay on drop target
        /// </summary>
        Overlay,

        /// <summary>
        /// Custom feedback template
        /// </summary>
        Custom
    }

    /// <summary>
    /// Drag-and-drop validation result
    /// </summary>
    public class DragDropValidationResult
    {
        /// <summary>
        /// Whether the data is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Validation severity
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// Suggested actions
        /// </summary>
        public IEnumerable<string> SuggestedActions { get; set; } = new List<string>();
    }


    /// <summary>
    /// File drop preview information
    /// </summary>
    public class FileDropPreview
    {
        /// <summary>
        /// File path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Whether the file is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// File type icon
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Preview text (for text files)
        /// </summary>
        public string PreviewText { get; set; }

        /// <summary>
        /// Validation message
        /// </summary>
        public string ValidationMessage { get; set; }
    }
}