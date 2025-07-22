using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.Threading.Tasks;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for Visual Studio theming integration
    /// </summary>
    public class VSThemingService : IVSThemingService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<string, Color> _cachedColors = new Dictionary<string, Color>();
        private readonly Dictionary<string, Brush> _cachedBrushes = new Dictionary<string, Brush>();
        private bool _disposed = false;

        public event EventHandler<Interfaces.ThemeChangedEventArgs> ThemeChanged;

        public VSThemingService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            // Subscribe to theme change notifications
            VSColorTheme.ThemeChanged += OnVSThemeChanged;
        }

        /// <summary>
        /// Gets the current Visual Studio theme
        /// </summary>
        public Interfaces.VSTheme GetCurrentTheme()
        {
            try
            {
                var themeService = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell5;
                if (themeService != null)
                {
                    var themeId = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
                    
                    // Determine theme based on background color
                    var luminance = GetLuminance(new Color() { R = themeId.R, G = themeId.G, B = themeId.B });
                    if (luminance > 0.5)
                        return Interfaces.VSTheme.Light;
                    else
                        return Interfaces.VSTheme.Dark;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetCurrentTheme");
            }

            return Interfaces.VSTheme.Light; // Default fallback
        }

        /// <summary>
        /// Gets a themed color by key
        /// </summary>
        public Color GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey key)
        {
            try
            {
                var cacheKey = key.ToString();
                if (_cachedColors.TryGetValue(cacheKey, out var cachedColor))
                    return cachedColor;

                Color color = GetVSColor(key);
                _cachedColors[cacheKey] = color;
                return color;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetThemedColor");
                return Colors.Gray; // Fallback color
            }
        }

        /// <summary>
        /// Gets a themed brush by key
        /// </summary>
        public Brush GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey key)
        {
            try
            {
                var cacheKey = key.ToString();
                if (_cachedBrushes.TryGetValue(cacheKey, out var cachedBrush))
                    return cachedBrush;

                var color = GetThemedColor(key);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                _cachedBrushes[cacheKey] = brush;
                return brush;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetThemedBrush");
                return Brushes.Gray; // Fallback brush
            }
        }

        /// <summary>
        /// Applies theming to a WPF element
        /// </summary>
        public void ApplyTheme(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                // Apply basic theming
                element.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ThemedDialogDefaultStylesKey);
                
                // Apply specific color resources
                var resources = element.Resources;
                
                // Background colors
                resources[SystemColors.WindowBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.WindowBackground);
                resources[SystemColors.WindowTextBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.WindowText);
                resources[SystemColors.ControlBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.ControlBackground);
                resources[SystemColors.ControlTextBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.ControlText);
                
                // Button colors
                resources[SystemColors.ControlBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.ButtonBackground);
                resources[SystemColors.ControlTextBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.ButtonText);
                resources[SystemColors.HighlightBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.Highlight);
                resources[SystemColors.HighlightTextBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.Highlight);
                
                // Border colors
                resources[SystemColors.ActiveBorderBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.ActiveBorder);
                resources[SystemColors.InactiveBorderBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.InactiveBorder);
                
                // Text colors
                resources[SystemColors.GrayTextBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.GrayText);
                resources[SystemColors.HotTrackBrushKey] = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.HotTrack);
                
                // Apply to child elements
                ApplyThemeToChildren(element);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.ApplyTheme");
            }
        }

        /// <summary>
        /// Gets themed colors for syntax highlighting
        /// </summary>
        public SyntaxHighlightingTheme GetSyntaxHighlightingTheme()
        {
            try
            {
                return new SyntaxHighlightingTheme
                {
                    BackgroundColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.EditorBackground),
                    ForegroundColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.EditorText),
                    
                    // Code highlighting colors
                    KeywordColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Keyword),
                    StringColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.String),
                    CommentColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Comment),
                    NumberColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Number),
                    OperatorColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Operator),
                    VariableColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Identifier),
                    
                    // Special highlighting
                    ErrorColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Error),
                    WarningColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Warning),
                    
                    // Line highlighting
                    LineNumberColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.LineNumber),
                    CurrentLineColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.CurrentLine),
                    SelectionBackgroundColor = GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.Selection)
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetSyntaxHighlightingTheme");
                return GetDefaultSyntaxTheme();
            }
        }

        /// <summary>
        /// Gets responsive UI settings based on screen size
        /// </summary>
        public ResponsiveUISettings GetResponsiveSettings()
        {
            try
            {
                var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
                var screenWidth = primaryScreen.Bounds.Width;
                var screenHeight = primaryScreen.Bounds.Height;
                var dpiX = primaryScreen.Bounds.Width / primaryScreen.WorkingArea.Width;
                
                return new ResponsiveUISettings
                {
                    ScreenWidth = screenWidth,
                    ScreenHeight = screenHeight,
                    DpiScaling = dpiX,
                    
                    // Responsive breakpoints
                    IsSmallScreen = screenWidth < 1366,
                    IsMediumScreen = screenWidth >= 1366 && screenWidth < 1920,
                    IsLargeScreen = screenWidth >= 1920,
                    
                    // UI scaling
                    FontSize = GetResponsiveFontSize(screenWidth, dpiX),
                    IconSize = GetResponsiveIconSize(screenWidth, dpiX),
                    Spacing = GetResponsiveSpacing(screenWidth, dpiX),
                    
                    // Layout settings
                    PreferCompactLayout = screenWidth < 1366 || screenHeight < 768,
                    ShowDetailedViews = screenWidth >= 1920,
                    UseCollapsibleSections = screenWidth < 1600
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetResponsiveSettings");
                return GetDefaultResponsiveSettings();
            }
        }

        /// <summary>
        /// Invalidates cached theme resources
        /// </summary>
        public void InvalidateThemeCache()
        {
            try
            {
                _cachedColors.Clear();
                _cachedBrushes.Clear();
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.InvalidateThemeCache");
            }
        }

        /// <summary>
        /// Gets accessibility settings
        /// </summary>
        public AccessibilitySettings GetAccessibilitySettings()
        {
            try
            {
                return new AccessibilitySettings
                {
                    HighContrast = SystemParameters.HighContrast,
                    IsKeyboardNavigationEnabled = true,
                    IsScreenReaderSupported = true,
                    
                    // Font settings
                    UseSystemFonts = true,
                    MinimumFontSize = SystemParameters.HighContrast ? 12 : 10,
                    
                    // Color settings
                    UseHighContrastColors = SystemParameters.HighContrast,
                    
                    // Animation settings
                    ReduceAnimations = !SystemParameters.ClientAreaAnimation,
                    
                    // Focus settings
                    ShowFocusVisuals = true,
                    FocusThickness = SystemParameters.HighContrast ? 3 : 2
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.GetAccessibilitySettings");
                return GetDefaultAccessibilitySettings();
            }
        }

        #region Private Methods

        private Color GetVSColor(AiderVSExtension.Interfaces.ThemeResourceKey key)
        {
            try
            {
                switch (key)
                {
                    case AiderVSExtension.Interfaces.ThemeResourceKey.WindowBackground:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.WindowText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.ControlBackground:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.ControlText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.ButtonBackground:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.ButtonText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Highlight:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.AccentBorderColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.HighlightText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.AccentPaleColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.ActiveBorder:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.AccentBorderColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.InactiveBorder:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.GrayText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.HotTrack:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarHoverColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.EditorBackground:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.EditorText:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Keyword:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.String:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Comment:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Number:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Operator:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Identifier:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Error:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Warning:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Information:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.LineNumber:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.CurrentLine:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.Selection:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.DiffAdded:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.DiffRemoved:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    case AiderVSExtension.Interfaces.ThemeResourceKey.DiffModified:
                        return ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                    default:
                        return Colors.Gray;
                }
            }
            catch
            {
                return Colors.Gray;
            }
        }

        private Color ConvertToWpfColor(System.Drawing.Color drawingColor)
        {
            return Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        private double GetLuminance(Color color)
        {
            // Calculate relative luminance using the formula: 0.299*R + 0.587*G + 0.114*B
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        }

        private void ApplyThemeToChildren(FrameworkElement element)
        {
            try
            {
                if (element is Panel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is FrameworkElement fe)
                        {
                            ApplyTheme(fe);
                        }
                    }
                }
                else if (element is ContentControl contentControl && contentControl.Content is FrameworkElement content)
                {
                    ApplyTheme(content);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "VSThemingService.ApplyThemeToChildren");
            }
        }

        private double GetResponsiveFontSize(int screenWidth, double dpiScaling)
        {
            var baseFontSize = 12.0;
            if (screenWidth < 1366)
                baseFontSize = 11.0;
            else if (screenWidth >= 1920)
                baseFontSize = 13.0;
            
            return baseFontSize * dpiScaling;
        }

        private double GetResponsiveIconSize(int screenWidth, double dpiScaling)
        {
            var baseIconSize = 16.0;
            if (screenWidth < 1366)
                baseIconSize = 14.0;
            else if (screenWidth >= 1920)
                baseIconSize = 18.0;
            
            return baseIconSize * dpiScaling;
        }

        private double GetResponsiveSpacing(int screenWidth, double dpiScaling)
        {
            var baseSpacing = 8.0;
            if (screenWidth < 1366)
                baseSpacing = 6.0;
            else if (screenWidth >= 1920)
                baseSpacing = 10.0;
            
            return baseSpacing * dpiScaling;
        }

        private SyntaxHighlightingTheme GetDefaultSyntaxTheme()
        {
            return new SyntaxHighlightingTheme
            {
                BackgroundColor = Colors.White,
                ForegroundColor = Colors.Black,
                KeywordColor = Colors.Blue,
                StringColor = Colors.Brown,
                CommentColor = Colors.Green,
                NumberColor = Colors.Red,
                OperatorColor = Colors.Black,
                VariableColor = Colors.Black,
                ErrorColor = Colors.Red,
                WarningColor = Colors.Orange,
                LineNumberColor = Colors.Gray,
                CurrentLineColor = Colors.LightBlue,
                SelectionBackgroundColor = Colors.LightBlue
            };
        }

        private ResponsiveUISettings GetDefaultResponsiveSettings()
        {
            return new ResponsiveUISettings
            {
                ScreenWidth = 1920,
                ScreenHeight = 1080,
                DpiScaling = 1.0,
                IsSmallScreen = false,
                IsMediumScreen = false,
                IsLargeScreen = true,
                FontSize = 12,
                IconSize = 16,
                Spacing = 8,
                PreferCompactLayout = false,
                ShowDetailedViews = true,
                UseCollapsibleSections = false
            };
        }

        private AccessibilitySettings GetDefaultAccessibilitySettings()
        {
            return new AccessibilitySettings
            {
                HighContrast = false,
                IsKeyboardNavigationEnabled = true,
                IsScreenReaderSupported = true,
                UseSystemFonts = true,
                MinimumFontSize = 10,
                UseHighContrastColors = false,
                ReduceAnimations = false,
                ShowFocusVisuals = true,
                FocusThickness = 2
            };
        }

        private async void OnVSThemeChanged(Microsoft.VisualStudio.PlatformUI.ThemeChangedEventArgs e)
        {
            try
            {
                InvalidateThemeCache();
                ThemeChanged?.Invoke(this, new Interfaces.ThemeChangedEventArgs
                {
                    NewTheme = GetCurrentTheme(),
                    ChangedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "VSThemingService.OnVSThemeChanged");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                VSColorTheme.ThemeChanged -= OnVSThemeChanged;
                _disposed = true;
            }
        }
    }
}