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
        /// Unique identifier for this configuration
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

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
        public string ApiKey { get; set; }

        /// <summary>
        /// Endpoint URL (primarily for Ollama local/remote endpoints)
        /// </summary>
        [JsonPropertyName("endpointUrl")]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Alias for EndpointUrl for backward compatibility
        /// </summary>
        [JsonIgnore]
        public string Endpoint 
        { 
            get => EndpointUrl; 
            set => EndpointUrl = value; 
        }

        /// <summary>
        /// Specific model name to use
        /// </summary>
        [JsonPropertyName("modelName")]
        public string ModelName { get; set; }

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
        /// Model parameters (temperature, max tokens, etc.)
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

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

            // Provider-specific validation using ValidationHelper
            switch (Provider)
            {
                case AIProvider.ChatGPT:
                case AIProvider.Claude:
                    var apiKeyError = ValidationHelper.ValidateApiKey(ApiKey, "API key", Provider);
                    if (apiKeyError != null) errors.Add(apiKeyError);
                    break;

                case AIProvider.Ollama:
                    var urlError = ValidationHelper.ValidateUrl(EndpointUrl, "Endpoint URL");
                    if (urlError != null) errors.Add(urlError);
                    break;
            }

            // Validate model name if provided
            if (!string.IsNullOrWhiteSpace(ModelName))
            {
                var modelError = ValidationHelper.ValidateModelName(ModelName, "Model name", Provider);
                if (modelError != null) errors.Add(modelError);
            }

            // Validate timeout and retry settings using ValidationHelper
            var timeoutError = ValidationHelper.ValidateRange(TimeoutSeconds, "Timeout", 1, 300);
            if (timeoutError != null) errors.Add(timeoutError);

            var retriesError = ValidationHelper.ValidateRange(MaxRetries, "Max retries", 0, 10);
            if (retriesError != null) errors.Add(retriesError);

            // Validate timestamp
            var timestampError = ValidationHelper.ValidateTimestamp(LastModified, "Last modified", allowFuture: true);
            if (timestampError != null) errors.Add(timestampError);

            // Add Data Annotations validation
            var dataAnnotationErrors = ValidationHelper.GetValidationErrors(this);
            errors.AddRange(dataAnnotationErrors);

            return errors;
        }

        /// <summary>
        /// Gets the connection string for this configuration
        /// </summary>
        /// <returns>Connection string for display purposes</returns>
        public string GetConnectionString()
        {
            switch (Provider)
            {
                case AIProvider.ChatGPT:
                    return $"OpenAI API ({ModelName ?? "default"})";
                case AIProvider.Claude:
                    return $"Anthropic API ({ModelName ?? "default"})";
                case AIProvider.Ollama:
                    return $"Ollama at {EndpointUrl} ({ModelName ?? "default"})";
                default:
                    return "Unknown provider";
            }
        }

        /// <summary>
        /// Checks if this configuration is ready for use
        /// </summary>
        /// <returns>True if configuration is complete and valid, false otherwise</returns>
        public bool IsReady()
        {
            return IsEnabled && IsValid() && GetValidationErrors().Count == 0;
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
                Id = Id,
                Provider = Provider,
                ApiKey = ApiKey,
                EndpointUrl = EndpointUrl,
                ModelName = ModelName,
                IsEnabled = IsEnabled,
                AdditionalSettings = new Dictionary<string, object>(AdditionalSettings),
                Parameters = new Dictionary<string, object>(Parameters),
                LastModified = LastModified,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetries = MaxRetries
            };
        }
    }
}