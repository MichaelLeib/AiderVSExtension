using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a complete conversation with Aider AI
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Title of the conversation
        /// </summary>
        [JsonPropertyName("title")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// List of messages in the conversation
        /// </summary>
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Timestamp when the conversation was created
        /// </summary>
        [JsonPropertyName("createdAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the conversation was last updated
        /// </summary>
        [JsonPropertyName("lastUpdatedAt")]
        [Required]
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the conversation is archived
        /// </summary>
        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Tags associated with the conversation
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Metadata associated with the conversation
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the number of messages in the conversation
        /// </summary>
        [JsonIgnore]
        public int MessageCount => Messages?.Count ?? 0;

        /// <summary>
        /// Gets whether the conversation has any messages
        /// </summary>
        [JsonIgnore]
        public bool HasMessages => MessageCount > 0;

        /// <summary>
        /// Gets the last message in the conversation
        /// </summary>
        [JsonIgnore]
        public ChatMessage LastMessage => Messages?.LastOrDefault();

        /// <summary>
        /// Gets the first message in the conversation
        /// </summary>
        [JsonIgnore]
        public ChatMessage FirstMessage => Messages?.FirstOrDefault();

        /// <summary>
        /// Gets the estimated total token count for the conversation
        /// </summary>
        [JsonIgnore]
        public int EstimatedTokenCount => Messages?.Sum(m => m.GetEstimatedTokenCount()) ?? 0;

        /// <summary>
        /// Adds a message to the conversation
        /// </summary>
        /// <param name="message">The message to add</param>
        public void AddMessage(ChatMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Messages.Add(message);
            LastUpdatedAt = DateTime.UtcNow;

            // Auto-generate title from first user message if not set
            if (string.IsNullOrEmpty(Title) && message.Type == MessageType.User)
            {
                Title = GenerateTitleFromMessage(message);
            }
        }

        /// <summary>
        /// Removes a message from the conversation
        /// </summary>
        /// <param name="messageId">The ID of the message to remove</param>
        /// <returns>True if message was removed, false if not found</returns>
        public bool RemoveMessage(string messageId)
        {
            var message = Messages.FirstOrDefault(m => m.Id == messageId);
            if (message != null)
            {
                Messages.Remove(message);
                LastUpdatedAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets messages of a specific type
        /// </summary>
        /// <param name="messageType">The type of messages to get</param>
        /// <returns>List of messages of the specified type</returns>
        public List<ChatMessage> GetMessagesByType(MessageType messageType)
        {
            return Messages.Where(m => m.Type == messageType).ToList();
        }

        /// <summary>
        /// Gets messages within a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <returns>List of messages within the date range</returns>
        public List<ChatMessage> GetMessagesByDateRange(DateTime startDate, DateTime endDate)
        {
            return Messages.Where(m => m.Timestamp >= startDate && m.Timestamp <= endDate).ToList();
        }

        /// <summary>
        /// Searches for messages containing specific text
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive</param>
        /// <returns>List of messages containing the search text</returns>
        public List<ChatMessage> SearchMessages(string searchText, bool caseSensitive = false)
        {
            if (string.IsNullOrEmpty(searchText))
                return new List<ChatMessage>();

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return Messages.Where(m => m.Content.Contains(searchText, comparison)).ToList();
        }

        /// <summary>
        /// Validates the conversation
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
                return false;

            if (CreatedAt == default)
                return false;

            if (LastUpdatedAt == default)
                return false;

            if (LastUpdatedAt < CreatedAt)
                return false;

            // Validate all messages
            if (Messages != null)
            {
                foreach (var message in Messages)
                {
                    if (!message.IsValid())
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets validation errors for the conversation
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Conversation ID is required");

            if (CreatedAt == default)
                errors.Add("Created date is required");

            if (LastUpdatedAt == default)
                errors.Add("Last updated date is required");

            if (LastUpdatedAt < CreatedAt)
                errors.Add("Last updated date cannot be before created date");

            if (!string.IsNullOrEmpty(Title) && Title.Length > 200)
                errors.Add("Title cannot exceed 200 characters");

            // Validate messages
            if (Messages != null)
            {
                for (int i = 0; i < Messages.Count; i++)
                {
                    var messageErrors = Messages[i].GetValidationErrors();
                    foreach (var error in messageErrors)
                    {
                        errors.Add($"Message {i + 1}: {error}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates a summary of the conversation
        /// </summary>
        /// <returns>Conversation summary</returns>
        public ConversationSummary CreateSummary()
        {
            return new ConversationSummary
            {
                Id = Id,
                Title = Title,
                MessageCount = MessageCount,
                CreatedAt = CreatedAt,
                LastUpdatedAt = LastUpdatedAt,
                IsArchived = IsArchived,
                Tags = new List<string>(Tags),
                LastMessagePreview = LastMessage?.Content.Length > 100 
                    ? LastMessage.Content.Substring(0, 100) + "..." 
                    : LastMessage?.Content ?? string.Empty,
                EstimatedTokenCount = EstimatedTokenCount
            };
        }

        /// <summary>
        /// Clears all messages from the conversation
        /// </summary>
        public void ClearMessages()
        {
            Messages.Clear();
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Archives the conversation
        /// </summary>
        public void Archive()
        {
            IsArchived = true;
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Unarchives the conversation
        /// </summary>
        public void Unarchive()
        {
            IsArchived = false;
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a tag to the conversation
        /// </summary>
        /// <param name="tag">The tag to add</param>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                Tags.Add(tag.Trim());
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes a tag from the conversation
        /// </summary>
        /// <param name="tag">The tag to remove</param>
        /// <returns>True if tag was removed, false if not found</returns>
        public bool RemoveTag(string tag)
        {
            var existingTag = Tags.FirstOrDefault(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            if (existingTag != null)
            {
                Tags.Remove(existingTag);
                LastUpdatedAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Generates a title from a message content
        /// </summary>
        /// <param name="message">The message to generate title from</param>
        /// <returns>Generated title</returns>
        private string GenerateTitleFromMessage(ChatMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
                return "New Conversation";

            // Take first 50 characters and clean up
            var title = message.Content.Length > 50 
                ? message.Content.Substring(0, 50).Trim() + "..."
                : message.Content.Trim();

            // Remove line breaks and extra spaces
            title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ");

            return string.IsNullOrEmpty(title) ? "New Conversation" : title;
        }
    }
}