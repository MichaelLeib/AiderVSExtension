using System;
using System.Windows;
using System.Windows.Media;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for Visual Studio theming integration
    /// </summary>
    public interface IVSThemingService
    {
        /// <summary>
        /// Event fired when the VS theme changes
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Gets the current Visual Studio theme
        /// </summary>
        /// <returns>Current theme</returns>
        VSTheme GetCurrentTheme();

        /// <summary>
        /// Gets a themed color by key
        /// </summary>
        /// <param name="key">Theme resource key</param>
        /// <returns>Themed color</returns>
        Color GetThemedColor(ThemeResourceKey key);

        /// <summary>
        /// Gets a themed brush by key
        /// </summary>
        /// <param name="key">Theme resource key</param>
        /// <returns>Themed brush</returns>
        Brush GetThemedBrush(ThemeResourceKey key);

        /// <summary>
        /// Applies theming to a WPF element
        /// </summary>
        /// <param name="element">Element to theme</param>
        void ApplyTheme(FrameworkElement element);

        /// <summary>
        /// Gets themed colors for syntax highlighting
        /// </summary>
        /// <returns>Syntax highlighting theme</returns>
        SyntaxHighlightingTheme GetSyntaxHighlightingTheme();

        /// <summary>
        /// Gets responsive UI settings based on screen size
        /// </summary>
        /// <returns>Responsive UI settings</returns>
        ResponsiveUISettings GetResponsiveSettings();

        /// <summary>
        /// Invalidates cached theme resources
        /// </summary>
        void InvalidateThemeCache();

        /// <summary>
        /// Gets accessibility settings
        /// </summary>
        /// <returns>Accessibility settings</returns>
        AccessibilitySettings GetAccessibilitySettings();
    }

    /// <summary>
    /// Visual Studio theme types
    /// </summary>
    public enum VSTheme
    {
        Light,
        Dark,
        Blue,
        HighContrast
    }

    /// <summary>
    /// Theme resource keys
    /// </summary>
    public enum ThemeResourceKey
    {
        WindowBackground,
        WindowText,
        ControlBackground,
        ControlText,
        ButtonBackground,
        ButtonText,
        Highlight,
        HighlightText,
        ActiveBorder,
        InactiveBorder,
        GrayText,
        HotTrack,
        EditorBackground,
        EditorText,
        Keyword,
        String,
        Comment,
        Number,
        Operator,
        Identifier,
        Error,
        Warning,
        Information,
        LineNumber,
        CurrentLine,
        Selection,
        DiffAdded,
        DiffRemoved,
        DiffModified
    }


    /// <summary>
    /// Accessibility settings
    /// </summary>
    public class AccessibilitySettings
    {
        public bool HighContrast { get; set; }
        public bool IsKeyboardNavigationEnabled { get; set; }
        public bool IsScreenReaderSupported { get; set; }
        public bool UseSystemFonts { get; set; }
        public double MinimumFontSize { get; set; }
        public bool UseHighContrastColors { get; set; }
        public bool ReduceAnimations { get; set; }
        public bool ShowFocusVisuals { get; set; }
        public double FocusThickness { get; set; }
    }

    /// <summary>
    /// Theme changed event arguments
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public VSTheme OldTheme { get; set; }
        public VSTheme NewTheme { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}