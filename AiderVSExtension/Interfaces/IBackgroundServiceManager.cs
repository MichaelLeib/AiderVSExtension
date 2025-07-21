using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing background services and initialization
    /// </summary>
    public interface IBackgroundServiceManager
    {
        /// <summary>
        /// Event fired when a background service starts
        /// </summary>
        event EventHandler<BackgroundServiceEventArgs> ServiceStarted;

        /// <summary>
        /// Event fired when a background service stops
        /// </summary>
        event EventHandler<BackgroundServiceEventArgs> ServiceStopped;

        /// <summary>
        /// Event fired when a background service fails
        /// </summary>
        event EventHandler<BackgroundServiceErrorEventArgs> ServiceFailed;

        /// <summary>
        /// Event fired when initialization phase completes
        /// </summary>
        event EventHandler<InitializationPhaseEventArgs> PhaseCompleted;

        /// <summary>
        /// Starts all background services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAllServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops all background services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StopAllServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a background service
        /// </summary>
        /// <param name="service">Background service to register</param>
        void RegisterService(IBackgroundService service);

        /// <summary>
        /// Unregisters a background service
        /// </summary>
        /// <param name="serviceId">ID of service to unregister</param>
        void UnregisterService(string serviceId);

        /// <summary>
        /// Gets the status of a background service
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <returns>Service status</returns>
        BackgroundServiceStatus GetServiceStatus(string serviceId);

        /// <summary>
        /// Gets status of all background services
        /// </summary>
        /// <returns>Dictionary of service statuses</returns>
        Dictionary<string, BackgroundServiceStatus> GetAllServiceStatuses();

        /// <summary>
        /// Restarts a specific background service
        /// </summary>
        /// <param name="serviceId">Service ID to restart</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RestartServiceAsync(string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs staged initialization
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets initialization progress
        /// </summary>
        /// <returns>Initialization progress information</returns>
        InitializationProgress GetInitializationProgress();

        /// <summary>
        /// Gets background service health information
        /// </summary>
        /// <returns>Service health information</returns>
        Task<ServiceHealthInfo> GetServiceHealthAsync();
    }

    /// <summary>
    /// Interface for background services
    /// </summary>
    public interface IBackgroundService
    {
        /// <summary>
        /// Service ID
        /// </summary>
        string ServiceId { get; }

        /// <summary>
        /// Service name
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Service priority (higher numbers start first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether the service is critical (startup will fail if critical service fails)
        /// </summary>
        bool IsCritical { get; }

        /// <summary>
        /// Service dependencies (services that must start before this one)
        /// </summary>
        List<string> Dependencies { get; }

        /// <summary>
        /// Current service status
        /// </summary>
        BackgroundServiceStatus Status { get; }

        /// <summary>
        /// Service startup timeout
        /// </summary>
        TimeSpan StartupTimeout { get; }

        /// <summary>
        /// Service shutdown timeout
        /// </summary>
        TimeSpan ShutdownTimeout { get; }

        /// <summary>
        /// Starts the background service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the background service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs health check
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets service metrics
        /// </summary>
        /// <returns>Service metrics</returns>
        Task<ServiceMetrics> GetMetricsAsync();
    }

    /// <summary>
    /// Interface for lazy-loaded components
    /// </summary>
    public interface ILazyComponent<T> where T : class
    {
        /// <summary>
        /// Whether the component is loaded
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Component loading priority
        /// </summary>
        int LoadPriority { get; }

        /// <summary>
        /// Component loading strategy
        /// </summary>
        LazyLoadingStrategy LoadingStrategy { get; }

        /// <summary>
        /// Gets the component value (loads if not already loaded)
        /// </summary>
        /// <returns>Component instance</returns>
        Task<T> GetValueAsync();

        /// <summary>
        /// Preloads the component
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PreloadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads the component to free memory
        /// </summary>
        Task UnloadAsync();

        /// <summary>
        /// Gets component loading metrics
        /// </summary>
        /// <returns>Loading metrics</returns>
        ComponentLoadingMetrics GetLoadingMetrics();
    }
}