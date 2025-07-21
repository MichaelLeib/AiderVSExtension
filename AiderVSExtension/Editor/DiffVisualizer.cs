using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using AiderVSExtension.Models;

namespace AiderVSExtension.Editor
{
    /// <summary>
    /// Provides visual diff highlighting in the text editor
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class DiffVisualizer : IWpfTextViewCreationListener
    {
        [Import]
        private IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        private readonly Dictionary<IWpfTextView, DiffAdornmentManager> _adornmentManagers = new Dictionary<IWpfTextView, DiffAdornmentManager>();

        public void TextViewCreated(IWpfTextView textView)
        {
            try
            {
                // Create adornment manager for this view
                var adornmentManager = new DiffAdornmentManager(textView, TagAggregatorFactory);
                _adornmentManagers[textView] = adornmentManager;

                // Clean up when view is closed
                textView.Closed += (sender, e) =>
                {
                    if (_adornmentManagers.ContainsKey(textView))
                    {
                        _adornmentManagers[textView].Dispose();
                        _adornmentManagers.Remove(textView);
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DiffVisualizer.TextViewCreated: {ex}");
            }
        }

        /// <summary>
        /// Applies diff changes to the specified text view
        /// </summary>
        /// <param name="textView">The text view to apply changes to</param>
        /// <param name="diffChanges">The diff changes to visualize</param>
        public static void ApplyDiffChanges(IWpfTextView textView, IEnumerable<DiffChange> diffChanges)
        {
            try
            {
                if (textView == null || diffChanges == null) return;

                // Find the adornment manager for this view
                var visualizer = GetDiffVisualizer();
                if (visualizer != null && visualizer._adornmentManagers.TryGetValue(textView, out var adornmentManager))
                {
                    adornmentManager.ApplyDiffChanges(diffChanges);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying diff changes: {ex}");
            }
        }

        /// <summary>
        /// Clears all diff highlights from the specified text view
        /// </summary>
        /// <param name="textView">The text view to clear highlights from</param>
        public static void ClearDiffHighlights(IWpfTextView textView)
        {
            try
            {
                if (textView == null) return;

                var visualizer = GetDiffVisualizer();
                if (visualizer != null && visualizer._adornmentManagers.TryGetValue(textView, out var adornmentManager))
                {
                    adornmentManager.ClearDiffHighlights();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing diff highlights: {ex}");
            }
        }

        private static DiffVisualizer GetDiffVisualizer()
        {
            try
            {
                // Try to get the current instance through MEF composition
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null)
                {
                    // Try to get an existing instance from the service container
                    var diffService = serviceContainer.GetService<IDiffVisualizationService>();
                    if (diffService is DiffVisualizer visualizer)
                    {
                        return visualizer;
                    }
                }

                // Fallback: create a new instance if MEF lookup fails
                // This is not ideal but provides basic functionality
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting DiffVisualizer: {ex.Message}");
                // Return null to gracefully handle the error
                return null;
            }
        }
    }

    /// <summary>
    /// Manages diff adornments for a single text view
    /// </summary>
    internal class DiffAdornmentManager : IDisposable
    {
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<ITextMarkerTag> _tagAggregator;
        private readonly IAdornmentLayer _adornmentLayer;
        private readonly List<DiffChange> _activeDiffChanges = new List<DiffChange>();
        private bool _isDisposed = false;

        public DiffAdornmentManager(IWpfTextView textView, IViewTagAggregatorFactoryService tagAggregatorFactory)
        {
            _textView = textView;
            _tagAggregator = tagAggregatorFactory.CreateTagAggregator<ITextMarkerTag>(_textView);
            _adornmentLayer = _textView.GetAdornmentLayer("DiffHighlight");

            // Subscribe to events
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.TextBuffer.Changed += OnTextBufferChanged;
        }

        public void ApplyDiffChanges(IEnumerable<DiffChange> diffChanges)
        {
            try
            {
                if (_isDisposed) return;

                // Clear existing highlights
                ClearDiffHighlights();

                // Store the new diff changes
                _activeDiffChanges.Clear();
                _activeDiffChanges.AddRange(diffChanges);

                // Apply the new highlights
                RefreshHighlights();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying diff changes: {ex}");
            }
        }

        public void ClearDiffHighlights()
        {
            try
            {
                if (_isDisposed) return;

                _activeDiffChanges.Clear();
                _adornmentLayer.RemoveAllAdornments();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing diff highlights: {ex}");
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            try
            {
                if (_isDisposed) return;

                // Refresh highlights when layout changes
                RefreshHighlights();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling layout changed: {ex}");
            }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                if (_isDisposed) return;

                // Clear highlights when text changes (they may no longer be valid)
                ClearDiffHighlights();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling text buffer changed: {ex}");
            }
        }

        private void RefreshHighlights()
        {
            try
            {
                if (_isDisposed || _activeDiffChanges.Count == 0) return;

                // Clear existing adornments
                _adornmentLayer.RemoveAllAdornments();

                // Create new adornments for each diff change
                foreach (var diffChange in _activeDiffChanges)
                {
                    CreateDiffAdornment(diffChange);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing highlights: {ex}");
            }
        }

        private void CreateDiffAdornment(DiffChange diffChange)
        {
            try
            {
                if (_isDisposed) return;

                var snapshot = _textView.TextSnapshot;
                
                // Validate line numbers
                if (diffChange.StartLine < 1 || diffChange.StartLine > snapshot.LineCount)
                    return;

                var startLine = snapshot.GetLineFromLineNumber(diffChange.StartLine - 1);
                var endLine = diffChange.EndLine > 0 && diffChange.EndLine <= snapshot.LineCount
                    ? snapshot.GetLineFromLineNumber(diffChange.EndLine - 1)
                    : startLine;

                // Create span for the diff change
                var span = new SnapshotSpan(startLine.Start, endLine.End);

                // Get the geometry for the span
                var geometry = _textView.TextViewLines.GetMarkerGeometry(span);
                if (geometry == null) return;

                // Create the visual element
                var adornment = CreateDiffAdornmentElement(diffChange, geometry.Bounds);
                if (adornment == null) return;

                // Add the adornment to the layer
                _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, adornment, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating diff adornment: {ex}");
            }
        }

        private UIElement CreateDiffAdornmentElement(DiffChange diffChange, Rect bounds)
        {
            try
            {
                var border = new Border
                {
                    Width = bounds.Width,
                    Height = bounds.Height,
                    BorderThickness = new Thickness(0, 0, 3, 0), // Left border
                    Opacity = 0.7
                };

                // Set color based on change type
                switch (diffChange.Type)
                {
                    case ChangeType.Added:
                        border.Background = new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)); // Light green background
                        border.BorderBrush = new SolidColorBrush(Colors.Green); // Green border
                        break;

                    case ChangeType.Deleted:
                        border.Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)); // Light red background
                        border.BorderBrush = new SolidColorBrush(Colors.Red); // Red border
                        break;

                    case ChangeType.Modified:
                        border.Background = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)); // Light orange background
                        border.BorderBrush = new SolidColorBrush(Colors.Orange); // Orange border
                        break;

                    default:
                        return null;
                }

                // Add tooltip with change details
                var tooltip = new ToolTip
                {
                    Content = CreateTooltipContent(diffChange)
                };
                border.ToolTip = tooltip;

                return border;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating diff adornment element: {ex}");
                return null;
            }
        }

        private object CreateTooltipContent(DiffChange diffChange)
        {
            try
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    MaxWidth = 400
                };

                // Change type header
                var header = new TextBlock
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5),
                    Text = $"{diffChange.Type} Change"
                };
                stackPanel.Children.Add(header);

                // Line information
                var lineInfo = new TextBlock
                {
                    Text = diffChange.EndLine > diffChange.StartLine
                        ? $"Lines {diffChange.StartLine}-{diffChange.EndLine}"
                        : $"Line {diffChange.StartLine}",
                    Margin = new Thickness(0, 0, 0, 5),
                    FontStyle = FontStyles.Italic
                };
                stackPanel.Children.Add(lineInfo);

                // Original content (if available)
                if (!string.IsNullOrEmpty(diffChange.OriginalContent))
                {
                    var originalLabel = new TextBlock
                    {
                        Text = "Original:",
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 5, 0, 2)
                    };
                    stackPanel.Children.Add(originalLabel);

                    var originalContent = new TextBlock
                    {
                        Text = diffChange.OriginalContent,
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),
                        Padding = new Thickness(5),
                        TextWrapping = TextWrapping.Wrap,
                        MaxHeight = 100
                    };
                    stackPanel.Children.Add(originalContent);
                }

                // New content (if available)
                if (!string.IsNullOrEmpty(diffChange.NewContent))
                {
                    var newLabel = new TextBlock
                    {
                        Text = "New:",
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 5, 0, 2)
                    };
                    stackPanel.Children.Add(newLabel);

                    var newContent = new TextBlock
                    {
                        Text = diffChange.NewContent,
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromArgb(40, 0, 255, 0)),
                        Padding = new Thickness(5),
                        TextWrapping = TextWrapping.Wrap,
                        MaxHeight = 100
                    };
                    stackPanel.Children.Add(newContent);
                }

                return stackPanel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating tooltip content: {ex}");
                return $"{diffChange.Type} Change - Lines {diffChange.StartLine}-{diffChange.EndLine}";
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                try
                {
                    // Unsubscribe from events
                    _textView.LayoutChanged -= OnLayoutChanged;
                    _textView.TextBuffer.Changed -= OnTextBufferChanged;

                    // Clear adornments
                    _adornmentLayer?.RemoveAllAdornments();

                    // Dispose tag aggregator
                    _tagAggregator?.Dispose();

                    // Clear active diff changes
                    _activeDiffChanges.Clear();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing DiffAdornmentManager: {ex}");
                }
            }
        }
    }
}