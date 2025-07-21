using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a time range with start and end times
    /// </summary>
    public class TimeRange
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        
        public bool Contains(DateTime time)
        {
            return time >= StartTime && time <= EndTime;
        }
    }

    /// <summary>
    /// Performance metric
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>
        /// Metric ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Operation name
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Metric category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Operation duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Memory usage at start (bytes)
        /// </summary>
        public long StartMemoryUsage { get; set; }

        /// <summary>
        /// Memory usage at end (bytes)
        /// </summary>
        public long EndMemoryUsage { get; set; }

        /// <summary>
        /// Peak memory usage during operation (bytes)
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Operation start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Operation end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Thread ID
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Operation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Operation checkpoints
        /// </summary>
        public List<PerformanceCheckpoint> Checkpoints { get; set; } = new List<PerformanceCheckpoint>();

        /// <summary>
        /// Performance severity
        /// </summary>
        public PerformanceSeverity Severity { get; set; }

        /// <summary>
        /// User ID (if applicable)
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Performance checkpoint
    /// </summary>
    public class PerformanceCheckpoint
    {
        /// <summary>
        /// Checkpoint name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Checkpoint description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Elapsed time from operation start
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Memory usage at checkpoint
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Checkpoint timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Additional checkpoint data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance statistics
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>
        /// Time range for statistics
        /// </summary>
        public TimeRange TimeRange { get; set; }

        /// <summary>
        /// Category filter applied
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Total number of operations
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// Number of successful operations
        /// </summary>
        public int SuccessfulOperations { get; set; }

        /// <summary>
        /// Number of failed operations
        /// </summary>
        public int FailedOperations { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Average operation duration
        /// </summary>
        public TimeSpan AverageDuration { get; set; }

        /// <summary>
        /// Median operation duration
        /// </summary>
        public TimeSpan MedianDuration { get; set; }

        /// <summary>
        /// Minimum operation duration
        /// </summary>
        public TimeSpan MinDuration { get; set; }

        /// <summary>
        /// Maximum operation duration
        /// </summary>
        public TimeSpan MaxDuration { get; set; }

        /// <summary>
        /// 95th percentile duration
        /// </summary>
        public TimeSpan P95Duration { get; set; }

        /// <summary>
        /// 99th percentile duration
        /// </summary>
        public TimeSpan P99Duration { get; set; }

        /// <summary>
        /// Average memory usage
        /// </summary>
        public long AverageMemoryUsage { get; set; }

        /// <summary>
        /// Peak memory usage
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// Average CPU usage
        /// </summary>
        public double AverageCpuUsage { get; set; }

        /// <summary>
        /// Operations per second
        /// </summary>
        public double OperationsPerSecond { get; set; }

        /// <summary>
        /// Top slowest operations
        /// </summary>
        public List<PerformanceMetric> SlowestOperations { get; set; } = new List<PerformanceMetric>();

        /// <summary>
        /// Most frequent operations
        /// </summary>
        public Dictionary<string, int> MostFrequentOperations { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Error distribution
        /// </summary>
        public Dictionary<string, int> ErrorDistribution { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Performance trends over time
        /// </summary>
        public List<PerformanceTrend> Trends { get; set; } = new List<PerformanceTrend>();
    }

    /// <summary>
    /// Current performance metrics
    /// </summary>
    public class CurrentPerformanceMetrics
    {
        /// <summary>
        /// Current memory usage (bytes)
        /// </summary>
        public long CurrentMemoryUsage { get; set; }

        /// <summary>
        /// Available memory (bytes)
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Current CPU usage percentage
        /// </summary>
        public double CurrentCpuUsage { get; set; }

        /// <summary>
        /// Number of active operations
        /// </summary>
        public int ActiveOperations { get; set; }

        /// <summary>
        /// Number of threads
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Application uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// GC collection counts
        /// </summary>
        public Dictionary<int, int> GCCollectionCounts { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Last GC collection time
        /// </summary>
        public DateTime LastGCCollection { get; set; }

        /// <summary>
        /// Current performance status
        /// </summary>
        public PerformanceStatus Status { get; set; }

        /// <summary>
        /// Active performance alerts
        /// </summary>
        public List<PerformanceAlert> ActiveAlerts { get; set; } = new List<PerformanceAlert>();

        /// <summary>
        /// Measurement timestamp
        /// </summary>
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance trend data
    /// </summary>
    public class PerformanceTrend
    {
        /// <summary>
        /// Time bucket (hour, day, etc.)
        /// </summary>
        public DateTime TimeBucket { get; set; }

        /// <summary>
        /// Average duration for this time bucket
        /// </summary>
        public TimeSpan AverageDuration { get; set; }

        /// <summary>
        /// Operation count for this time bucket
        /// </summary>
        public int OperationCount { get; set; }

        /// <summary>
        /// Error count for this time bucket
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Average memory usage for this time bucket
        /// </summary>
        public long AverageMemoryUsage { get; set; }
    }

    /// <summary>
    /// Performance threshold
    /// </summary>
    public class PerformanceThreshold
    {
        /// <summary>
        /// Threshold name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Operation pattern to match
        /// </summary>
        public string OperationPattern { get; set; }

        /// <summary>
        /// Maximum allowed duration
        /// </summary>
        public TimeSpan MaxDuration { get; set; }

        /// <summary>
        /// Maximum allowed memory usage (bytes)
        /// </summary>
        public long MaxMemoryUsage { get; set; }

        /// <summary>
        /// Maximum allowed CPU usage percentage
        /// </summary>
        public double MaxCpuUsage { get; set; }

        /// <summary>
        /// Maximum allowed error rate percentage
        /// </summary>
        public double MaxErrorRate { get; set; }

        /// <summary>
        /// Whether the threshold is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Threshold severity
        /// </summary>
        public PerformanceSeverity Severity { get; set; } = PerformanceSeverity.Warning;

        /// <summary>
        /// Action to take when threshold is exceeded
        /// </summary>
        public ThresholdAction Action { get; set; } = ThresholdAction.Log;

        /// <summary>
        /// Custom action parameters
        /// </summary>
        public Dictionary<string, object> ActionParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance recommendation
    /// </summary>
    public class PerformanceRecommendation
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
        /// Recommendation category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Recommendation priority
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// Expected performance improvement
        /// </summary>
        public string ExpectedImprovement { get; set; }

        /// <summary>
        /// Implementation complexity
        /// </summary>
        public RecommendationComplexity Complexity { get; set; }

        /// <summary>
        /// Recommended actions
        /// </summary>
        public List<string> Actions { get; set; } = new List<string>();

        /// <summary>
        /// Supporting metrics
        /// </summary>
        public Dictionary<string, object> SupportingMetrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Related operations
        /// </summary>
        public List<string> RelatedOperations { get; set; } = new List<string>();

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance alert
    /// </summary>
    public class PerformanceAlert
    {
        /// <summary>
        /// Alert ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Alert title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Alert message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Alert severity
        /// </summary>
        public PerformanceSeverity Severity { get; set; }

        /// <summary>
        /// Related operation
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Threshold that was exceeded
        /// </summary>
        public string ThresholdName { get; set; }

        /// <summary>
        /// Current value that exceeded threshold
        /// </summary>
        public object CurrentValue { get; set; }

        /// <summary>
        /// Threshold value
        /// </summary>
        public object ThresholdValue { get; set; }

        /// <summary>
        /// Alert creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the alert is acknowledged
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// Alert acknowledgment time
        /// </summary>
        public DateTime AcknowledgedAt { get; set; }

        /// <summary>
        /// Additional alert data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance metrics event arguments
    /// </summary>
    public class PerformanceMetricsEventArgs : EventArgs
    {
        /// <summary>
        /// Performance metric
        /// </summary>
        public PerformanceMetric Metric { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance threshold event arguments
    /// </summary>
    public class PerformanceThresholdEventArgs : EventArgs
    {
        /// <summary>
        /// Threshold that was exceeded
        /// </summary>
        public PerformanceThreshold Threshold { get; set; }

        /// <summary>
        /// Performance metric that exceeded threshold
        /// </summary>
        public PerformanceMetric Metric { get; set; }

        /// <summary>
        /// Generated alert
        /// </summary>
        public PerformanceAlert Alert { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance severity levels
    /// </summary>
    public enum PerformanceSeverity
    {
        /// <summary>
        /// Informational
        /// </summary>
        Info,

        /// <summary>
        /// Warning level
        /// </summary>
        Warning,

        /// <summary>
        /// Error level
        /// </summary>
        Error,

        /// <summary>
        /// Critical level
        /// </summary>
        Critical
    }

    /// <summary>
    /// Performance status
    /// </summary>
    public enum PerformanceStatus
    {
        /// <summary>
        /// Optimal performance
        /// </summary>
        Optimal,

        /// <summary>
        /// Good performance
        /// </summary>
        Good,

        /// <summary>
        /// Fair performance
        /// </summary>
        Fair,

        /// <summary>
        /// Poor performance
        /// </summary>
        Poor,

        /// <summary>
        /// Critical performance issues
        /// </summary>
        Critical
    }

    /// <summary>
    /// Threshold actions
    /// </summary>
    public enum ThresholdAction
    {
        /// <summary>
        /// Log the threshold violation
        /// </summary>
        Log,

        /// <summary>
        /// Send notification
        /// </summary>
        Notify,

        /// <summary>
        /// Create alert
        /// </summary>
        Alert,

        /// <summary>
        /// Execute custom action
        /// </summary>
        Custom,

        /// <summary>
        /// Stop operation
        /// </summary>
        Stop
    }

    /// <summary>
    /// Recommendation complexity levels
    /// </summary>
    public enum RecommendationComplexity
    {
        /// <summary>
        /// Low complexity - easy to implement
        /// </summary>
        Low,

        /// <summary>
        /// Medium complexity - moderate effort required
        /// </summary>
        Medium,

        /// <summary>
        /// High complexity - significant effort required
        /// </summary>
        High,

        /// <summary>
        /// Very high complexity - major changes required
        /// </summary>
        VeryHigh
    }

    /// <summary>
    /// Performance data export formats
    /// </summary>
    public enum PerformanceDataFormat
    {
        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// CSV format
        /// </summary>
        Csv,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// Binary format
        /// </summary>
        Binary
    }
}