using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Service for tracking analytics related to configuration changes
    /// </summary>
    public interface IConfigurationAnalyticsService
    {
        /// <summary>
        /// Tracks a configuration change event
        /// </summary>
        /// <param name="configurationKey">The configuration key that changed</param>
        /// <param name="oldValue">The previous value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="source">The source of the change</param>
        Task TrackConfigurationChangeAsync(string configurationKey, object oldValue, object newValue, string source);

        /// <summary>
        /// Tracks configuration access
        /// </summary>
        /// <param name="configurationKey">The configuration key accessed</param>
        /// <param name="accessType">The type of access (read/write)</param>
        Task TrackConfigurationAccessAsync(string configurationKey, string accessType);

        /// <summary>
        /// Tracks configuration validation events
        /// </summary>
        /// <param name="configurationKey">The configuration key validated</param>
        /// <param name="isValid">Whether the validation passed</param>
        /// <param name="validationErrors">Any validation errors</param>
        Task TrackConfigurationValidationAsync(string configurationKey, bool isValid, IEnumerable<string> validationErrors);

        /// <summary>
        /// Gets configuration usage statistics
        /// </summary>
        /// <param name="timeRange">The time range for statistics</param>
        /// <returns>Usage statistics</returns>
        Task<Dictionary<string, object>> GetConfigurationUsageStatsAsync(TimeSpan timeRange);

        /// <summary>
        /// Tracks configuration migration events
        /// </summary>
        /// <param name="fromVersion">The version migrated from</param>
        /// <param name="toVersion">The version migrated to</param>
        /// <param name="migratedKeys">The configuration keys that were migrated</param>
        Task TrackConfigurationMigrationAsync(string fromVersion, string toVersion, IEnumerable<string> migratedKeys);

        /// <summary>
        /// Tracks configuration export/import events
        /// </summary>
        /// <param name="operation">The operation type (export/import)</param>
        /// <param name="format">The format used</param>
        /// <param name="keyCount">The number of keys processed</param>
        Task TrackConfigurationPortabilityAsync(string operation, string format, int keyCount);
    }
}
