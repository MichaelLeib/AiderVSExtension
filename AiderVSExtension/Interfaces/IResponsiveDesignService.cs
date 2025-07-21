using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for responsive design and adaptive UI layouts
    /// </summary>
    public interface IResponsiveDesignService
    {
        /// <summary>
        /// Event fired when responsive state changes
        /// </summary>
        event EventHandler<ResponsiveStateChangedEventArgs> ResponsiveStateChanged;

        /// <summary>
        /// Makes a UI element responsive
        /// </summary>
        /// <param name="element">Element to make responsive</param>
        /// <param name="configuration">Responsive configuration</param>
        void MakeResponsive(FrameworkElement element, ResponsiveConfiguration configuration);

        /// <summary>
        /// Removes responsive behavior from an element
        /// </summary>
        /// <param name="element">Element to remove responsive behavior from</param>
        void RemoveResponsive(FrameworkElement element);

        /// <summary>
        /// Gets the current responsive settings
        /// </summary>
        /// <returns>Current responsive settings</returns>
        ResponsiveUISettings GetCurrentSettings();

        /// <summary>
        /// Updates responsive settings based on current screen size
        /// </summary>
        void UpdateCurrentSettings();

        /// <summary>
        /// Registers a custom breakpoint
        /// </summary>
        /// <param name="name">Breakpoint name</param>
        /// <param name="breakpoint">Breakpoint definition</param>
        void RegisterBreakpoint(string name, ResponsiveBreakpoint breakpoint);

        /// <summary>
        /// Gets the active breakpoint for current screen size
        /// </summary>
        /// <returns>Active breakpoint name</returns>
        string GetActiveBreakpoint();

        /// <summary>
        /// Gets all registered breakpoints
        /// </summary>
        /// <returns>Dictionary of breakpoint names and definitions</returns>
        Dictionary<string, ResponsiveBreakpoint> GetBreakpoints();

        /// <summary>
        /// Applies responsive layout to a container
        /// </summary>
        /// <param name="container">Container to apply layout to</param>
        /// <param name="layout">Layout configuration</param>
        void ApplyResponsiveLayout(Panel container, ResponsiveLayout layout);

        /// <summary>
        /// Creates a responsive grid layout
        /// </summary>
        /// <param name="grid">Grid to make responsive</param>
        /// <param name="configuration">Grid configuration</param>
        void CreateResponsiveGrid(Grid grid, ResponsiveGridConfiguration configuration);

        /// <summary>
        /// Applies responsive typography settings
        /// </summary>
        /// <param name="element">Element to apply typography to</param>
        /// <param name="typography">Typography configuration</param>
        void ApplyResponsiveTypography(FrameworkElement element, ResponsiveTypography typography);

        /// <summary>
        /// Creates adaptive margins and padding
        /// </summary>
        /// <param name="element">Element to apply spacing to</param>
        /// <param name="spacing">Spacing configuration</param>
        void ApplyAdaptiveSpacing(FrameworkElement element, ResponsiveSpacing spacing);
    }
}