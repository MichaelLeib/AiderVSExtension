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
        /// Overall health status
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// Health score (0-100)
        /// </summary>
        public int HealthScore { get; set; }

        /// <summary>
        /// List of detected issues
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();

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
    /// Analytics event arguments
    /// </summary>
    public class AnalyticsEventArgs : EventArgs
    {
        public string EventType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Health status changed event arguments
    /// </summary>
    public class HealthStatusChangedEventArgs : EventArgs
    {
        public ConfigurationHealthStatus OldStatus { get; set; }
        public ConfigurationHealthStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Recommendation generated event arguments
    /// </summary>
    public class RecommendationGeneratedEventArgs : EventArgs
    {
        public string RecommendationType { get; set; }
        public string Description { get; set; }
        public string ConfigurationId { get; set; }
        public int Priority { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}