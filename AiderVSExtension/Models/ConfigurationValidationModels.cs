using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        /// <summary>
        /// Profile ID being validated
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Profile name being validated
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<ValidationWarning> ValidationWarnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTime ValidationTimestamp { get; set; }

        /// <summary>
        /// Time taken to validate
        /// </summary>
        public TimeSpan ValidationDuration { get; set; }

        /// <summary>
        /// Validation context information
        /// </summary>
        public string ValidationContext { get; set; }

        /// <summary>
        /// Whether validation was performed with strict rules
        /// </summary>
        public bool IsStrictValidation { get; set; }

        /// <summary>
        /// Additional validation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Property name that failed validation
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error code for programmatic handling
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Error severity
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

        /// <summary>
        /// Suggested fix for the error
        /// </summary>
        public string SuggestedFix { get; set; }

        /// <summary>
        /// URL to documentation for this error
        /// </summary>
        public string DocumentationUrl { get; set; }

        /// <summary>
        /// Error category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Whether this error can be auto-fixed
        /// </summary>
        public bool CanAutoFix { get; set; }

        /// <summary>
        /// Auto-fix action if available
        /// </summary>
        public Action AutoFixAction { get; set; }
    }

    /// <summary>
    /// Validation warning
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Property name that triggered the warning
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Warning message
        /// </summary>
        public string WarningMessage { get; set; }

        /// <summary>
        /// Warning code for programmatic handling
        /// </summary>
        public string WarningCode { get; set; }

        /// <summary>
        /// Warning severity
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;

        /// <summary>
        /// Suggested action for the warning
        /// </summary>
        public string SuggestedAction { get; set; }

        /// <summary>
        /// URL to documentation for this warning
        /// </summary>
        public string DocumentationUrl { get; set; }

        /// <summary>
        /// Warning category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Whether this warning can be ignored
        /// </summary>
        public bool CanIgnore { get; set; } = true;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information level
        /// </summary>
        Information,

        /// <summary>
        /// Warning level
        /// </summary>
        Warning,

        /// <summary>
        /// Error level
        /// </summary>
        Error,

        /// <summary>
        /// Critical error level
        /// </summary>
        Critical
    }

    /// <summary>
    /// Validation event arguments
    /// </summary>
    public class ValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Validation result
        /// </summary>
        public ConfigurationValidationResult Result { get; set; }

        /// <summary>
        /// Configuration that was validated
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// API key validator
    /// </summary>
    public class ApiKeyValidator : IConfigurationValidator
    {
        public string Name => "ApiKey";

        public async Task<ConfigurationValidationResult> ValidateAsync(object configuration)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            if (configuration is AIModelConfiguration modelConfig)
            {
                if (modelConfig.Provider != AIProvider.Ollama)
                {
                    if (string.IsNullOrWhiteSpace(modelConfig.ApiKey))
                    {
                        result.IsValid = false;
                        result.ValidationErrors.Add(new ValidationError
                        {
                            PropertyName = "ApiKey",
                            ErrorMessage = "API key is required for cloud providers",
                            ErrorCode = "MISSING_API_KEY",
                            SuggestedFix = "Enter a valid API key for your provider"
                        });
                    }
                    else
                    {
                        // Validate API key format
                        switch (modelConfig.Provider)
                        {
                            case AIProvider.ChatGPT:
                                if (!modelConfig.ApiKey.StartsWith("sk-"))
                                {
                                    result.ValidationWarnings.Add(new ValidationWarning
                                    {
                                        PropertyName = "ApiKey",
                                        WarningMessage = "OpenAI API keys typically start with 'sk-'",
                                        WarningCode = "UNUSUAL_OPENAI_KEY_FORMAT"
                                    });
                                }
                                break;
                            case AIProvider.Claude:
                                if (!modelConfig.ApiKey.StartsWith("sk-ant-"))
                                {
                                    result.ValidationWarnings.Add(new ValidationWarning
                                    {
                                        PropertyName = "ApiKey",
                                        WarningMessage = "Claude API keys typically start with 'sk-ant-'",
                                        WarningCode = "UNUSUAL_CLAUDE_KEY_FORMAT"
                                    });
                                }
                                break;
                        }
                    }
                }
            }

            return result;
        }

        public bool SupportsType(Type configurationType)
        {
            return configurationType == typeof(AIModelConfiguration);
        }
    }

    /// <summary>
    /// Endpoint validator
    /// </summary>
    public class EndpointValidator : IConfigurationValidator
    {
        public string Name => "Endpoint";

        public async Task<ConfigurationValidationResult> ValidateAsync(object configuration)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            if (configuration is AIModelConfiguration modelConfig)
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
                            ErrorCode = "INVALID_ENDPOINT_FORMAT",
                            SuggestedFix = "Enter a valid HTTP or HTTPS URL"
                        });
                    }
                    else
                    {
                        if (uri.Scheme != "http" && uri.Scheme != "https")
                        {
                            result.IsValid = false;
                            result.ValidationErrors.Add(new ValidationError
                            {
                                PropertyName = "Endpoint",
                                ErrorMessage = "Endpoint must use HTTP or HTTPS protocol",
                                ErrorCode = "INVALID_ENDPOINT_SCHEME",
                                SuggestedFix = "Use http:// or https:// prefix"
                            });
                        }

                        if (uri.Scheme == "http" && !uri.Host.Contains("localhost") && !uri.Host.Contains("127.0.0.1"))
                        {
                            result.ValidationWarnings.Add(new ValidationWarning
                            {
                                PropertyName = "Endpoint",
                                WarningMessage = "HTTP endpoints are not secure for production use",
                                WarningCode = "INSECURE_ENDPOINT",
                                SuggestedAction = "Use HTTPS for secure communication"
                            });
                        }
                    }
                }
            }

            return result;
        }

        public bool SupportsType(Type configurationType)
        {
            return configurationType == typeof(AIModelConfiguration);
        }
    }

    /// <summary>
    /// Model name validator
    /// </summary>
    public class ModelNameValidator : IConfigurationValidator
    {
        public string Name => "ModelName";

        private readonly Dictionary<AIProvider, HashSet<string>> _knownModels = new Dictionary<AIProvider, HashSet<string>>
        {
            [AIProvider.ChatGPT] = new HashSet<string>
            {
                "gpt-4", "gpt-4-turbo", "gpt-4-turbo-preview", "gpt-3.5-turbo", "gpt-3.5-turbo-16k",
                "text-davinci-003", "text-davinci-002", "code-davinci-002"
            },
            [AIProvider.Claude] = new HashSet<string>
            {
                "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307",
                "claude-2.1", "claude-2.0", "claude-instant-1.2"
            },
            [AIProvider.Ollama] = new HashSet<string>
            {
                "llama2", "llama2:7b", "llama2:13b", "llama2:70b", "codellama", "codellama:7b",
                "codellama:13b", "codellama:34b", "mistral", "mixtral", "phi", "gemma"
            }
        };

        public async Task<ConfigurationValidationResult> ValidateAsync(object configuration)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            if (configuration is AIModelConfiguration modelConfig)
            {
                if (string.IsNullOrWhiteSpace(modelConfig.ModelName))
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError
                    {
                        PropertyName = "ModelName",
                        ErrorMessage = "Model name is required",
                        ErrorCode = "MISSING_MODEL_NAME",
                        SuggestedFix = "Select a model from the list or enter a custom model name"
                    });
                }
                else
                {
                    // Check if model name is known for the provider
                    if (_knownModels.TryGetValue(modelConfig.Provider, out var knownModels))
                    {
                        if (!knownModels.Contains(modelConfig.ModelName))
                        {
                            result.ValidationWarnings.Add(new ValidationWarning
                            {
                                PropertyName = "ModelName",
                                WarningMessage = $"'{modelConfig.ModelName}' is not a known model for {modelConfig.Provider}",
                                WarningCode = "UNKNOWN_MODEL_NAME",
                                SuggestedAction = "Verify the model name is correct and available"
                            });
                        }
                    }
                }
            }

            return result;
        }

        public bool SupportsType(Type configurationType)
        {
            return configurationType == typeof(AIModelConfiguration);
        }
    }

    /// <summary>
    /// Parameters validator
    /// </summary>
    public class ParametersValidator : IConfigurationValidator
    {
        public string Name => "Parameters";

        public async Task<ConfigurationValidationResult> ValidateAsync(object configuration)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            if (configuration is AIModelConfiguration modelConfig && modelConfig.Parameters != null)
            {
                var parameters = modelConfig.Parameters;

                // Validate parameter combinations
                if (parameters.ContainsKey("temperature") && parameters.ContainsKey("top_p"))
                {
                    if (parameters.TryGetValue("temperature", out var temp) && 
                        parameters.TryGetValue("top_p", out var topP) &&
                        temp is double tempValue && topP is double topPValue)
                    {
                        if (tempValue > 1.0 && topPValue < 0.9)
                        {
                            result.ValidationWarnings.Add(new ValidationWarning
                            {
                                PropertyName = "Parameters",
                                WarningMessage = "High temperature with low top_p may produce inconsistent results",
                                WarningCode = "INCONSISTENT_PARAMETERS",
                                SuggestedAction = "Consider adjusting temperature or top_p values"
                            });
                        }
                    }
                }

                // Validate provider-specific parameters
                ValidateProviderSpecificParameters(modelConfig, result);
            }

            return result;
        }

        private void ValidateProviderSpecificParameters(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            switch (modelConfig.Provider)
            {
                case AIProvider.ChatGPT:
                    ValidateOpenAIParameters(modelConfig, result);
                    break;
                case AIProvider.Claude:
                    ValidateClaudeParameters(modelConfig, result);
                    break;
                case AIProvider.Ollama:
                    ValidateOllamaParameters(modelConfig, result);
                    break;
            }
        }

        private void ValidateOpenAIParameters(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (modelConfig.Parameters?.ContainsKey("best_of") == true)
            {
                result.ValidationWarnings.Add(new ValidationWarning
                {
                    PropertyName = "Parameters",
                    WarningMessage = "The 'best_of' parameter is deprecated in newer OpenAI models",
                    WarningCode = "DEPRECATED_PARAMETER",
                    SuggestedAction = "Remove the 'best_of' parameter"
                });
            }
        }

        private void ValidateClaudeParameters(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (modelConfig.Parameters?.ContainsKey("frequency_penalty") == true)
            {
                result.ValidationWarnings.Add(new ValidationWarning
                {
                    PropertyName = "Parameters",
                    WarningMessage = "Claude models do not support frequency_penalty parameter",
                    WarningCode = "UNSUPPORTED_PARAMETER",
                    SuggestedAction = "Remove the frequency_penalty parameter for Claude models"
                });
            }
        }

        private void ValidateOllamaParameters(AIModelConfiguration modelConfig, ConfigurationValidationResult result)
        {
            if (modelConfig.Parameters?.ContainsKey("logprobs") == true)
            {
                result.ValidationWarnings.Add(new ValidationWarning
                {
                    PropertyName = "Parameters",
                    WarningMessage = "Ollama models may not support logprobs parameter",
                    WarningCode = "POTENTIALLY_UNSUPPORTED_PARAMETER",
                    SuggestedAction = "Verify that your Ollama model supports logprobs"
                });
            }
        }

        public bool SupportsType(Type configurationType)
        {
            return configurationType == typeof(AIModelConfiguration);
        }
    }
}