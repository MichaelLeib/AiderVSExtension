using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for configuration migration and import/export
    /// </summary>
    public interface IConfigurationMigrationService
    {
        /// <summary>
        /// Event fired during migration progress
        /// </summary>
        event EventHandler<MigrationProgressEventArgs> MigrationProgress;

        /// <summary>
        /// Event fired when migration is completed
        /// </summary>
        event EventHandler<MigrationCompletedEventArgs> MigrationCompleted;

        /// <summary>
        /// Event fired when migration fails
        /// </summary>
        event EventHandler<MigrationFailedEventArgs> MigrationFailed;

        /// <summary>
        /// Migrates configuration from one version to another
        /// </summary>
        /// <param name="profile">Profile to migrate</param>
        /// <param name="targetVersion">Target version</param>
        /// <param name="options">Migration options</param>
        /// <returns>Migrated profile</returns>
        Task<ConfigurationProfile> MigrateAsync(ConfigurationProfile profile, string targetVersion, MigrationOptions options = null);

        /// <summary>
        /// Imports configuration from external file
        /// </summary>
        /// <param name="filePath">Path to configuration file</param>
        /// <param name="format">File format</param>
        /// <param name="options">Import options</param>
        /// <returns>Imported profile</returns>
        Task<ConfigurationProfile> ImportAsync(string filePath, ConfigurationFormat format, ImportOptions options = null);

        /// <summary>
        /// Exports configuration to external file
        /// </summary>
        /// <param name="profile">Profile to export</param>
        /// <param name="filePath">Export file path</param>
        /// <param name="format">Export format</param>
        /// <param name="options">Export options</param>
        /// <returns>Export result</returns>
        Task<ExportResult> ExportAsync(ConfigurationProfile profile, string filePath, ConfigurationFormat format, ExportOptions options = null);

        /// <summary>
        /// Gets available migration paths
        /// </summary>
        /// <param name="sourceVersion">Source version</param>
        /// <param name="targetVersion">Target version</param>
        /// <returns>List of migration steps</returns>
        Task<List<MigrationStep>> GetMigrationPathAsync(string sourceVersion, string targetVersion);

        /// <summary>
        /// Gets supported import/export formats
        /// </summary>
        /// <returns>List of supported formats</returns>
        IEnumerable<ConfigurationFormat> GetSupportedFormats();

        /// <summary>
        /// Checks if migration is required
        /// </summary>
        /// <param name="profile">Profile to check</param>
        /// <param name="targetVersion">Target version</param>
        /// <returns>True if migration is required</returns>
        bool IsMigrationRequired(ConfigurationProfile profile, string targetVersion);

        /// <summary>
        /// Creates backup before migration
        /// </summary>
        /// <param name="profile">Profile to backup</param>
        /// <param name="backupPath">Backup file path</param>
        /// <returns>Backup result</returns>
        Task<BackupResult> CreateBackupAsync(ConfigurationProfile profile, string backupPath = null);
    }

}