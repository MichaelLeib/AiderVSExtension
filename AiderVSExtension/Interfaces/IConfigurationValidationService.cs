using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for configuration validation and feedback
    /// </summary>
    public interface IConfigurationValidationService
    {
        /// <summary>
        /// Event fired when validation is completed successfully
        /// </summary>
        event EventHandler<ValidationEventArgs> ValidationCompleted;

        /// <summary>
        /// Event fired when validation fails
        /// </summary>
        event EventHandler<ValidationEventArgs> ValidationFailed;

        /// <summary>
        /// Validates a configuration profile
        /// </summary>
        /// <param name="profile">Profile to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateProfileAsync(ConfigurationProfile profile);

        /// <summary>
        /// Validates AI model configuration
        /// </summary>
        /// <param name="modelConfig">Model configuration to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateAIModelConfigurationAsync(AIModelConfiguration modelConfig);

        /// <summary>
        /// Validates advanced parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateAdvancedParametersAsync(AIModelAdvancedParameters parameters);

        /// <summary>
        /// Registers a custom validator
        /// </summary>
        /// <param name="name">Validator name</param>
        /// <param name="validator">Validator instance</param>
        void RegisterValidator(string name, IConfigurationValidator validator);

        /// <summary>
        /// Unregisters a custom validator
        /// </summary>
        /// <param name="name">Validator name</param>
        void UnregisterValidator(string name);

        /// <summary>
        /// Gets cached validation result
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns>Cached validation result or null</returns>
        ConfigurationValidationResult GetCachedResult(string profileId);

        /// <summary>
        /// Clears validation cache
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Provides suggestions for fixing validation errors
        /// </summary>
        /// <param name="error">Validation error</param>
        /// <returns>List of suggestions</returns>
        List<string> GetSuggestions(ValidationError error);
    }

    /// <summary>
    /// Interface for custom configuration validators
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validator name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Validates configuration
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Validation result</returns>
        Task<ConfigurationValidationResult> ValidateAsync(object configuration);

        /// <summary>
        /// Whether this validator supports the given configuration type
        /// </summary>
        /// <param name="configurationType">Configuration type</param>
        /// <returns>True if supported</returns>
        bool SupportsType(Type configurationType);
    }
}