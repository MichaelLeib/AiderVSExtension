using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Background service status
    /// </summary>
    public enum BackgroundServiceStatus
    {
        /// <summary>
        /// Service is not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Service is starting
        /// </summary>
        Starting,

        /// <summary>
        /// Service is running
        /// </summary>
        Running,

        /// <summary>
        /// Service is stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Service is stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Service failed to start or crashed
        /// </summary>
        Failed,

        /// <summary>
        /// Service is disabled
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Lazy loading strategies
    /// </summary>
    public enum LazyLoadingStrategy
    {
        /// <summary>
        /// Load on first access
        /// </summary>
        OnDemand,

        /// <summary>
        /// Load in background after startup
        /// </summary>
        Background,

        /// <summary>
        /// Load immediately when possible
        /// </summary>
        Immediate,

        /// <summary>
        /// Load based on usage patterns
        /// </summary>
        Adaptive,

        /// <summary>
        /// Never load automatically
        /// </summary>
        Manual
    }

    /// <summary>
    /// Background service event arguments
    /// </summary>
    public class BackgroundServiceEventArgs : EventArgs
    {
        /// <summary>
        /// Service ID
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Service status
        /// </summary>
        public BackgroundServiceStatus Status { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional event data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Background service error event arguments
    /// </summary>
    public class BackgroundServiceErrorEventArgs : BackgroundServiceEventArgs
    {
        /// <summary>
        /// Error that occurred
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Error context
        /// </summary>
        public string ErrorContext { get; set; }

        /// <summary>
        /// Whether the service can be restarted
        /// </summary>
        public bool CanRestart { get; set; }

        /// <summary>
        /// Number of restart attempts made
        /// </summary>
        public int RestartAttempts { get; set; }
    }

    /// <summary>
    /// Initialization phase event arguments
    /// </summary>
    public class InitializationPhaseEventArgs : EventArgs
    {
        /// <summary>
        /// Phase name
        /// </summary>
        public string PhaseName { get; set; }

        /// <summary>
        /// Phase duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Whether phase completed successfully
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Services started in this phase
        /// </summary>
        public List<string> ServicesStarted { get; set; } = new List<string>();

        /// <summary>
        /// Services that failed in this phase
        /// </summary>
        public List<string> ServicesFailed { get; set; } = new List<string>();

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Initialization progress information
    /// </summary>
    public class InitializationProgress
    {
        /// <summary>
        /// Current phase name
        /// </summary>
        public string CurrentPhase { get; set; }

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public double OverallProgress { get; set; }

        /// <summary>
        /// Current phase progress percentage (0-100)
        /// </summary>
        public double PhaseProgress { get; set; }

        /// <summary>
        /// Total number of phases
        /// </summary>
        public int TotalPhases { get; set; }

        /// <summary>
        /// Current phase number
        /// </summary>
        public int CurrentPhaseNumber { get; set; }

        /// <summary>
        /// Services completed
        /// </summary>
        public int ServicesCompleted { get; set; }

        /// <summary>
        /// Total services to initialize
        /// </summary>
        public int TotalServices { get; set; }

        /// <summary>
        /// Services currently starting
        /// </summary>
        public List<string> ServicesStarting { get; set; } = new List<string>();

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Initialization start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Whether initialization is complete
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Whether initialization failed
        /// </summary>
        public bool HasFailed { get; set; }

        /// <summary>
        /// Failure reasons if any
        /// </summary>
        public List<string> FailureReasons { get; set; } = new List<string>();
    }

    /// <summary>
    /// Service health information
    /// </summary>
    public class ServiceHealthInfo
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public HealthStatus OverallStatus { get; set; }

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Individual service health results
        /// </summary>
        public Dictionary<string, HealthCheckResult> ServiceHealth { get; set; } = new Dictionary<string, HealthCheckResult>();

        /// <summary>
        /// System resource usage
        /// </summary>
        public ResourceUsage ResourceUsage { get; set; }

        /// <summary>
        /// Health check duration
        /// </summary>
        public TimeSpan CheckDuration { get; set; }

        /// <summary>
        /// Health recommendations
        /// </summary>
        public List<HealthRecommendation> Recommendations { get; set; } = new List<HealthRecommendation>();
    }

    /// <summary>
    /// Health check result
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Health status
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// Health check message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Check duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Exception if check failed
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Additional health data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service metrics
    /// </summary>
    public class ServiceMetrics
    {
        /// <summary>
        /// Service uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Number of operations processed
        /// </summary>
        public long OperationsProcessed { get; set; }

        /// <summary>
        /// Number of errors encountered
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Average processing time
        /// </summary>
        public TimeSpan AverageProcessingTime { get; set; }

        /// <summary>
        /// Memory usage (bytes)
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Service-specific metrics
        /// </summary>
        public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource usage information
    /// </summary>
    public class ResourceUsage
    {
        /// <summary>
        /// Total memory usage (bytes)
        /// </summary>
        public long TotalMemoryUsage { get; set; }

        /// <summary>
        /// Available memory (bytes)
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        public double MemoryUsagePercentage { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// Number of active threads
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Number of handles
        /// </summary>
        public int HandleCount { get; set; }

        /// <summary>
        /// Working set size (bytes)
        /// </summary>
        public long WorkingSetSize { get; set; }

        /// <summary>
        /// GC pressure
        /// </summary>
        public GCPressure GCPressure { get; set; }
    }

    /// <summary>
    /// Garbage collection pressure information
    /// </summary>
    public class GCPressure
    {
        /// <summary>
        /// Generation 0 collection count
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Generation 1 collection count
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Generation 2 collection count
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Total allocated bytes
        /// </summary>
        public long TotalAllocatedBytes { get; set; }

        /// <summary>
        /// Heap size (bytes)
        /// </summary>
        public long HeapSize { get; set; }

        /// <summary>
        /// Time spent in GC percentage
        /// </summary>
        public double TimeInGCPercentage { get; set; }
    }

    /// <summary>
    /// Health recommendation
    /// </summary>
    public class HealthRecommendation
    {
        /// <summary>
        /// Recommendation ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Recommendation title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Recommendation description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Recommendation severity
        /// </summary>
        public RecommendationSeverity Severity { get; set; }

        /// <summary>
        /// Affected service
        /// </summary>
        public string AffectedService { get; set; }

        /// <summary>
        /// Recommended actions
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new List<string>();

        /// <summary>
        /// Implementation priority
        /// </summary>
        public RecommendationPriority Priority { get; set; }
    }

    /// <summary>
    /// Component loading metrics
    /// </summary>
    public class ComponentLoadingMetrics
    {
        /// <summary>
        /// Loading duration
        /// </summary>
        public TimeSpan LoadingDuration { get; set; }

        /// <summary>
        /// Memory used by component (bytes)
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Number of times loaded
        /// </summary>
        public int LoadCount { get; set; }

        /// <summary>
        /// Number of times unloaded
        /// </summary>
        public int UnloadCount { get; set; }

        /// <summary>
        /// Last load time
        /// </summary>
        public DateTime LastLoadTime { get; set; }

        /// <summary>
        /// Last unload time
        /// </summary>
        public DateTime? LastUnloadTime { get; set; }

        /// <summary>
        /// Average loading time
        /// </summary>
        public TimeSpan AverageLoadingTime { get; set; }

        /// <summary>
        /// Whether loading failed
        /// </summary>
        public bool HasLoadingFailed { get; set; }

        /// <summary>
        /// Last loading error
        /// </summary>
        public Exception LastLoadingError { get; set; }

        /// <summary>
        /// Loading success rate percentage
        /// </summary>
        public double SuccessRate { get; set; }
    }


    /// <summary>
    /// Recommendation severity levels
    /// </summary>
    public enum RecommendationSeverity
    {
        /// <summary>
        /// Informational
        /// </summary>
        Info,

        /// <summary>
        /// Low severity
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity
        /// </summary>
        Medium,

        /// <summary>
        /// High severity
        /// </summary>
        High,

        /// <summary>
        /// Critical severity
        /// </summary>
        Critical
    }
}