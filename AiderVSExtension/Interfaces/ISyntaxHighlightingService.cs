using System;
using System.Collections.Generic;
using System.Windows.Documents;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for syntax highlighting services
    /// </summary>
    public interface ISyntaxHighlightingService
    {
        /// <summary>
        /// Event fired when the theme changes
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Highlights syntax in a text document
        /// </summary>
        /// <param name="text">Text to highlight</param>
        /// <param name="language">Programming language</param>
        /// <param name="theme">Theme to use (optional)</param>
        /// <returns>Highlighted document</returns>
        FlowDocument HighlightSyntax(string text, string language = null, SyntaxHighlightingTheme theme = null);

        /// <summary>
        /// Highlights syntax in a text run collection
        /// </summary>
        /// <param name="text">Text to highlight</param>
        /// <param name="language">Programming language</param>
        /// <param name="theme">Theme to use (optional)</param>
        /// <returns>Collection of highlighted runs</returns>
        IEnumerable<Run> HighlightText(string text, string language = null, SyntaxHighlightingTheme theme = null);

        /// <summary>
        /// Registers a custom syntax highlighting theme
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <param name="theme">Theme definition</param>
        void RegisterTheme(string name, SyntaxHighlightingTheme theme);

        /// <summary>
        /// Gets a registered theme by name
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <returns>Theme or null if not found</returns>
        SyntaxHighlightingTheme GetTheme(string name);

        /// <summary>
        /// Gets all available themes
        /// </summary>
        /// <returns>Dictionary of theme names and themes</returns>
        Dictionary<string, SyntaxHighlightingTheme> GetAllThemes();

        /// <summary>
        /// Sets the current theme
        /// </summary>
        /// <param name="theme">Theme to set</param>
        void SetCurrentTheme(SyntaxHighlightingTheme theme);

        /// <summary>
        /// Detects the programming language from code text
        /// </summary>
        /// <param name="text">Code text</param>
        /// <returns>Detected language</returns>
        string DetectLanguage(string text);

        /// <summary>
        /// Gets supported languages
        /// </summary>
        /// <returns>List of supported languages</returns>
        IEnumerable<string> GetSupportedLanguages();
    }
}