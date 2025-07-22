using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Configuration usage analytics data
    /// </summary>
    public class ConfigurationUsageAnalytics
    {
        /// <summary>
        /// Configuration ID
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Time range for analytics
        /// </summary>
        public TimeRange TimeRange { get; set; }

        /// <summary>
        /// Total actions performed
        /// </summary>
        public int TotalActions { get; set; }

        /// <summary>
        /// Number of unique profiles
        /// </summary>
        public int UniqueProfiles { get; set; }

        /// <summary>
        /// Number of unique sessions
        /// </summary>
        public int UniqueSessions { get; set; }

        /// <summary>
        /// Action counts by type
        /// </summary>
        public Dictionary<string, int> ActionCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Daily usage statistics
        /// </summary>
        public Dictionary<DateTime, int> DailyUsage { get; set; } = new Dictionary<DateTime, int>();

        /// <summary>
        /// Top actions by frequency
        /// </summary>
        public Dictionary<string, int> TopActions { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Average actions per session
        /// </summary>
        public double AverageActionsPerSession { get; set; }

        /// <summary>
        /// Number of times configuration was loaded
        /// </summary>
        public int LoadCount { get; set; }

        /// <summary>
        /// Total time configuration was active
        /// </summary>
        public TimeSpan ActiveTime { get; set; }

        /// <summary>
        /// Number of times configuration was modified
        /// </summary>
        public int ModificationCount { get; set; }

        /// <summary>
        /// Last accessed timestamp
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// Most frequently used settings
        /// </summary>
        public Dictionary<string, int> SettingUsage { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Error count associated with this configuration
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Configuration performance analytics
    /// </summary>
    public class ConfigurationPerformanceAnalytics
    {
        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Time range for analytics
        /// </summary>
        public TimeRange TimeRange { get; set; }

        /// <summary>
        /// Total operations performed
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
        /// Operation counts by type
        /// </summary>
        public Dictionary<string, int> OperationCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Slow operations (over 5 seconds)
        /// </summary>
        public List<ConfigurationPerformanceMetric> SlowOperations { get; set; } = new List<ConfigurationPerformanceMetric>();

        /// <summary>
        /// Error distribution by operation
        /// </summary>
        public Dictionary<string, int> ErrorsByOperation { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Configuration load time metrics
        /// </summary>
        public TimeSpan AverageLoadTime { get; set; }

        /// <summary>
        /// Configuration save time metrics
        /// </summary>
        public TimeSpan AverageSaveTime { get; set; }

        /// <summary>
        /// Memory usage by configuration
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// CPU usage percentage during configuration operations
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Validation time for configuration
        /// </summary>
        public TimeSpan ValidationTime { get; set; }

        /// <summary>
        /// Number of operations per second
        /// </summary>
        public double OperationsPerSecond { get; set; }
    }

    /// <summary>
    /// Configuration health status
    /// </summary>
    public class ConfigurationHealthStatus
    {
        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Overall health status
        /// </summary>
        public HealthLevel OverallHealth { get; set; }

        /// <summary>
        /// Health score (0-100)
        /// </summary>
        public int HealthScore { get; set; }

        /// <summary>
        /// List of detected issues
        /// </summary>
        public List<HealthIssue> Issues { get; set; } = new List<HealthIssue>();

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Health check results
        /// </summary>
        public Dictionary<string, bool> Checks { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// List of warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Recommendations for improvement
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Performance metrics
        /// </summary>
        public ConfigurationPerformanceMetric Performance { get; set; }
    }

    /// <summary>
    /// Configuration performance metric
    /// </summary>
    public class ConfigurationPerformanceMetric
    {
        /// <summary>
        /// Metric ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Operation name
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Operation duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Whether operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Operation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Memory usage at time of operation
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public double ResponseTimeMs { get; set; }

        /// <summary>
        /// Throughput operations per second
        /// </summary>
        public double ThroughputOps { get; set; }

        /// <summary>
        /// Error rate percentage
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Resource utilization percentage
        /// </summary>
        public double ResourceUtilization { get; set; }

        /// <summary>
        /// Availability percentage
        /// </summary>
        public double Availability { get; set; }
    }

    /// <summary>
    /// Configuration usage metric
    /// </summary>
    public class ConfigurationUsageMetric
    {
        /// <summary>
        /// Metric ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Action performed
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Metric timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Metric name
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// Metric value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Metric unit
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Metric category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Associated configuration ID
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration report
    /// </summary>
    public class ConfigurationReport
    {
        /// <summary>
        /// Profile ID
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Profile name
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// Time range for report
        /// </summary>
        public TimeRange TimeRange { get; set; }

        /// <summary>
        /// Report generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// User who generated the report
        /// </summary>
        public string GeneratedBy { get; set; }

        /// <summary>
        /// Usage analytics
        /// </summary>
        public ConfigurationUsageAnalytics UsageAnalytics { get; set; }

        /// <summary>
        /// Performance analytics
        /// </summary>
        public ConfigurationPerformanceAnalytics PerformanceAnalytics { get; set; }

        /// <summary>
        /// Health status
        /// </summary>
        public ConfigurationHealthStatus HealthStatus { get; set; }

        /// <summary>
        /// Recommendations
        /// </summary>
        public List<ConfigurationRecommendation> Recommendations { get; set; } = new List<ConfigurationRecommendation>();

        /// <summary>
        /// Report summary
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Report ID
        /// </summary>
        public string ReportId { get; set; }

        /// <summary>
        /// Report title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Report type
        /// </summary>
        public string ReportType { get; set; }

        /// <summary>
        /// Configuration ID this report is for
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Detailed findings
        /// </summary>
        public List<ReportFinding> Findings { get; set; } = new List<ReportFinding>();

        /// <summary>
        /// Report metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Report status
        /// </summary>
        public ReportStatus Status { get; set; }
    }

    /// <summary>
    /// Analytics event arguments
    /// </summary>
    public class AnalyticsEventArgs : EventArgs
    {
        /// <summary>
        /// Metric type
        /// </summary>
        public AnalyticsMetricType MetricType { get; set; }

        /// <summary>
        /// Metric data
        /// </summary>
        public object Metric { get; set; }

        /// <summary>
        /// Event type
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Event properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Health status changed event arguments
    /// </summary>
    public class HealthStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Old health status
        /// </summary>
        public ConfigurationHealthStatus OldStatus { get; set; }

        /// <summary>
        /// New health status
        /// </summary>
        public ConfigurationHealthStatus NewStatus { get; set; }

        /// <summary>
        /// Change timestamp
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Recommendation generated event arguments
    /// </summary>
    public class RecommendationGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// Recommendation type
        /// </summary>
        public string RecommendationType { get; set; }

        /// <summary>
        /// Recommendation description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Configuration ID
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Recommendation priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Analytics metric types
    /// </summary>
    public enum AnalyticsMetricType
    {
        /// <summary>
        /// Usage metric
        /// </summary>
        Usage,

        /// <summary>
        /// Performance metric
        /// </summary>
        Performance,

        /// <summary>
        /// Health metric
        /// </summary>
        Health,

        /// <summary>
        /// Error metric
        /// </summary>
        Error
    }

    /// <summary>
    /// Health levels
    /// </summary>
    public enum HealthLevel
    {
        /// <summary>
        /// Excellent health
        /// </summary>
        Excellent,

        /// <summary>
        /// Good health
        /// </summary>
        Good,

        /// <summary>
        /// Fair health
        /// </summary>
        Fair,

        /// <summary>
        /// Poor health
        /// </summary>
        Poor,

        /// <summary>
        /// Critical health
        /// </summary>
        Critical,

        /// <summary>
        /// Unknown health status
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Issue severity levels
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Informational issue
        /// </summary>
        Info,

        /// <summary>
        /// Warning issue
        /// </summary>
        Warning,

        /// <summary>
        /// Error issue
        /// </summary>
        Error,

        /// <summary>
        /// Critical issue
        /// </summary>
        Critical
    }


    /// <summary>
    /// Report status
    /// </summary>
    public enum ReportStatus
    {
        /// <summary>
        /// Report is pending
        /// </summary>
        Pending,

        /// <summary>
        /// Report is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Report is completed
        /// </summary>
        Completed,

        /// <summary>
        /// Report failed
        /// </summary>
        Failed,

        /// <summary>
        /// Report is cancelled
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Report finding
    /// </summary>
    public class ReportFinding
    {
        /// <summary>
        /// Finding ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Finding title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Finding description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Finding severity
        /// </summary>
        public ReportSeverity Severity { get; set; }

        /// <summary>
        /// Category of the finding
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Suggested action
        /// </summary>
        public string SuggestedAction { get; set; }

        /// <summary>
        /// Related configuration path
        /// </summary>
        public string ConfigurationPath { get; set; }
    }

    /// <summary>
    /// Report severity levels
    /// </summary>
    public enum ReportSeverity
    {
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