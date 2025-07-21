using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Service for visualizing code differences in the editor
    /// </summary>
    public interface IDiffVisualizationService
    {
        /// <summary>
        /// Applies diff changes to the specified text view
        /// </summary>
        /// <param name="textView">The text view to apply changes to</param>
        /// <param name="diffChanges">The diff changes to visualize</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyDiffChangesAsync(IWpfTextView textView, IEnumerable<DiffChange> diffChanges);

        /// <summary>
        /// Applies diff changes to the active text view
        /// </summary>
        /// <param name="diffChanges">The diff changes to visualize</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyDiffChangesAsync(IEnumerable<DiffChange> diffChanges);

        /// <summary>
        /// Clears all diff highlights from the specified text view
        /// </summary>
        /// <param name="textView">The text view to clear highlights from</param>
        /// <returns>Task representing the async operation</returns>
        Task ClearDiffHighlightsAsync(IWpfTextView textView);

        /// <summary>
        /// Clears all diff highlights from all text views
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ClearAllDiffHighlightsAsync();

        /// <summary>
        /// Gets the active text view
        /// </summary>
        /// <returns>The active text view or null if none is active</returns>
        IWpfTextView GetActiveTextView();

        /// <summary>
        /// Gets all open text views
        /// </summary>
        /// <returns>Collection of open text views</returns>
        IEnumerable<IWpfTextView> GetOpenTextViews();

        /// <summary>
        /// Creates diff changes from before and after text content
        /// </summary>
        /// <param name="beforeContent">The original content</param>
        /// <param name="afterContent">The modified content</param>
        /// <param name="filePath">The file path for context</param>
        /// <returns>Collection of diff changes</returns>
        IEnumerable<DiffChange> CreateDiffChanges(string beforeContent, string afterContent, string filePath = null);

        /// <summary>
        /// Event fired when diff highlights are applied
        /// </summary>
        event System.EventHandler<DiffHighlightsAppliedEventArgs> DiffHighlightsApplied;

        /// <summary>
        /// Event fired when diff highlights are cleared
        /// </summary>
        event System.EventHandler<DiffHighlightsClearedEventArgs> DiffHighlightsCleared;
    }

    /// <summary>
    /// Event arguments for diff highlights applied event
    /// </summary>
    public class DiffHighlightsAppliedEventArgs : System.EventArgs
    {
        public IWpfTextView TextView { get; set; }
        public IEnumerable<DiffChange> DiffChanges { get; set; }
    }

    /// <summary>
    /// Event arguments for diff highlights cleared event
    /// </summary>
    public class DiffHighlightsClearedEventArgs : System.EventArgs
    {
        public IWpfTextView TextView { get; set; }
        public bool ClearedAll { get; set; }
    }
}