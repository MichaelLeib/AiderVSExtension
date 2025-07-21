using System;
using System.Threading.Tasks;
using System.Windows;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for context-aware UI updates and adaptive interface behavior
    /// </summary>
    public interface IContextAwareUIService
    {
        /// <summary>
        /// Event fired when UI context changes
        /// </summary>
        event EventHandler<ContextChangedEventArgs> ContextChanged;

        /// <summary>
        /// Event fired when UI is updated
        /// </summary>
        event EventHandler<UIUpdateEventArgs> UIUpdated;

        /// <summary>
        /// Registers a UI element to be context-aware
        /// </summary>
        /// <param name="element">Element to register</param>
        /// <param name="configuration">Context configuration</param>
        void RegisterContextAwareElement(FrameworkElement element, ContextAwareConfiguration configuration);

        /// <summary>
        /// Unregisters a UI element from context-aware updates
        /// </summary>
        /// <param name="element">Element to unregister</param>
        void UnregisterContextAwareElement(FrameworkElement element);

        /// <summary>
        /// Updates the current UI context
        /// </summary>
        /// <param name="contextInfo">Context information</param>
        Task UpdateContextAsync(ContextInfo contextInfo);

        /// <summary>
        /// Gets the current UI context
        /// </summary>
        /// <returns>Current context</returns>
        UIContext GetCurrentContext();

        /// <summary>
        /// Updates a specific element based on current context
        /// </summary>
        /// <param name="element">Element to update</param>
        Task UpdateElementAsync(FrameworkElement element);

        /// <summary>
        /// Forces an update of all context-aware elements
        /// </summary>
        Task UpdateAllElementsAsync();

        /// <summary>
        /// Adds a custom context provider
        /// </summary>
        /// <param name="name">Provider name</param>
        /// <param name="provider">Context provider</param>
        void AddContextProvider(string name, IContextProvider provider);

        /// <summary>
        /// Creates a context snapshot for debugging
        /// </summary>
        /// <returns>Context snapshot</returns>
        ContextSnapshot CreateSnapshot();
    }

    /// <summary>
    /// Interface for custom context providers
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>
        /// Provider name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets context information
        /// </summary>
        /// <returns>Context information</returns>
        Task<ContextInfo> GetContextAsync();

        /// <summary>
        /// Whether the provider is available
        /// </summary>
        bool IsAvailable { get; }
    }
}