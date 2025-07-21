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
    /// Manager for background services and staged initialization
    /// </summary>
    public class BackgroundServiceManager : IBackgroundServiceManager, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IPerformanceMonitoringService _performanceMonitor;
        private readonly ConcurrentDictionary<string, IBackgroundService> _services = new ConcurrentDictionary<string, IBackgroundService>();
        private readonly ConcurrentDictionary<string, BackgroundServiceStatus> _serviceStatuses = new ConcurrentDictionary<string, BackgroundServiceStatus>();
        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _lockObject = new object();
        private InitializationProgress _initializationProgress;
        private bool _disposed = false;

        // Collection size limits to prevent unbounded growth
        private const int MaxServices = 100;

        public event EventHandler<BackgroundServiceEventArgs> ServiceStarted;
        public event EventHandler<BackgroundServiceEventArgs> ServiceStopped;
        public event EventHandler<BackgroundServiceErrorEventArgs> ServiceFailed;
        public event EventHandler<InitializationPhaseEventArgs> PhaseCompleted;

        public BackgroundServiceManager(
            IErrorHandler errorHandler,
            IPerformanceMonitoringService performanceMonitor)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            
            _initializationProgress = new InitializationProgress
            {
                StartTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Starts all background services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task StartAllServicesAsync(CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("StartAllServices", "ServiceManager");
            
            try
            {
                var services = _services.Values.OrderByDescending(s => s.Priority).ToList();
                var startTasks = new List<Task>();

                foreach (var service in services)
                {
                    if (_serviceStatuses.GetValueOrDefault(service.ServiceId) != BackgroundServiceStatus.Running)
                    {
                        startTasks.Add(StartServiceAsync(service, cancellationToken));
                    }
                }

                await Task.WhenAll(startTasks).ConfigureAwait(false);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "BackgroundServiceManager.StartAllServicesAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Stops all background services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task StopAllServicesAsync(CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("StopAllServices", "ServiceManager");
            
            try
            {
                var services = _services.Values.OrderBy(s => s.Priority).ToList(); // Reverse order for shutdown
                var stopTasks = new List<Task>();

                foreach (var service in services)
                {
                    if (_serviceStatuses.GetValueOrDefault(service.ServiceId) == BackgroundServiceStatus.Running)
                    {
                        stopTasks.Add(StopServiceAsync(service, cancellationToken));
                    }
                }

                await Task.WhenAll(stopTasks).ConfigureAwait(false);
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "BackgroundServiceManager.StopAllServicesAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Registers a background service
        /// </summary>
        /// <param name="service">Background service to register</param>
        public void RegisterService(IBackgroundService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            // Enforce collection size limits to prevent unbounded growth
            if (_services.Count >= MaxServices)
            {
                throw new InvalidOperationException($"Cannot register more than {MaxServices} services. Current count: {_services.Count}");
            }

            _services.TryAdd(service.ServiceId, service);
            _serviceStatuses.TryAdd(service.ServiceId, BackgroundServiceStatus.NotStarted);
        }

        /// <summary>
        /// Unregisters a background service
        /// </summary>
        /// <param name="serviceId">ID of service to unregister</param>
        public void UnregisterService(string serviceId)
        {
            if (_services.TryRemove(serviceId, out var service))
            {
                _serviceStatuses.TryRemove(serviceId, out _);
                
                // Stop the service if it's running - use fire-and-forget with proper exception handling
                if (service.Status == BackgroundServiceStatus.Running)
                {
                    _ = StopServiceSafelyAsync(service);
                }
            }
        }

        private async Task StopServiceSafelyAsync(IBackgroundService service)
        {
            try
            {
                await StopServiceAsync(service, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, $"BackgroundServiceManager.StopServiceSafelyAsync.{service.ServiceId}").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the status of a background service
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <returns>Service status</returns>
        public BackgroundServiceStatus GetServiceStatus(string serviceId)
        {
            return _serviceStatuses.GetValueOrDefault(serviceId, BackgroundServiceStatus.NotStarted);
        }

        /// <summary>
        /// Gets status of all background services
        /// </summary>
        /// <returns>Dictionary of service statuses</returns>
        public Dictionary<string, BackgroundServiceStatus> GetAllServiceStatuses()
        {
            return new Dictionary<string, BackgroundServiceStatus>(_serviceStatuses);
        }

        /// <summary>
        /// Restarts a specific background service
        /// </summary>
        /// <param name="serviceId">Service ID to restart</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task RestartServiceAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation($"RestartService-{serviceId}", "ServiceManager");
            
            try
            {
                if (!_services.TryGetValue(serviceId, out var service))
                {
                    throw new ArgumentException($"Service with ID '{serviceId}' not found", nameof(serviceId));
                }

                // Stop the service first
                if (service.Status == BackgroundServiceStatus.Running)
                {
                    await StopServiceAsync(service, cancellationToken).ConfigureAwait(false);
                }

                // Wait a bit for cleanup
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                // Start the service
                await StartServiceAsync(service, cancellationToken).ConfigureAwait(false);
                
                tracker.Complete();
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "BackgroundServiceManager.RestartServiceAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Performs staged initialization
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("StageInitialization", "ServiceManager");
            
            await _initializationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            
            try
            {
                _initializationProgress.IsComplete = false;
                _initializationProgress.HasFailed = false;
                _initializationProgress.StartTime = DateTime.UtcNow;

                var phases = GetInitializationPhases();
                _initializationProgress.TotalPhases = phases.Count;
                _initializationProgress.TotalServices = _services.Count;

                for (int i = 0; i < phases.Count; i++)
                {
                    var phase = phases[i];
                    _initializationProgress.CurrentPhase = phase.Name;
                    _initializationProgress.CurrentPhaseNumber = i + 1;

                    var phaseStartTime = DateTime.UtcNow;
                    var servicesStarted = new List<string>();
                    var servicesFailed = new List<string>();

                    try
                    {
                        await ExecutePhaseAsync(phase, servicesStarted, servicesFailed, cancellationToken).ConfigureAwait(false);
                        
                        var phaseArgs = new InitializationPhaseEventArgs
                        {
                            PhaseName = phase.Name,
                            Duration = DateTime.UtcNow - phaseStartTime,
                            IsSuccessful = servicesFailed.Count == 0,
                            ServicesStarted = servicesStarted,
                            ServicesFailed = servicesFailed
                        };

                        PhaseCompleted?.Invoke(this, phaseArgs);

                        if (servicesFailed.Any())
                        {
                            // Check if any critical services failed
                            var criticalFailures = servicesFailed.Where(id => 
                                _services.TryGetValue(id, out var svc) && svc.IsCritical).ToList();
                            
                            if (criticalFailures.Any())
                            {
                                _initializationProgress.HasFailed = true;
                                _initializationProgress.FailureReasons.AddRange(
                                    criticalFailures.Select(id => $"Critical service '{id}' failed to start"));
                                break;
                            }
                        }

                        _initializationProgress.OverallProgress = ((double)(i + 1) / phases.Count) * 100;
                    }
                    catch (Exception ex)
                    {
                        _initializationProgress.HasFailed = true;
                        _initializationProgress.FailureReasons.Add($"Phase '{phase.Name}' failed: {ex.Message}");
                        await _errorHandler.HandleExceptionAsync(ex, $"BackgroundServiceManager.InitializeAsync.Phase.{phase.Name}").ConfigureAwait(false);
                        break;
                    }
                }

                _initializationProgress.IsComplete = true;
                
                if (_initializationProgress.HasFailed)
                {
                    tracker.Fail(new InvalidOperationException("Initialization failed"));
                }
                else
                {
                    tracker.Complete();
                }
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets initialization progress
        /// </summary>
        /// <returns>Initialization progress information</returns>
        public InitializationProgress GetInitializationProgress()
        {
            return _initializationProgress;
        }

        /// <summary>
        /// Gets background service health information
        /// </summary>
        /// <returns>Service health information</returns>
        public async Task<ServiceHealthInfo> GetServiceHealthAsync()
        {
            using var tracker = _performanceMonitor.StartOperation("GetServiceHealth", "ServiceManager");
            
            try
            {
                var healthInfo = new ServiceHealthInfo
                {
                    OverallStatus = HealthStatus.Healthy,
                    CheckedAt = DateTime.UtcNow
                };

                var healthCheckStart = DateTime.UtcNow;
                var healthTasks = _services.Values.Select(async service =>
                {
                    try
                    {
                        var healthResult = await service.CheckHealthAsync(CancellationToken.None).ConfigureAwait(false);
                        healthInfo.ServiceHealth[service.ServiceId] = healthResult;
                        return healthResult.Status;
                    }
                    catch (Exception ex)
                    {
                        var errorResult = new HealthCheckResult
                        {
                            Status = HealthStatus.Critical,
                            Message = $"Health check failed: {ex.Message}",
                            Exception = ex
                        };
                        healthInfo.ServiceHealth[service.ServiceId] = errorResult;
                        return HealthStatus.Critical;
                    }
                });

                var healthResults = await Task.WhenAll(healthTasks).ConfigureAwait(false);
                healthInfo.CheckDuration = DateTime.UtcNow - healthCheckStart;

                // Determine overall status
                if (healthResults.Any(h => h == HealthStatus.Critical))
                    healthInfo.OverallStatus = HealthStatus.Critical;
                else if (healthResults.Any(h => h == HealthStatus.Unhealthy))
                    healthInfo.OverallStatus = HealthStatus.Unhealthy;
                else if (healthResults.Any(h => h == HealthStatus.Degraded))
                    healthInfo.OverallStatus = HealthStatus.Degraded;

                // Get resource usage
                healthInfo.ResourceUsage = await GetResourceUsageAsync().ConfigureAwait(false);

                // Generate recommendations
                healthInfo.Recommendations = await GenerateHealthRecommendationsAsync(healthInfo).ConfigureAwait(false);

                tracker.Complete();
                return healthInfo;
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "BackgroundServiceManager.GetServiceHealthAsync").ConfigureAwait(false);
                
                return new ServiceHealthInfo
                {
                    OverallStatus = HealthStatus.Critical,
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        #region Private Methods

        private async Task StartServiceAsync(IBackgroundService service, CancellationToken cancellationToken)
        {
            using var tracker = _performanceMonitor.StartOperation($"StartService-{service.ServiceId}", "ServiceManager");
            
            try
            {
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Starting;

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(service.StartupTimeout);

                await service.StartAsync(timeoutCts.Token).ConfigureAwait(false);
                
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Running;

                ServiceStarted?.Invoke(this, new BackgroundServiceEventArgs
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    Status = BackgroundServiceStatus.Running
                });

                tracker.Complete();
            }
            catch (Exception ex)
            {
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Failed;
                
                var errorArgs = new BackgroundServiceErrorEventArgs
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    Status = BackgroundServiceStatus.Failed,
                    Error = ex,
                    ErrorContext = "Service startup",
                    CanRestart = !service.IsCritical
                };

                ServiceFailed?.Invoke(this, errorArgs);
                tracker.Fail(ex);

                if (service.IsCritical)
                {
                    throw new InvalidOperationException($"Critical service '{service.ServiceName}' failed to start", ex);
                }
            }
        }

        private async Task StopServiceAsync(IBackgroundService service, CancellationToken cancellationToken)
        {
            using var tracker = _performanceMonitor.StartOperation($"StopService-{service.ServiceId}", "ServiceManager");
            
            try
            {
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Stopping;

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(service.ShutdownTimeout);

                await service.StopAsync(timeoutCts.Token).ConfigureAwait(false);
                
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Stopped;

                ServiceStopped?.Invoke(this, new BackgroundServiceEventArgs
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    Status = BackgroundServiceStatus.Stopped
                });

                tracker.Complete();
            }
            catch (Exception ex)
            {
                _serviceStatuses[service.ServiceId] = BackgroundServiceStatus.Failed;
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, $"BackgroundServiceManager.StopServiceAsync.{service.ServiceId}").ConfigureAwait(false);
            }
        }

        private List<InitializationPhase> GetInitializationPhases()
        {
            var services = _services.Values.ToList();
            
            return new List<InitializationPhase>
            {
                new InitializationPhase
                {
                    Name = "Critical Services",
                    Services = services.Where(s => s.IsCritical && s.Priority >= 90).ToList()
                },
                new InitializationPhase
                {
                    Name = "Core Services",
                    Services = services.Where(s => s.Priority >= 70 && s.Priority < 90).ToList()
                },
                new InitializationPhase
                {
                    Name = "Standard Services",
                    Services = services.Where(s => s.Priority >= 50 && s.Priority < 70).ToList()
                },
                new InitializationPhase
                {
                    Name = "Background Services",
                    Services = services.Where(s => s.Priority < 50).ToList()
                }
            };
        }

        private async Task ExecutePhaseAsync(InitializationPhase phase, List<string> servicesStarted, List<string> servicesFailed, CancellationToken cancellationToken)
        {
            _initializationProgress.ServicesStarting = phase.Services.Select(s => s.ServiceName).ToList();

            var startTasks = phase.Services.Select(async service =>
            {
                try
                {
                    // Check dependencies
                    if (service.Dependencies.Any())
                    {
                        var unmetDependencies = service.Dependencies
                            .Where(dep => GetServiceStatus(dep) != BackgroundServiceStatus.Running)
                            .ToList();

                        if (unmetDependencies.Any())
                        {
                            throw new InvalidOperationException(
                                $"Service '{service.ServiceName}' has unmet dependencies: {string.Join(", ", unmetDependencies)}");
                        }
                    }

                    await StartServiceAsync(service, cancellationToken).ConfigureAwait(false);
                    servicesStarted.Add(service.ServiceId);
                    _initializationProgress.ServicesCompleted++;
                }
                catch (Exception ex)
                {
                    servicesFailed.Add(service.ServiceId);
                    await _errorHandler.HandleExceptionAsync(ex, $"BackgroundServiceManager.ExecutePhaseAsync.{service.ServiceId}").ConfigureAwait(false);
                }
            });

            await Task.WhenAll(startTasks).ConfigureAwait(false);
            _initializationProgress.ServicesStarting.Clear();
        }

        private async Task<ResourceUsage> GetResourceUsageAsync()
        {
            try
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                
                return new ResourceUsage
                {
                    TotalMemoryUsage = GC.GetTotalMemory(false),
                    WorkingSetSize = currentProcess.WorkingSet64,
                    ThreadCount = currentProcess.Threads.Count,
                    HandleCount = currentProcess.HandleCount,
                    GCPressure = new GCPressure
                    {
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2),
                        HeapSize = GC.GetTotalMemory(false)
                    }
                };
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "BackgroundServiceManager.GetResourceUsageAsync").ConfigureAwait(false);
                return new ResourceUsage();
            }
        }

        private async Task<List<HealthRecommendation>> GenerateHealthRecommendationsAsync(ServiceHealthInfo healthInfo)
        {
            var recommendations = new List<HealthRecommendation>();

            // Check for failed services
            var failedServices = healthInfo.ServiceHealth
                .Where(kvp => kvp.Value.Status == HealthStatus.Critical || kvp.Value.Status == HealthStatus.Unhealthy)
                .ToList();

            foreach (var failedService in failedServices)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"Service Health Issue: {failedService.Key}",
                    Description = $"Service '{failedService.Key}' is in {failedService.Value.Status} state",
                    Severity = failedService.Value.Status == HealthStatus.Critical ? 
                        RecommendationSeverity.Critical : RecommendationSeverity.High,
                    AffectedService = failedService.Key,
                    RecommendedActions = new List<string>
                    {
                        "Restart the service",
                        "Check service logs for errors",
                        "Verify service dependencies",
                        "Check system resources"
                    },
                    Priority = RecommendationPriority.High
                });
            }

            // Check for high memory usage
            if (healthInfo.ResourceUsage?.TotalMemoryUsage > 1024 * 1024 * 1024) // 1GB
            {
                recommendations.Add(new HealthRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "High Memory Usage",
                    Description = $"Total memory usage is {healthInfo.ResourceUsage.TotalMemoryUsage / (1024 * 1024):F0}MB",
                    Severity = RecommendationSeverity.Medium,
                    RecommendedActions = new List<string>
                    {
                        "Review memory-intensive services",
                        "Force garbage collection",
                        "Consider restarting services",
                        "Monitor for memory leaks"
                    },
                    Priority = RecommendationPriority.Medium
                });
            }

            return recommendations;
        }

        #endregion

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
                    // Stop all services first
                    try
                    {
                        var stopTask = StopAllServicesAsync(CancellationToken.None);
                        if (!stopTask.Wait(TimeSpan.FromSeconds(30)))
                        {
                            // If timeout, continue with disposal
                        }
                    }
                    catch
                    {
                        // Ignore exceptions during disposal
                    }

                    // Dispose registered services that implement IDisposable
                    foreach (var service in _services.Values.ToList())
                    {
                        try
                        {
                            if (service is IDisposable disposableService)
                            {
                                disposableService.Dispose();
                            }
                        }
                        catch
                        {
                            // Ignore exceptions during disposal
                        }
                    }

                    // Clear collections
                    _services.Clear();
                    _serviceStatuses.Clear();

                    // Clear event handlers to prevent memory leaks
                    ServiceStarted = null;
                    ServiceStopped = null;
                    ServiceFailed = null;
                    PhaseCompleted = null;

                    // Dispose semaphore
                    _initializationSemaphore?.Dispose();

                    _disposed = true;
                }
            }
        }

        ~BackgroundServiceManager()
        {
            Dispose(false);
        }
    }

    #region Helper Classes

    internal class InitializationPhase
    {
        public string Name { get; set; }
        public List<IBackgroundService> Services { get; set; } = new List<IBackgroundService>();
    }

    #endregion
}