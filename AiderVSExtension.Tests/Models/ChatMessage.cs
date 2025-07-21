using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public string? ModelUsed { get; set; }

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

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Message ID is required");

            if (string.IsNullOrWhiteSpace(Content))
                errors.Add("Message content is required");

            if (Content?.Length > 50000)
                errors.Add("Message content cannot exceed 50,000 characters");

            if (Timestamp == default)
                errors.Add("Message timestamp is required");

            // Validate references
            if (References != null)
            {
                for (int i = 0; i < References.Count; i++)
                {
                    var referenceErrors = References[i].GetValidationErrors();
                    foreach (var error in referenceErrors)
                    {
                        errors.Add($"Reference {i + 1}: {error}");
                    }
                }
            }

            return errors;
        }
    }
}