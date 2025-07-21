using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing AI model configurations and connections
    /// </summary>
    public interface IAIModelManager
    {
        /// <summary>
        /// Event fired when the active model changes
        /// </summary>
        event EventHandler<ModelChangedEventArgs> ActiveModelChanged;

        /// <summary>
        /// Gets the currently active AI model
        /// </summary>
        AIModelConfiguration ActiveModel { get; }

        /// <summary>
        /// Gets all available AI model configurations
        /// </summary>
        /// <returns>List of AI model configurations</returns>
        IEnumerable<AIModelConfiguration> GetAvailableModels();

        /// <summary>
        /// Sets the active AI model
        /// </summary>
        /// <param name="provider">The AI provider to use</param>
        /// <returns>Task representing the async operation</returns>
        Task SetActiveModelAsync(AIProvider provider);

        /// <summary>
        /// Tests the connection to the specified AI model
        /// </summary>
        /// <param name="configuration">The model configuration to test</param>
        /// <returns>Connection test result</returns>
        Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration configuration);

        /// <summary>
        /// Sends a completion request to the active AI model
        /// </summary>
        /// <param name="request">The completion request</param>
        /// <returns>The completion response</returns>
        Task<CompletionResponse> GetCompletionAsync(CompletionRequest request);

        /// <summary>
        /// Sends a chat message to the active AI model
        /// </summary>
        /// <param name="messages">The chat messages</param>
        /// <returns>The chat response</returns>
        Task<ChatResponse> SendChatAsync(IEnumerable<ChatMessage> messages);

        /// <summary>
        /// Gets the available models for a specific provider
        /// </summary>
        /// <param name="provider">The AI provider</param>
        /// <returns>List of available model names</returns>
        Task<IEnumerable<string>> GetAvailableModelsForProviderAsync(AIProvider provider);

        /// <summary>
        /// Initializes the AI model manager
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Gets the current model configuration
        /// </summary>
        /// <returns>Current model configuration</returns>
        AIModelConfiguration GetCurrentModel();

        /// <summary>
        /// Generates a completion using the current AI model
        /// </summary>
        /// <param name="prompt">The prompt for completion</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated completion text</returns>
        Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Event arguments for model changed events
    /// </summary>
    public class ModelChangedEventArgs : EventArgs
    {
        public AIModelConfiguration PreviousModel { get; set; }
        public AIModelConfiguration NewModel { get; set; }
    }



}