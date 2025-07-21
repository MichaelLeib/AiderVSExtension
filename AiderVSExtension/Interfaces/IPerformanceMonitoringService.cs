using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for performance monitoring and telemetry
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Event fired when performance metrics are collected
        /// </summary>
        event EventHandler<PerformanceMetricsEventArgs> MetricsCollected;

        /// <summary>
        /// Event fired when performance threshold is exceeded
        /// </summary>
        event EventHandler<PerformanceThresholdEventArgs> ThresholdExceeded;

        /// <summary>
        /// Starts monitoring a performance operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="category">Operation category</param>
        /// <returns>Performance tracker</returns>
        IPerformanceTracker StartOperation(string operationName, string category = null);

        /// <summary>
        /// Records a performance metric
        /// </summary>
        /// <param name="metric">Performance metric to record</param>
        Task RecordMetricAsync(PerformanceMetric metric);

        /// <summary>
        /// Records multiple performance metrics
        /// </summary>
        /// <param name="metrics">Performance metrics to record</param>
        Task RecordMetricsAsync(IEnumerable<PerformanceMetric> metrics);

        /// <summary>
        /// Gets performance statistics for a time period
        /// </summary>
        /// <param name="timeRange">Time range for statistics</param>
        /// <param name="category">Optional category filter</param>
        /// <returns>Performance statistics</returns>
        Task<PerformanceStatistics> GetStatisticsAsync(TimeRange timeRange, string category = null);

        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        /// <returns>Current performance metrics</returns>
        Task<CurrentPerformanceMetrics> GetCurrentMetricsAsync();

        /// <summary>
        /// Clears old performance data
        /// </summary>
        /// <param name="olderThan">Clear data older than this date</param>
        Task ClearOldDataAsync(DateTime? olderThan = null);

        /// <summary>
        /// Sets performance thresholds for monitoring
        /// </summary>
        /// <param name="thresholds">Performance thresholds</param>
        Task SetThresholdsAsync(Dictionary<string, PerformanceThreshold> thresholds);

        /// <summary>
        /// Gets performance recommendations
        /// </summary>
        /// <returns>Performance recommendations</returns>
        Task<List<PerformanceRecommendation>> GetRecommendationsAsync();

        /// <summary>
        /// Exports performance data
        /// </summary>
        /// <param name="format">Export format</param>
        /// <param name="timeRange">Time range for export</param>
        /// <returns>Exported performance data</returns>
        Task<string> ExportDataAsync(PerformanceDataFormat format, TimeRange timeRange);
    }

    /// <summary>
    /// Interface for tracking individual performance operations
    /// </summary>
    public interface IPerformanceTracker : IDisposable
    {
        /// <summary>
        /// Operation name
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Operation category
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Operation start time
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Current elapsed time
        /// </summary>
        TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Whether the operation is still active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Additional metadata for the operation
        /// </summary>
        Dictionary<string, object> Metadata { get; }

        /// <summary>
        /// Adds metadata to the operation
        /// </summary>
        /// <param name="key">Metadata key</param>
        /// <param name="value">Metadata value</param>
        void AddMetadata(string key, object value);

        /// <summary>
        /// Records a checkpoint in the operation
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint</param>
        /// <param name="description">Optional description</param>
        void RecordCheckpoint(string checkpointName, string description = null);

        /// <summary>
        /// Marks the operation as completed successfully
        /// </summary>
        void Complete();

        /// <summary>
        /// Marks the operation as failed
        /// </summary>
        /// <param name="error">Error that caused the failure</param>
        void Fail(Exception error);

        /// <summary>
        /// Marks the operation as cancelled
        /// </summary>
        void Cancel();
    }
}