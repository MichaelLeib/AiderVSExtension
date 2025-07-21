using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Security;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for monitoring performance and telemetry
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
        private readonly ConcurrentDictionary<string, PerformanceThreshold> _thresholds = new ConcurrentDictionary<string, PerformanceThreshold>();
        private readonly ConcurrentDictionary<string, PerformanceTracker> _activeTrackers = new ConcurrentDictionary<string, PerformanceTracker>();
        private readonly Timer _cleanupTimer;
        private readonly Timer _healthCheckTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly Process _currentProcess;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        // Collection size limits to prevent unbounded growth
        private const int MaxMetrics = 10000;
        private const int MaxActiveTrackers = 1000;
        private const int MaxThresholds = 100;

        public event EventHandler<PerformanceMetricsEventArgs> MetricsCollected;
        public event EventHandler<PerformanceThresholdEventArgs> ThresholdExceeded;

        public PerformanceMonitoringService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _currentProcess = Process.GetCurrentProcess();
            
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
            catch (Exception ex)
            {
                // CPU counter not available on all systems
                _errorHandler?.HandleExceptionAsync(ex, "PerformanceMonitoringService.Constructor");
            }

            // Cleanup old metrics every 5 minutes
            _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            // Health check every minute
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            
            InitializeDefaultThresholds();
        }

        /// <summary>
        /// Starts monitoring a performance operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="category">Operation category</param>
        /// <returns>Performance tracker</returns>
        public IPerformanceTracker StartOperation(string operationName, string category = null)
        {
            try
            {
                // Enforce collection size limits to prevent unbounded growth
                if (_activeTrackers.Count >= MaxActiveTrackers)
                {
                    // Remove oldest completed trackers to make room
                    var completedTrackers = _activeTrackers.Values
                        .Where(t => !t.IsActive)
                        .OrderBy(t => t.StartTime)
                        .Take(100)
                        .ToList();

                    foreach (var completedTracker in completedTrackers)
                    {
                        _activeTrackers.TryRemove(completedTracker.Id, out _);
                    }

                    // If still at limit, return null tracker
                    if (_activeTrackers.Count >= MaxActiveTrackers)
                    {
                        return new NullPerformanceTracker(operationName, category);
                    }
                }

                var tracker = new PerformanceTracker(operationName, category, this);
                _activeTrackers.TryAdd(tracker.Id, tracker);
                return tracker;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "PerformanceMonitoringService.StartOperation");
                return new NullPerformanceTracker(operationName, category);
            }
        }

        /// <summary>
        /// Records a performance metric
        /// </summary>
        /// <param name="metric">Performance metric to record</param>
        public async Task RecordMetricAsync(PerformanceMetric metric)
        {
            try
            {
                if (metric == null) return;

                metric.Id = metric.Id ?? Guid.NewGuid().ToString();
                metric.SessionId = metric.SessionId ?? GetCurrentSessionId();
                metric.UserId = metric.UserId ?? Environment.UserName;
                
                // Enforce collection size limits to prevent unbounded growth
                if (_metrics.Count >= MaxMetrics)
                {
                    // Remove oldest metrics to make room (keep most recent 80%)
                    var metricsToRemove = _metrics.Values
                        .OrderBy(m => m.StartTime)
                        .Take(MaxMetrics / 5) // Remove 20%
                        .Select(m => m.Id)
                        .ToList();

                    foreach (var id in metricsToRemove)
                    {
                        _metrics.TryRemove(id, out _);
                    }
                }

                _metrics.TryAdd(metric.Id, metric);

                // Check thresholds
                await CheckThresholdsAsync(metric).ConfigureAwait(false);

                // Fire event
                MetricsCollected?.Invoke(this, new PerformanceMetricsEventArgs { Metric = metric });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.RecordMetricAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Records multiple performance metrics
        /// </summary>
        /// <param name="metrics">Performance metrics to record</param>
        public async Task RecordMetricsAsync(IEnumerable<PerformanceMetric> metrics)
        {
            try
            {
                var tasks = metrics.Select(RecordMetricAsync);
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.RecordMetricsAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets performance statistics for a time period
        /// </summary>
        /// <param name="timeRange">Time range for statistics</param>
        /// <param name="category">Optional category filter</param>
        /// <returns>Performance statistics</returns>
        public async Task<PerformanceStatistics> GetStatisticsAsync(TimeRange timeRange, string category = null)
        {
            try
            {
                var filteredMetrics = _metrics.Values
                    .Where(m => m.StartTime >= timeRange.StartTime && m.StartTime <= timeRange.EndTime)
                    .Where(m => string.IsNullOrEmpty(category) || m.Category == category)
                    .ToList();

                if (!filteredMetrics.Any())
                {
                    return new PerformanceStatistics
                    {
                        TimeRange = timeRange,
                        Category = category
                    };
                }

                var durations = filteredMetrics.Select(m => m.Duration).OrderBy(d => d).ToList();
                var memoryUsages = filteredMetrics.Select(m => m.EndMemoryUsage).ToList();

                var statistics = new PerformanceStatistics
                {
                    TimeRange = timeRange,
                    Category = category,
                    TotalOperations = filteredMetrics.Count,
                    SuccessfulOperations = filteredMetrics.Count(m => m.IsSuccessful),
                    FailedOperations = filteredMetrics.Count(m => !m.IsSuccessful),
                    SuccessRate = filteredMetrics.Count > 0 ? (double)filteredMetrics.Count(m => m.IsSuccessful) / filteredMetrics.Count * 100 : 0,
                    AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)),
                    MedianDuration = GetMedianDuration(durations),
                    MinDuration = durations.First(),
                    MaxDuration = durations.Last(),
                    P95Duration = GetPercentileDuration(durations, 95),
                    P99Duration = GetPercentileDuration(durations, 99),
                    AverageMemoryUsage = memoryUsages.Any() ? (long)memoryUsages.Average() : 0,
                    PeakMemoryUsage = memoryUsages.Any() ? memoryUsages.Max() : 0,
                    AverageCpuUsage = filteredMetrics.Average(m => m.CpuUsage),
                    OperationsPerSecond = CalculateOperationsPerSecond(filteredMetrics, timeRange),
                    SlowestOperations = filteredMetrics.OrderByDescending(m => m.Duration).Take(10).ToList(),
                    MostFrequentOperations = filteredMetrics.GroupBy(m => m.OperationName)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ErrorDistribution = filteredMetrics.Where(m => !m.IsSuccessful)
                        .GroupBy(m => m.ErrorMessage ?? "Unknown Error")
                        .ToDictionary(g => g.Key, g => g.Count()),
                    Trends = GeneratePerformanceTrends(filteredMetrics, timeRange)
                };

                return statistics;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.GetStatisticsAsync").ConfigureAwait(false);
                return new PerformanceStatistics { TimeRange = timeRange, Category = category };
            }
        }

        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        /// <returns>Current performance metrics</returns>
        public async Task<CurrentPerformanceMetrics> GetCurrentMetricsAsync()
        {
            try
            {
                var currentMetrics = new CurrentPerformanceMetrics
                {
                    CurrentMemoryUsage = GC.GetTotalMemory(false),
                    AvailableMemory = GC.GetTotalMemory(false), // Approximation
                    ThreadCount = _currentProcess.Threads.Count,
                    ActiveOperations = _activeTrackers.Count,
                    Uptime = DateTime.UtcNow - _currentProcess.StartTime,
                    GCCollectionCounts = new Dictionary<int, int>
                    {
                        [0] = GC.CollectionCount(0),
                        [1] = GC.CollectionCount(1),
                        [2] = GC.CollectionCount(2)
                    },
                    Status = DeterminePerformanceStatus()
                };

                // Get CPU usage if counter is available
                if (_cpuCounter != null)
                {
                    try
                    {
                        currentMetrics.CurrentCpuUsage = _cpuCounter.NextValue();
                    }
                    catch
                    {
                        // Ignore CPU counter errors
                    }
                }

                // Get active alerts
                currentMetrics.ActiveAlerts = await GetActiveAlertsAsync().ConfigureAwait(false);

                return currentMetrics;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.GetCurrentMetricsAsync").ConfigureAwait(false);
                return new CurrentPerformanceMetrics { Status = PerformanceStatus.Critical };
            }
        }

        /// <summary>
        /// Clears old performance data
        /// </summary>
        /// <param name="olderThan">Clear data older than this date</param>
        public async Task ClearOldDataAsync(DateTime? olderThan = null)
        {
            try
            {
                var cutoffDate = olderThan ?? DateTime.UtcNow.AddHours(-24);
                
                var keysToRemove = _metrics.Values
                    .Where(m => m.StartTime < cutoffDate)
                    .Select(m => m.Id)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _metrics.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.ClearOldDataAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets performance thresholds for monitoring
        /// </summary>
        /// <param name="thresholds">Performance thresholds</param>
        public async Task SetThresholdsAsync(Dictionary<string, PerformanceThreshold> thresholds)
        {
            try
            {
                foreach (var kvp in thresholds)
                {
                    // Enforce collection size limits to prevent unbounded growth
                    if (_thresholds.Count >= MaxThresholds && !_thresholds.ContainsKey(kvp.Key))
                    {
                        throw new InvalidOperationException($"Cannot add more than {MaxThresholds} thresholds. Current count: {_thresholds.Count}");
                    }

                    _thresholds.AddOrUpdate(kvp.Key, kvp.Value, (key, old) => kvp.Value);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.SetThresholdsAsync").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets performance recommendations
        /// </summary>
        /// <returns>Performance recommendations</returns>
        public async Task<List<PerformanceRecommendation>> GetRecommendationsAsync()
        {
            try
            {
                var recommendations = new List<PerformanceRecommendation>();
                
                var recentMetrics = _metrics.Values
                    .Where(m => m.StartTime > DateTime.UtcNow.AddHours(-1))
                    .ToList();

                // Check for slow operations
                var slowOperations = recentMetrics
                    .Where(m => m.Duration > TimeSpan.FromSeconds(5))
                    .GroupBy(m => m.OperationName)
                    .Where(g => g.Count() > 3)
                    .ToList();

                foreach (var group in slowOperations)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Slow Operation: {group.Key}",
                        Description = $"Operation '{group.Key}' is consistently taking longer than 5 seconds",
                        Category = "Performance",
                        Priority = RecommendationPriority.High,
                        ExpectedImprovement = "Reduced response times",
                        Complexity = RecommendationComplexity.Medium,
                        Actions = new List<string>
                        {
                            "Profile the operation to identify bottlenecks",
                            "Consider caching frequently accessed data",
                            "Optimize database queries if applicable",
                            "Implement asynchronous processing where possible"
                        },
                        RelatedOperations = new List<string> { group.Key }
                    });
                }

                // Check for high memory usage
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > 500 * 1024 * 1024) // 500MB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "High Memory Usage",
                        Description = $"Current memory usage is {currentMemory / (1024 * 1024):F0}MB",
                        Category = "Memory",
                        Priority = RecommendationPriority.Medium,
                        ExpectedImprovement = "Reduced memory footprint",
                        Complexity = RecommendationComplexity.Medium,
                        Actions = new List<string>
                        {
                            "Force garbage collection",
                            "Review large object allocations",
                            "Implement object pooling for frequently used objects",
                            "Consider unloading unused components"
                        }
                    });
                }

                // Check for high error rates
                var errorRate = recentMetrics.Any() ? (double)recentMetrics.Count(m => !m.IsSuccessful) / recentMetrics.Count : 0;
                if (errorRate > 0.1) // 10% error rate
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "High Error Rate",
                        Description = $"Error rate is {errorRate:P1} in the last hour",
                        Category = "Reliability",
                        Priority = RecommendationPriority.High,
                        ExpectedImprovement = "Improved reliability and user experience",
                        Complexity = RecommendationComplexity.High,
                        Actions = new List<string>
                        {
                            "Review error logs for common patterns",
                            "Implement better error handling",
                            "Add retry logic for transient failures",
                            "Improve input validation"
                        }
                    });
                }

                return recommendations;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.GetRecommendationsAsync").ConfigureAwait(false);
                return new List<PerformanceRecommendation>();
            }
        }

        /// <summary>
        /// Exports performance data
        /// </summary>
        /// <param name="format">Export format</param>
        /// <param name="timeRange">Time range for export</param>
        /// <returns>Exported performance data</returns>
        public async Task<string> ExportDataAsync(PerformanceDataFormat format, TimeRange timeRange)
        {
            try
            {
                var filteredMetrics = _metrics.Values
                    .Where(m => m.StartTime >= timeRange.StartTime && m.StartTime <= timeRange.EndTime)
                    .OrderBy(m => m.StartTime)
                    .ToList();

                switch (format)
                {
                    case PerformanceDataFormat.Json:
                        return SecureJsonSerializer.Serialize(filteredMetrics);
                    
                    case PerformanceDataFormat.Csv:
                        return ExportToCsv(filteredMetrics);
                    
                    case PerformanceDataFormat.Xml:
                        return ExportToXml(filteredMetrics);
                    
                    default:
                        return SecureJsonSerializer.Serialize(filteredMetrics);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.ExportDataAsync").ConfigureAwait(false);
                return string.Empty;
            }
        }

        #region Private Methods

        internal async Task RecordTrackerMetricAsync(PerformanceTracker tracker)
        {
            try
            {
                var metric = new PerformanceMetric
                {
                    Id = tracker.Id,
                    OperationName = tracker.OperationName,
                    Category = tracker.Category,
                    Duration = tracker.ElapsedTime,
                    StartMemoryUsage = tracker.StartMemoryUsage,
                    EndMemoryUsage = GC.GetTotalMemory(false),
                    PeakMemoryUsage = tracker.PeakMemoryUsage,
                    CpuUsage = tracker.CpuUsage,
                    IsSuccess = tracker.Status == OperationStatus.Completed,
                    ErrorMessage = tracker.ErrorMessage,
                    StartTime = tracker.StartTime,
                    EndTime = DateTime.UtcNow,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    Metadata = new Dictionary<string, object>(tracker.Metadata),
                    Checkpoints = new List<PerformanceCheckpoint>(tracker.Checkpoints),
                    Severity = DetermineSeverity(tracker)
                };

                await RecordMetricAsync(metric).ConfigureAwait(false);
                _activeTrackers.TryRemove(tracker.Id, out _);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.RecordTrackerMetricAsync").ConfigureAwait(false);
            }
        }

        private void InitializeDefaultThresholds()
        {
            _thresholds.TryAdd("SlowOperation", new PerformanceThreshold
            {
                Name = "Slow Operation",
                OperationPattern = "*",
                MaxDuration = TimeSpan.FromSeconds(10),
                Severity = PerformanceSeverity.Warning,
                Action = ThresholdAction.Log
            });

            _thresholds.TryAdd("VerySlowOperation", new PerformanceThreshold
            {
                Name = "Very Slow Operation",
                OperationPattern = "*",
                MaxDuration = TimeSpan.FromSeconds(30),
                Severity = PerformanceSeverity.Error,
                Action = ThresholdAction.Alert
            });

            _thresholds.TryAdd("HighMemoryUsage", new PerformanceThreshold
            {
                Name = "High Memory Usage",
                OperationPattern = "*",
                MaxMemoryUsage = 1024 * 1024 * 1024, // 1GB
                Severity = PerformanceSeverity.Warning,
                Action = ThresholdAction.Log
            });

            _thresholds.TryAdd("HighErrorRate", new PerformanceThreshold
            {
                Name = "High Error Rate",
                OperationPattern = "*",
                MaxErrorRate = 10.0, // 10%
                Severity = PerformanceSeverity.Error,
                Action = ThresholdAction.Alert
            });
        }

        private async Task CheckThresholdsAsync(PerformanceMetric metric)
        {
            try
            {
                foreach (var threshold in _thresholds.Values.Where(t => t.IsEnabled))
                {
                    if (IsThresholdExceeded(metric, threshold))
                    {
                        var alert = new PerformanceAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Title = $"Threshold Exceeded: {threshold.Name}",
                            Message = GenerateThresholdMessage(metric, threshold),
                            Severity = threshold.Severity,
                            OperationName = metric.OperationName,
                            ThresholdName = threshold.Name,
                            CurrentValue = GetCurrentValue(metric, threshold),
                            ThresholdValue = GetThresholdValue(threshold)
                        };

                        ThresholdExceeded?.Invoke(this, new PerformanceThresholdEventArgs
                        {
                            Threshold = threshold,
                            Metric = metric,
                            Alert = alert
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.CheckThresholdsAsync").ConfigureAwait(false);
            }
        }

        private bool IsThresholdExceeded(PerformanceMetric metric, PerformanceThreshold threshold)
        {
            if (threshold.MaxDuration.HasValue && metric.Duration > threshold.MaxDuration.Value)
                return true;

            if (threshold.MaxMemoryUsage.HasValue && metric.EndMemoryUsage > threshold.MaxMemoryUsage.Value)
                return true;

            if (threshold.MaxCpuUsage.HasValue && metric.CpuUsage > threshold.MaxCpuUsage.Value)
                return true;

            return false;
        }

        private string GenerateThresholdMessage(PerformanceMetric metric, PerformanceThreshold threshold)
        {
            if (threshold.MaxDuration.HasValue && metric.Duration > threshold.MaxDuration.Value)
                return $"Operation '{metric.OperationName}' took {metric.Duration.TotalSeconds:F2}s, exceeding threshold of {threshold.MaxDuration.Value.TotalSeconds:F2}s";

            if (threshold.MaxMemoryUsage.HasValue && metric.EndMemoryUsage > threshold.MaxMemoryUsage.Value)
                return $"Operation '{metric.OperationName}' used {metric.EndMemoryUsage / (1024 * 1024):F0}MB, exceeding threshold of {threshold.MaxMemoryUsage.Value / (1024 * 1024):F0}MB";

            if (threshold.MaxCpuUsage.HasValue && metric.CpuUsage > threshold.MaxCpuUsage.Value)
                return $"Operation '{metric.OperationName}' used {metric.CpuUsage:F1}% CPU, exceeding threshold of {threshold.MaxCpuUsage.Value:F1}%";

            return $"Threshold '{threshold.Name}' exceeded for operation '{metric.OperationName}'";
        }

        private object GetCurrentValue(PerformanceMetric metric, PerformanceThreshold threshold)
        {
            if (threshold.MaxDuration.HasValue)
                return metric.Duration;
            if (threshold.MaxMemoryUsage.HasValue)
                return metric.EndMemoryUsage;
            if (threshold.MaxCpuUsage.HasValue)
                return metric.CpuUsage;
            return null;
        }

        private object GetThresholdValue(PerformanceThreshold threshold)
        {
            if (threshold.MaxDuration.HasValue)
                return threshold.MaxDuration.Value;
            if (threshold.MaxMemoryUsage.HasValue)
                return threshold.MaxMemoryUsage.Value;
            if (threshold.MaxCpuUsage.HasValue)
                return threshold.MaxCpuUsage.Value;
            return null;
        }

        private PerformanceSeverity DetermineSeverity(PerformanceTracker tracker)
        {
            if (tracker.Status == OperationStatus.Failed)
                return PerformanceSeverity.Error;
            if (tracker.ElapsedTime > TimeSpan.FromSeconds(30))
                return PerformanceSeverity.Warning;
            if (tracker.ElapsedTime > TimeSpan.FromSeconds(10))
                return PerformanceSeverity.Warning;
            return PerformanceSeverity.Info;
        }

        private PerformanceStatus DeterminePerformanceStatus()
        {
            var recentMetrics = _metrics.Values
                .Where(m => m.StartTime > DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            if (!recentMetrics.Any())
                return PerformanceStatus.Good;

            var errorRate = (double)recentMetrics.Count(m => !m.IsSuccessful) / recentMetrics.Count;
            var avgDuration = recentMetrics.Average(m => m.Duration.TotalMilliseconds);

            if (errorRate > 0.2 || avgDuration > 30000) // 20% error rate or 30+ second average
                return PerformanceStatus.Critical;
            if (errorRate > 0.1 || avgDuration > 10000) // 10% error rate or 10+ second average
                return PerformanceStatus.Poor;
            if (errorRate > 0.05 || avgDuration > 5000) // 5% error rate or 5+ second average
                return PerformanceStatus.Fair;

            return PerformanceStatus.Good;
        }

        private async Task<List<PerformanceAlert>> GetActiveAlertsAsync()
        {
            // This would typically be retrieved from a persistent store
            // For now, return empty list
            return new List<PerformanceAlert>();
        }

        private string GetCurrentSessionId()
        {
            return "session-" + DateTime.UtcNow.ToString("yyyyMMdd");
        }

        private TimeSpan GetMedianDuration(List<TimeSpan> durations)
        {
            if (!durations.Any())
                return TimeSpan.Zero;

            var count = durations.Count;
            if (count % 2 == 0)
            {
                var mid1 = durations[count / 2 - 1];
                var mid2 = durations[count / 2];
                return TimeSpan.FromMilliseconds((mid1.TotalMilliseconds + mid2.TotalMilliseconds) / 2);
            }
            else
            {
                return durations[count / 2];
            }
        }

        private TimeSpan GetPercentileDuration(List<TimeSpan> durations, int percentile)
        {
            if (!durations.Any())
                return TimeSpan.Zero;

            var index = (int)Math.Ceiling(durations.Count * percentile / 100.0) - 1;
            index = Math.Max(0, Math.Min(index, durations.Count - 1));
            return durations[index];
        }

        private double CalculateOperationsPerSecond(List<PerformanceMetric> metrics, TimeRange timeRange)
        {
            if (!metrics.Any())
                return 0;

            var totalSeconds = timeRange.Duration.TotalSeconds;
            return totalSeconds > 0 ? metrics.Count / totalSeconds : 0;
        }

        private List<PerformanceTrend> GeneratePerformanceTrends(List<PerformanceMetric> metrics, TimeRange timeRange)
        {
            var trends = new List<PerformanceTrend>();
            
            // Group by hour
            var hourGroups = metrics.GroupBy(m => new DateTime(m.StartTime.Year, m.StartTime.Month, m.StartTime.Day, m.StartTime.Hour, 0, 0));
            
            foreach (var group in hourGroups)
            {
                var groupMetrics = group.ToList();
                trends.Add(new PerformanceTrend
                {
                    TimeBucket = group.Key,
                    AverageDuration = TimeSpan.FromMilliseconds(groupMetrics.Average(m => m.Duration.TotalMilliseconds)),
                    OperationCount = groupMetrics.Count,
                    ErrorCount = groupMetrics.Count(m => !m.IsSuccessful),
                    AverageMemoryUsage = (long)groupMetrics.Average(m => m.EndMemoryUsage)
                });
            }
            
            return trends.OrderBy(t => t.TimeBucket).ToList();
        }

        private string ExportToCsv(List<PerformanceMetric> metrics)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,OperationName,Category,Duration(ms),MemoryUsage(bytes),CpuUsage(%),IsSuccess,StartTime,EndTime,ErrorMessage");
            
            foreach (var metric in metrics)
            {
                sb.AppendLine($"{metric.Id},{metric.OperationName},{metric.Category},{metric.Duration.TotalMilliseconds},{metric.EndMemoryUsage},{metric.CpuUsage},{metric.IsSuccessful},{metric.StartTime:yyyy-MM-dd HH:mm:ss},{metric.EndTime:yyyy-MM-dd HH:mm:ss},\"{metric.ErrorMessage ?? ""}\"");
            }
            
            return sb.ToString();
        }

        private string ExportToXml(List<PerformanceMetric> metrics)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<PerformanceMetrics>");
            
            foreach (var metric in metrics)
            {
                sb.AppendLine($"  <Metric>");
                sb.AppendLine($"    <Id>{metric.Id}</Id>");
                sb.AppendLine($"    <OperationName>{metric.OperationName}</OperationName>");
                sb.AppendLine($"    <Category>{metric.Category}</Category>");
                sb.AppendLine($"    <Duration>{metric.Duration.TotalMilliseconds}</Duration>");
                sb.AppendLine($"    <MemoryUsage>{metric.EndMemoryUsage}</MemoryUsage>");
                sb.AppendLine($"    <CpuUsage>{metric.CpuUsage}</CpuUsage>");
                sb.AppendLine($"    <IsSuccess>{metric.IsSuccessful}</IsSuccess>");
                sb.AppendLine($"    <StartTime>{metric.StartTime:yyyy-MM-dd HH:mm:ss}</StartTime>");
                sb.AppendLine($"    <EndTime>{metric.EndTime:yyyy-MM-dd HH:mm:ss}</EndTime>");
                sb.AppendLine($"    <ErrorMessage>{metric.ErrorMessage ?? ""}</ErrorMessage>");
                sb.AppendLine($"  </Metric>");
            }
            
            sb.AppendLine("</PerformanceMetrics>");
            return sb.ToString();
        }

        private void CleanupOldMetrics(object state)
        {
            _ = ClearOldDataAsync();
        }

        private void PerformHealthCheck(object state)
        {
            _ = PerformHealthCheckAsync();
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                var currentMetrics = await GetCurrentMetricsAsync().ConfigureAwait(false);
                // Additional health check logic could be added here
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "PerformanceMonitoringService.PerformHealthCheck").ConfigureAwait(false);
            }
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
                    // Dispose timers first to stop background operations
                    _cleanupTimer?.Dispose();
                    _healthCheckTimer?.Dispose();

                    // Dispose performance counter
                    _cpuCounter?.Dispose();

                    // Dispose process reference
                    _currentProcess?.Dispose();
                    
                    // Dispose all active trackers
                    foreach (var tracker in _activeTrackers.Values.ToList())
                    {
                        try
                        {
                            tracker.Dispose();
                        }
                        catch
                        {
                            // Ignore exceptions during disposal
                        }
                    }

                    // Clear collections to prevent memory leaks
                    _activeTrackers.Clear();
                    _metrics.Clear();
                    _thresholds.Clear();

                    // Clear event handlers to prevent memory leaks
                    MetricsCollected = null;
                    ThresholdExceeded = null;
                    
                    _disposed = true;
                }
            }
        }

        ~PerformanceMonitoringService()
        {
            Dispose(false);
        }
    }

    #region Performance Tracker Implementation

    internal enum OperationStatus
    {
        Active,
        Completed,
        Failed,
        Cancelled
    }

    internal class PerformanceTracker : IPerformanceTracker
    {
        private readonly PerformanceMonitoringService _service;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public string Id { get; } = Guid.NewGuid().ToString();
        public string OperationName { get; }
        public string Category { get; }
        public DateTime StartTime { get; }
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;
        public bool IsActive => Status == OperationStatus.Active;
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
        public List<PerformanceCheckpoint> Checkpoints { get; } = new List<PerformanceCheckpoint>();

        internal OperationStatus Status { get; private set; } = OperationStatus.Active;
        internal long StartMemoryUsage { get; }
        internal long PeakMemoryUsage { get; private set; }
        internal double CpuUsage { get; private set; }
        internal string ErrorMessage { get; private set; }

        public PerformanceTracker(string operationName, string category, PerformanceMonitoringService service)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            Category = category ?? "General";
            _service = service ?? throw new ArgumentNullException(nameof(service));
            StartTime = DateTime.UtcNow;
            StartMemoryUsage = GC.GetTotalMemory(false);
            PeakMemoryUsage = StartMemoryUsage;
            
            _stopwatch = Stopwatch.StartNew();
        }

        public void AddMetadata(string key, object value)
        {
            if (IsActive)
            {
                Metadata[key] = value;
            }
        }

        public void RecordCheckpoint(string checkpointName, string description = null)
        {
            if (IsActive)
            {
                var currentMemory = GC.GetTotalMemory(false);
                if (currentMemory > PeakMemoryUsage)
                {
                    PeakMemoryUsage = currentMemory;
                }

                Checkpoints.Add(new PerformanceCheckpoint
                {
                    Name = checkpointName,
                    Description = description,
                    ElapsedTime = ElapsedTime,
                    MemoryUsage = currentMemory,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public void Complete()
        {
            if (IsActive)
            {
                Status = OperationStatus.Completed;
                _stopwatch.Stop();
                _ = _service.RecordTrackerMetricAsync(this);
            }
        }

        public void Fail(Exception error)
        {
            if (IsActive)
            {
                Status = OperationStatus.Failed;
                ErrorMessage = error?.Message;
                _stopwatch.Stop();
                _ = _service.RecordTrackerMetricAsync(this);
            }
        }

        public void Cancel()
        {
            if (IsActive)
            {
                Status = OperationStatus.Cancelled;
                _stopwatch.Stop();
                _ = _service.RecordTrackerMetricAsync(this);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (IsActive)
                {
                    Complete();
                }
                _disposed = true;
            }
        }
    }

    internal class NullPerformanceTracker : IPerformanceTracker
    {
        public string OperationName { get; }
        public string Category { get; }
        public DateTime StartTime { get; } = DateTime.UtcNow;
        public TimeSpan ElapsedTime => TimeSpan.Zero;
        public bool IsActive => false;
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public NullPerformanceTracker(string operationName, string category)
        {
            OperationName = operationName;
            Category = category;
        }

        public void AddMetadata(string key, object value) { }
        public void RecordCheckpoint(string checkpointName, string description = null) { }
        public void Complete() { }
        public void Fail(Exception error) { }
        public void Cancel() { }
        public void Dispose() { }
    }

    #endregion
}