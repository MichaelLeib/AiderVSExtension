using System;
using System.Net.Http;
using AiderVSExtension.Models;
// using OpenAI;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Factory interface for creating AI client instances
    /// </summary>
    public interface IAIClientFactory : IDisposable
    {
        /// <summary>
        /// Creates an OpenAI client for the specified configuration
        /// </summary>
        /// <param name="config">OpenAI configuration</param>
        /// <returns>OpenAI client instance</returns>
        // OpenAIClient CreateOpenAIClient(AIModelConfiguration config);

        /// <summary>
        /// Creates an HTTP client for Anthropic Claude API
        /// </summary>
        /// <param name="config">Claude configuration</param>
        /// <returns>HTTP client configured for Claude API</returns>
        HttpClient CreateClaudeClient(AIModelConfiguration config);

        /// <summary>
        /// Creates an HTTP client for Ollama API
        /// </summary>
        /// <param name="config">Ollama configuration</param>
        /// <returns>HTTP client configured for Ollama API</returns>
        HttpClient CreateOllamaClient(AIModelConfiguration config);

        /// <summary>
        /// Creates an appropriate client for the given configuration
        /// </summary>
        /// <param name="config">AI model configuration</param>
        /// <returns>Client instance appropriate for the provider</returns>
        object CreateClient(AIModelConfiguration config);
    }
}