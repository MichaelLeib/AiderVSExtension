using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Core service for communicating with Aider AI via AgentAPI
    /// </summary>
    public class AiderService : IAiderService, IDisposable
    {
        private readonly IAgentApiService _agentApiService;
        private readonly IAiderDependencyChecker _dependencyChecker;
        private readonly IErrorHandler _errorHandler;
        private readonly ITelemetryService _telemetryService;
        private readonly ICorrelationService _correlationService;

        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private bool _isInitialized = false;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        public bool IsConnected => _agentApiService?.IsRunning ?? false;

        public AiderService(
            IAgentApiService agentApiService,
            IAiderDependencyChecker dependencyChecker,
            IErrorHandler errorHandler,
            ITelemetryService telemetryService,
            ICorrelationService correlationService)
        {
            _agentApiService = agentApiService ?? throw new ArgumentNullException(nameof(agentApiService));
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _correlationService = correlationService ?? throw new ArgumentNullException(nameof(correlationService));

            // Subscribe to AgentAPI events
            _agentApiService.StatusChanged += OnAgentApiStatusChanged;
            _agentApiService.MessageReceived += OnAgentApiMessageReceived;
        }

        /// <summary>
        /// Initializes the Aider service connection
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                using var context = _correlationService.StartOperation("AiderService.Initialize");
                await _errorHandler.LogWithCorrelationAsync("Initializing Aider service", context, "AiderService.InitializeAsync").ConfigureAwait(false);

                // Check dependencies first
                var dependencyStatus = await _dependencyChecker.CheckDependenciesAsync().ConfigureAwait(false);
                if (!dependencyStatus.IsAiderInstalled)
                {
                    OnConnectionStatusChanged(false, "Aider is not installed. Please install Aider to use this extension.");
                    return;
                }

                // Start AgentAPI server
                var started = await _agentApiService.StartAsync().ConfigureAwait(false);
                if (!started)
                {
                    OnConnectionStatusChanged(false, "Failed to start AgentAPI server");
                    return;
                }

                _isInitialized = true;
                OnConnectionStatusChanged(true, "Connected to Aider via AgentAPI");

                _telemetryService?.TrackEvent("AiderService.Initialized", new Dictionary<string, string>
                {
                    ["AiderVersion"] = dependencyStatus.AiderVersion,
                    ["ServerUrl"] = _agentApiService.ServerUrl
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error initializing Aider service").ConfigureAwait(false);
                OnConnectionStatusChanged(false, $"Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a message to Aider AI
        /// </summary>
        public async Task SendMessageAsync(string message, IEnumerable<FileReference> fileReferences = null)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Aider service is not initialized. Call InitializeAsync() first.");
            }

            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to Aider. Please check your connection.");
            }

            try
            {
                using var context = _correlationService.StartOperation("AiderService.SendMessage");
                await _errorHandler.LogWithCorrelationAsync($"Sending message to Aider: {message.Substring(0, Math.Min(100, message.Length))}...", context, "AiderService.SendMessageAsync").ConfigureAwait(false);

                var request = new AgentApiRequest
                {
                    Content = message,
                    Type = "user"
                };

                // Add file references if provided
                if (fileReferences != null)
                {
                    request.Files.AddRange(fileReferences
                        .Where(fr => !string.IsNullOrEmpty(fr.FilePath))
                        .Select(fr => fr.FilePath));
                }

                // Add correlation context to metadata
                request.Metadata["correlationId"] = context.CorrelationId;
                request.Metadata["operationId"] = context.OperationId;

                var response = await _agentApiService.SendMessageAsync(request).ConfigureAwait(false);

                // Convert AgentAPI response to ChatMessage
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = response.Content ?? "",
                    Type = response.Role == "assistant" ? MessageType.Assistant : MessageType.User,
                    Timestamp = response.Timestamp != default ? response.Timestamp : DateTime.UtcNow,
                    References = new List<FileReference>()
                };

                // Add modified files as references
                if (response.FilesModified?.Any() == true)
                {
                    chatMessage.References.AddRange(response.FilesModified.Select(file => new FileReference
                    {
                        FilePath = file,
                        Type = ReferenceType.File,
                        LineNumber = 0 // Will be populated by file context service if needed
                    }));
                }

                OnMessageReceived(chatMessage);

                _telemetryService?.TrackEvent("AiderService.MessageSent", new Dictionary<string, string>
                {
                    ["MessageLength"] = message.Length.ToString(),
                    ["FileCount"] = (fileReferences?.Count() ?? 0).ToString(),
                    ["ResponseLength"] = (response.Content?.Length ?? 0).ToString(),
                    ["FilesModified"] = (response.FilesModified?.Count ?? 0).ToString()
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error sending message to Aider").ConfigureAwait(false);
                _telemetryService?.TrackException(ex, new Dictionary<string, string>
                {
                    ["Operation"] = "SendMessage",
                    ["MessageLength"] = message.Length.ToString()
                });
                throw;
            }
        }

        /// <summary>
        /// Sends a message to Aider AI and returns the response
        /// </summary>
        public async Task<ChatMessage> SendMessageAsync(ChatMessage userMessage)
        {
            if (userMessage == null)
                throw new ArgumentNullException(nameof(userMessage));

            try
            {
                using var context = _correlationService.StartOperation("AiderService.SendMessageWithResponse");

                var request = new AgentApiRequest
                {
                    Content = userMessage.Content,
                    Type = "user"
                };

                // Add file references from the user message
                if (userMessage.References?.Any() == true)
                {
                    request.Files.AddRange(userMessage.References
                        .Where(fr => !string.IsNullOrEmpty(fr.FilePath))
                        .Select(fr => fr.FilePath));
                }

                // Add correlation context
                request.Metadata["correlationId"] = context.CorrelationId;
                request.Metadata["operationId"] = context.OperationId;
                request.Metadata["userMessageId"] = userMessage.Id;

                var response = await _agentApiService.SendMessageAsync(request).ConfigureAwait(false);

                // Convert to ChatMessage
                var assistantMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = response.Content ?? "",
                    Type = MessageType.Assistant,
                    Timestamp = response.Timestamp != default ? response.Timestamp : DateTime.UtcNow,
                    References = new List<FileReference>()
                };

                // Add modified files as references
                if (response.FilesModified?.Any() == true)
                {
                    assistantMessage.References.AddRange(response.FilesModified.Select(file => new FileReference
                    {
                        FilePath = file,
                        Type = ReferenceType.File,
                        LineNumber = 0
                    }));
                }

                // Add suggested changes as metadata
                if (response.SuggestedChanges?.Any() == true)
                {
                    assistantMessage.Metadata = assistantMessage.Metadata ?? new Dictionary<string, object>();
                    assistantMessage.Metadata["suggestedChanges"] = response.SuggestedChanges;
                }

                OnMessageReceived(assistantMessage);
                return assistantMessage;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error sending message with response to Aider").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Gets the chat history (not implemented for AgentAPI)
        /// </summary>
        public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync()
        {
            // AgentAPI doesn't provide chat history endpoint
            // Return empty list for now
            await Task.CompletedTask;
            return new List<ChatMessage>();
        }

        /// <summary>
        /// Clears the chat history (not implemented for AgentAPI)
        /// </summary>
        public async Task ClearChatHistoryAsync()
        {
            // AgentAPI doesn't provide clear history endpoint
            // This is a no-op for now
            await Task.CompletedTask;
        }

        /// <summary>
        /// Saves the current conversation (not implemented for AgentAPI)
        /// </summary>
        public async Task SaveConversationAsync()
        {
            // AgentAPI handles persistence internally
            // This is a no-op for now
            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads a previously saved conversation (not implemented for AgentAPI)
        /// </summary>
        public async Task LoadConversationAsync()
        {
            // AgentAPI handles persistence internally
            // This is a no-op for now
            await Task.CompletedTask;
        }

        /// <summary>
        /// Archives the current conversation and starts a new one
        /// </summary>
        public async Task ArchiveConversationAsync(string archiveName = null)
        {
            try
            {
                // Restart the AgentAPI server to start fresh
                var restarted = await _agentApiService.RestartAsync().ConfigureAwait(false);
                if (restarted)
                {
                    _telemetryService?.TrackEvent("AiderService.ConversationArchived", new Dictionary<string, string>
                    {
                        ["ArchiveName"] = archiveName ?? "Unnamed"
                    });
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "Error archiving conversation").ConfigureAwait(false);
                throw;
            }
        }

        private void OnAgentApiStatusChanged(object sender, AgentApiEventArgs e)
        {
            var isConnected = e.EventType == "running";
            OnConnectionStatusChanged(isConnected, e.Message);
        }

        private void OnAgentApiMessageReceived(object sender, AgentApiEventArgs e)
        {
            if (e.Data is AgentApiResponse response)
            {
                var chatMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = response.Content ?? "",
                    Type = MessageType.Assistant,
                    Timestamp = response.Timestamp != default ? response.Timestamp : DateTime.UtcNow,
                    References = response.FilesModified?.Select(file => new FileReference
                    {
                        FilePath = file,
                        Type = ReferenceType.File
                    }).ToList() ?? new List<FileReference>()
                };

                OnMessageReceived(chatMessage);
            }
        }

        private void OnMessageReceived(ChatMessage message)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        private void OnConnectionStatusChanged(bool isConnected, string errorMessage = null)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
            {
                IsConnected = isConnected,
                ErrorMessage = errorMessage
            });
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (!_disposed)
                {
                    try
                    {
                        // Unsubscribe from events to prevent memory leaks
                        if (_agentApiService != null)
                        {
                            _agentApiService.StatusChanged -= OnAgentApiStatusChanged;
                            _agentApiService.MessageReceived -= OnAgentApiMessageReceived;
                        }

                        // Dispose underlying services if they implement IDisposable
                        // Note: Only dispose services that this class owns/created
                        if (_agentApiService is IDisposable agentApiDisposable)
                        {
                            agentApiDisposable.Dispose();
                        }

                        // Clear event handlers to prevent memory leaks
                        MessageReceived = null;
                        ConnectionStatusChanged = null;

                        // Note: _errorHandler and other injected dependencies are typically
                        // managed by the DI container and should not be disposed here
                    }
                    catch (Exception ex)
                    {
                        _errorHandler?.LogErrorAsync("Error during AiderService disposal", ex, "AiderService.Dispose");
                    }

                    _disposed = true;
                }
            }
        }
    }
}