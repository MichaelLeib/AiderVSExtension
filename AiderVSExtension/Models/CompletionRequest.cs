using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a request for AI completion
    /// </summary>
    public class CompletionRequest
    {
        /// <summary>
        /// The prompt text for completion
        /// </summary>
        [JsonPropertyName("prompt")]
        [Required]
        [StringLength(10000, ErrorMessage = "Prompt cannot exceed 10,000 characters")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        [JsonPropertyName("maxTokens")]
        [Range(1, 4000, ErrorMessage = "Max tokens must be between 1 and 4000")]
        public int MaxTokens { get; set; } = 150;

        /// <summary>
        /// Temperature for randomness (0.0 to 2.0)
        /// </summary>
        [JsonPropertyName("temperature")]
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Context from the current file or selection
        /// </summary>
        [JsonPropertyName("context")]
        public string Context { get; set; }

        /// <summary>
        /// File references for additional context
        /// </summary>
        [JsonPropertyName("fileReferences")]
        public List<FileReference> FileReferences { get; set; } = new List<FileReference>();

        /// <summary>
        /// Language of the code being completed
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; }

        /// <summary>
        /// Timestamp when the request was created
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Validates the completion request
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Prompt))
                return false;

            if (MaxTokens < 1 || MaxTokens > 4000)
                return false;

            if (Temperature < 0.0 || Temperature > 2.0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation errors for the request
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Prompt))
                errors.Add("Prompt is required");

            if (Prompt?.Length > 10000)
                errors.Add("Prompt cannot exceed 10,000 characters");

            if (MaxTokens < 1 || MaxTokens > 4000)
                errors.Add("Max tokens must be between 1 and 4000");

            if (Temperature < 0.0 || Temperature > 2.0)
                errors.Add("Temperature must be between 0.0 and 2.0");

            return errors;
        }
    }
}