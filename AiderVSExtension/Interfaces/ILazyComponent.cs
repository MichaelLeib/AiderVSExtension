using System;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for components that support lazy initialization
    /// </summary>
    public interface ILazyComponent
    {
        /// <summary>
        /// Gets a value indicating whether the component has been initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets a value indicating whether the component is currently initializing
        /// </summary>
        bool IsInitializing { get; }

        /// <summary>
        /// Initializes the component asynchronously
        /// </summary>
        /// <returns>A task representing the initialization operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Ensures the component is initialized, initializing it if necessary
        /// </summary>
        /// <returns>A task representing the ensure operation</returns>
        Task EnsureInitializedAsync();

        /// <summary>
        /// Resets the component to an uninitialized state
        /// </summary>
        Task ResetAsync();

        /// <summary>
        /// Event raised when the component starts initializing
        /// </summary>
        event EventHandler InitializationStarted;

        /// <summary>
        /// Event raised when the component completes initialization
        /// </summary>
        event EventHandler InitializationCompleted;

        /// <summary>
        /// Event raised when initialization fails
        /// </summary>
        event EventHandler<Exception> InitializationFailed;
    }
}
