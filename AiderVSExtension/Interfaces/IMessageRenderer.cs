using System;
using System.Collections.Generic;
using System.Windows;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for rendering chat messages with syntax highlighting and formatting
    /// </summary>
    public interface IMessageRenderer
    {
        /// <summary>
        /// Renders a chat message to a UI element
        /// </summary>
        /// <param name="message">The message to render</param>
        /// <returns>The rendered UI element</returns>
        UIElement RenderMessage(ChatMessage message);

        /// <summary>
        /// Renders code content with syntax highlighting
        /// </summary>
        /// <param name="code">The code content</param>
        /// <param name="language">The programming language</param>
        /// <returns>The rendered code element</returns>
        UIElement RenderCode(string code, string language);

        /// <summary>
        /// Renders a file reference as a clickable link
        /// </summary>
        /// <param name="fileReference">The file reference</param>
        /// <returns>The rendered file reference element</returns>
        UIElement RenderFileReference(FileReference fileReference);

        /// <summary>
        /// Renders markdown content
        /// </summary>
        /// <param name="markdown">The markdown content</param>
        /// <returns>The rendered markdown element</returns>
        UIElement RenderMarkdown(string markdown);

        /// <summary>
        /// Event fired when a file reference is clicked
        /// </summary>
        event EventHandler<FileReferenceClickedEventArgs> FileReferenceClicked;

        /// <summary>
        /// Gets or sets the current theme for rendering
        /// </summary>
        RenderTheme Theme { get; set; }
    }

    /// <summary>
    /// Event arguments for file reference clicked events
    /// </summary>
    public class FileReferenceClickedEventArgs : EventArgs
    {
        public FileReference FileReference { get; set; }
    }

    /// <summary>
    /// Represents rendering theme information
    /// </summary>
    public class RenderTheme
    {
        public string BackgroundColor { get; set; }
        public string ForegroundColor { get; set; }
        public string AccentColor { get; set; }
        public string CodeBackgroundColor { get; set; }
        public string LinkColor { get; set; }
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
    }
}