using System;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Manages the overall session state including conversation persistence and application lifecycle
    /// </summary>
    public class SessionManager : IDisposable
    {
        private readonly IApplicationStateService _applicationStateService;
        private readonly IConversationPersistenceService _conversationPersistenceService;
        private readonly object _lockObject = new object();
        
        private bool _disposed = false;
        private Conversation? _currentConversation;
        private string? _activeConversationId;
        private DateTime _sessionStartTime;
        private bool _isSessionActive = false;

        // Events
        public event EventHandler<SessionStartedEventArgs>? SessionStarted;
        public event EventHandler<SessionEndedEventArgs>? SessionEnded;
        public event EventHandler<ConversationChangedEventArgs>? ConversationChanged;
        public event EventHandler<SessionErrorEventArgs>? SessionError;

        // Properties
        public bool IsSessionActive => _isSessionActive;
        public Conversation? CurrentConversation => _currentConversation;
        public string? ActiveConversationId => _activeConversationId;
        public DateTime SessionStartTime => _sessionStartTime;
        public TimeSpan SessionDuration => _isSessionActive ? DateTime.UtcNow - _sessionStartTime : TimeSpan.Zero;

        public SessionManager(
            IApplicationStateService applicationStateService,
            IConversationPersistenceService conversationPersistenceService)
        {
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _conversationPersistenceService = conversationPersistenceService ?? throw new ArgumentNullException(nameof(conversationPersistenceService));

            // Subscribe to application state events
            _applicationStateService.ExtensionInitialized += OnExtensionInitialized;
            _applicationStateService.ExtensionShuttingDown += OnExtensionShuttingDown;
            _applicationStateService.SolutionOpened += OnSolutionOpened;
            _applicationStateService.SolutionClosed += OnSolutionClosed;
        }

        /// <summary>
        /// Starts a new session
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task StartSessionAsync()
        {
            if (_isSessionActive)
                return;

            try
            {
                _sessionStartTime = DateTime.UtcNow;
                _isSessionActive = true;

                // Try to restore the last active conversation
                await RestoreLastConversationAsync();

                // Perform session cleanup
                await PerformSessionCleanupAsync();

                SessionStarted?.Invoke(this, new SessionStartedEventArgs
                {
                    StartTime = _sessionStartTime,
                    RestoredConversationId = _activeConversationId
                });
            }
            catch (Exception ex)
            {
                _isSessionActive = false;
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "StartSessionAsync",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Ends the current session
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task EndSessionAsync()
        {
            if (!_isSessionActive)
                return;

            try
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - _sessionStartTime;

                // Save current conversation if active
                if (_currentConversation != null)
                {
                    await SaveCurrentConversationAsync();
                }

                // Save session state
                await SaveSessionStateAsync();

                // Perform cleanup
                await PerformSessionCleanupAsync();

                _isSessionActive = false;

                SessionEnded?.Invoke(this, new SessionEndedEventArgs
                {
                    EndTime = endTime,
                    Duration = duration,
                    ConversationId = _activeConversationId,
                    MessageCount = _currentConversation?.MessageCount ?? 0
                });

                // Clear current conversation
                _currentConversation = null;
                _activeConversationId = null;
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "EndSessionAsync",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Creates a new conversation and sets it as active
        /// </summary>
        /// <param name="title">Optional title for the conversation</param>
        /// <returns>The created conversation</returns>
        public async Task<Conversation> CreateNewConversationAsync(string? title = null)
        {
            try
            {
                // Save current conversation if exists
                if (_currentConversation != null)
                {
                    await SaveCurrentConversationAsync();
                }

                // Create new conversation
                var conversation = new Conversation
                {
                    Title = title ?? "New Conversation",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                // Set as current conversation
                await SetActiveConversationAsync(conversation);

                return conversation;
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "CreateNewConversationAsync",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Loads and sets a conversation as active
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to load</param>
        /// <returns>The loaded conversation, or null if not found</returns>
        public async Task<Conversation?> LoadConversationAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));

            try
            {
                // Save current conversation if exists
                if (_currentConversation != null && _currentConversation.Id != conversationId)
                {
                    await SaveCurrentConversationAsync();
                }

                // Load the requested conversation
                var conversation = await _conversationPersistenceService.LoadConversationAsync(conversationId);
                if (conversation != null)
                {
                    await SetActiveConversationAsync(conversation);
                }

                return conversation;
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = $"LoadConversationAsync({conversationId})",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Saves the current conversation
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveCurrentConversationAsync()
        {
            if (_currentConversation == null)
                return;

            try
            {
                _currentConversation.LastUpdatedAt = DateTime.UtcNow;
                await _conversationPersistenceService.SaveConversationAsync(_currentConversation);
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "SaveCurrentConversationAsync",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Adds a message to the current conversation
        /// </summary>
        /// <param name="message">The message to add</param>
        /// <returns>Task representing the async operation</returns>
        public async Task AddMessageToCurrentConversationAsync(ChatMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Create new conversation if none exists
                if (_currentConversation == null)
                {
                    await CreateNewConversationAsync();
                }

                _currentConversation!.AddMessage(message);

                // Auto-save after adding message
                await SaveCurrentConversationAsync();
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "AddMessageToCurrentConversationAsync",
                    Exception = ex
                });
                throw;
            }
        }

        /// <summary>
        /// Performs graceful recovery from errors
        /// </summary>
        /// <param name="errorContext">Context about the error</param>
        /// <returns>Task representing the async operation</returns>
        public async Task RecoverFromErrorAsync(string errorContext)
        {
            try
            {
                // Try to save current conversation if possible
                if (_currentConversation != null)
                {
                    try
                    {
                        await SaveCurrentConversationAsync();
                    }
                    catch
                    {
                        // If we can't save, at least try to preserve the conversation in memory
                    }
                }

                // Delegate to application state service for general recovery
                await _applicationStateService.RecoverFromErrorAsync(errorContext);

                // Try to restart session if it was active
                if (!_isSessionActive)
                {
                    await StartSessionAsync();
                }
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = $"RecoverFromErrorAsync({errorContext})",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Sets a conversation as the active conversation
        /// </summary>
        /// <param name="conversation">The conversation to set as active</param>
        /// <returns>Task representing the async operation</returns>
        private async Task SetActiveConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            lock (_lockObject)
            {
                var oldConversationId = _activeConversationId;
                _currentConversation = conversation;
                _activeConversationId = conversation.Id;

                ConversationChanged?.Invoke(this, new ConversationChangedEventArgs
                {
                    OldConversationId = oldConversationId,
                    NewConversationId = conversation.Id,
                    ChangedAt = DateTime.UtcNow
                });
            }

            // Save the conversation to ensure it's persisted
            await SaveCurrentConversationAsync();
        }

        /// <summary>
        /// Restores the last active conversation from the previous session
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        private async Task RestoreLastConversationAsync()
        {
            try
            {
                // Get the most recent conversation
                var summaries = await _conversationPersistenceService.GetConversationSummariesAsync(false);
                if (summaries.Count > 0)
                {
                    var mostRecent = summaries[0]; // Already ordered by LastUpdatedAt descending
                    var conversation = await _conversationPersistenceService.LoadConversationAsync(mostRecent.Id);
                    if (conversation != null)
                    {
                        await SetActiveConversationAsync(conversation);
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't throw during restore - just log and continue
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "RestoreLastConversationAsync",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Saves session state information
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        private async Task SaveSessionStateAsync()
        {
            try
            {
                // Save application state
                await _applicationStateService.SaveStateAsync();
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "SaveSessionStateAsync",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Performs session cleanup tasks
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        private async Task PerformSessionCleanupAsync()
        {
            try
            {
                // Cleanup old conversations (runs in background)
                _ = CleanupOldConversationsAsync();

                // Perform application cleanup
                await _applicationStateService.PerformCleanupAsync();
            }
            catch (Exception ex)
            {
                SessionError?.Invoke(this, new SessionErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Context = "PerformSessionCleanupAsync",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Safely cleans up old conversations in the background
        /// </summary>
        private async Task CleanupOldConversationsAsync()
        {
            try
            {
                await _conversationPersistenceService.CleanupOldConversationsAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Handles extension initialized event
        /// </summary>
        private async void OnExtensionInitialized(object? sender, ExtensionInitializedEventArgs e)
        {
            if (e.IsSuccessfulful)
            {
                try
                {
                    await StartSessionAsync();
                }
                catch (Exception ex)
                {
                    await _applicationStateService.HandleUnhandledExceptionAsync(ex, "SessionManager.OnExtensionInitialized");
                }
            }
        }

        /// <summary>
        /// Handles extension shutting down event
        /// </summary>
        private async void OnExtensionShuttingDown(object? sender, ExtensionShuttingDownEventArgs e)
        {
            try
            {
                await EndSessionAsync();
            }
            catch (Exception ex)
            {
                await _applicationStateService.HandleUnhandledExceptionAsync(ex, "SessionManager.OnExtensionShuttingDown");
            }
        }

        /// <summary>
        /// Handles solution opened event
        /// </summary>
        private async void OnSolutionOpened(object? sender, SolutionOpenedEventArgs e)
        {
            try
            {
                // Auto-save current conversation when solution changes
                if (_currentConversation != null)
                {
                    await SaveCurrentConversationAsync();
                }
            }
            catch (Exception ex)
            {
                await _applicationStateService.HandleUnhandledExceptionAsync(ex, "SessionManager.OnSolutionOpened");
            }
        }

        /// <summary>
        /// Handles solution closed event
        /// </summary>
        private async void OnSolutionClosed(object? sender, SolutionClosedEventArgs e)
        {
            try
            {
                // Auto-save current conversation when solution closes
                if (_currentConversation != null)
                {
                    await SaveCurrentConversationAsync();
                }
            }
            catch (Exception ex)
            {
                await _applicationStateService.HandleUnhandledExceptionAsync(ex, "SessionManager.OnSolutionClosed");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Unsubscribe from application state service events
                try
                {
                    _applicationStateService.ExtensionInitialized -= OnExtensionInitialized;
                    _applicationStateService.ExtensionShuttingDown -= OnExtensionShuttingDown;
                    _applicationStateService.SolutionOpened -= OnSolutionOpened;
                    _applicationStateService.SolutionClosed -= OnSolutionClosed;
                }
                catch
                {
                    // Ignore errors during event unsubscription
                }

                // Try to end session gracefully
                try
                {
                    var endTask = EndSessionAsync();
                    if (!endTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        // Session didn't end gracefully within timeout
                        _isSessionActive = false;
                    }
                }
                catch
                {
                    // Ignore errors during disposal
                    _isSessionActive = false;
                }

                // Clear own event handlers to prevent memory leaks
                SessionStarted = null;
                SessionEnded = null;
                ConversationChanged = null;
                SessionError = null;

                // Clear conversation references
                _currentConversation = null;
                _activeConversationId = null;

                _disposed = true;
            }
        }

        ~SessionManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Event arguments for session started events
    /// </summary>
    public class SessionStartedEventArgs : EventArgs
    {
        public DateTime StartTime { get; set; }
        public string? RestoredConversationId { get; set; }
    }

    /// <summary>
    /// Event arguments for session ended events
    /// </summary>
    public class SessionEndedEventArgs : EventArgs
    {
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ConversationId { get; set; }
        public int MessageCount { get; set; }
    }

    /// <summary>
    /// Event arguments for conversation changed events
    /// </summary>
    public class ConversationChangedEventArgs : EventArgs
    {
        public string? OldConversationId { get; set; }
        public string? NewConversationId { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for session error events
    /// </summary>
    public class SessionErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}