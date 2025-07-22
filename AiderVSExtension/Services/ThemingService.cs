using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Models;
// VSTheme type from Models namespace

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for Visual Studio theming integration
    /// </summary>
    public class ThemingService : AiderVSExtension.Interfaces.IThemingService, IDisposable
    {
        private readonly IVsUIShell5 _uiShell;
        private readonly AiderVSExtension.Interfaces.IVSThemingService _vsThemingService;
        private readonly AiderVSExtension.Interfaces.IConfigurationService _configurationService;
        private readonly Dictionary<string, Style> _cachedStyles;
        private uint _themeChangedCookie;
        private bool _disposed = false;

        public event EventHandler<AiderVSExtension.Interfaces.ThemeChangedEventArgs> ThemeChanged;

        public ThemingService(AiderVSExtension.Interfaces.IVSThemingService vsThemingService, AiderVSExtension.Interfaces.IConfigurationService configurationService)
        {
            _vsThemingService = vsThemingService ?? throw new ArgumentNullException(nameof(vsThemingService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _cachedStyles = new Dictionary<string, Style>();

            // Get VS UI Shell for theme notifications
            _uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell5;
            
            RegisterForThemeChanges();
        }

        /// <summary>
        /// Gets the current Visual Studio theme
        /// </summary>
        public AiderVSExtension.Interfaces.VSTheme GetCurrentTheme()
        {
            return _vsThemingService.GetCurrentTheme();
        }

        /// <summary>
        /// Gets a themed color for the specified color key
        /// </summary>
        public Color GetThemedColor(string colorKey)
        {
            return _vsThemingService.GetThemedColor(colorKey);
        }

        /// <summary>
        /// Gets a themed brush for the specified color key
        /// </summary>
        public Brush GetThemedBrush(string colorKey)
        {
            return _vsThemingService.GetThemedBrush(colorKey);
        }

        /// <summary>
        /// Gets themed colors for syntax highlighting
        /// </summary>
        public SyntaxHighlightingTheme GetSyntaxHighlightingTheme()
        {
            var currentTheme = GetCurrentTheme();
            
            return new SyntaxHighlightingTheme
            {
                Name = currentTheme.ToString(),
                BackgroundColor = GetThemedColor("Window.Background"),
                ForegroundColor = GetThemedColor("Window.Text"),
                KeywordColor = GetThemedColor("Keyword"),
                StringColor = GetThemedColor("String"),
                CommentColor = GetThemedColor("Comment"),
                NumberColor = GetThemedColor("Number"),
                OperatorColor = GetThemedColor("Operator"),
                TypeColor = GetThemedColor("UserType"),
                MethodColor = GetThemedColor("Method"),
                VariableColor = GetThemedColor("Identifier"),
                ErrorColor = GetThemedColor("SyntaxError"),
                WarningColor = GetThemedColor("Warning"),
                LineNumberColor = GetThemedColor("LineNumber"),
                SelectionBackgroundColor = GetThemedColor("Selection"),
                CurrentLineColor = GetThemedColor("CurrentLine")
            };
        }

        /// <summary>
        /// Applies theme to a UI element
        /// </summary>
        public void ApplyTheme(FrameworkElement element, ThemeProfile themeProfile = null)
        {
            if (element == null) return;

            // Apply basic theming
            element.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ThemedDialogDefaultStylesKey);
            
            // Apply custom theme profile if provided
            if (themeProfile != null)
            {
                var resources = CreateThemedResourceDictionary(themeProfile);
                element.Resources.MergedDictionaries.Add(resources);
            }
        }

        /// <summary>
        /// Gets theme-aware styles for common UI elements
        /// </summary>
        public Dictionary<string, Style> GetThemedStyles()
        {
            if (_cachedStyles.Any())
                return new Dictionary<string, Style>(_cachedStyles);

            var styles = new Dictionary<string, Style>();
            var currentTheme = GetCurrentTheme();

            // Create styles for common elements
            styles["Button"] = CreateButtonStyle(currentTheme);
            styles["TextBox"] = CreateTextBoxStyle(currentTheme);
            styles["ListBox"] = CreateListBoxStyle(currentTheme);
            styles["ComboBox"] = CreateComboBoxStyle(currentTheme);
            styles["Label"] = CreateLabelStyle(currentTheme);
            styles["CheckBox"] = CreateCheckBoxStyle(currentTheme);
            styles["RadioButton"] = CreateRadioButtonStyle(currentTheme);

            // Cache the styles
            foreach (var style in styles)
            {
                _cachedStyles[style.Key] = style.Value;
            }

            return styles;
        }

        /// <summary>
        /// Registers for theme change notifications
        /// </summary>
        public void RegisterForThemeChanges()
        {
            if (_uiShell != null && _themeChangedCookie == 0)
            {
                // _uiShell.AdviseUIShellPropertyChanges(this, out _themeChangedCookie); // Not available in VS 2022
            }
        }

        /// <summary>
        /// Unregisters from theme change notifications
        /// </summary>
        public void UnregisterFromThemeChanges()
        {
            if (_uiShell != null && _themeChangedCookie != 0)
            {
                // _uiShell.UnadviseUIShellPropertyChanges(_themeChangedCookie); // Not available in VS 2022
                _themeChangedCookie = 0;
            }
        }

        /// <summary>
        /// Gets high contrast theme if enabled
        /// </summary>
        public HighContrastTheme GetHighContrastTheme()
        {
            if (!IsHighContrastEnabled())
                return null;

            return new HighContrastTheme
            {
                BackgroundColor = GetThemedColor("Window.Background"),
                ForegroundColor = GetThemedColor("Window.Text"),
                AccentColor = GetThemedColor("Accent"),
                BorderColor = GetThemedColor("Border"),
                DisabledColor = GetThemedColor("GrayText"),
                HighlightColor = GetThemedColor("Highlight"),
                LinkColor = GetThemedColor("HotTrack"),
                VisitedLinkColor = GetThemedColor("VisitedHyperlink")
            };
        }

        /// <summary>
        /// Checks if high contrast mode is enabled
        /// </summary>
        public bool IsHighContrastEnabled()
        {
            return SystemParameters.HighContrast;
        }

        /// <summary>
        /// Gets accessibility-friendly colors
        /// </summary>
        public AccessibilityColorScheme GetAccessibilityColors()
        {
            return new AccessibilityColorScheme
            {
                BackgroundColor = GetThemedColor("Window.Background"),
                ForegroundColor = GetThemedColor("Window.Text"),
                AccentColor = GetThemedColor("Accent"),
                ErrorColor = GetThemedColor("Error"),
                WarningColor = GetThemedColor("Warning"),
                SuccessColor = GetThemedColor("Success"),
                InfoColor = GetThemedColor("Info"),
                FocusColor = GetThemedColor("Focus"),
                SelectionColor = GetThemedColor("Selection"),
                DisabledColor = GetThemedColor("GrayText"),
                ContrastRatio = IsHighContrastEnabled() ? 7.0 : 4.5
            };
        }

        /// <summary>
        /// Creates a themed resource dictionary
        /// </summary>
        public ResourceDictionary CreateThemedResourceDictionary(ThemeProfile themeProfile = null)
        {
            var resources = new ResourceDictionary();
            var theme = GetCurrentTheme();

            // Add themed colors
            resources["BackgroundColor"] = GetThemedColor("Window.Background");
            resources["ForegroundColor"] = GetThemedColor("Window.Text");
            resources["AccentColor"] = GetThemedColor("Accent");
            resources["BorderColor"] = GetThemedColor("Border");

            // Add themed brushes
            resources["BackgroundBrush"] = GetThemedBrush("Window.Background");
            resources["ForegroundBrush"] = GetThemedBrush("Window.Text");
            resources["AccentBrush"] = GetThemedBrush("Accent");
            resources["BorderBrush"] = GetThemedBrush("Border");

            // Add themed styles
            var styles = GetThemedStyles();
            foreach (var style in styles)
            {
                resources[style.Key + "Style"] = style.Value;
            }

            return resources;
        }

        /// <summary>
        /// Gets custom theme profiles
        /// </summary>
        public async Task<List<ThemeProfile>> GetCustomThemeProfilesAsync()
        {
            try
            {
                var profilesJson = await _configurationService.GetValueAsync<string>("CustomThemeProfiles", "[]");
                return System.Text.Json.JsonSerializer.Deserialize<List<ThemeProfile>>(profilesJson) ?? new List<ThemeProfile>();
            }
            catch (Exception)
            {
                return new List<ThemeProfile>();
            }
        }

        /// <summary>
        /// Saves a custom theme profile
        /// </summary>
        public async Task SaveCustomThemeProfileAsync(ThemeProfile profile)
        {
            if (profile == null) return;

            var profiles = await GetCustomThemeProfilesAsync();
            var existingIndex = profiles.FindIndex(p => p.Id == profile.Id);
            
            if (existingIndex >= 0)
            {
                profiles[existingIndex] = profile;
            }
            else
            {
                profiles.Add(profile);
            }

            var profilesJson = System.Text.Json.JsonSerializer.Serialize(profiles);
            await _configurationService.SetValueAsync("CustomThemeProfiles", profilesJson);
        }

        /// <summary>
        /// Deletes a custom theme profile
        /// </summary>
        public async Task DeleteCustomThemeProfileAsync(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return;

            var profiles = await GetCustomThemeProfilesAsync();
            profiles.RemoveAll(p => p.Id == profileId);

            var profilesJson = System.Text.Json.JsonSerializer.Serialize(profiles);
            await _configurationService.SetValueAsync("CustomThemeProfiles", profilesJson);
        }

        #region Private Helper Methods

        private Style CreateButtonStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Button.BackgroundProperty, GetThemedBrush("Button.Background")));
            style.Setters.Add(new Setter(Button.ForegroundProperty, GetThemedBrush("Button.Text")));
            style.Setters.Add(new Setter(Button.BorderBrushProperty, GetThemedBrush("Button.Border")));
            return style;
        }

        private Style CreateTextBoxStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(TextBox));
            style.Setters.Add(new Setter(TextBox.BackgroundProperty, GetThemedBrush("TextBox.Background")));
            style.Setters.Add(new Setter(TextBox.ForegroundProperty, GetThemedBrush("TextBox.Text")));
            style.Setters.Add(new Setter(TextBox.BorderBrushProperty, GetThemedBrush("TextBox.Border")));
            return style;
        }

        private Style CreateListBoxStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(ListBox));
            style.Setters.Add(new Setter(ListBox.BackgroundProperty, GetThemedBrush("ListBox.Background")));
            style.Setters.Add(new Setter(ListBox.ForegroundProperty, GetThemedBrush("ListBox.Text")));
            style.Setters.Add(new Setter(ListBox.BorderBrushProperty, GetThemedBrush("ListBox.Border")));
            return style;
        }

        private Style CreateComboBoxStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(ComboBox));
            style.Setters.Add(new Setter(ComboBox.BackgroundProperty, GetThemedBrush("ComboBox.Background")));
            style.Setters.Add(new Setter(ComboBox.ForegroundProperty, GetThemedBrush("ComboBox.Text")));
            style.Setters.Add(new Setter(ComboBox.BorderBrushProperty, GetThemedBrush("ComboBox.Border")));
            return style;
        }

        private Style CreateLabelStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(Label));
            style.Setters.Add(new Setter(Label.ForegroundProperty, GetThemedBrush("Label.Text")));
            return style;
        }

        private Style CreateCheckBoxStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(CheckBox.ForegroundProperty, GetThemedBrush("CheckBox.Text")));
            return style;
        }

        private Style CreateRadioButtonStyle(AiderVSExtension.Interfaces.VSTheme theme)
        {
            var style = new Style(typeof(RadioButton));
            style.Setters.Add(new Setter(RadioButton.ForegroundProperty, GetThemedBrush("RadioButton.Text")));
            return style;
        }

        private void OnThemeChanged(AiderVSExtension.Interfaces.VSTheme oldTheme, AiderVSExtension.Interfaces.VSTheme newTheme)
        {
            // Clear cached styles
            _cachedStyles.Clear();

            // Raise theme changed event
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
            {
                OldTheme = oldTheme,
                NewTheme = newTheme,
                ChangedAt = DateTime.UtcNow
            });
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterFromThemeChanges();
                _cachedStyles.Clear();
                _disposed = true;
            }
        }
    }
}