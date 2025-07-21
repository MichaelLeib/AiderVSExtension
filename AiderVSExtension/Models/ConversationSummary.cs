using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a summary of a conversation for listing and management purposes
    /// </summary>
    public class ConversationSummary
    {
        /// <summary>
        /// Unique identifier for the conversation
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title of the conversation
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Number of messages in the conversation
        /// </summary>
        [JsonPropertyName("messageCount")]
        public int MessageCount { get; set; }

        /// <summary>
        /// Timestamp when the conversation was created
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the conversation was last updated
        /// </summary>
        [JsonPropertyName("lastUpdatedAt")]
        public DateTime LastUpdatedAt { get; set; }

        /// <summary>
        /// Whether the conversation is archived
        /// </summary>
        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; }

        /// <summary>
        /// Tags associated with the conversation
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Preview of the last message content
        /// </summary>
        [JsonPropertyName("lastMessagePreview")]
        public string LastMessagePreview { get; set; } = string.Empty;

        /// <summary>
        /// Estimated total token count for the conversation
        /// </summary>
        [JsonPropertyName("estimatedTokenCount")]
        public int EstimatedTokenCount { get; set; }

        /// <summary>
        /// File size of the conversation in bytes
        /// </summary>
        [JsonPropertyName("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Gets the display title for UI binding
        /// </summary>
        [JsonIgnore]
        public string DisplayTitle => string.IsNullOrEmpty(Title) ? "Untitled Conversation" : Title;

        /// <summary>
        /// Gets the formatted creation date for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedCreatedDate => CreatedAt.ToString("MMM dd, yyyy HH:mm");

        /// <summary>
        /// Gets the formatted last updated date for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedLastUpdatedDate => LastUpdatedAt.ToString("MMM dd, yyyy HH:mm");

        /// <summary>
        /// Gets the relative time since last update
        /// </summary>
        [JsonIgnore]
        public string RelativeLastUpdated
        {
            get
            {
                var timeSpan = DateTime.UtcNow - LastUpdatedAt;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";
                
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
                
                if (timeSpan.TotalDays < 365)
                    return $"{(int)(timeSpan.TotalDays / 30)} months ago";
                
                return $"{(int)(timeSpan.TotalDays / 365)} years ago";
            }
        }

        /// <summary>
        /// Gets the formatted file size for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedFileSize
        {
            get
            {
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                
                if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024:F1} KB";
                
                if (FileSizeBytes < 1024 * 1024 * 1024)
                    return $"{FileSizeBytes / (1024 * 1024):F1} MB";
                
                return $"{FileSizeBytes / (1024 * 1024 * 1024):F1} GB";
            }
        }

        /// <summary>
        /// Gets the status icon for the conversation
        /// </summary>
        [JsonIgnore]
        public string StatusIcon => IsArchived ? "📦" : "💬";

        /// <summary>
        /// Gets whether the conversation is recent (updated within last 24 hours)
        /// </summary>
        [JsonIgnore]
        public bool IsRecent => (DateTime.UtcNow - LastUpdatedAt).TotalHours < 24;

        /// <summary>
        /// Gets whether the conversation is old (not updated in last 30 days)
        /// </summary>
        [JsonIgnore]
        public bool IsOld => (DateTime.UtcNow - LastUpdatedAt).TotalDays > 30;

        /// <summary>
        /// Gets the conversation activity level based on message count and recency
        /// </summary>
        [JsonIgnore]
        public ConversationActivity ActivityLevel
        {
            get
            {
                if (IsOld)
                    return ConversationActivity.Inactive;
                
                if (MessageCount > 50 && IsRecent)
                    return ConversationActivity.VeryActive;
                
                if (MessageCount > 20 && (DateTime.UtcNow - LastUpdatedAt).TotalDays < 7)
                    return ConversationActivity.Active;
                
                if (MessageCount > 5)
                    return ConversationActivity.Moderate;
                
                return ConversationActivity.Light;
            }
        }

        /// <summary>
        /// Gets a preview of tags as a comma-separated string
        /// </summary>
        [JsonIgnore]
        public string TagsPreview
        {
            get
            {
                if (Tags == null || Tags.Count == 0)
                    return string.Empty;
                
                if (Tags.Count <= 3)
                    return string.Join(", ", Tags);
                
                return string.Join(", ", Tags.Take(3)) + $" (+{Tags.Count - 3} more)";
            }
        }

        /// <summary>
        /// Checks if the conversation matches a search query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive</param>
        /// <returns>True if the conversation matches the query</returns>
        public bool MatchesSearch(string query, bool caseSensitive = false)
        {
            if (string.IsNullOrEmpty(query))
                return true;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Search in title
            if (!string.IsNullOrEmpty(Title) && Title.Contains(query, comparison))
                return true;

            // Search in last message preview
            if (!string.IsNullOrEmpty(LastMessagePreview) && LastMessagePreview.Contains(query, comparison))
                return true;

            // Search in tags
            if (Tags != null && Tags.Any(tag => tag.Contains(query, comparison)))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the conversation has a specific tag
        /// </summary>
        /// <param name="tag">The tag to check for</param>
        /// <returns>True if the conversation has the tag</returns>
        public bool HasTag(string tag)
        {
            return Tags != null && Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Enumeration for conversation activity levels
    /// </summary>
    public enum ConversationActivity
    {
        Inactive,
        Light,
        Moderate,
        Active,
        VeryActive
    }
}