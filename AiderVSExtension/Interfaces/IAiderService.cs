using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for the core Aider AI service that handles communication with the Aider backend
    /// </summary>
    public interface IAiderService
    {
        /// <summary>
        /// Event fired when a new message is received from Aider
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event fired when the connection status changes
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Gets the current connection status
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initializes the Aider service connection
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Sends a message to Aider AI
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="fileReferences">Optional file references to include</param>
        /// <returns>Task representing the async operation</returns>
        Task SendMessageAsync(string message, IEnumerable<FileReference> fileReferences = null);

        /// <summary>
        /// Sends a message to Aider AI and returns the response
        /// </summary>
        /// <param name="userMessage">The user message to send</param>
        /// <returns>The response from Aider AI</returns>
        Task<ChatMessage> SendMessageAsync(ChatMessage userMessage);

        /// <summary>
        /// Gets the chat history
        /// </summary>
        /// <returns>List of chat messages</returns>
        Task<IEnumerable<ChatMessage>> GetChatHistoryAsync();

        /// <summary>
        /// Clears the chat history
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ClearChatHistoryAsync();

        /// <summary>
        /// Saves the current conversation to persistent storage
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SaveConversationAsync();

        /// <summary>
        /// Loads a previously saved conversation from persistent storage
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task LoadConversationAsync();

        /// <summary>
        /// Archives the current conversation and starts a new one
        /// </summary>
        /// <param name="archiveName">Optional name for the archived conversation</param>
        /// <returns>Task representing the async operation</returns>
        Task ArchiveConversationAsync(string archiveName = null);
    }

}