using System;
using System.Collections.Generic;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Migration context
    /// </summary>
    public class MigrationContext
    {
        /// <summary>
        /// Migration ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Source configuration profile
        /// </summary>
        public ConfigurationProfile SourceProfile { get; set; }

        /// <summary>
        /// Source version
        /// </summary>
        public string SourceVersion { get; set; }

        /// <summary>
        /// Target version
        /// </summary>
        public string TargetVersion { get; set; }

        /// <summary>
        /// Migration options
        /// </summary>
        public MigrationOptions Options { get; set; }

        /// <summary>
        /// Migration start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Migration end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether migration was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Migration steps
        /// </summary>
        public List<MigrationStep> Steps { get; set; } = new List<MigrationStep>();

        /// <summary>
        /// Result profile
        /// </summary>
        public ConfigurationProfile ResultProfile { get; set; }

        /// <summary>
        /// Migration duration
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Migration step context
    /// </summary>
    public class MigrationStepContext
    {
        /// <summary>
        /// Source profile
        /// </summary>
        public ConfigurationProfile SourceProfile { get; set; }

        /// <summary>
        /// Migration step
        /// </summary>
        public MigrationStep Step { get; set; }

        /// <summary>
        /// Overall migration context
        /// </summary>
        public MigrationContext Context { get; set; }

        /// <summary>
        /// Step start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Step end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether step was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Result profile
        /// </summary>
        public ConfigurationProfile ResultProfile { get; set; }

        /// <summary>
        /// Step duration
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Migration step
    /// </summary>
    public class MigrationStep
    {
        /// <summary>
        /// Step ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Step description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Source version
        /// </summary>
        public string SourceVersion { get; set; }

        /// <summary>
        /// Target version
        /// </summary>
        public string TargetVersion { get; set; }

        /// <summary>
        /// Migration strategy
        /// </summary>
        public IMigrationStrategy Strategy { get; set; }

        /// <summary>
        /// Whether the step is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Estimated duration
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Step priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Step metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Migration options
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// Whether to create backup before migration
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Whether to validate after migration
        /// </summary>
        public bool ValidateAfterMigration { get; set; } = true;

        /// <summary>
        /// Whether to preserve original metadata
        /// </summary>
        public bool PreserveMetadata { get; set; } = true;

        /// <summary>
        /// Custom migration settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Migration timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Whether to continue on non-critical errors
        /// </summary>
        public bool ContinueOnErrors { get; set; } = false;
    }

    /// <summary>
    /// Import options
    /// </summary>
    public class ImportOptions
    {
        /// <summary>
        /// Whether to validate after import
        /// </summary>
        public bool ValidateAfterImport { get; set; } = true;

        /// <summary>
        /// Whether to merge with existing configuration
        /// </summary>
        public bool MergeWithExisting { get; set; } = false;

        /// <summary>
        /// Import format specific options
        /// </summary>
        public Dictionary<string, object> FormatOptions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Fields to exclude from import
        /// </summary>
        public List<string> ExcludeFields { get; set; } = new List<string>();

        /// <summary>
        /// Fields to include in import
        /// </summary>
        public List<string> IncludeFields { get; set; } = new List<string>();

        /// <summary>
        /// Default values for missing fields
        /// </summary>
        public Dictionary<string, object> DefaultValues { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Export options
    /// </summary>
    public class ExportOptions
    {
        /// <summary>
        /// Whether to include sensitive data
        /// </summary>
        public bool IncludeSensitiveData { get; set; } = false;

        /// <summary>
        /// Whether to pretty format output
        /// </summary>
        public bool PrettyFormat { get; set; } = true;

        /// <summary>
        /// Export format specific options
        /// </summary>
        public Dictionary<string, object> FormatOptions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Fields to exclude from export
        /// </summary>
        public List<string> ExcludeFields { get; set; } = new List<string>();

        /// <summary>
        /// Fields to include in export
        /// </summary>
        public List<string> IncludeFields { get; set; } = new List<string>();

        /// <summary>
        /// Whether to compress output
        /// </summary>
        public bool Compress { get; set; } = false;
    }

    /// <summary>
    /// Export result
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Whether export was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Exported file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Export format
        /// </summary>
        public ConfigurationFormat Format { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Export timestamp
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// Exported by
        /// </summary>
        public string ExportedBy { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Export metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Backup result
    /// </summary>
    public class BackupResult
    {
        /// <summary>
        /// Whether backup was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Backup file path
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Backup creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Backup file size
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Backup metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Import metadata
    /// </summary>
    public class ImportMetadata
    {
        /// <summary>
        /// Source file path
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// Source format
        /// </summary>
        public ConfigurationFormat SourceFormat { get; set; }

        /// <summary>
        /// Import timestamp
        /// </summary>
        public DateTime ImportedAt { get; set; }

        /// <summary>
        /// Imported by
        /// </summary>
        public string ImportedBy { get; set; }

        /// <summary>
        /// Validation result
        /// </summary>
        public ConfigurationValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Import options used
        /// </summary>
        public ImportOptions Options { get; set; }

        /// <summary>
        /// Source file hash
        /// </summary>
        public string SourceFileHash { get; set; }
    }

    /// <summary>
    /// Migration progress event arguments
    /// </summary>
    public class MigrationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Migration ID
        /// </summary>
        public string MigrationId { get; set; }

        /// <summary>
        /// Current migration step
        /// </summary>
        public MigrationStep CurrentStep { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Progress timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Migration completed event arguments
    /// </summary>
    public class MigrationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Migration ID
        /// </summary>
        public string MigrationId { get; set; }

        /// <summary>
        /// Source profile
        /// </summary>
        public ConfigurationProfile SourceProfile { get; set; }

        /// <summary>
        /// Result profile
        /// </summary>
        public ConfigurationProfile ResultProfile { get; set; }

        /// <summary>
        /// Migration duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Validation result
        /// </summary>
        public ConfigurationValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Completion timestamp
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Migration failed event arguments
    /// </summary>
    public class MigrationFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Migration ID
        /// </summary>
        public string MigrationId { get; set; }

        /// <summary>
        /// Source profile
        /// </summary>
        public ConfigurationProfile SourceProfile { get; set; }

        /// <summary>
        /// Error that occurred
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Failed step
        /// </summary>
        public MigrationStep FailedStep { get; set; }

        /// <summary>
        /// Failure timestamp
        /// </summary>
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration formats
    /// </summary>
    public enum ConfigurationFormat
    {
        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// YAML format
        /// </summary>
        Yaml,

        /// <summary>
        /// TOML format
        /// </summary>
        Toml,

        /// <summary>
        /// INI format
        /// </summary>
        Ini,

        /// <summary>
        /// Binary format
        /// </summary>
        Binary,

        /// <summary>
        /// Custom format
        /// </summary>
        Custom
    }
}