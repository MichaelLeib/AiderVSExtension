using System;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a response from AI completion
    /// </summary>
    public class CompletionResponse
    {
        /// <summary>
        /// Whether the completion was successful
        /// </summary>
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The completed text content
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Error message if completion failed
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Number of tokens used in the completion
        /// </summary>
        [JsonPropertyName("tokensUsed")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// Time taken to generate the completion
        /// </summary>
        [JsonPropertyName("responseTime")]
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Model used for the completion
        /// </summary>
        [JsonPropertyName("modelUsed")]
        public string ModelUsed { get; set; }

        /// <summary>
        /// Timestamp when the response was generated
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful completion response
        /// </summary>
        /// <param name="content">The completed content</param>
        /// <param name="tokensUsed">Number of tokens used</param>
        /// <param name="responseTime">Time taken for completion</param>
        /// <param name="modelUsed">Model used for completion</param>
        /// <returns>Successful completion response</returns>
        public static CompletionResponse Success(string content, int tokensUsed = 0, TimeSpan responseTime = default, string modelUsed = null)
        {
            return new CompletionResponse
            {
                IsSuccess = true,
                Content = content,
                TokensUsed = tokensUsed,
                ResponseTime = responseTime,
                ModelUsed = modelUsed
            };
        }

        /// <summary>
        /// Creates a failed completion response
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <param name="modelUsed">Model that was attempted</param>
        /// <returns>Failed completion response</returns>
        public static CompletionResponse Failure(string errorMessage, string modelUsed = null)
        {
            return new CompletionResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ModelUsed = modelUsed
            };
        }
    }
}