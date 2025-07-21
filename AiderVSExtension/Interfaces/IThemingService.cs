using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for Visual Studio theming integration
    /// </summary>
    public interface IThemingService
    {
        /// <summary>
        /// Event fired when the theme changes
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Gets the current Visual Studio theme
        /// </summary>
        /// <returns>Current theme information</returns>
        VSTheme GetCurrentTheme();

        /// <summary>
        /// Gets a themed color for the specified color key
        /// </summary>
        /// <param name="colorKey">Color key from VS theme</param>
        /// <returns>Themed color</returns>
        Color GetThemedColor(string colorKey);

        /// <summary>
        /// Gets a themed brush for the specified color key
        /// </summary>
        /// <param name="colorKey">Color key from VS theme</param>
        /// <returns>Themed brush</returns>
        Brush GetThemedBrush(string colorKey);

        /// <summary>
        /// Gets themed colors for syntax highlighting
        /// </summary>
        /// <returns>Syntax highlighting theme</returns>
        SyntaxHighlightingTheme GetSyntaxHighlightingTheme();

        /// <summary>
        /// Applies theme to a UI element
        /// </summary>
        /// <param name="element">UI element to theme</param>
        /// <param name="themeProfile">Optional theme profile</param>
        void ApplyTheme(FrameworkElement element, ThemeProfile themeProfile = null);

        /// <summary>
        /// Gets theme-aware styles for common UI elements
        /// </summary>
        /// <returns>Dictionary of themed styles</returns>
        Dictionary<string, Style> GetThemedStyles();

        /// <summary>
        /// Registers for theme change notifications
        /// </summary>
        void RegisterForThemeChanges();

        /// <summary>
        /// Unregisters from theme change notifications
        /// </summary>
        void UnregisterFromThemeChanges();

        /// <summary>
        /// Gets high contrast theme if enabled
        /// </summary>
        /// <returns>High contrast theme or null</returns>
        HighContrastTheme GetHighContrastTheme();

        /// <summary>
        /// Checks if high contrast mode is enabled
        /// </summary>
        /// <returns>True if high contrast is enabled</returns>
        bool IsHighContrastEnabled();

        /// <summary>
        /// Gets accessibility-friendly colors
        /// </summary>
        /// <returns>Accessibility color scheme</returns>
        AccessibilityColorScheme GetAccessibilityColors();

        /// <summary>
        /// Creates a themed resource dictionary
        /// </summary>
        /// <param name="themeProfile">Theme profile to use</param>
        /// <returns>Resource dictionary with themed resources</returns>
        ResourceDictionary CreateThemedResourceDictionary(ThemeProfile themeProfile = null);

        /// <summary>
        /// Gets custom theme profiles
        /// </summary>
        /// <returns>Available custom theme profiles</returns>
        Task<List<ThemeProfile>> GetCustomThemeProfilesAsync();

        /// <summary>
        /// Saves a custom theme profile
        /// </summary>
        /// <param name="profile">Theme profile to save</param>
        Task SaveCustomThemeProfileAsync(ThemeProfile profile);

        /// <summary>
        /// Deletes a custom theme profile
        /// </summary>
        /// <param name="profileId">Profile ID to delete</param>
        Task DeleteCustomThemeProfileAsync(string profileId);
    }
}