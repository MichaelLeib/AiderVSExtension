using System;
using System.Collections.Generic;
using AiderVSExtension.Models;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a chat response from an AI model
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// The content of the chat response
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The AI model that generated the response
        /// </summary>
        public string ModelUsed { get; set; }

        /// <summary>
        /// The number of tokens used in the response
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// The time it took to generate the response
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// List of suggested code changes from the AI
        /// </summary>
        public List<DiffChange> SuggestedChanges { get; set; }

        /// <summary>
        /// Indicates if the response was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if the response failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Unique identifier for the response
        /// </summary>
        public string ResponseId { get; set; }

        /// <summary>
        /// The original request that generated this response
        /// </summary>
        public string OriginalRequest { get; set; }

        /// <summary>
        /// Additional metadata about the response
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// File references mentioned in the response
        /// </summary>
        public List<FileReference> ReferencedFiles { get; set; }

        /// <summary>
        /// Confidence score of the response (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Indicates if the response was cached
        /// </summary>
        public bool IsFromCache { get; set; }

        /// <summary>
        /// The conversation context this response belongs to
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Type of response (e.g., Code, Explanation, Error, etc.)
        /// </summary>
        public ChatResponseType ResponseType { get; set; }

        /// <summary>
        /// Initializes a new instance of the ChatResponse class
        /// </summary>
        public ChatResponse()
        {
            SuggestedChanges = new List<DiffChange>();
            Metadata = new Dictionary<string, object>();
            ReferencedFiles = new List<FileReference>();
            Timestamp = DateTime.UtcNow;
            ResponseId = Guid.NewGuid().ToString();
            IsSuccess = true;
            ConfidenceScore = 1.0;
            ResponseType = ChatResponseType.Text;
        }

        /// <summary>
        /// Creates a successful chat response
        /// </summary>
        /// <param name="content">The response content</param>
        /// <param name="modelUsed">The model that generated the response</param>
        /// <param name="tokensUsed">Number of tokens used</param>
        /// <param name="responseTime">Time taken to generate response</param>
        /// <returns>A new ChatResponse instance</returns>
        public static ChatResponse Success(string content, string modelUsed, int tokensUsed, TimeSpan responseTime)
        {
            return new ChatResponse
            {
                Content = content,
                ModelUsed = modelUsed,
                TokensUsed = tokensUsed,
                ResponseTime = responseTime,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a failed chat response
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="modelUsed">The model that was attempted</param>
        /// <returns>A new ChatResponse instance</returns>
        public static ChatResponse Error(string errorMessage, string modelUsed = null)
        {
            return new ChatResponse
            {
                ErrorMessage = errorMessage,
                ModelUsed = modelUsed,
                IsSuccess = false,
                ResponseType = ChatResponseType.Error
            };
        }

        /// <summary>
        /// Creates a cached chat response
        /// </summary>
        /// <param name="content">The cached response content</param>
        /// <param name="modelUsed">The model that originally generated the response</param>
        /// <param name="tokensUsed">Number of tokens used in original response</param>
        /// <returns>A new ChatResponse instance</returns>
        public static ChatResponse FromCache(string content, string modelUsed, int tokensUsed)
        {
            return new ChatResponse
            {
                Content = content,
                ModelUsed = modelUsed,
                TokensUsed = tokensUsed,
                IsSuccess = true,
                IsFromCache = true,
                ResponseTime = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Adds a suggested code change to the response
        /// </summary>
        /// <param name="change">The diff change to add</param>
        public void AddSuggestedChange(DiffChange change)
        {
            SuggestedChanges?.Add(change);
        }

        /// <summary>
        /// Adds a file reference to the response
        /// </summary>
        /// <param name="fileReference">The file reference to add</param>
        public void AddFileReference(FileReference fileReference)
        {
            ReferencedFiles?.Add(fileReference);
        }

        /// <summary>
        /// Adds metadata to the response
        /// </summary>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        /// <summary>
        /// Gets metadata value by key
        /// </summary>
        /// <typeparam name="T">The type of the metadata value</typeparam>
        /// <param name="key">The metadata key</param>
        /// <returns>The metadata value or default if not found</returns>
        public T GetMetadata<T>(string key)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        /// <summary>
        /// Returns a string representation of the chat response
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return $"ChatResponse [{ResponseId}]: {(IsSuccess ? "Success" : "Error")} - {(IsSuccess ? Content?.Substring(0, Math.Min(50, Content?.Length ?? 0)) : ErrorMessage)}";
        }
    }

    /// <summary>
    /// Represents the type of chat response
    /// </summary>
    public enum ChatResponseType
    {
        /// <summary>
        /// Plain text response
        /// </summary>
        Text,

        /// <summary>
        /// Code response with syntax highlighting
        /// </summary>
        Code,

        /// <summary>
        /// Explanation or documentation response
        /// </summary>
        Explanation,

        /// <summary>
        /// Error response
        /// </summary>
        Error,

        /// <summary>
        /// Warning response
        /// </summary>
        Warning,

        /// <summary>
        /// Success confirmation response
        /// </summary>
        Success,

        /// <summary>
        /// System message response
        /// </summary>
        System,

        /// <summary>
        /// Diff or change suggestion response
        /// </summary>
        Diff,

        /// <summary>
        /// Question or prompt response
        /// </summary>
        Question
    }
}