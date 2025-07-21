using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Differencing;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Editor;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for visualizing code differences in the editor
    /// </summary>
    public class DiffVisualizationService : IDiffVisualizationService
    {
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<IWpfTextView, List<DiffChange>> _activeDiffChanges = new Dictionary<IWpfTextView, List<DiffChange>>();

        public event EventHandler<DiffHighlightsAppliedEventArgs> DiffHighlightsApplied;
        public event EventHandler<DiffHighlightsClearedEventArgs> DiffHighlightsCleared;

        public DiffVisualizationService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public async Task ApplyDiffChangesAsync(IWpfTextView textView, IEnumerable<DiffChange> diffChanges)
        {
            try
            {
                if (textView == null || diffChanges == null)
                {
                    await _errorHandler.LogWarningAsync("Invalid parameters for ApplyDiffChangesAsync", "DiffVisualizationService.ApplyDiffChangesAsync");
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Store the diff changes for this view
                _activeDiffChanges[textView] = diffChanges.ToList();

                // Apply the visual highlights
                DiffVisualizer.ApplyDiffChanges(textView, diffChanges);

                // Fire event
                DiffHighlightsApplied?.Invoke(this, new DiffHighlightsAppliedEventArgs
                {
                    TextView = textView,
                    DiffChanges = diffChanges
                });

                await _errorHandler.LogInfoAsync($"Applied {diffChanges.Count()} diff changes to text view", "DiffVisualizationService.ApplyDiffChangesAsync");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DiffVisualizationService.ApplyDiffChangesAsync");
            }
        }

        public async Task ApplyDiffChangesAsync(IEnumerable<DiffChange> diffChanges)
        {
            try
            {
                var activeTextView = GetActiveTextView();
                if (activeTextView == null)
                {
                    await _errorHandler.LogWarningAsync("No active text view found", "DiffVisualizationService.ApplyDiffChangesAsync");
                    return;
                }

                await ApplyDiffChangesAsync(activeTextView, diffChanges);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DiffVisualizationService.ApplyDiffChangesAsync");
            }
        }

        public async Task ClearDiffHighlightsAsync(IWpfTextView textView)
        {
            try
            {
                if (textView == null)
                {
                    await _errorHandler.LogWarningAsync("Invalid text view for ClearDiffHighlightsAsync", "DiffVisualizationService.ClearDiffHighlightsAsync");
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Clear the visual highlights
                DiffVisualizer.ClearDiffHighlights(textView);

                // Remove from active diff changes
                _activeDiffChanges.Remove(textView);

                // Fire event
                DiffHighlightsCleared?.Invoke(this, new DiffHighlightsClearedEventArgs
                {
                    TextView = textView,
                    ClearedAll = false
                });

                await _errorHandler.LogInfoAsync("Cleared diff highlights from text view", "DiffVisualizationService.ClearDiffHighlightsAsync");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DiffVisualizationService.ClearDiffHighlightsAsync");
            }
        }

        public async Task ClearAllDiffHighlightsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var textViews = _activeDiffChanges.Keys.ToList();
                foreach (var textView in textViews)
                {
                    DiffVisualizer.ClearDiffHighlights(textView);
                }

                _activeDiffChanges.Clear();

                // Fire event
                DiffHighlightsCleared?.Invoke(this, new DiffHighlightsClearedEventArgs
                {
                    TextView = null,
                    ClearedAll = true
                });

                await _errorHandler.LogInfoAsync($"Cleared diff highlights from {textViews.Count} text views", "DiffVisualizationService.ClearAllDiffHighlightsAsync");
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "DiffVisualizationService.ClearAllDiffHighlightsAsync");
            }
        }

        public IWpfTextView GetActiveTextView()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var serviceProvider = ServiceProvider.GlobalProvider;
                var textManager = serviceProvider.GetService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager)) as Microsoft.VisualStudio.TextManager.Interop.IVsTextManager;
                if (textManager == null) return null;

                textManager.GetActiveView(1, null, out Microsoft.VisualStudio.TextManager.Interop.IVsTextView activeView);
                if (activeView == null) return null;

                var userData = activeView as Microsoft.VisualStudio.Shell.Interop.IVsUserData;
                if (userData == null) return null;

                var guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out object holder);
                
                var viewHost = holder as IWpfTextViewHost;
                return viewHost?.TextView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active text view: {ex}");
                return null;
            }
        }

        public IEnumerable<IWpfTextView> GetOpenTextViews()
        {
            try
            {
                return _activeDiffChanges.Keys.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting open text views: {ex}");
                return Enumerable.Empty<IWpfTextView>();
            }
        }

        public IEnumerable<DiffChange> CreateDiffChanges(string beforeContent, string afterContent, string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(beforeContent) && string.IsNullOrEmpty(afterContent))
                {
                    return Enumerable.Empty<DiffChange>();
                }

                var diffChanges = new List<DiffChange>();

                // Split content into lines
                var beforeLines = SplitIntoLines(beforeContent ?? string.Empty);
                var afterLines = SplitIntoLines(afterContent ?? string.Empty);

                // Simple line-by-line comparison
                var maxLines = Math.Max(beforeLines.Length, afterLines.Length);
                
                for (int i = 0; i < maxLines; i++)
                {
                    var beforeLine = i < beforeLines.Length ? beforeLines[i] : null;
                    var afterLine = i < afterLines.Length ? afterLines[i] : null;

                    if (beforeLine == null && afterLine != null)
                    {
                        // Line was added
                        diffChanges.Add(new DiffChange
                        {
                            Type = ChangeType.Added,
                            StartLine = i + 1,
                            EndLine = i + 1,
                            NewContent = afterLine,
                            FilePath = filePath
                        });
                    }
                    else if (beforeLine != null && afterLine == null)
                    {
                        // Line was deleted
                        diffChanges.Add(new DiffChange
                        {
                            Type = ChangeType.Deleted,
                            StartLine = i + 1,
                            EndLine = i + 1,
                            OriginalContent = beforeLine,
                            FilePath = filePath
                        });
                    }
                    else if (beforeLine != null && afterLine != null && beforeLine != afterLine)
                    {
                        // Line was modified
                        diffChanges.Add(new DiffChange
                        {
                            Type = ChangeType.Modified,
                            StartLine = i + 1,
                            EndLine = i + 1,
                            OriginalContent = beforeLine,
                            NewContent = afterLine,
                            FilePath = filePath
                        });
                    }
                }

                return diffChanges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating diff changes: {ex}");
                return Enumerable.Empty<DiffChange>();
            }
        }

        private string[] SplitIntoLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new string[0];

            return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Gets the diff changes currently applied to a text view
        /// </summary>
        /// <param name="textView">The text view</param>
        /// <returns>Collection of diff changes or empty collection if none</returns>
        public IEnumerable<DiffChange> GetActiveDiffChanges(IWpfTextView textView)
        {
            try
            {
                if (textView != null && _activeDiffChanges.TryGetValue(textView, out var changes))
                {
                    return changes;
                }
                return Enumerable.Empty<DiffChange>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active diff changes: {ex}");
                return Enumerable.Empty<DiffChange>();
            }
        }

        /// <summary>
        /// Checks if a text view has active diff highlights
        /// </summary>
        /// <param name="textView">The text view</param>
        /// <returns>True if the text view has active diff highlights</returns>
        public bool HasActiveDiffHighlights(IWpfTextView textView)
        {
            try
            {
                return textView != null && _activeDiffChanges.ContainsKey(textView);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking active diff highlights: {ex}");
                return false;
            }
        }
    }
}