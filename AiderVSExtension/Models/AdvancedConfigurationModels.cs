using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a configuration profile
    /// </summary>
    public class ConfigurationProfile
    {
        /// <summary>
        /// The profile ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The profile name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// The profile description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Whether this is the default profile
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Whether this profile is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The profile category
        /// </summary>
        public ProfileCategory Category { get; set; } = ProfileCategory.User;

        /// <summary>
        /// The profile tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// The AI model configuration
        /// </summary>
        public AIModelConfiguration AIModelConfiguration { get; set; } = new AIModelConfiguration();

        /// <summary>
        /// Alias for AIModelConfiguration for backward compatibility
        /// </summary>
        public AIModelConfiguration AIConfiguration => AIModelConfiguration;

        /// <summary>
        /// Advanced AI model parameters
        /// </summary>
        public Dictionary<AIProvider, AIModelAdvancedParameters> AdvancedParameters { get; set; } = new Dictionary<AIProvider, AIModelAdvancedParameters>();

        /// <summary>
        /// Configuration settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Profile metadata
        /// </summary>
        public ProfileMetadata Metadata { get; set; } = new ProfileMetadata();

        /// <summary>
        /// Profile version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Profile author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Whether this profile is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Profile icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Profile color
        /// </summary>
        public string Color { get; set; }
    }

    /// <summary>
    /// Represents a configuration template
    /// </summary>
    public class ConfigurationTemplate
    {
        /// <summary>
        /// The template ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The template name
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// The template description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// The template category
        /// </summary>
        public TemplateCategory Category { get; set; } = TemplateCategory.User;

        /// <summary>
        /// The template tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Template settings
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// AI model configuration template
        /// </summary>
        public AIModelConfiguration AIModelConfiguration { get; set; } = new AIModelConfiguration();

        /// <summary>
        /// Advanced parameters template
        /// </summary>
        public Dictionary<AIProvider, AIModelAdvancedParameters> AdvancedParameters { get; set; } = new Dictionary<AIProvider, AIModelAdvancedParameters>();

        /// <summary>
        /// Template metadata
        /// </summary>
        public TemplateMetadata Metadata { get; set; } = new TemplateMetadata();

        /// <summary>
        /// Template version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Template author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Whether this template is built-in
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Template icon
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Template color
        /// </summary>
        public string Color { get; set; }
    }

    /// <summary>
    /// Advanced AI model parameters
    /// </summary>
    public class AIModelAdvancedParameters
    {
        /// <summary>
        /// The AI provider
        /// </summary>
        public AIProvider Provider { get; set; }

        /// <summary>
        /// The model name
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Temperature for response randomness (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Maximum tokens in response
        /// </summary>
        [Range(1, 32000)]
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Top-p for nucleus sampling (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double TopP { get; set; } = 0.95;

        /// <summary>
        /// Top-k for top-k sampling
        /// </summary>
        [Range(1, 100)]
        public int TopK { get; set; } = 40;

        /// <summary>
        /// Frequency penalty (-2.0 to 2.0)
        /// </summary>
        [Range(-2.0, 2.0)]
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// Presence penalty (-2.0 to 2.0)
        /// </summary>
        [Range(-2.0, 2.0)]
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// Stop sequences
        /// </summary>
        public List<string> StopSequences { get; set; } = new List<string>();

        /// <summary>
        /// System prompt
        /// </summary>
        [StringLength(10000)]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Context window size
        /// </summary>
        [Range(1, 128000)]
        public int ContextWindow { get; set; } = 4096;

        /// <summary>
        /// Response timeout in seconds
        /// </summary>
        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of retries on failure
        /// </summary>
        [Range(0, 10)]
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Streaming response
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// Custom parameters for specific providers
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Parameter validation rules
        /// </summary>
        public List<ParameterValidationRule> ValidationRules { get; set; } = new List<ParameterValidationRule>();
    }

    /// <summary>
    /// Configuration backup
    /// </summary>
    public class ConfigurationBackup
    {
        /// <summary>
        /// The backup ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The backup name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The backup description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Backup size in bytes
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Backup type
        /// </summary>
        public BackupType Type { get; set; } = BackupType.Manual;

        /// <summary>
        /// Configuration data
        /// </summary>
        public string ConfigurationData { get; set; }

        /// <summary>
        /// Backup version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Backup checksum
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Backup metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration setting with metadata
    /// </summary>
    public class ConfigurationSetting
    {
        /// <summary>
        /// The setting key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The setting value
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The setting default value
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// The setting data type
        /// </summary>
        public Type DataType { get; set; }

        /// <summary>
        /// The setting display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The setting description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The setting category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Whether the setting is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Whether the setting is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the setting is advanced
        /// </summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// Setting validation rules
        /// </summary>
        public List<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();

        /// <summary>
        /// Possible values for the setting
        /// </summary>
        public List<object> PossibleValues { get; set; } = new List<object>();

        /// <summary>
        /// Setting metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration recommendation
    /// </summary>
    public class ConfigurationRecommendation
    {
        /// <summary>
        /// The recommendation ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The recommendation title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The recommendation description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The recommendation category
        /// </summary>
        public RecommendationCategory Category { get; set; }

        /// <summary>
        /// The recommendation priority
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// The setting key to modify
        /// </summary>
        public string SettingKey { get; set; }

        /// <summary>
        /// The recommended value
        /// </summary>
        public object RecommendedValue { get; set; }

        /// <summary>
        /// The current value
        /// </summary>
        public object CurrentValue { get; set; }

        /// <summary>
        /// The expected impact
        /// </summary>
        public string ExpectedImpact { get; set; }

        /// <summary>
        /// Whether the recommendation is automated
        /// </summary>
        public bool IsAutomated { get; set; }

        /// <summary>
        /// The recommendation reason
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Additional information
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The recommendation type
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// Recommended actions to take
        /// </summary>
        public List<string> Actions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration health check result
    /// </summary>
    public class ConfigurationHealthCheck
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// Health check score (0-100)
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Health check issues
        /// </summary>
        public List<HealthIssue> Issues { get; set; } = new List<HealthIssue>();

        /// <summary>
        /// Health check recommendations
        /// </summary>
        public List<ConfigurationRecommendation> Recommendations { get; set; } = new List<ConfigurationRecommendation>();

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Health check details
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Configuration usage statistics
    /// </summary>
    public class ConfigurationUsageStatistics
    {
        /// <summary>
        /// Total configuration changes
        /// </summary>
        public int TotalChanges { get; set; }

        /// <summary>
        /// Most used settings
        /// </summary>
        public List<SettingUsage> MostUsedSettings { get; set; } = new List<SettingUsage>();

        /// <summary>
        /// Most used AI models
        /// </summary>
        public List<ModelUsage> MostUsedModels { get; set; } = new List<ModelUsage>();

        /// <summary>
        /// Configuration errors
        /// </summary>
        public List<ConfigurationError> Errors { get; set; } = new List<ConfigurationError>();

        /// <summary>
        /// Usage by time period
        /// </summary>
        public Dictionary<string, int> UsageByPeriod { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Statistics generation date
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration change record
    /// </summary>
    public class ConfigurationChangeRecord
    {
        /// <summary>
        /// The change ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The setting key that changed
        /// </summary>
        public string SettingKey { get; set; }

        /// <summary>
        /// The old value
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// The new value
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// The change type
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// The change timestamp
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user who made the change
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// The change reason
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Parameter test result
    /// </summary>
    public class ParameterTestResult
    {
        /// <summary>
        /// Whether the test was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Test error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Test response time
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Test output
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Test metrics
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Test timestamp
        /// </summary>
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Parameter validation result
    /// </summary>
    public class ParameterValidationResult
    {
        /// <summary>
        /// Whether the parameters are valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Validation suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration schema
    /// </summary>
    public class ConfigurationSchema
    {
        /// <summary>
        /// Schema version
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Schema settings
        /// </summary>
        public List<ConfigurationSetting> Settings { get; set; } = new List<ConfigurationSetting>();

        /// <summary>
        /// Schema categories
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Schema metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    #region Supporting Classes

    /// <summary>
    /// Profile metadata
    /// </summary>
    public class ProfileMetadata
    {
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> CustomMetadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Template metadata
    /// </summary>
    public class TemplateMetadata
    {
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> CustomMetadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Parameter validation rule
    /// </summary>
    public class ParameterValidationRule
    {
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
        public Func<object, bool> ValidationFunction { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Validation rule
    /// </summary>
    public class ValidationRule
    {
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Health issue
    /// </summary>
    public class HealthIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public HealthIssueSeverity Severity { get; set; }
        public string SettingKey { get; set; }
        public string Resolution { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Issue category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Issue impact description
        /// </summary>
        public string Impact { get; set; }
    }

    /// <summary>
    /// Setting usage
    /// </summary>
    public class SettingUsage
    {
        public string SettingKey { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public object MostUsedValue { get; set; }
    }

    /// <summary>
    /// Model usage
    /// </summary>
    public class ModelUsage
    {
        public AIProvider Provider { get; set; }
        public string ModelName { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public double AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Configuration error
    /// </summary>
    public class ConfigurationError
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ErrorMessage { get; set; }
        public string SettingKey { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public int Count { get; set; }
        public string Resolution { get; set; }
    }

    #endregion

    #region Event Arguments

    /// <summary>
    /// Configuration profile changed event arguments
    /// </summary>
    public class ConfigurationProfileChangedEventArgs : EventArgs
    {
        public string OldProfileId { get; set; }
        public string NewProfileId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuration backup event arguments
    /// </summary>
    public class ConfigurationBackupEventArgs : EventArgs
    {
        public string BackupId { get; set; }
        public string BackupName { get; set; }
        public DateTime BackupAt { get; set; } = DateTime.UtcNow;
        public long BackupSize { get; set; }
    }

    /// <summary>
    /// Configuration restore event arguments
    /// </summary>
    public class ConfigurationRestoreEventArgs : EventArgs
    {
        public string BackupId { get; set; }
        public string BackupName { get; set; }
        public DateTime RestoredAt { get; set; } = DateTime.UtcNow;
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Profile category
    /// </summary>
    public enum ProfileCategory
    {
        User,
        System,
        Team,
        Template,
        Preset
    }

    /// <summary>
    /// Template category
    /// </summary>
    public enum TemplateCategory
    {
        User,
        System,
        BuiltIn,
        Community,
        Enterprise
    }

    /// <summary>
    /// Configuration export format
    /// </summary>
    public enum ConfigurationExportFormat
    {
        Json,
        Xml,
        Yaml,
        Binary
    }

    /// <summary>
    /// Backup type
    /// </summary>
    public enum BackupType
    {
        Manual,
        Automatic,
        Scheduled,
        PreChange
    }

    /// <summary>
    /// Recommendation category
    /// </summary>
    public enum RecommendationCategory
    {
        Performance,
        Security,
        Usability,
        Reliability,
        Maintainability
    }

    /// <summary>
    /// Recommendation priority
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Recommendation type
    /// </summary>
    public enum RecommendationType
    {
        Usage,
        Performance,
        Configuration,
        Update,
        Security
    }

    /// <summary>
    /// Health status
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    /// <summary>
    /// Health issue severity
    /// </summary>
    public enum HealthIssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }


    #endregion

    /// <summary>
    /// Health check results
    /// </summary>
    public class HealthCheckResults
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// List of health check items
        /// </summary>
        public List<HealthCheckItem> Items { get; set; } = new List<HealthCheckItem>();

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Individual health check item
    /// </summary>
    public class HealthCheckItem
    {
        /// <summary>
        /// Check name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Check status
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Check message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error details if any
        /// </summary>
        public string Error { get; set; }
    }
}