using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Advanced configuration service with profiles, templates, and advanced features
    /// </summary>
    public interface IAdvancedConfigurationService : IConfigurationService
    {
        /// <summary>
        /// Event fired when configuration profile changes
        /// </summary>
        event EventHandler<ConfigurationProfileChangedEventArgs> ProfileChanged;

        /// <summary>
        /// Event fired when configuration is backed up
        /// </summary>
        event EventHandler<ConfigurationBackupEventArgs> ConfigurationBackedUp;

        /// <summary>
        /// Event fired when configuration is restored
        /// </summary>
        event EventHandler<ConfigurationRestoreEventArgs> ConfigurationRestored;

        // Profile Management
        /// <summary>
        /// Gets all available configuration profiles
        /// </summary>
        /// <returns>List of configuration profiles</returns>
        Task<IEnumerable<ConfigurationProfile>> GetProfilesAsync();

        /// <summary>
        /// Gets a specific configuration profile
        /// </summary>
        /// <param name="profileId">The profile ID</param>
        /// <returns>The configuration profile</returns>
        Task<ConfigurationProfile> GetProfileAsync(string profileId);

        /// <summary>
        /// Creates a new configuration profile
        /// </summary>
        /// <param name="profile">The profile to create</param>
        /// <returns>The created profile</returns>
        Task<ConfigurationProfile> CreateProfileAsync(ConfigurationProfile profile);

        /// <summary>
        /// Updates an existing configuration profile
        /// </summary>
        /// <param name="profile">The profile to update</param>
        /// <returns>The updated profile</returns>
        Task<ConfigurationProfile> UpdateProfileAsync(ConfigurationProfile profile);

        /// <summary>
        /// Deletes a configuration profile
        /// </summary>
        /// <param name="profileId">The profile ID to delete</param>
        /// <returns>Task representing the async operation</returns>
        Task DeleteProfileAsync(string profileId);

        /// <summary>
        /// Activates a configuration profile
        /// </summary>
        /// <param name="profileId">The profile ID to activate</param>
        /// <returns>Task representing the async operation</returns>
        Task ActivateProfileAsync(string profileId);

        /// <summary>
        /// Gets the currently active profile
        /// </summary>
        /// <returns>The active configuration profile</returns>
        Task<ConfigurationProfile> GetActiveProfileAsync();

        /// <summary>
        /// Duplicates a configuration profile
        /// </summary>
        /// <param name="profileId">The profile ID to duplicate</param>
        /// <param name="newName">The name for the new profile</param>
        /// <returns>The duplicated profile</returns>
        Task<ConfigurationProfile> DuplicateProfileAsync(string profileId, string newName);

        // Template Management
        /// <summary>
        /// Gets all available configuration templates
        /// </summary>
        /// <returns>List of configuration templates</returns>
        Task<IEnumerable<ConfigurationTemplate>> GetTemplatesAsync();

        /// <summary>
        /// Gets a specific configuration template
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <returns>The configuration template</returns>
        Task<ConfigurationTemplate> GetTemplateAsync(string templateId);

        /// <summary>
        /// Creates a new configuration template
        /// </summary>
        /// <param name="template">The template to create</param>
        /// <returns>The created template</returns>
        Task<ConfigurationTemplate> CreateTemplateAsync(ConfigurationTemplate template);

        /// <summary>
        /// Updates an existing configuration template
        /// </summary>
        /// <param name="template">The template to update</param>
        /// <returns>The updated template</returns>
        Task<ConfigurationTemplate> UpdateTemplateAsync(ConfigurationTemplate template);

        /// <summary>
        /// Deletes a configuration template
        /// </summary>
        /// <param name="templateId">The template ID to delete</param>
        /// <returns>Task representing the async operation</returns>
        Task DeleteTemplateAsync(string templateId);

        /// <summary>
        /// Applies a configuration template to a profile
        /// </summary>
        /// <param name="templateId">The template ID to apply</param>
        /// <param name="profileId">The profile ID to apply to</param>
        /// <returns>Task representing the async operation</returns>
        Task ApplyTemplateAsync(string templateId, string profileId);

        /// <summary>
        /// Creates a template from an existing profile
        /// </summary>
        /// <param name="profileId">The profile ID to create template from</param>
        /// <param name="templateName">The name for the new template</param>
        /// <returns>The created template</returns>
        Task<ConfigurationTemplate> CreateTemplateFromProfileAsync(string profileId, string templateName);

        // Advanced AI Model Configuration
        /// <summary>
        /// Gets advanced AI model parameters
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <returns>Advanced model parameters</returns>
        Task<AIModelAdvancedParameters> GetAdvancedParametersAsync(AIProvider provider);

        /// <summary>
        /// Sets advanced AI model parameters
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <param name="parameters">The parameters to set</param>
        /// <returns>Task representing the async operation</returns>
        Task SetAdvancedParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters);

        /// <summary>
        /// Gets default parameters for a specific AI model
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <param name="modelName">The model name</param>
        /// <returns>Default parameters</returns>
        Task<AIModelAdvancedParameters> GetDefaultParametersAsync(AIProvider provider, string modelName);

        /// <summary>
        /// Tests AI model parameters
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <param name="parameters">The parameters to test</param>
        /// <returns>Test result</returns>
        Task<ParameterTestResult> TestParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters);

        // Backup and Restore
        /// <summary>
        /// Creates a backup of current configuration
        /// </summary>
        /// <param name="backupName">The backup name</param>
        /// <returns>The created backup</returns>
        Task<ConfigurationBackup> CreateBackupAsync(string backupName);

        /// <summary>
        /// Gets all available configuration backups
        /// </summary>
        /// <returns>List of configuration backups</returns>
        Task<IEnumerable<ConfigurationBackup>> GetBackupsAsync();

        /// <summary>
        /// Restores configuration from a backup
        /// </summary>
        /// <param name="backupId">The backup ID to restore</param>
        /// <returns>Task representing the async operation</returns>
        Task RestoreFromBackupAsync(string backupId);

        /// <summary>
        /// Deletes a configuration backup
        /// </summary>
        /// <param name="backupId">The backup ID to delete</param>
        /// <returns>Task representing the async operation</returns>
        Task DeleteBackupAsync(string backupId);

        /// <summary>
        /// Automatically creates backups on configuration changes
        /// </summary>
        /// <param name="enabled">Whether to enable auto-backup</param>
        /// <returns>Task representing the async operation</returns>
        Task SetAutoBackupAsync(bool enabled);

        /// <summary>
        /// Sets backup retention policy
        /// </summary>
        /// <param name="maxBackups">Maximum number of backups to retain</param>
        /// <param name="retentionDays">Number of days to retain backups</param>
        /// <returns>Task representing the async operation</returns>
        Task SetBackupRetentionAsync(int maxBackups, int retentionDays);

        // Import/Export Extensions
        /// <summary>
        /// Exports configuration profile to file
        /// </summary>
        /// <param name="profileId">The profile ID to export</param>
        /// <param name="filePath">The file path to export to</param>
        /// <param name="format">The export format</param>
        /// <returns>Task representing the async operation</returns>
        Task ExportProfileAsync(string profileId, string filePath, ConfigurationExportFormat format);

        /// <summary>
        /// Imports configuration profile from file
        /// </summary>
        /// <param name="filePath">The file path to import from</param>
        /// <param name="format">The import format</param>
        /// <returns>The imported profile</returns>
        Task<ConfigurationProfile> ImportProfileAsync(string filePath, ConfigurationExportFormat format);

        /// <summary>
        /// Exports configuration template to file
        /// </summary>
        /// <param name="templateId">The template ID to export</param>
        /// <param name="filePath">The file path to export to</param>
        /// <param name="format">The export format</param>
        /// <returns>Task representing the async operation</returns>
        Task ExportTemplateAsync(string templateId, string filePath, ConfigurationExportFormat format);

        /// <summary>
        /// Imports configuration template from file
        /// </summary>
        /// <param name="filePath">The file path to import from</param>
        /// <param name="format">The import format</param>
        /// <returns>The imported template</returns>
        Task<ConfigurationTemplate> ImportTemplateAsync(string filePath, ConfigurationExportFormat format);

        // Validation and Feedback
        /// <summary>
        /// Validates a configuration profile
        /// </summary>
        /// <param name="profile">The profile to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateProfileAsync(ConfigurationProfile profile);

        /// <summary>
        /// Validates AI model parameters
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <param name="parameters">The parameters to validate</param>
        /// <returns>Validation result</returns>
        Task<ParameterValidationResult> ValidateParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters);

        /// <summary>
        /// Gets configuration recommendations
        /// </summary>
        /// <returns>Configuration recommendations</returns>
        Task<IEnumerable<ConfigurationRecommendation>> GetRecommendationsAsync();

        /// <summary>
        /// Gets configuration health check
        /// </summary>
        /// <returns>Health check result</returns>
        Task<ConfigurationHealthCheck> GetHealthCheckAsync();

        // Settings Management
        /// <summary>
        /// Gets all configuration settings with metadata
        /// </summary>
        /// <returns>List of configuration settings</returns>
        Task<IEnumerable<ConfigurationSetting>> GetAllSettingsAsync();

        /// <summary>
        /// Gets configuration setting by key
        /// </summary>
        /// <param name="key">The setting key</param>
        /// <returns>The configuration setting</returns>
        Task<ConfigurationSetting> GetSettingAsync(string key);

        /// <summary>
        /// Updates configuration setting
        /// </summary>
        /// <param name="setting">The setting to update</param>
        /// <returns>Task representing the async operation</returns>
        Task UpdateSettingAsync(ConfigurationSetting setting);

        /// <summary>
        /// Resets specific setting to default
        /// </summary>
        /// <param name="key">The setting key to reset</param>
        /// <returns>Task representing the async operation</returns>
        Task ResetSettingAsync(string key);

        /// <summary>
        /// Gets configuration schema
        /// </summary>
        /// <returns>Configuration schema</returns>
        Task<ConfigurationSchema> GetSchemaAsync();

        // Monitoring and Analytics
        /// <summary>
        /// Gets configuration usage statistics
        /// </summary>
        /// <returns>Usage statistics</returns>
        Task<ConfigurationUsageStatistics> GetUsageStatisticsAsync();

        /// <summary>
        /// Gets configuration change history
        /// </summary>
        /// <param name="days">Number of days to look back</param>
        /// <returns>Change history</returns>
        Task<IEnumerable<ConfigurationChangeRecord>> GetChangeHistoryAsync(int days = 30);

        /// <summary>
        /// Records configuration usage
        /// </summary>
        /// <param name="feature">The feature used</param>
        /// <param name="value">The value used</param>
        /// <returns>Task representing the async operation</returns>
        Task RecordUsageAsync(string feature, string value);

        // Additional methods referenced in UI
        /// <summary>
        /// Saves advanced parameters
        /// </summary>
        /// <param name="parameters">The parameters to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveAdvancedParametersAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// Enables automatic backup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task EnableAutoBackupAsync();

        /// <summary>
        /// Disables automatic backup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task DisableAutoBackupAsync();

        /// <summary>
        /// Runs health check
        /// </summary>
        /// <returns>Health check results</returns>
        Task<HealthCheckResults> RunHealthCheckAsync();

        /// <summary>
        /// Gets configuration settings
        /// </summary>
        /// <returns>Configuration settings</returns>
        Task<Dictionary<string, object>> GetSettingsAsync();

        /// <summary>
        /// Saves configuration settings
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveSettingsAsync(Dictionary<string, object> settings);
    }
}