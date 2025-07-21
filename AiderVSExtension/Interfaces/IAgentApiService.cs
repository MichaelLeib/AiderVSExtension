using System;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing AgentAPI server and communication with Aider
    /// </summary>
    public interface IAgentApiService
    {
        /// <summary>
        /// Event fired when server status changes
        /// </summary>
        event EventHandler<AgentApiEventArgs> StatusChanged;

        /// <summary>
        /// Event fired when a message is received from Aider
        /// </summary>
        event EventHandler<AgentApiEventArgs> MessageReceived;

        /// <summary>
        /// Gets whether the AgentAPI server is currently running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the server URL
        /// </summary>
        string ServerUrl { get; }

        /// <summary>
        /// Starts the AgentAPI server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if server started successfully</returns>
        Task<bool> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the AgentAPI server
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task StopAsync();

        /// <summary>
        /// Sends a message to Aider via AgentAPI
        /// </summary>
        /// <param name="request">Message request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from Aider</returns>
        Task<AgentApiResponse> SendMessageAsync(AgentApiRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of the AgentAPI server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Server status</returns>
        Task<AgentApiStatus> GetStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Restarts the AgentAPI server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if restart was successful</returns>
        Task<bool> RestartAsync(CancellationToken cancellationToken = default);
    }
}