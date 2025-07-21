using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a chat message in the Aider conversation
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Content of the message
        /// </summary>
        [JsonPropertyName("content")]
        [Required]
        [StringLength(50000, ErrorMessage = "Message content cannot exceed 50,000 characters")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type of the message (User, Assistant, System)
        /// </summary>
        [JsonPropertyName("type")]
        [Required]
        public MessageType Type { get; set; }

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        [JsonPropertyName("timestamp")]
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of file references associated with this message
        /// </summary>
        [JsonPropertyName("references")]
        public List<FileReference> References { get; set; } = new List<FileReference>();

        /// <summary>
        /// AI model used to generate this message (for Assistant messages)
        /// </summary>
        [JsonPropertyName("modelUsed")]
        public string ModelUsed { get; set; }

        /// <summary>
        /// Gets whether this message has file references
        /// </summary>
        [JsonIgnore]
        public bool HasReferences => References != null && References.Count > 0;

        /// <summary>
        /// Gets whether to show timestamp in UI
        /// </summary>
        [JsonIgnore]
        public bool ShowTimestamp { get; set; } = true;

        /// <summary>
        /// Validates the chat message properties
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
                return false;

            if (string.IsNullOrWhiteSpace(Content))
                return false;

            if (Content.Length > 50000)
                return false;

            if (Timestamp == default)
                return false;

            // Validate all references
            if (References != null)
            {
                foreach (var reference in References)
                {
                    if (!reference.IsValid())
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets validation errors for the message
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // Use validation helper for consistent validation
            var guidError = ValidationHelper.ValidateGuidString(Id, "Message ID");
            if (guidError != null) errors.Add(guidError);

            var contentError = ValidationHelper.ValidateRequired(Content, "Message content");
            if (contentError != null) errors.Add(contentError);

            var lengthError = ValidationHelper.ValidateStringLength(Content, "Message content", 
                maxLength: Constants.DefaultValues.MaxMessageContentLength);
            if (lengthError != null) errors.Add(lengthError);

            var timestampError = ValidationHelper.ValidateTimestamp(Timestamp, "Message timestamp");
            if (timestampError != null) errors.Add(timestampError);

            // Validate model name if provided
            if (!string.IsNullOrWhiteSpace(ModelUsed))
            {
                var modelError = ValidationHelper.ValidateStringLength(ModelUsed, "Model name", maxLength: 100);
                if (modelError != null) errors.Add(modelError);
            }

            // Validate references using validation helper
            if (References != null)
            {
                var referenceErrors = ValidationHelper.ValidateCollection(References, "References", 
                    (reference, index) => reference.GetValidationErrors(), 
                    maxItems: 50);
                errors.AddRange(referenceErrors);
            }

            // Add Data Annotations validation
            var dataAnnotationErrors = ValidationHelper.GetValidationErrors(this);
            errors.AddRange(dataAnnotationErrors);

            return errors;
        }

        /// <summary>
        /// Gets a summary of the message for display purposes
        /// </summary>
        /// <returns>Message summary</returns>
        public string GetSummary()
        {
            var summary = Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;
            return $"[{Type}] {summary}";
        }

        /// <summary>
        /// Gets the estimated token count for this message
        /// </summary>
        /// <returns>Estimated token count</returns>
        public int GetEstimatedTokenCount()
        {
            // Rough estimation: 1 token ≈ 4 characters for English text
            var contentTokens = Content.Length / 4;
            
            // Add tokens for references
            var referenceTokens = References?.Sum(r => r.Content.Length / 4) ?? 0;
            
            return contentTokens + referenceTokens;
        }

        /// <summary>
        /// Creates a sanitized copy of the message for logging
        /// </summary>
        /// <returns>Sanitized message copy</returns>
        public ChatMessage CreateSanitizedCopy()
        {
            return new ChatMessage
            {
                Id = Id,
                Content = Content.Length > 200 ? Content.Substring(0, 200) + "..." : Content,
                Type = Type,
                Timestamp = Timestamp,
                ModelUsed = ModelUsed,
                References = References?.Select(r => r.CreateSanitizedCopy()).ToList() ?? new List<FileReference>(),
                ShowTimestamp = ShowTimestamp
            };
        }
    }
}