using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for drag-and-drop operations
    /// </summary>
    public interface IDragDropService
    {
        /// <summary>
        /// Event fired when files are dropped
        /// </summary>
        event EventHandler<DragDropEventArgs> FilesDropped;

        /// <summary>
        /// Event fired when text is dropped
        /// </summary>
        event EventHandler<DragDropEventArgs> TextDropped;

        /// <summary>
        /// Event fired when drag enters an element
        /// </summary>
        event EventHandler<DragDropEventArgs> DragEnter;

        /// <summary>
        /// Event fired when drag leaves an element
        /// </summary>
        event EventHandler<DragDropEventArgs> DragLeave;

        /// <summary>
        /// Enables drag-and-drop on a UI element
        /// </summary>
        /// <param name="element">Element to enable drag-drop on</param>
        /// <param name="options">Drag-drop options</param>
        void EnableDragDrop(FrameworkElement element, DragDropOptions options = null);

        /// <summary>
        /// Disables drag-and-drop on a UI element
        /// </summary>
        /// <param name="element">Element to disable drag-drop on</param>
        void DisableDragDrop(FrameworkElement element);

        /// <summary>
        /// Processes dropped files
        /// </summary>
        /// <param name="filePaths">File paths that were dropped</param>
        /// <param name="element">Target element</param>
        /// <returns>Processing result</returns>
        Task<DragDropResult> ProcessDroppedFilesAsync(IEnumerable<string> filePaths, FrameworkElement element);

        /// <summary>
        /// Processes dropped text
        /// </summary>
        /// <param name="text">Text that was dropped</param>
        /// <param name="element">Target element</param>
        /// <returns>Processing result</returns>
        Task<DragDropResult> ProcessDroppedTextAsync(string text, FrameworkElement element);

        /// <summary>
        /// Gets drag effects for data
        /// </summary>
        /// <param name="data">Drag data</param>
        /// <param name="options">Drag-drop options</param>
        /// <returns>Drag effects</returns>
        DragDropEffects GetDragEffects(IDataObject data, DragDropOptions options);

        /// <summary>
        /// Checks if a file type is valid for processing
        /// </summary>
        /// <param name="filePath">File path to check</param>
        /// <returns>True if valid</returns>
        bool IsValidFileType(string filePath);

        /// <summary>
        /// Creates drag data for an element
        /// </summary>
        /// <param name="element">Element to create drag data for</param>
        /// <returns>Drag data</returns>
        DataObject CreateDragData(FrameworkElement element);
    }
}