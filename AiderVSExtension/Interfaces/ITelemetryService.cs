using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;
using AiderVSExtension.Services;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for telemetry and monitoring services
    /// </summary>
    public interface ITelemetryService : IDisposable
    {
        /// <summary>
        /// Tracks an event with optional properties and metrics
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        /// <param name="metrics">Event metrics</param>
        void TrackEvent(string eventName, Dictionary<string, string> properties = null, Dictionary<string, double> metrics = null);

        /// <summary>
        /// Tracks an exception with context information
        /// </summary>
        /// <param name="exception">Exception to track</param>
        /// <param name="properties">Additional properties</param>
        void TrackException(Exception exception, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks performance metrics for an operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="success">Whether the operation succeeded</param>
        /// <param name="properties">Additional properties</param>
        void TrackPerformance(string operationName, TimeSpan duration, bool success = true, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks user interaction events
        /// </summary>
        /// <param name="action">User action performed</param>
        /// <param name="component">UI component involved</param>
        /// <param name="properties">Additional properties</param>
        void TrackUserAction(string action, string component, Dictionary<string, string> properties = null);

        /// <summary>
        /// Tracks AI service usage
        /// </summary>
        /// <param name="provider">AI provider used</param>
        /// <param name="operation">Operation performed</param>
        /// <param name="responseTime">Response time</param>
        /// <param name="success">Whether the operation succeeded</param>
        /// <param name="tokenCount">Number of tokens used</param>
        void TrackAIUsage(AIProvider provider, string operation, TimeSpan responseTime, bool success, int tokenCount = 0);

        /// <summary>
        /// Gets performance summary for a specific operation
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Performance summary</returns>
        PerformanceSummary GetPerformanceSummary(string operationName);

        /// <summary>
        /// Flushes all pending telemetry events
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task FlushAsync();
    }
}