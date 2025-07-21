using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing extension configuration and settings
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Event fired when configuration changes
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        /// <summary>
        /// Gets the current AI model configuration
        /// </summary>
        /// <returns>The current AI model configuration</returns>
        AIModelConfiguration GetAIModelConfiguration();

        /// <summary>
        /// Sets the AI model configuration
        /// </summary>
        /// <param name="configuration">The configuration to set</param>
        /// <returns>Task representing the async operation</returns>
        Task SetAIModelConfigurationAsync(AIModelConfiguration configuration);

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <typeparam name="T">The type of the configuration value</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        T GetValue<T>(string key, T defaultValue = default(T));

        /// <summary>
        /// Sets a configuration value by key
        /// </summary>
        /// <typeparam name="T">The type of the configuration value</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        /// <returns>Task representing the async operation</returns>
        Task SetValueAsync<T>(string key, T value);

        /// <summary>
        /// Gets a configuration setting by key (alias for GetValue for compatibility)
        /// </summary>
        /// <typeparam name="T">The type of the configuration value</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        T GetSetting<T>(string key, T defaultValue = default(T));

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        /// <returns>Validation result with any errors</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationAsync();

        /// <summary>
        /// Resets configuration to default values
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ResetToDefaultsAsync();

        /// <summary>
        /// Exports configuration to a file
        /// </summary>
        /// <param name="filePath">The file path to export to</param>
        /// <returns>Task representing the async operation</returns>
        Task ExportConfigurationAsync(string filePath);

        /// <summary>
        /// Imports configuration from a file
        /// </summary>
        /// <param name="filePath">The file path to import from</param>
        /// <returns>Task representing the async operation</returns>
        Task ImportConfigurationAsync(string filePath);

        /// <summary>
        /// Tests the connection to the configured AI provider
        /// </summary>
        /// <returns>Connection test result</returns>
        Task<ConnectionTestResult> TestConnectionAsync();
        
        /// <summary>
        /// Tests the connection for a specific configuration
        /// </summary>
        /// <param name="configuration">The configuration to test</param>
        /// <returns>Connection test result</returns>
        Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration configuration);
        
        /// <summary>
        /// Validates a specific configuration
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <returns>Validation result with any errors</returns>
        Task<ConfigurationValidationResult> ValidateConfigurationAsync(AIModelConfiguration configuration);
        
        /// <summary>
        /// Saves a configuration asynchronously
        /// </summary>
        /// <param name="configuration">The configuration to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveConfigurationAsync(AIModelConfiguration configuration);
        
        /// <summary>
        /// Gets a configuration asynchronously
        /// </summary>
        /// <returns>Task containing the current configuration</returns>
        Task<AIModelConfiguration> GetConfigurationAsync();
        
        /// <summary>
        /// Gets a configuration synchronously
        /// </summary>
        /// <returns>The current configuration</returns>
        AIModelConfiguration GetConfiguration();
        
        /// <summary>
        /// Gets whether AI completion is enabled
        /// </summary>
        bool IsAICompletionEnabled { get; set; }
        
        /// <summary>
        /// Toggles AI completion on/off
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ToggleAICompletionAsync();

        /// <summary>
        /// Gets all available AI model configurations
        /// </summary>
        /// <returns>List of available configurations</returns>
        List<AIModelConfiguration> GetAvailableConfigurations();

        /// <summary>
        /// Gets all AI model configurations asynchronously
        /// </summary>
        /// <returns>Task containing list of all model configurations</returns>
        Task<IEnumerable<AIModelConfiguration>> GetAllModelConfigurationsAsync();

        /// <summary>
        /// Gets the ID of the currently active AI model
        /// </summary>
        /// <returns>Task containing the active model ID</returns>
        Task<string> GetActiveModelIdAsync();

        /// <summary>
        /// Sets the ID of the active AI model
        /// </summary>
        /// <param name="modelId">The model ID to set as active</param>
        /// <returns>Task representing the async operation</returns>
        Task SetActiveModelIdAsync(string modelId);

        /// <summary>
        /// Migrates configuration from older versions
        /// </summary>
        /// <returns>Migration result</returns>
        Task<MigrationResult> MigrateConfigurationAsync();
    }

    /// <summary>
    /// Event arguments for configuration changed events
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }



    /// <summary>
    /// Migration result
    /// </summary>
    public class MigrationResult
    {
        public bool IsSuccessful { get; set; }
        public string? FromVersion { get; set; }
        public string? ToVersion { get; set; }
        public List<string> MigrationSteps { get; set; }
        public List<string> Warnings { get; set; }

        public MigrationResult()
        {
            MigrationSteps = new List<string>();
            Warnings = new List<string>();
        }
    }
}