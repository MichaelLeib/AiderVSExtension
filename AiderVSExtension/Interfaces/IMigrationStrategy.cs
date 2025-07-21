using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for implementing migration strategies
    /// </summary>
    public interface IMigrationStrategy
    {
        /// <summary>
        /// Gets the name of the migration strategy
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version this strategy migrates from
        /// </summary>
        string FromVersion { get; }

        /// <summary>
        /// Gets the version this strategy migrates to
        /// </summary>
        string ToVersion { get; }

        /// <summary>
        /// Gets a value indicating whether this migration can be rolled back
        /// </summary>
        bool CanRollback { get; }

        /// <summary>
        /// Determines if this strategy can handle the migration from the specified version
        /// </summary>
        /// <param name="currentVersion">The current version</param>
        /// <returns>True if this strategy can handle the migration</returns>
        bool CanMigrate(string currentVersion);

        /// <summary>
        /// Executes the migration
        /// </summary>
        /// <param name="context">The migration context</param>
        /// <returns>The migration result</returns>
        Task<MigrationResult> MigrateAsync(IMigrationContext context);

        /// <summary>
        /// Rolls back the migration if supported
        /// </summary>
        /// <param name="context">The migration context</param>
        /// <returns>The rollback result</returns>
        Task<MigrationResult> RollbackAsync(IMigrationContext context);

        /// <summary>
        /// Validates the migration before execution
        /// </summary>
        /// <param name="context">The migration context</param>
        /// <returns>The validation result</returns>
        Task<ValidationResult> ValidateMigrationAsync(IMigrationContext context);
    }

    /// <summary>
    /// Interface for migration context
    /// </summary>
    public interface IMigrationContext
    {
        /// <summary>
        /// Gets the current configuration data
        /// </summary>
        Dictionary<string, object> CurrentData { get; }

        /// <summary>
        /// Gets the backup data (if available)
        /// </summary>
        Dictionary<string, object> BackupData { get; }

        /// <summary>
        /// Gets migration-specific settings
        /// </summary>
        Dictionary<string, object> Settings { get; }

        /// <summary>
        /// Logs a migration message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level</param>
        void Log(string message, LogLevel level = LogLevel.Information);

        /// <summary>
        /// Reports migration progress
        /// </summary>
        /// <param name="percentage">The completion percentage (0-100)</param>
        /// <param name="message">Optional progress message</param>
        void ReportProgress(int percentage, string message = null);
    }


    /// <summary>
    /// Log levels for migration logging
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }
}
