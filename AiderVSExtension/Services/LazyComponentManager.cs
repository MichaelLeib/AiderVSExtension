using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Manager for lazy-loaded components
    /// </summary>
    public class LazyComponentManager : IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IPerformanceMonitoringService _performanceMonitor;
        private readonly ConcurrentDictionary<string, ILazyComponentInternal> _components = new ConcurrentDictionary<string, ILazyComponentInternal>();
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        // Collection size limits to prevent unbounded growth
        private const int MaxComponents = 100;

        public LazyComponentManager(
            IErrorHandler errorHandler,
            IPerformanceMonitoringService performanceMonitor)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            
            // Cleanup unused components every 30 minutes
            _cleanupTimer = new Timer(CleanupUnusedComponents, null, 
                TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// Creates a lazy component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="factory">Factory function to create the component</param>
        /// <param name="strategy">Loading strategy</param>
        /// <param name="priority">Loading priority</param>
        /// <returns>Lazy component</returns>
        public ILazyComponent<T> CreateLazyComponent<T>(
            Func<Task<T>> factory,
            LazyLoadingStrategy strategy = LazyLoadingStrategy.OnDemand,
            int priority = 50) where T : class
        {
            // Enforce collection size limits before adding new component
            if (_components.Count >= MaxComponents)
            {
                // Remove oldest unused components (20% of collection)
                var componentsToRemove = _components.Values
                    .Where(c => !c.IsLoaded)
                    .OrderBy(c => c.LastAccessTime)
                    .Take(MaxComponents / 5)
                    .ToList();

                // If no unloaded components, remove oldest loaded components
                if (!componentsToRemove.Any())
                {
                    componentsToRemove = _components.Values
                        .OrderBy(c => c.LastAccessTime)
                        .Take(MaxComponents / 5)
                        .ToList();
                }

                foreach (var componentToRemove in componentsToRemove)
                {
                    if (_components.TryRemove(componentToRemove.Id, out var removed))
                    {
                        try
                        {
                            removed.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _ = _errorHandler.HandleExceptionAsync(ex, $"LazyComponentManager.CreateLazyComponent.RemoveComponent.{componentToRemove.Id}");
                        }
                    }
                }
            }

            var component = new LazyComponent<T>(factory, strategy, priority, _performanceMonitor, _errorHandler);
            _components.TryAdd(component.Id, component);
            return component;
        }

        /// <summary>
        /// Preloads components based on priority and strategy
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task PreloadComponentsAsync(CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("PreloadComponents", "LazyLoading");
            
            try
            {
                var componentsToPreload = _components.Values
                    .Where(c => c.LoadingStrategy == LazyLoadingStrategy.Background ||
                               c.LoadingStrategy == LazyLoadingStrategy.Immediate)
                    .OrderByDescending(c => c.LoadPriority)
                    .ToList();

                var preloadTasks = componentsToPreload.Select(async component =>
                {
                    try
                    {
                        await component.PreloadAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _errorHandler.HandleExceptionAsync(ex, $"LazyComponentManager.PreloadComponentsAsync.{component.Id}").ConfigureAwait(false);
                    }
                });

                await Task.WhenAll(preloadTasks).ConfigureAwait(false);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "LazyComponentManager.PreloadComponentsAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unloads all components to free memory
        /// </summary>
        public async Task UnloadAllComponentsAsync()
        {
            using var tracker = _performanceMonitor.StartOperation("UnloadAllComponents", "LazyLoading");
            
            try
            {
                var unloadTasks = _components.Values.Select(async component =>
                {
                    try
                    {
                        await component.UnloadAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _errorHandler.HandleExceptionAsync(ex, $"LazyComponentManager.UnloadAllComponentsAsync.{component.Id}").ConfigureAwait(false);
                    }
                });

                await Task.WhenAll(unloadTasks).ConfigureAwait(false);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "LazyComponentManager.UnloadAllComponentsAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets loading statistics for all components
        /// </summary>
        /// <returns>Loading statistics</returns>
        public LazyLoadingStatistics GetLoadingStatistics()
        {
            var components = _components.Values.ToList();
            
            return new LazyLoadingStatistics
            {
                TotalComponents = components.Count,
                LoadedComponents = components.Count(c => c.IsLoaded),
                UnloadedComponents = components.Count(c => !c.IsLoaded),
                ComponentsByStrategy = components.GroupBy(c => c.LoadingStrategy)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageLoadTime = components.Where(c => c.IsLoaded)
                    .Select(c => c.GetLoadingMetrics().LoadingDuration)
                    .DefaultIfEmpty()
                    .Average(ts => ts.TotalMilliseconds),
                TotalMemoryUsage = components.Where(c => c.IsLoaded)
                    .Sum(c => c.GetLoadingMetrics().MemoryUsage),
                FailedComponents = components.Count(c => c.GetLoadingMetrics().HasLoadingFailed),
                SuccessRate = components.Any() ? 
                    (double)components.Count(c => !c.GetLoadingMetrics().HasLoadingFailed) / components.Count * 100 : 100
            };
        }

        private void CleanupUnusedComponents(object state)
        {
            // Use fire-and-forget with proper exception handling
            _ = CleanupUnusedComponentsAsync();
        }

        private async Task CleanupUnusedComponentsAsync()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-60); // Unload components not used in last hour
                var componentsToUnload = _components.Values
                    .Where(c => c.IsLoaded && c.LastAccessTime < cutoffTime)
                    .ToList();

                foreach (var component in componentsToUnload)
                {
                    try
                    {
                        await component.UnloadAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _errorHandler.HandleExceptionAsync(ex, $"LazyComponentManager.CleanupUnusedComponents.{component.Id}").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "LazyComponentManager.CleanupUnusedComponents").ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lockObject)
            {
                if (!_disposed && disposing)
                {
                    // Dispose timer first to stop background operations
                    _cleanupTimer?.Dispose();
                    
                    // Use synchronous disposal pattern to avoid deadlocks
                    try
                    {
                        // Try to unload components synchronously with timeout
                        var unloadTask = UnloadAllComponentsAsync();
                        if (!unloadTask.Wait(TimeSpan.FromSeconds(10)))
                        {
                            // If timeout, continue with disposal
                        }
                    }
                    catch
                    {
                        // Ignore exceptions during disposal
                    }
                    
                    // Dispose all components
                    foreach (var component in _components.Values)
                    {
                        try
                        {
                            component.Dispose();
                        }
                        catch
                        {
                            // Ignore exceptions during disposal
                        }
                    }
                    _components.Clear();
                    
                    _disposed = true;
                }
            }
        }

        ~LazyComponentManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Lazy loading statistics
    /// </summary>
    public class LazyLoadingStatistics
    {
        public int TotalComponents { get; set; }
        public int LoadedComponents { get; set; }
        public int UnloadedComponents { get; set; }
        public Dictionary<LazyLoadingStrategy, int> ComponentsByStrategy { get; set; } = new Dictionary<LazyLoadingStrategy, int>();
        public double AverageLoadTime { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int FailedComponents { get; set; }
        public double SuccessRate { get; set; }
    }

    #region Internal Interfaces and Implementation

    internal interface ILazyComponentInternal : IDisposable
    {
        string Id { get; }
        LazyLoadingStrategy LoadingStrategy { get; }
        int LoadPriority { get; }
        bool IsLoaded { get; }
        DateTime LastAccessTime { get; }
        Task PreloadAsync(CancellationToken cancellationToken = default);
        Task UnloadAsync();
        ComponentLoadingMetrics GetLoadingMetrics();
    }

    internal class LazyComponent<T> : ILazyComponent<T>, ILazyComponentInternal where T : class
    {
        private readonly Func<Task<T>> _factory;
        private readonly IPerformanceMonitoringService _performanceMonitor;
        private readonly IErrorHandler _errorHandler;
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        private readonly ComponentLoadingMetrics _metrics = new ComponentLoadingMetrics();
        
        private T _value;
        private bool _disposed = false;

        public string Id { get; } = Guid.NewGuid().ToString();
        public LazyLoadingStrategy LoadingStrategy { get; }
        public int LoadPriority { get; }
        public bool IsLoaded => _value != null;
        public DateTime LastAccessTime { get; private set; } = DateTime.UtcNow;

        public LazyComponent(
            Func<Task<T>> factory,
            LazyLoadingStrategy strategy,
            int priority,
            IPerformanceMonitoringService performanceMonitor,
            IErrorHandler errorHandler)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            LoadingStrategy = strategy;
            LoadPriority = priority;
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public async Task<T> GetValueAsync()
        {
            LastAccessTime = DateTime.UtcNow;
            
            if (_value != null)
            {
                return _value;
            }

            await _loadSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_value != null) // Double-check pattern
                {
                    return _value;
                }

                using var tracker = _performanceMonitor.StartOperation($"LoadComponent-{typeof(T).Name}", "LazyLoading");
                tracker.AddMetadata("ComponentId", Id);
                tracker.AddMetadata("LoadingStrategy", LoadingStrategy.ToString());
                
                try
                {
                    var startTime = DateTime.UtcNow;
                    var startMemory = GC.GetTotalMemory(false);

                    _value = await _factory().ConfigureAwait(false);

                    var endTime = DateTime.UtcNow;
                    var endMemory = GC.GetTotalMemory(false);

                    _metrics.LoadingDuration = endTime - startTime;
                    _metrics.MemoryUsage = endMemory - startMemory;
                    _metrics.LoadCount++;
                    _metrics.LastLoadTime = endTime;
                    _metrics.AverageLoadingTime = TimeSpan.FromMilliseconds(
                        (_metrics.AverageLoadingTime.TotalMilliseconds * (_metrics.LoadCount - 1) + _metrics.LoadingDuration.TotalMilliseconds) / _metrics.LoadCount);
                    _metrics.HasLoadingFailed = false;
                    _metrics.SuccessRate = (double)(_metrics.LoadCount - (_metrics.HasLoadingFailed ? 1 : 0)) / _metrics.LoadCount * 100;

                    tracker.AddMetadata("LoadingDuration", _metrics.LoadingDuration.TotalMilliseconds);
                    tracker.AddMetadata("MemoryUsage", _metrics.MemoryUsage);
                    tracker.Complete();

                    return _value;
                }
                catch (Exception ex)
                {
                    _metrics.HasLoadingFailed = true;
                    _metrics.LastLoadingError = ex;
                    _metrics.SuccessRate = (double)_metrics.LoadCount / (_metrics.LoadCount + 1) * 100;
                    
                    tracker.Fail(ex);
                    await _errorHandler.HandleExceptionAsync(ex, $"LazyComponent.GetValueAsync.{typeof(T).Name}").ConfigureAwait(false);
                    throw;
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public async Task PreloadAsync(CancellationToken cancellationToken = default)
        {
            if (!IsLoaded && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await GetValueAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, $"LazyComponent.PreloadAsync.{typeof(T).Name}").ConfigureAwait(false);
                }
            }
        }

        public async Task UnloadAsync()
        {
            await _loadSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_value != null)
                {
                    if (_value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    _value = null;
                    _metrics.UnloadCount++;
                    _metrics.LastUnloadTime = DateTime.UtcNow;
                    
                    // Force garbage collection to free memory
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public ComponentLoadingMetrics GetLoadingMetrics()
        {
            return _metrics;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Use synchronous disposal pattern to avoid deadlocks
                try
                {
                    var unloadTask = UnloadAsync();
                    if (!unloadTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        // If timeout, continue with disposal
                    }
                }
                catch
                {
                    // Ignore exceptions during disposal
                }
                
                _loadSemaphore?.Dispose();
                _disposed = true;
            }
        }

        ~LazyComponent()
        {
            Dispose(false);
        }
    }

    #endregion
}