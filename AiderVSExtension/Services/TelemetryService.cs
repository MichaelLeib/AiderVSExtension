using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for collecting and reporting telemetry data
    /// </summary>
    public class TelemetryService : ITelemetryService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly ConcurrentQueue<TelemetryEvent> _eventQueue;
        private readonly ConcurrentDictionary<string, PerformanceCounter> _performanceCounters;
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore;
        private bool _disposed = false;

        // Configuration
        private readonly int _maxQueueSize;
        private readonly TimeSpan _flushInterval;
        private readonly bool _enableTelemetry;
        
        // Memory optimization
        private readonly Timer _performanceCounterCleanupTimer;
        private readonly object _counterCleanupLock = new object();
        private volatile bool _isHighVolumeMode = false;
        private int _eventsSinceLastCheck = 0;
        private DateTime _lastVolumeCheck = DateTime.UtcNow;

        public TelemetryService(IErrorHandler errorHandler, IConfigurationService configurationService = null)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _eventQueue = new ConcurrentQueue<TelemetryEvent>();
            _performanceCounters = new ConcurrentDictionary<string, PerformanceCounter>();
            _flushSemaphore = new SemaphoreSlim(1, 1);

            // Load configuration
            _maxQueueSize = configurationService?.GetValue("Telemetry.MaxQueueSize", 1000) ?? 1000;
            _flushInterval = TimeSpan.FromSeconds(configurationService?.GetValue("Telemetry.FlushIntervalSeconds", 30) ?? 30);
            _enableTelemetry = configurationService?.GetValue("Telemetry.Enabled", true) ?? true;

            // Start flush timer
            _flushTimer = new Timer(FlushTelemetryAsync, null, _flushInterval, _flushInterval);
            
            // Start performance counter cleanup timer (every hour)
            _performanceCounterCleanupTimer = new Timer(CleanupPerformanceCounters, null, 
                TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        /// <summary>
        /// Tracks an event with optional properties and metrics
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, string> properties = null, Dictionary<string, double> metrics = null)
        {
            if (!_enableTelemetry || _disposed)
                return;

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));

            var telemetryEvent = new TelemetryEvent
            {
                EventName = eventName,
                Timestamp = DateTime.UtcNow,
                SessionId = GetSessionId(),
                Properties = properties ?? new Dictionary<string, string>(),
                Metrics = metrics ?? new Dictionary<string, double>()
            };

            // Check for high volume mode and apply sampling
            Interlocked.Increment(ref _eventsSinceLastCheck);
            CheckForHighVolumeMode();
            
            if (_isHighVolumeMode && ShouldSampleEvent(eventName))
            {
                return; // Skip this event due to sampling
            }

            // Add common properties
            AddCommonProperties(telemetryEvent.Properties);

            // Check queue size before enqueueing
            if (_eventQueue.Count >= _maxQueueSize * 2)
            {
                // Emergency mode: drop events to prevent memory overflow
                return;
            }

            _eventQueue.Enqueue(telemetryEvent);

            // Check queue size and flush if necessary
            if (_eventQueue.Count >= _maxQueueSize)
            {
                _ = Task.Run(async () => await FlushTelemetryAsync(null));
            }
        }

        /// <summary>
        /// Tracks an exception with context information
        /// </summary>
        public void TrackException(Exception exception, Dictionary<string, string> properties = null)
        {
            if (!_enableTelemetry || _disposed || exception == null)
                return;

            var exceptionProperties = properties ?? new Dictionary<string, string>();
            exceptionProperties["Exception.Type"] = exception.GetType().Name;
            exceptionProperties["Exception.Message"] = exception.Message;
            exceptionProperties["Exception.StackTrace"] = exception.StackTrace ?? "";

            if (exception.InnerException != null)
            {
                exceptionProperties["InnerException.Type"] = exception.InnerException.GetType().Name;
                exceptionProperties["InnerException.Message"] = exception.InnerException.Message;
            }

            TrackEvent("Exception", exceptionProperties, new Dictionary<string, double>
            {
                ["Exception.Severity"] = GetExceptionSeverity(exception)
            });
        }

        /// <summary>
        /// Tracks performance metrics for an operation
        /// </summary>
        public void TrackPerformance(string operationName, TimeSpan duration, bool success = true, Dictionary<string, string> properties = null)
        {
            if (!_enableTelemetry || _disposed)
                return;

            var performanceProperties = properties ?? new Dictionary<string, string>();
            performanceProperties["Operation.Success"] = success.ToString();

            TrackEvent($"Performance.{operationName}", performanceProperties, new Dictionary<string, double>
            {
                ["Duration.Milliseconds"] = duration.TotalMilliseconds,
                ["Duration.Seconds"] = duration.TotalSeconds
            });

            // Update performance counter
            var counterKey = $"Performance.{operationName}";
            _performanceCounters.AddOrUpdate(counterKey,
                new PerformanceCounter { Count = 1, TotalDuration = duration, SuccessCount = success ? 1 : 0 },
                (key, existing) => new PerformanceCounter
                {
                    Count = existing.Count + 1,
                    TotalDuration = existing.TotalDuration + duration,
                    SuccessCount = existing.SuccessCount + (success ? 1 : 0)
                });
        }

        /// <summary>
        /// Tracks user interaction events
        /// </summary>
        public void TrackUserAction(string action, string component, Dictionary<string, string> properties = null)
        {
            if (!_enableTelemetry || _disposed)
                return;

            var actionProperties = properties ?? new Dictionary<string, string>();
            actionProperties["User.Action"] = action;
            actionProperties["UI.Component"] = component;

            TrackEvent("UserAction", actionProperties);
        }

        /// <summary>
        /// Tracks AI service usage
        /// </summary>
        public void TrackAIUsage(AIProvider provider, string operation, TimeSpan responseTime, bool success, int tokenCount = 0)
        {
            if (!_enableTelemetry || _disposed)
                return;

            TrackEvent("AI.Usage", new Dictionary<string, string>
            {
                ["AI.Provider"] = provider.ToString(),
                ["AI.Operation"] = operation,
                ["AI.Success"] = success.ToString()
            }, new Dictionary<string, double>
            {
                ["AI.ResponseTime.Milliseconds"] = responseTime.TotalMilliseconds,
                ["AI.TokenCount"] = tokenCount
            });
        }

        /// <summary>
        /// Gets performance summary for a specific operation
        /// </summary>
        public PerformanceSummary GetPerformanceSummary(string operationName)
        {
            var counterKey = $"Performance.{operationName}";
            if (_performanceCounters.TryGetValue(counterKey, out var counter))
            {
                return new PerformanceSummary
                {
                    OperationName = operationName,
                    TotalCount = counter.Count,
                    SuccessCount = counter.SuccessCount,
                    FailureCount = counter.Count - counter.SuccessCount,
                    AverageDuration = counter.Count > 0 ? counter.TotalDuration.Divide(counter.Count) : TimeSpan.Zero,
                    TotalDuration = counter.TotalDuration,
                    SuccessRate = counter.Count > 0 ? (double)counter.SuccessCount / counter.Count : 0.0
                };
            }

            return new PerformanceSummary { OperationName = operationName };
        }

        /// <summary>
        /// Flushes all pending telemetry events
        /// </summary>
        public async Task FlushAsync()
        {
            if (_disposed)
                return;

            await FlushTelemetryAsync(null).ConfigureAwait(false);
        }

        private async void FlushTelemetryAsync(object state)
        {
            if (_disposed || !await _flushSemaphore.WaitAsync(100))
                return;

            try
            {
                var events = new List<TelemetryEvent>();
                
                // Dequeue all events
                while (_eventQueue.TryDequeue(out var telemetryEvent) && events.Count < _maxQueueSize)
                {
                    events.Add(telemetryEvent);
                }

                if (events.Count > 0)
                {
                    // In a real implementation, you would send these to a telemetry service
                    // For now, we'll just log them locally
                    await LogTelemetryEvents(events);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler?.LogWarningAsync($"Failed to flush telemetry: {ex.Message}", "TelemetryService.FlushTelemetryAsync").ConfigureAwait(false);
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        private async Task LogTelemetryEvents(List<TelemetryEvent> events)
        {
            try
            {
                // Group events by type for summary logging
                var eventGroups = events.GroupBy(e => e.EventName).ToList();
                
                foreach (var group in eventGroups)
                {
                    var eventType = group.Key;
                    var count = group.Count();
                    var sample = group.First();
                    
                    // Use StringBuilder for efficient string construction
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"Telemetry: {eventType} ({count} events) - Sample: ");
                    
                    var sampleProps = sample.Properties.Take(3).ToList();
                    for (int i = 0; i < sampleProps.Count; i++)
                    {
                        var prop = sampleProps[i];
                        sb.Append($"{prop.Key}={prop.Value}");
                        if (i < sampleProps.Count - 1)
                            sb.Append(", ");
                    }
                    
                    await _errorHandler?.LogInfoAsync(sb.ToString(), "TelemetryService").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log telemetry events: {ex.Message}");
            }
        }

        private void AddCommonProperties(Dictionary<string, string> properties)
        {
            properties["VS.Version"] = GetVSVersion();
            properties["Extension.Version"] = GetExtensionVersion();
            properties["OS.Version"] = Environment.OSVersion.ToString();
            properties["CLR.Version"] = Environment.Version.ToString();
            properties["Machine.Name"] = Environment.MachineName;
            properties["User.Name"] = Environment.UserName;
            properties["Timestamp.UTC"] = DateTime.UtcNow.ToString("O");
        }

        private string GetSessionId()
        {
            // Generate or retrieve session ID - for now, use a simple approach
            return Process.GetCurrentProcess().Id.ToString();
        }

        private string GetVSVersion()
        {
            try
            {
                // Use async pattern properly - don't block with JoinableTaskFactory.Run in library code
                var task = GetVSVersionAsync();
                return task.GetAwaiter().GetResult();
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<string> GetVSVersionAsync()
        {
            try
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var shell = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShell)) as Microsoft.VisualStudio.Shell.Interop.IVsShell;
                if (shell != null)
                {
                    shell.GetProperty((int)Microsoft.VisualStudio.Shell.Interop.__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionObj);
                    return versionObj?.ToString() ?? "Unknown";
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetExtensionVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private double GetExceptionSeverity(Exception exception)
        {
            return exception switch
            {
                OutOfMemoryException => 5.0,
                StackOverflowException => 5.0,
                AccessViolationException => 5.0,
                NullReferenceException => 4.0,
                ArgumentException => 2.0,
                InvalidOperationException => 3.0,
                NotImplementedException => 1.0,
                _ => 3.0
            };
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _flushTimer?.Dispose();
                    _performanceCounterCleanupTimer?.Dispose();
                    
                    // Flush remaining events synchronously during disposal
                    try
                    {
                        // Use GetAwaiter().GetResult() for disposal - this is acceptable in dispose
                        FlushAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // Ignore errors during disposal
                    }
                    
                    _flushSemaphore?.Dispose();
                    
                    // Clear performance counters
                    _performanceCounters.Clear();
                }

                _disposed = true;
            }
        }

        #region Memory Optimization

        private void CheckForHighVolumeMode()
        {
            var now = DateTime.UtcNow;
            if (now - _lastVolumeCheck >= TimeSpan.FromMinutes(1))
            {
                var eventsPerMinute = _eventsSinceLastCheck;
                _isHighVolumeMode = eventsPerMinute > 1000; // Switch to sampling if >1000 events/minute
                
                Interlocked.Exchange(ref _eventsSinceLastCheck, 0);
                _lastVolumeCheck = now;
            }
        }

        private bool ShouldSampleEvent(string eventName)
        {
            // Sample based on event type and hash
            var hash = eventName.GetHashCode();
            
            // Keep 10% of events during high volume
            return (hash % 10) != 0;
        }

        private void CleanupPerformanceCounters(object state)
        {
            if (_disposed) return;

            try
            {
                lock (_counterCleanupLock)
                {
                    if (_disposed) return;

                    // Remove counters that haven't been updated in the last hour
                    var cutoffTime = DateTime.UtcNow.AddHours(-1);
                    var keysToRemove = new List<string>();

                    // Since PerformanceCounter doesn't have timestamp, we'll clean based on low usage
                    foreach (var kvp in _performanceCounters)
                    {
                        var counter = kvp.Value;
                        // Remove counters with very low activity (less than 5 operations)
                        if (counter.Count < 5)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }

                    // Limit the number of performance counters to prevent unbounded growth
                    if (_performanceCounters.Count > 1000)
                    {
                        // Keep only the most active counters
                        var sortedCounters = _performanceCounters
                            .OrderByDescending(kvp => kvp.Value.Count)
                            .Take(500)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        _performanceCounters.Clear();
                        foreach (var kvp in sortedCounters)
                        {
                            _performanceCounters[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        // Remove low-activity counters
                        foreach (var key in keysToRemove)
                        {
                            _performanceCounters.TryRemove(key, out _);
                        }
                    }

                    // Force garbage collection if we cleaned up significant data
                    if (keysToRemove.Count > 50)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors to prevent timer issues
            }
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Represents a telemetry event
    /// </summary>
    public class TelemetryEvent
    {
        public string EventName { get; set; }
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
    }

    /// <summary>
    /// Performance counter for tracking operation metrics
    /// </summary>
    public class PerformanceCounter
    {
        public int Count { get; set; }
        public int SuccessCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// Performance summary for an operation
    /// </summary>
    public class PerformanceSummary
    {
        public string OperationName { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double SuccessRate { get; set; }
    }
}