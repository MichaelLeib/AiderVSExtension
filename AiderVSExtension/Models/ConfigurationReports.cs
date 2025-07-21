using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Configuration report data
    /// </summary>
    public class ConfigurationReport
    {
        /// <summary>
        /// Report ID
        /// </summary>
        public string ReportId { get; set; }

        /// <summary>
        /// Report title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Report generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Report type
        /// </summary>
        public string ReportType { get; set; }

        /// <summary>
        /// Configuration ID this report is for
        /// </summary>
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Report summary
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Detailed findings
        /// </summary>
        public List<ReportFinding> Findings { get; set; } = new List<ReportFinding>();

        /// <summary>
        /// Recommendations
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();

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
    /// Configuration usage metric
    /// </summary>
    public class ConfigurationUsageMetric
    {
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
        /// Measurement timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

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
    /// Report status
    /// </summary>
    public enum ReportStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Report severity
    /// </summary>
    public enum ReportSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }
}