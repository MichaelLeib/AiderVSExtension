using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for visualizing Git diffs in the editor
    /// </summary>
    public class GitDiffVisualizationService : IGitDiffVisualizationService
    {
        private readonly IGitService _gitService;
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<string, List<GitDiffGutter>> _gutterDecorations;
        private readonly object _lockObject = new object();

        public GitDiffVisualizationService(IGitService gitService, IErrorHandler errorHandler)
        {
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _gutterDecorations = new Dictionary<string, List<GitDiffGutter>>();
        }

        /// <summary>
        /// Visualizes Git diff in the editor
        /// </summary>
        public async Task VisualizeGitDiffAsync(ITextView textView, string filePath)
        {
            try
            {
                if (textView == null || string.IsNullOrEmpty(filePath))
                    return;

                // Get Git diff for the file
                var diff = await _gitService.GetFileDiffAsync(filePath);
                if (diff?.FileChanges?.Any() != true)
                    return;

                var fileDiff = diff.FileChanges.First();
                await VisualizeFileDiffAsync(textView, fileDiff);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitDiffVisualizationService.VisualizeGitDiffAsync");
            }
        }

        /// <summary>
        /// Visualizes a file diff in the editor
        /// </summary>
        private async Task VisualizeFileDiffAsync(ITextView textView, GitFileDiff fileDiff)
        {
            try
            {
                ClearGutterDecorations(textView);

                var gutterDecorations = new List<GitDiffGutter>();
                
                foreach (var hunk in fileDiff.Hunks)
                {
                    var currentNewLine = hunk.NewStartLine;
                    
                    foreach (var line in hunk.Lines)
                    {
                        var decoration = CreateGutterDecoration(line, currentNewLine);
                        if (decoration != null)
                        {
                            gutterDecorations.Add(decoration);
                        }

                        if (line.LineType == GitDiffLineType.Added || line.LineType == GitDiffLineType.Context)
                        {
                            currentNewLine++;
                        }
                    }
                }

                lock (_lockObject)
                {
                    _gutterDecorations[textView.TextBuffer.ContentType.TypeName] = gutterDecorations;
                }

                await ApplyGutterDecorationsAsync(textView, gutterDecorations);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitDiffVisualizationService.VisualizeFileDiffAsync");
            }
        }

        /// <summary>
        /// Creates a gutter decoration for a diff line
        /// </summary>
        private GitDiffGutter CreateGutterDecoration(GitDiffLine line, int lineNumber)
        {
            if (line.LineType == GitDiffLineType.Context)
                return null; // Don't decorate context lines

            var decoration = new GitDiffGutter
            {
                LineNumber = lineNumber,
                LineType = line.LineType,
                Content = line.Content
            };

            switch (line.LineType)
            {
                case GitDiffLineType.Added:
                    decoration.BackgroundColor = Colors.LightGreen;
                    decoration.ForegroundColor = Colors.DarkGreen;
                    decoration.Symbol = "+";
                    break;
                case GitDiffLineType.Deleted:
                    decoration.BackgroundColor = Colors.LightPink;
                    decoration.ForegroundColor = Colors.DarkRed;
                    decoration.Symbol = "-";
                    break;
                case GitDiffLineType.NoNewline:
                    decoration.BackgroundColor = Colors.LightYellow;
                    decoration.ForegroundColor = Colors.DarkOrange;
                    decoration.Symbol = "\\";
                    break;
            }

            return decoration;
        }

        /// <summary>
        /// Applies gutter decorations to the editor
        /// </summary>
        private async Task ApplyGutterDecorationsAsync(ITextView textView, List<GitDiffGutter> decorations)
        {
            await Task.Run(() =>
            {
                try
                {
                    // This would integrate with Visual Studio's gutter services
                    // For now, we'll store the decorations for later use
                    foreach (var decoration in decorations)
                    {
                        // Apply decoration to the specified line
                        // This would use VS editor services to add gutter indicators
                        ApplyGutterDecoration(textView, decoration);
                    }
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitDiffVisualizationService.ApplyGutterDecorationsAsync");
                }
            });
        }

        /// <summary>
        /// Applies a single gutter decoration
        /// </summary>
        private void ApplyGutterDecoration(ITextView textView, GitDiffGutter decoration)
        {
            try
            {
                var textBuffer = textView.TextBuffer;
                var snapshot = textBuffer.CurrentSnapshot;
                
                if (decoration.LineNumber <= 0 || decoration.LineNumber > snapshot.LineCount)
                    return;

                var line = snapshot.GetLineFromLineNumber(decoration.LineNumber - 1);
                var span = new SnapshotSpan(line.Start, line.Length);
                
                // This would create actual Visual Studio gutter indicators
                // For now, we'll just track the decoration
                decoration.Span = span;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "GitDiffVisualizationService.ApplyGutterDecoration");
            }
        }

        /// <summary>
        /// Clears all gutter decorations for the text view
        /// </summary>
        private void ClearGutterDecorations(ITextView textView)
        {
            try
            {
                lock (_lockObject)
                {
                    var key = textView.TextBuffer.ContentType.TypeName;
                    if (_gutterDecorations.ContainsKey(key))
                    {
                        _gutterDecorations[key].Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "GitDiffVisualizationService.ClearGutterDecorations");
            }
        }

        /// <summary>
        /// Gets Git diff information for a specific line
        /// </summary>
        public async Task<GitDiffLineInfo> GetDiffInfoForLineAsync(ITextView textView, int lineNumber)
        {
            try
            {
                lock (_lockObject)
                {
                    var key = textView.TextBuffer.ContentType.TypeName;
                    if (_gutterDecorations.ContainsKey(key))
                    {
                        var decoration = _gutterDecorations[key]
                            .FirstOrDefault(d => d.LineNumber == lineNumber);
                        
                        if (decoration != null)
                        {
                            return new GitDiffLineInfo
                            {
                                LineNumber = lineNumber,
                                LineType = decoration.LineType,
                                Content = decoration.Content,
                                Symbol = decoration.Symbol
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitDiffVisualizationService.GetDiffInfoForLineAsync");
            }

            return null;
        }

        /// <summary>
        /// Shows Git blame information for a line
        /// </summary>
        public async Task ShowBlameAsync(ITextView textView, string filePath, int lineNumber)
        {
            try
            {
                var blame = await _gitService.GetBlameAsync(filePath);
                if (blame?.Hunks?.Any() != true)
                    return;

                var hunk = blame.Hunks.FirstOrDefault(h => 
                    lineNumber >= h.StartLine && lineNumber <= h.EndLine);

                if (hunk != null)
                {
                    var blameInfo = new GitBlameInfo
                    {
                        LineNumber = lineNumber,
                        CommitHash = hunk.CommitHash,
                        AuthorName = hunk.AuthorName,
                        AuthorEmail = hunk.AuthorEmail,
                        CommitDate = hunk.CommitDate
                    };

                    // Show blame information (this would integrate with VS UI)
                    await ShowBlameInfoAsync(textView, blameInfo);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitDiffVisualizationService.ShowBlameAsync");
            }
        }

        /// <summary>
        /// Shows blame information in the UI
        /// </summary>
        private async Task ShowBlameInfoAsync(ITextView textView, GitBlameInfo blameInfo)
        {
            await Task.Run(() =>
            {
                try
                {
                    // This would show blame info in a tooltip or status bar
                    // For now, we'll just log it
                    var message = $"Line {blameInfo.LineNumber}: {blameInfo.AuthorName} ({blameInfo.CommitHash.Substring(0, 7)}) - {blameInfo.CommitDate:yyyy-MM-dd}";
                    _errorHandler?.LogInfoAsync(message, "GitDiffVisualizationService.ShowBlameInfoAsync");
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitDiffVisualizationService.ShowBlameInfoAsync");
                }
            });
        }

        /// <summary>
        /// Refreshes Git diff visualization
        /// </summary>
        public async Task RefreshDiffVisualizationAsync(ITextView textView, string filePath)
        {
            try
            {
                ClearGutterDecorations(textView);
                await VisualizeGitDiffAsync(textView, filePath);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitDiffVisualizationService.RefreshDiffVisualizationAsync");
            }
        }
    }

    /// <summary>
    /// Interface for Git diff visualization service
    /// </summary>
    public interface IGitDiffVisualizationService
    {
        /// <summary>
        /// Visualizes Git diff in the editor
        /// </summary>
        Task VisualizeGitDiffAsync(ITextView textView, string filePath);

        /// <summary>
        /// Gets Git diff information for a specific line
        /// </summary>
        Task<GitDiffLineInfo> GetDiffInfoForLineAsync(ITextView textView, int lineNumber);

        /// <summary>
        /// Shows Git blame information for a line
        /// </summary>
        Task ShowBlameAsync(ITextView textView, string filePath, int lineNumber);

        /// <summary>
        /// Refreshes Git diff visualization
        /// </summary>
        Task RefreshDiffVisualizationAsync(ITextView textView, string filePath);
    }

    /// <summary>
    /// Represents a Git diff gutter decoration
    /// </summary>
    public class GitDiffGutter
    {
        public int LineNumber { get; set; }
        public GitDiffLineType LineType { get; set; }
        public string Content { get; set; }
        public string Symbol { get; set; }
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public SnapshotSpan Span { get; set; }
    }

    /// <summary>
    /// Represents Git diff line information
    /// </summary>
    public class GitDiffLineInfo
    {
        public int LineNumber { get; set; }
        public GitDiffLineType LineType { get; set; }
        public string Content { get; set; }
        public string Symbol { get; set; }
    }

    /// <summary>
    /// Represents Git blame information
    /// </summary>
    public class GitBlameInfo
    {
        public int LineNumber { get; set; }
        public string CommitHash { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public DateTime CommitDate { get; set; }
    }
}