using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the AI provider for model configuration
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AIProvider
    {
        /// <summary>
        /// OpenAI ChatGPT models
        /// </summary>
        ChatGPT = 0,

        /// <summary>
        /// Anthropic Claude models
        /// </summary>
        Claude = 1,

        /// <summary>
        /// Ollama local/remote models
        /// </summary>
        Ollama = 2
    }
}