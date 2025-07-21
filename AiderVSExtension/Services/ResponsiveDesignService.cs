using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.Threading.Tasks;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for responsive design and adaptive UI layouts
    /// </summary>
    public class ResponsiveDesignService : IResponsiveDesignService, IDisposable
    {
        private readonly IVSThemingService _themingService;
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<FrameworkElement, ResponsiveConfiguration> _responsiveElements = new Dictionary<FrameworkElement, ResponsiveConfiguration>();
        private readonly Dictionary<string, ResponsiveBreakpoint> _breakpoints = new Dictionary<string, ResponsiveBreakpoint>();
        private ResponsiveUISettings _currentSettings;
        private bool _disposed = false;

        public event EventHandler<ResponsiveStateChangedEventArgs> ResponsiveStateChanged;

        public ResponsiveDesignService(IVSThemingService themingService, IErrorHandler errorHandler)
        {
            _themingService = themingService ?? throw new ArgumentNullException(nameof(themingService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            InitializeBreakpoints();
            UpdateCurrentSettings();
            
            // Subscribe to theme changes
            _themingService.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// Makes a UI element responsive
        /// </summary>
        /// <param name="element">Element to make responsive</param>
        /// <param name="configuration">Responsive configuration</param>
        public void MakeResponsive(FrameworkElement element, ResponsiveConfiguration configuration)
        {
            try
            {
                if (element == null || configuration == null)
                    return;

                _responsiveElements[element] = configuration;
                
                // Apply initial responsive settings
                ApplyResponsiveSettings(element, configuration);
                
                // Subscribe to size changes
                element.SizeChanged += OnElementSizeChanged;
                element.Loaded += OnElementLoaded;
                element.Unloaded += OnElementUnloaded;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.MakeResponsive");
            }
        }

        /// <summary>
        /// Removes responsive behavior from an element
        /// </summary>
        /// <param name="element">Element to remove responsive behavior from</param>
        public void RemoveResponsive(FrameworkElement element)
        {
            try
            {
                if (element == null || !_responsiveElements.ContainsKey(element))
                    return;

                element.SizeChanged -= OnElementSizeChanged;
                element.Loaded -= OnElementLoaded;
                element.Unloaded -= OnElementUnloaded;
                
                _responsiveElements.Remove(element);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.RemoveResponsive");
            }
        }

        /// <summary>
        /// Gets the current responsive settings
        /// </summary>
        /// <returns>Current responsive settings</returns>
        public ResponsiveUISettings GetCurrentSettings()
        {
            return _currentSettings;
        }

        /// <summary>
        /// Updates responsive settings based on current screen size
        /// </summary>
        public void UpdateCurrentSettings()
        {
            try
            {
                _currentSettings = _themingService.GetResponsiveSettings();
                
                // Update all responsive elements
                foreach (var kvp in _responsiveElements.ToList())
                {
                    ApplyResponsiveSettings(kvp.Key, kvp.Value);
                }
                
                ResponsiveStateChanged?.Invoke(this, new ResponsiveStateChangedEventArgs
                {
                    NewSettings = _currentSettings,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.UpdateCurrentSettings");
            }
        }

        /// <summary>
        /// Registers a custom breakpoint
        /// </summary>
        /// <param name="name">Breakpoint name</param>
        /// <param name="breakpoint">Breakpoint definition</param>
        public void RegisterBreakpoint(string name, ResponsiveBreakpoint breakpoint)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || breakpoint == null)
                    return;

                _breakpoints[name] = breakpoint;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.RegisterBreakpoint");
            }
        }

        /// <summary>
        /// Gets the active breakpoint for current screen size
        /// </summary>
        /// <returns>Active breakpoint name</returns>
        public string GetActiveBreakpoint()
        {
            try
            {
                var screenWidth = _currentSettings.ScreenWidth;
                
                var activeBreakpoint = _breakpoints
                    .Where(bp => screenWidth >= bp.Value.MinWidth && screenWidth <= bp.Value.MaxWidth)
                    .OrderByDescending(bp => bp.Value.Priority)
                    .FirstOrDefault();
                
                return activeBreakpoint.Key ?? "default";
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.GetActiveBreakpoint");
                return "default";
            }
        }

        /// <summary>
        /// Gets all registered breakpoints
        /// </summary>
        /// <returns>Dictionary of breakpoint names and definitions</returns>
        public Dictionary<string, ResponsiveBreakpoint> GetBreakpoints()
        {
            return new Dictionary<string, ResponsiveBreakpoint>(_breakpoints);
        }

        /// <summary>
        /// Applies responsive layout to a container
        /// </summary>
        /// <param name="container">Container to apply layout to</param>
        /// <param name="layout">Layout configuration</param>
        public void ApplyResponsiveLayout(Panel container, ResponsiveLayout layout)
        {
            try
            {
                if (container == null || layout == null)
                    return;

                var activeBreakpoint = GetActiveBreakpoint();
                var breakpointLayout = layout.BreakpointLayouts.GetValueOrDefault(activeBreakpoint) ?? layout.DefaultLayout;
                
                // Apply layout properties
                ApplyLayoutProperties(container, breakpointLayout);
                
                // Apply child arrangements
                ApplyChildArrangements(container, breakpointLayout);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyResponsiveLayout");
            }
        }

        /// <summary>
        /// Creates a responsive grid layout
        /// </summary>
        /// <param name="grid">Grid to make responsive</param>
        /// <param name="configuration">Grid configuration</param>
        public void CreateResponsiveGrid(Grid grid, ResponsiveGridConfiguration configuration)
        {
            try
            {
                if (grid == null || configuration == null)
                    return;

                var activeBreakpoint = GetActiveBreakpoint();
                var breakpointConfig = configuration.BreakpointConfigurations.GetValueOrDefault(activeBreakpoint) ?? configuration.DefaultConfiguration;
                
                // Clear existing definitions
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Clear();
                
                // Create rows
                for (int i = 0; i < breakpointConfig.Rows; i++)
                {
                    var height = i < breakpointConfig.RowHeights.Count ? breakpointConfig.RowHeights[i] : GridLength.Auto;
                    grid.RowDefinitions.Add(new RowDefinition { Height = height });
                }
                
                // Create columns
                for (int i = 0; i < breakpointConfig.Columns; i++)
                {
                    var width = i < breakpointConfig.ColumnWidths.Count ? breakpointConfig.ColumnWidths[i] : new GridLength(1, GridUnitType.Star);
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
                }
                
                // Apply spacing
                if (breakpointConfig.RowSpacing > 0)
                {
                    // Add row spacing implementation
                }
                
                if (breakpointConfig.ColumnSpacing > 0)
                {
                    // Add column spacing implementation
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.CreateResponsiveGrid");
            }
        }

        /// <summary>
        /// Applies responsive typography settings
        /// </summary>
        /// <param name="element">Element to apply typography to</param>
        /// <param name="typography">Typography configuration</param>
        public void ApplyResponsiveTypography(FrameworkElement element, ResponsiveTypography typography)
        {
            try
            {
                if (element == null || typography == null)
                    return;

                var activeBreakpoint = GetActiveBreakpoint();
                var breakpointTypography = typography.BreakpointTypography.GetValueOrDefault(activeBreakpoint) ?? typography.DefaultTypography;
                
                // Apply font size
                if (element is Control control)
                {
                    control.FontSize = breakpointTypography.FontSize;
                    control.FontFamily = new FontFamily(breakpointTypography.FontFamily);
                    control.FontWeight = breakpointTypography.FontWeight;
                    control.FontStyle = breakpointTypography.FontStyle;
                }
                else if (element is TextBlock textBlock)
                {
                    textBlock.FontSize = breakpointTypography.FontSize;
                    textBlock.FontFamily = new FontFamily(breakpointTypography.FontFamily);
                    textBlock.FontWeight = breakpointTypography.FontWeight;
                    textBlock.FontStyle = breakpointTypography.FontStyle;
                    textBlock.LineHeight = breakpointTypography.LineHeight;
                }
                
                // Apply accessibility adjustments
                var accessibilitySettings = _themingService.GetAccessibilitySettings();
                if (accessibilitySettings.MinimumFontSize > 0)
                {
                    if (element is Control ctrl && ctrl.FontSize < accessibilitySettings.MinimumFontSize)
                    {
                        ctrl.FontSize = accessibilitySettings.MinimumFontSize;
                    }
                    else if (element is TextBlock tb && tb.FontSize < accessibilitySettings.MinimumFontSize)
                    {
                        tb.FontSize = accessibilitySettings.MinimumFontSize;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyResponsiveTypography");
            }
        }

        /// <summary>
        /// Creates adaptive margins and padding
        /// </summary>
        /// <param name="element">Element to apply spacing to</param>
        /// <param name="spacing">Spacing configuration</param>
        public void ApplyAdaptiveSpacing(FrameworkElement element, ResponsiveSpacing spacing)
        {
            try
            {
                if (element == null || spacing == null)
                    return;

                var activeBreakpoint = GetActiveBreakpoint();
                var breakpointSpacing = spacing.BreakpointSpacing.GetValueOrDefault(activeBreakpoint) ?? spacing.DefaultSpacing;
                
                // Apply margin
                element.Margin = breakpointSpacing.Margin;
                
                // Apply padding (if supported)
                if (element is Control control)
                {
                    control.Padding = breakpointSpacing.Padding;
                }
                else if (element is Border border)
                {
                    border.Padding = breakpointSpacing.Padding;
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyAdaptiveSpacing");
            }
        }

        #region Private Methods

        private void InitializeBreakpoints()
        {
            // Standard breakpoints
            _breakpoints["xs"] = new ResponsiveBreakpoint
            {
                Name = "Extra Small",
                MinWidth = 0,
                MaxWidth = 575,
                Priority = 1
            };
            
            _breakpoints["sm"] = new ResponsiveBreakpoint
            {
                Name = "Small",
                MinWidth = 576,
                MaxWidth = 767,
                Priority = 2
            };
            
            _breakpoints["md"] = new ResponsiveBreakpoint
            {
                Name = "Medium",
                MinWidth = 768,
                MaxWidth = 991,
                Priority = 3
            };
            
            _breakpoints["lg"] = new ResponsiveBreakpoint
            {
                Name = "Large",
                MinWidth = 992,
                MaxWidth = 1199,
                Priority = 4
            };
            
            _breakpoints["xl"] = new ResponsiveBreakpoint
            {
                Name = "Extra Large",
                MinWidth = 1200,
                MaxWidth = 1399,
                Priority = 5
            };
            
            _breakpoints["xxl"] = new ResponsiveBreakpoint
            {
                Name = "Extra Extra Large",
                MinWidth = 1400,
                MaxWidth = int.MaxValue,
                Priority = 6
            };
        }

        private void ApplyResponsiveSettings(FrameworkElement element, ResponsiveConfiguration configuration)
        {
            try
            {
                var activeBreakpoint = GetActiveBreakpoint();
                
                // Apply size constraints
                if (configuration.SizeConstraints.ContainsKey(activeBreakpoint))
                {
                    var constraints = configuration.SizeConstraints[activeBreakpoint];
                    
                    if (constraints.MinWidth.HasValue)
                        element.MinWidth = constraints.MinWidth.Value;
                    if (constraints.MaxWidth.HasValue)
                        element.MaxWidth = constraints.MaxWidth.Value;
                    if (constraints.MinHeight.HasValue)
                        element.MinHeight = constraints.MinHeight.Value;
                    if (constraints.MaxHeight.HasValue)
                        element.MaxHeight = constraints.MaxHeight.Value;
                }
                
                // Apply visibility
                if (configuration.Visibility.ContainsKey(activeBreakpoint))
                {
                    element.Visibility = configuration.Visibility[activeBreakpoint];
                }
                
                // Apply layout properties
                if (configuration.LayoutProperties.ContainsKey(activeBreakpoint))
                {
                    var properties = configuration.LayoutProperties[activeBreakpoint];
                    
                    if (properties.HorizontalAlignment.HasValue)
                        element.HorizontalAlignment = properties.HorizontalAlignment.Value;
                    if (properties.VerticalAlignment.HasValue)
                        element.VerticalAlignment = properties.VerticalAlignment.Value;
                    if (properties.Margin.HasValue)
                        element.Margin = properties.Margin.Value;
                }
                
                // Apply responsive typography
                if (configuration.Typography != null)
                {
                    ApplyResponsiveTypography(element, configuration.Typography);
                }
                
                // Apply responsive spacing
                if (configuration.Spacing != null)
                {
                    ApplyAdaptiveSpacing(element, configuration.Spacing);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyResponsiveSettings");
            }
        }

        private void ApplyLayoutProperties(Panel container, LayoutConfiguration layoutConfig)
        {
            try
            {
                if (container is StackPanel stackPanel)
                {
                    if (layoutConfig.Orientation.HasValue)
                        stackPanel.Orientation = layoutConfig.Orientation.Value;
                }
                else if (container is WrapPanel wrapPanel)
                {
                    if (layoutConfig.Orientation.HasValue)
                        wrapPanel.Orientation = layoutConfig.Orientation.Value;
                }
                
                // Apply common properties
                if (layoutConfig.Background != null)
                    container.Background = layoutConfig.Background;
                if (layoutConfig.Margin.HasValue)
                    container.Margin = layoutConfig.Margin.Value;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyLayoutProperties");
            }
        }

        private void ApplyChildArrangements(Panel container, LayoutConfiguration layoutConfig)
        {
            try
            {
                if (layoutConfig.ChildArrangements == null)
                    return;

                for (int i = 0; i < container.Children.Count && i < layoutConfig.ChildArrangements.Count; i++)
                {
                    var child = container.Children[i];
                    var arrangement = layoutConfig.ChildArrangements[i];
                    
                    if (child is FrameworkElement element)
                    {
                        if (arrangement.Visibility.HasValue)
                            element.Visibility = arrangement.Visibility.Value;
                        if (arrangement.HorizontalAlignment.HasValue)
                            element.HorizontalAlignment = arrangement.HorizontalAlignment.Value;
                        if (arrangement.VerticalAlignment.HasValue)
                            element.VerticalAlignment = arrangement.VerticalAlignment.Value;
                        if (arrangement.Margin.HasValue)
                            element.Margin = arrangement.Margin.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ResponsiveDesignService.ApplyChildArrangements");
            }
        }

        private async void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && _responsiveElements.ContainsKey(element))
                {
                    var configuration = _responsiveElements[element];
                    ApplyResponsiveSettings(element, configuration);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ResponsiveDesignService.OnElementSizeChanged");
            }
        }

        private async void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && _responsiveElements.ContainsKey(element))
                {
                    var configuration = _responsiveElements[element];
                    ApplyResponsiveSettings(element, configuration);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ResponsiveDesignService.OnElementLoaded");
            }
        }

        private void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                RemoveResponsive(element);
            }
        }

        private async void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            try
            {
                UpdateCurrentSettings();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ResponsiveDesignService.OnThemeChanged");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _themingService.ThemeChanged -= OnThemeChanged;
                
                foreach (var element in _responsiveElements.Keys.ToList())
                {
                    RemoveResponsive(element);
                }
                
                _responsiveElements.Clear();
                _breakpoints.Clear();
                _disposed = true;
            }
        }
    }
}