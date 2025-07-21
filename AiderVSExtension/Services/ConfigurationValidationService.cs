using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for validating configuration settings and providing feedback
    /// </summary>
    public class ConfigurationValidationService : IConfigurationValidationService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly INotificationService _notificationService;
        private readonly Dictionary<string, IConfigurationValidator> _validators = new Dictionary<string, IConfigurationValidator>();
        private readonly Dictionary<string, AiderVSExtension.Interfaces.ValidationResult> _cachedResults = new Dictionary<string, AiderVSExtension.Interfaces.ValidationResult>();
        private bool _disposed = false;

        public event EventHandler<ValidationEventArgs> ValidationCompleted;
        public event EventHandler<ValidationEventArgs> ValidationFailed;

        public ConfigurationValidationService(IErrorHandler errorHandler, INotificationService notificationService)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            
            RegisterDefaultValidators();
        }

        /// <summary>
        /// Validates a configuration profile
        /// </summary>
        /// <param name="profile">Profile to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ConfigurationValidationResult> ValidateProfileAsync(ConfigurationProfile profile)
        {
            try
            {
                var result = new ConfigurationValidationResult
                {
                    ProfileId = profile?.Id,
                    ProfileName = profile?.Name,
                    IsValid = true,
                    ValidationErrors = new List<ValidationError>(),
                    ValidationWarnings = new List<ValidationWarning>(),
                    ValidationTimestamp = DateTime.UtcNow
                };

                if (profile == null)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "Profile",
                        ErrorMessage = "Profile cannot be null",
                        ErrorCode = "NULL_PROFILE"
                    });
                    return result;
                }

                // Validate basic properties
                await ValidateBasicPropertiesAsync(profile, result);
                
                // Validate AI model configuration
                await ValidateAIModelConfigurationAsync(profile, result);
                
                // Validate advanced parameters
                await ValidateAdvancedParametersAsync(profile, result);
                
                // Validate custom validators
                await ValidateCustomValidatorsAsync(profile, result);
                
                // Cache result
                _cachedResults[profile.Id] = result;
                
                // Fire event
                if (result.IsValid)
                {
                    ValidationCompleted?.Invoke(this, new ValidationEventArgs { Result = result });
                }
                else
                {
                    ValidationFailed?.Invoke(this, new ValidationEventArgs { Result = result });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationValidationService.ValidateProfileAsync");
                return new ConfigurationValidationResult
                {
                    IsValid = false,
                    ValidationErrors = new List<ValidationError>
                    {
                        new ValidationError
                        {
                            PropertyName = "General",
                            ErrorMessage = $"Validation failed: {ex.Message}",
                            ErrorCode = "VALIDATION_EXCEPTION"
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Validates AI model configuration
        /// </summary>
        /// <param name="modelConfig">Model configuration to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ConfigurationValidationResult> ValidateAIModelConfigurationAsync(AIModelConfiguration modelConfig)
        {
            try
            {
                var result = new ConfigurationValidationResult
                {
                    IsValid = true,
                    ValidationErrors = new List<ValidationError>(),
                    ValidationWarnings = new List<ValidationWarning>(),
                    ValidationTimestamp = DateTime.UtcNow
                };

                if (modelConfig == null)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "ModelConfiguration",
                        ErrorMessage = "AI model configuration cannot be null",
                        ErrorCode = "NULL_MODEL_CONFIG"
                    });
                    return result;
                }

                // Validate API key
                await ValidateApiKeyAsync(modelConfig, result);
                
                // Validate model name
                await ValidateModelNameAsync(modelConfig, result);
                
                // Validate endpoint
                await ValidateEndpointAsync(modelConfig, result);
                
                // Validate parameters
                await ValidateModelParametersAsync(modelConfig, result);
                
                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationValidationService.ValidateAIModelConfigurationAsync");
                return new ConfigurationValidationResult
                {
                    IsValid = false,
                    ValidationErrors = new List<ValidationError>
                    {
                        new ValidationError
                        {
                            PropertyName = "ModelConfiguration",
                            ErrorMessage = $"Model configuration validation failed: {ex.Message}",
                            ErrorCode = "MODEL_VALIDATION_EXCEPTION"
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Validates advanced parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Validation result</returns>
        public async Task<ConfigurationValidationResult> ValidateAdvancedParametersAsync(AIModelAdvancedParameters parameters)
        {
            try
            {
                var result = new ConfigurationValidationResult
                {
                    IsValid = true,
                    ValidationErrors = new List<ValidationError>(),
                    ValidationWarnings = new List<ValidationWarning>(),
                    ValidationTimestamp = DateTime.UtcNow
                };

                if (parameters == null)
                {
                    return result; // Advanced parameters are optional
                }

                // Validate temperature
                if (parameters.Temperature < 0 || parameters.Temperature > 2)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "Temperature",
                        ErrorMessage = "Temperature must be between 0 and 2",
                        ErrorCode = "INVALID_TEMPERATURE"
                    });
                }

                // Validate max tokens
                if (parameters.MaxTokens <= 0 || parameters.MaxTokens > 32768)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "MaxTokens",
                        ErrorMessage = "Max tokens must be between 1 and 32768",
                        ErrorCode = "INVALID_MAX_TOKENS"
                    });
                }

                // Validate TopP
                if (parameters.TopP < 0 || parameters.TopP > 1)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "TopP",
                        ErrorMessage = "TopP must be between 0 and 1",
                        ErrorCode = "INVALID_TOP_P"
                    });
                }

                // Validate TopK
                if (parameters.TopK < 0 || parameters.TopK > 100)
                {
                    result.ValidationWarnings.Add(new ValidationWarning
                    {
                        PropertyName = "TopK",
                        WarningMessage = "TopK values above 100 may not be supported by all models",
                        WarningCode = "HIGH_TOP_K"
                    });
                }

                // Validate frequency penalty
                if (parameters.FrequencyPenalty < -2 || parameters.FrequencyPenalty > 2)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "FrequencyPenalty",
                        ErrorMessage = "Frequency penalty must be between -2 and 2",
                        ErrorCode = "INVALID_FREQUENCY_PENALTY"
                    });
                }

                // Validate presence penalty
                if (parameters.PresencePenalty < -2 || parameters.PresencePenalty > 2)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "PresencePenalty",
                        ErrorMessage = "Presence penalty must be between -2 and 2",
                        ErrorCode = "INVALID_PRESENCE_PENALTY"
                    });
                }

                // Validate context window
                if (parameters.ContextWindow < 1024 || parameters.ContextWindow > 128000)
                {
                    result.ValidationWarnings.Add(new ValidationWarning
                    {
                        PropertyName = "ContextWindow",
                        WarningMessage = "Context window outside typical range (1024-128000)",
                        WarningCode = "UNUSUAL_CONTEXT_WINDOW"
                    });
                }

                // Validate timeout
                if (parameters.TimeoutSeconds < 5 || parameters.TimeoutSeconds > 300)
                {
                    result.ValidationWarnings.Add(new ValidationWarning
                    {
                        PropertyName = "TimeoutSeconds",
                        WarningMessage = "Timeout outside recommended range (5-300 seconds)",
                        WarningCode = "UNUSUAL_TIMEOUT"
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationValidationService.ValidateAdvancedParametersAsync");
                return new ConfigurationValidationResult
                {
                    IsValid = false,
                    ValidationErrors = new List<ValidationError>
                    {
                        new ValidationError
                        {
                            PropertyName = "AdvancedParameters",
                            ErrorMessage = $"Advanced parameters validation failed: {ex.Message}",
                            ErrorCode = "ADVANCED_PARAMS_VALIDATION_EXCEPTION"
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Registers a custom validator
        /// </summary>
        /// <param name="name">Validator name</param>
        /// <param name="validator">Validator instance</param>
        public void RegisterValidator(string name, IConfigurationValidator validator)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || validator == null)
                    return;

                _validators[name] = validator;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ConfigurationValidationService.RegisterValidator");
            }
        }

        /// <summary>
        /// Unregisters a custom validator
        /// </summary>
        /// <param name="name">Validator name</param>
        public void UnregisterValidator(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return;

                _validators.Remove(name);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ConfigurationValidationService.UnregisterValidator");
            }
        }

        /// <summary>
        /// Gets cached validation result
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns>Cached validation result or null</returns>
        public ConfigurationValidationResult GetCachedResult(string profileId)
        {
            try
            {
                return _cachedResults.GetValueOrDefault(profileId);
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ConfigurationValidationService.GetCachedResult");
                return null;
            }
        }

        /// <summary>
        /// Clears validation cache
        /// </summary>
        public void ClearCache()
        {
            try
            {
                _cachedResults.Clear();
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ConfigurationValidationService.ClearCache");
            }
        }

        /// <summary>
        /// Provides suggestions for fixing validation errors
        /// </summary>
        /// <param name="error">Validation error</param>
        /// <returns>List of suggestions</returns>
        public List<string> GetSuggestions(ValidationError error)
        {
            try
            {
                var suggestions = new List<string>();

                switch (error.ErrorCode)
                {
                    case "INVALID_TEMPERATURE":
                        suggestions.Add("Use a value between 0 (deterministic) and 2 (very creative)");
                        suggestions.Add("Try 0.7 for balanced creativity");
                        suggestions.Add("Use 0.1 for factual responses");
                        break;
                    case "INVALID_MAX_TOKENS":
                        suggestions.Add("Use a value between 1 and 32768");
                        suggestions.Add("Try 2000 for typical responses");
                        suggestions.Add("Use 4000 for longer responses");
                        break;
                    case "INVALID_API_KEY":
                        suggestions.Add("Check that your API key is correct");
                        suggestions.Add("Verify the API key has necessary permissions");
                        suggestions.Add("Try regenerating the API key");
                        break;
                    case "INVALID_ENDPOINT":
                        suggestions.Add("Check that the endpoint URL is correct");
                        suggestions.Add("Verify the endpoint is accessible");
                        suggestions.Add("Try using the default endpoint");
                        break;
                    default:
                        suggestions.Add("Check the configuration documentation");
                        suggestions.Add("Reset to default values");
                        break;
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "ConfigurationValidationService.GetSuggestions");
                return new List<string>();
            }
        }

        #region Private Methods

        private void RegisterDefaultValidators()
        {
            _validators["ApiKey"] = new ApiKeyValidator();
            _validators["Endpoint"] = new EndpointValidator();
            _validators["ModelName"] = new ModelNameValidator();
            _validators["Parameters"] = new ParametersValidator();
        }

        private async Task ValidateBasicPropertiesAsync(ConfigurationProfile profile, ConfigurationValidationResult result)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new ValidationError
                {
                    PropertyName = "Name",
                    ErrorMessage = "Profile name cannot be empty",
                    ErrorCode = "EMPTY_NAME"
                });
            }

            // Validate ID
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new ValidationError
                {
                    PropertyName = "Id",
                    ErrorMessage = "Profile ID cannot be empty",
                    ErrorCode = "EMPTY_ID"
                });
            }

            // Validate description length
            if (!string.IsNullOrEmpty(profile.Description) && profile.Description.Length > 1000)
            {
                result.ValidationWarnings.Add(new ValidationWarning
                {
                    PropertyName = "Description",
                    WarningMessage = "Description is very long (over 1000 characters)",
                    WarningCode = "LONG_DESCRIPTION"
                });
            }
        }

        private async Task ValidateAIModelConfigurationAsync(ConfigurationProfile profile, ConfigurationValidationResult result)
        {
            if (profile.AIModelConfiguration != null)
            {
                var modelResult = await ValidateAIModelConfigurationAsync(profile.AIModelConfiguration);
                result.ValidationErrors.AddRange(modelResult.ValidationErrors);
                result.ValidationWarnings.AddRange(modelResult.ValidationWarnings);
                
                if (!modelResult.IsValid)
                {
                    result.IsValid = false;
                }
            }
        }

        private async Task ValidateAdvancedParametersAsync(ConfigurationProfile profile, ConfigurationValidationResult result)
        {
            if (profile.AdvancedParameters != null)
            {
                var paramResult = await ValidateAdvancedParametersAsync(profile.AdvancedParameters);
                result.ValidationErrors.AddRange(paramResult.ValidationErrors);
                result.ValidationWarnings.AddRange(paramResult.ValidationWarnings);
                
                if (!paramResult.IsValid)
                {
                    result.IsValid = false;
                }
            }
        }

        private async Task ValidateCustomValidatorsAsync(ConfigurationProfile profile, ConfigurationValidationResult result)
        {
            foreach (var validator in _validators.Values)
            {
                try
                {
                    var validationResult = await validator.ValidateAsync(profile);
                    if (validationResult != null)
                    {
                        result.ValidationErrors.AddRange(validationResult.ValidationErrors);
                        result.ValidationWarnings.AddRange(validationResult.ValidationWarnings);
                        
                        if (!validationResult.IsValid)
                        {
                            result.IsValid = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, $"ConfigurationValidationService.ValidateCustomValidatorsAsync - {validator.GetType().Name}");
                }
            }
        }

        private async Task ValidateApiKeyAsync(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (modelConfig.Provider != AIProvider.Ollama && string.IsNullOrWhiteSpace(modelConfig.ApiKey))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new ValidationError
                {
                    PropertyName = "ApiKey",
                    ErrorMessage = "API key is required for cloud providers",
                    ErrorCode = "MISSING_API_KEY"
                });
            }
            else if (!string.IsNullOrWhiteSpace(modelConfig.ApiKey))
            {
                // Validate API key format
                if (_validators.TryGetValue("ApiKey", out var validator))
                {
                    var validationResult = await validator.ValidateAsync(modelConfig);
                    if (validationResult != null && !validationResult.IsValid)
                    {
                        result.IsValid = false;
                        result.ValidationErrors.AddRange(validationResult.ValidationErrors);
                    }
                }
            }
        }

        private async Task ValidateModelNameAsync(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(modelConfig.ModelName))
            {
                result.IsValid = false;
                result.ValidationErrors.Add(new ValidationError
                {
                    PropertyName = "ModelName",
                    ErrorMessage = "Model name is required",
                    ErrorCode = "MISSING_MODEL_NAME"
                });
            }
            else if (_validators.TryGetValue("ModelName", out var validator))
            {
                var validationResult = await validator.ValidateAsync(modelConfig);
                if (validationResult != null && !validationResult.IsValid)
                {
                    result.IsValid = false;
                    result.ValidationErrors.AddRange(validationResult.ValidationErrors);
                }
            }
        }

        private async Task ValidateEndpointAsync(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(modelConfig.Endpoint))
            {
                if (!Uri.TryCreate(modelConfig.Endpoint, UriKind.Absolute, out var uri))
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "Endpoint",
                        ErrorMessage = "Invalid endpoint URL format",
                        ErrorCode = "INVALID_ENDPOINT_FORMAT"
                    });
                }
                else if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "Endpoint",
                        ErrorMessage = "Endpoint must use HTTP or HTTPS",
                        ErrorCode = "INVALID_ENDPOINT_SCHEME"
                    });
                }
            }
        }

        private async Task ValidateModelParametersAsync(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (modelConfig.Parameters != null && _validators.TryGetValue("Parameters", out var validator))
            {
                var validationResult = await validator.ValidateAsync(modelConfig);
                if (validationResult != null && !validationResult.IsValid)
                {
                    result.IsValid = false;
                    result.ValidationErrors.AddRange(validationResult.ValidationErrors);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _validators.Clear();
                _cachedResults.Clear();
                _disposed = true;
            }
        }
    }
}