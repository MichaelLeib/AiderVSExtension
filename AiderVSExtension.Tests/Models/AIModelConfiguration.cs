using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents configuration for an AI model provider
    /// </summary>
    public class AIModelConfiguration
    {
        /// <summary>
        /// AI provider type (ChatGPT, Claude, Ollama)
        /// </summary>
        [JsonPropertyName("provider")]
        [Required]
        public AIProvider Provider { get; set; }

        /// <summary>
        /// API key for the provider (required for ChatGPT and Claude)
        /// </summary>
        [JsonPropertyName("apiKey")]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Endpoint URL (primarily for Ollama local/remote endpoints)
        /// </summary>
        [JsonPropertyName("endpointUrl")]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Specific model name to use
        /// </summary>
        [JsonPropertyName("modelName")]
        public string? ModelName { get; set; }

        /// <summary>
        /// Whether this configuration is enabled
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Additional provider-specific settings
        /// </summary>
        [JsonPropertyName("additionalSettings")]
        public Dictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when the configuration was last modified
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        [JsonPropertyName("maxRetries")]
        [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Validates the AI model configuration
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            // Provider-specific validation
            switch (Provider)
            {
                case AIProvider.ChatGPT:
                case AIProvider.Claude:
                    if (string.IsNullOrWhiteSpace(ApiKey))
                        return false;
                    break;

                case AIProvider.Ollama:
                    if (string.IsNullOrWhiteSpace(EndpointUrl))
                        return false;
                    
                    // Validate URL format
                    if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri))
                        return false;
                    
                    if (uri.Scheme != "http" && uri.Scheme != "https")
                        return false;
                    break;
            }

            // Validate timeout and retry settings
            if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
                return false;

            if (MaxRetries < 0 || MaxRetries > 10)
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation errors for the configuration
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // Provider-specific validation
            switch (Provider)
            {
                case AIProvider.ChatGPT:
                    if (string.IsNullOrWhiteSpace(ApiKey))
                        errors.Add("API key is required for ChatGPT");
                    break;

                case AIProvider.Claude:
                    if (string.IsNullOrWhiteSpace(ApiKey))
                        errors.Add("API key is required for Claude");
                    break;

                case AIProvider.Ollama:
                    if (string.IsNullOrWhiteSpace(EndpointUrl))
                    {
                        errors.Add("Endpoint URL is required for Ollama");
                    }
                    else
                    {
                        if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri))
                        {
                            errors.Add("Invalid endpoint URL format");
                        }
                        else if (uri.Scheme != "http" && uri.Scheme != "https")
                        {
                            errors.Add("Endpoint URL must use HTTP or HTTPS protocol");
                        }
                    }
                    break;
            }

            // Validate timeout and retry settings
            if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
                errors.Add("Timeout must be between 1 and 300 seconds");

            if (MaxRetries < 0 || MaxRetries > 10)
                errors.Add("Max retries must be between 0 and 10");

            return errors;
        }

        /// <summary>
        /// Gets the display name for this configuration
        /// </summary>
        /// <returns>Formatted display name</returns>
        public string GetDisplayName()
        {
            var baseName = Provider.ToString();
            if (!string.IsNullOrWhiteSpace(ModelName))
            {
                baseName += $" ({ModelName})";
            }
            return baseName;
        }

        /// <summary>
        /// Checks if the configuration requires an API key
        /// </summary>
        /// <returns>True if API key is required, false otherwise</returns>
        public bool RequiresApiKey()
        {
            return Provider == AIProvider.ChatGPT || Provider == AIProvider.Claude;
        }

        /// <summary>
        /// Checks if the configuration supports custom endpoints
        /// </summary>
        /// <returns>True if custom endpoints are supported, false otherwise</returns>
        public bool SupportsCustomEndpoint()
        {
            return Provider == AIProvider.Ollama;
        }

        /// <summary>
        /// Creates a copy of this configuration
        /// </summary>
        /// <returns>Deep copy of the configuration</returns>
        public AIModelConfiguration Clone()
        {
            return new AIModelConfiguration
            {
                Provider = Provider,
                ApiKey = ApiKey,
                EndpointUrl = EndpointUrl,
                ModelName = ModelName,
                IsEnabled = IsEnabled,
                AdditionalSettings = new Dictionary<string, object>(AdditionalSettings),
                LastModified = LastModified,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetries = MaxRetries
            };
        }
    }
}