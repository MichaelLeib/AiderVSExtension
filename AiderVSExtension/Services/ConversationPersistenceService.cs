using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for persisting and managing conversation data
    /// </summary>
    public class ConversationPersistenceService : IConversationPersistenceService, IDisposable
    {
        private readonly string _conversationsDirectory;
        private readonly string _archiveDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lockObject = new object();
        private Dictionary<string, ConversationSummary> _conversationCache;
        private Dictionary<string, DateTime> _cacheAccessTimes;
        private readonly Timer _cacheCleanupTimer;

        // Memory optimization constants
        private const int MAX_CACHE_SIZE = 100;
        private const int CACHE_CLEANUP_INTERVAL_MINUTES = 30;
        private const int CACHE_EXPIRY_HOURS = 2;

        private bool _initialized = false;
        private bool _disposed = false;

        public ConversationPersistenceService()
        {
            // Set up directories in user's AppData (lightweight path operations only)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var extensionDataPath = Path.Combine(appDataPath, "AiderVSExtension");
            
            _conversationsDirectory = Path.Combine(extensionDataPath, "Conversations");
            _archiveDirectory = Path.Combine(extensionDataPath, "Archive");

            // Configure JSON serialization options (lightweight)
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            _conversationCache = new Dictionary<string, ConversationSummary>();
            _cacheAccessTimes = new Dictionary<string, DateTime>();
            
            // Initialize cache cleanup timer
            _cacheCleanupTimer = new Timer(CleanupCache, null, 
                TimeSpan.FromMinutes(CACHE_CLEANUP_INTERVAL_MINUTES),
                TimeSpan.FromMinutes(CACHE_CLEANUP_INTERVAL_MINUTES));
            
            // NOTE: Directory creation and cache initialization moved to InitializeAsync()
            // to avoid blocking the UI thread during service construction
        }

        /// <summary>
        /// Initializes the service asynchronously (creates directories and loads cache)
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await Task.Run(() =>
            {
                try
                {
                    // Create directories on background thread
                    Directory.CreateDirectory(_conversationsDirectory);
                    Directory.CreateDirectory(_archiveDirectory);

                    // Initialize cache on background thread
                    InitializeCache();

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing ConversationPersistenceService: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Ensures the service is initialized before use
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Saves a conversation to persistent storage
        /// </summary>
        /// <param name="conversation">The conversation to save</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            if (!conversation.IsValid())
                throw new ArgumentException("Invalid conversation data", nameof(conversation));

            // Ensure service is initialized
            await EnsureInitializedAsync();

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var filePath = GetConversationFilePath(conversation.Id, conversation.IsArchived);
                var json = JsonSerializer.Serialize(conversation, _jsonOptions);

                lock (_lockObject)
                {
                    File.WriteAllText(filePath, json);
                    
                    // Update cache with memory optimization
                    var summary = conversation.CreateSummary();
                    summary.FileSizeBytes = new FileInfo(filePath).Length;
                    
                    // Add to cache with LRU management
                    AddToCacheWithLRU(conversation.Id, summary);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save conversation {conversation.Id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads a conversation from persistent storage
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to load</param>
        /// <returns>The loaded conversation, or null if not found</returns>
        public async Task<Conversation?> LoadConversationAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var filePath = FindConversationFile(conversationId);
                if (filePath == null || !File.Exists(filePath))
                    return null;

                lock (_lockObject)
                {
                    var json = File.ReadAllText(filePath);
                    var conversation = JsonSerializer.Deserialize<Conversation>(json, _jsonOptions);
                    
                    if (conversation != null && !conversation.IsValid())
                    {
                        throw new InvalidDataException($"Loaded conversation {conversationId} contains invalid data");
                    }

                    // Update cache access time for LRU tracking
                    if (conversation != null)
                    {
                        _cacheAccessTimes[conversationId] = DateTime.UtcNow;
                    }

                    return conversation;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load conversation {conversationId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a conversation from persistent storage
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to delete</param>
        /// <returns>True if deleted successfully, false if not found</returns>
        public async Task<bool> DeleteConversationAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var filePath = FindConversationFile(conversationId);
                if (filePath == null || !File.Exists(filePath))
                    return false;

                lock (_lockObject)
                {
                    File.Delete(filePath);
                    _conversationCache.Remove(conversationId);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete conversation {conversationId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets summaries of all conversations
        /// </summary>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>List of conversation summaries</returns>
        public async Task<List<ConversationSummary>> GetConversationSummariesAsync(bool includeArchived = false)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                lock (_lockObject)
                {
                    var summaries = _conversationCache.Values.ToList();
                    
                    if (!includeArchived)
                    {
                        summaries = summaries.Where(s => !s.IsArchived).ToList();
                    }

                    return summaries.OrderByDescending(s => s.LastUpdatedAt).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get conversation summaries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Archives a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to archive</param>
        /// <returns>True if archived successfully, false if not found</returns>
        public async Task<bool> ArchiveConversationAsync(string conversationId)
        {
            var conversation = await LoadConversationAsync(conversationId);
            if (conversation == null)
                return false;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Move file from conversations to archive directory
                var currentPath = GetConversationFilePath(conversationId, false);
                var archivePath = GetConversationFilePath(conversationId, true);

                lock (_lockObject)
                {
                    if (File.Exists(currentPath))
                    {
                        conversation.Archive();
                        var json = JsonSerializer.Serialize(conversation, _jsonOptions);
                        
                        File.WriteAllText(archivePath, json);
                        File.Delete(currentPath);

                        // Update cache
                        var summary = conversation.CreateSummary();
                        summary.FileSizeBytes = new FileInfo(archivePath).Length;
                        _conversationCache[conversationId] = summary;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to archive conversation {conversationId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unarchives a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to unarchive</param>
        /// <returns>True if unarchived successfully, false if not found</returns>
        public async Task<bool> UnarchiveConversationAsync(string conversationId)
        {
            var conversation = await LoadConversationAsync(conversationId);
            if (conversation == null)
                return false;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Move file from archive to conversations directory
                var archivePath = GetConversationFilePath(conversationId, true);
                var conversationPath = GetConversationFilePath(conversationId, false);

                lock (_lockObject)
                {
                    if (File.Exists(archivePath))
                    {
                        conversation.Unarchive();
                        var json = JsonSerializer.Serialize(conversation, _jsonOptions);
                        
                        File.WriteAllText(conversationPath, json);
                        File.Delete(archivePath);

                        // Update cache
                        var summary = conversation.CreateSummary();
                        summary.FileSizeBytes = new FileInfo(conversationPath).Length;
                        _conversationCache[conversationId] = summary;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to unarchive conversation {conversationId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Searches conversations by query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive</param>
        /// <returns>List of matching conversation summaries</returns>
        public async Task<List<ConversationSummary>> SearchConversationsAsync(string query, bool includeArchived = false, bool caseSensitive = false)
        {
            var allSummaries = await GetConversationSummariesAsync(includeArchived);
            
            if (string.IsNullOrWhiteSpace(query))
                return allSummaries;

            return allSummaries.Where(s => s.MatchesSearch(query, caseSensitive)).ToList();
        }

        /// <summary>
        /// Gets conversations by tag
        /// </summary>
        /// <param name="tag">The tag to filter by</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>List of conversation summaries with the specified tag</returns>
        public async Task<List<ConversationSummary>> GetConversationsByTagAsync(string tag, bool includeArchived = false)
        {
            var allSummaries = await GetConversationSummariesAsync(includeArchived);
            
            if (string.IsNullOrWhiteSpace(tag))
                return allSummaries;

            return allSummaries.Where(s => s.HasTag(tag)).ToList();
        }

        /// <summary>
        /// Cleans up old conversations based on retention policy
        /// </summary>
        /// <param name="maxAge">Maximum age in days for non-archived conversations</param>
        /// <param name="maxCount">Maximum number of conversations to keep</param>
        /// <returns>Number of conversations cleaned up</returns>
        public async Task<int> CleanupOldConversationsAsync(int maxAge = 90, int maxCount = 1000)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var summaries = await GetConversationSummariesAsync(false);
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAge);
                var cleanupCount = 0;

                // Archive old conversations
                var oldConversations = summaries
                    .Where(s => s.LastUpdatedAt < cutoffDate)
                    .OrderBy(s => s.LastUpdatedAt)
                    .ToList();

                foreach (var summary in oldConversations)
                {
                    if (await ArchiveConversationAsync(summary.Id))
                    {
                        cleanupCount++;
                    }
                }

                // If still over limit, archive oldest conversations
                var remainingSummaries = await GetConversationSummariesAsync(false);
                if (remainingSummaries.Count > maxCount)
                {
                    var excessConversations = remainingSummaries
                        .OrderBy(s => s.LastUpdatedAt)
                        .Take(remainingSummaries.Count - maxCount)
                        .ToList();

                    foreach (var summary in excessConversations)
                    {
                        if (await ArchiveConversationAsync(summary.Id))
                        {
                            cleanupCount++;
                        }
                    }
                }

                return cleanupCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to cleanup old conversations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports conversations to a backup file
        /// </summary>
        /// <param name="filePath">The path to export to</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>Number of conversations exported</returns>
        public async Task<int> ExportConversationsAsync(string filePath, bool includeArchived = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var summaries = await GetConversationSummariesAsync(includeArchived);
                var conversations = new List<Conversation>();

                foreach (var summary in summaries)
                {
                    var conversation = await LoadConversationAsync(summary.Id);
                    if (conversation != null)
                    {
                        conversations.Add(conversation);
                    }
                }

                var exportData = new
                {
                    ExportDate = DateTime.UtcNow,
                    Version = "1.0",
                    ConversationCount = conversations.Count,
                    Conversations = conversations
                };

                var json = JsonSerializer.Serialize(exportData, _jsonOptions);
                File.WriteAllText(filePath, json);

                return conversations.Count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export conversations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Imports conversations from a backup file
        /// </summary>
        /// <param name="filePath">The path to import from</param>
        /// <returns>Number of conversations imported</returns>
        public async Task<int> ImportConversationsAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Import file not found: {filePath}");

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var json = File.ReadAllText(filePath);
                var importData = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);

                if (!importData.TryGetProperty("Conversations", out var conversationsElement))
                    throw new InvalidDataException("Invalid import file format: missing Conversations property");

                var conversations = JsonSerializer.Deserialize<List<Conversation>>(conversationsElement.GetRawText(), _jsonOptions);
                if (conversations == null)
                    throw new InvalidDataException("Failed to deserialize conversations from import file");

                var importCount = 0;
                foreach (var conversation in conversations)
                {
                    if (conversation.IsValid())
                    {
                        // Generate new ID if conversation already exists
                        var existingConversation = await LoadConversationAsync(conversation.Id);
                        if (existingConversation != null)
                        {
                            conversation.Id = Guid.NewGuid().ToString();
                        }

                        await SaveConversationAsync(conversation);
                        importCount++;
                    }
                }

                return importCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import conversations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the file path for a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="isArchived">Whether the conversation is archived</param>
        /// <returns>The file path</returns>
        private string GetConversationFilePath(string conversationId, bool isArchived)
        {
            var directory = isArchived ? _archiveDirectory : _conversationsDirectory;
            return Path.Combine(directory, $"{conversationId}.json");
        }

        /// <summary>
        /// Finds the file path for a conversation (checks both active and archived)
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <returns>The file path if found, null otherwise</returns>
        private string? FindConversationFile(string conversationId)
        {
            var activePath = GetConversationFilePath(conversationId, false);
            if (File.Exists(activePath))
                return activePath;

            var archivedPath = GetConversationFilePath(conversationId, true);
            if (File.Exists(archivedPath))
                return archivedPath;

            return null;
        }

        /// <summary>
        /// Initializes the conversation cache
        /// </summary>
        private void InitializeCache()
        {
            try
            {
                // Load active conversations
                LoadConversationsToCache(_conversationsDirectory, false);
                
                // Load archived conversations
                LoadConversationsToCache(_archiveDirectory, true);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - cache will be empty but service will still work
                System.Diagnostics.Debug.WriteLine($"Failed to initialize conversation cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads conversations from a directory into the cache
        /// </summary>
        /// <param name="directory">The directory to load from</param>
        /// <param name="isArchived">Whether the conversations are archived</param>
        private void LoadConversationsToCache(string directory, bool isArchived)
        {
            if (!Directory.Exists(directory))
                return;

            var files = Directory.GetFiles(directory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var conversation = JsonSerializer.Deserialize<Conversation>(json, _jsonOptions);
                    
                    if (conversation != null && conversation.IsValid())
                    {
                        var summary = conversation.CreateSummary();
                        summary.FileSizeBytes = new FileInfo(file).Length;
                        _conversationCache[conversation.Id] = summary;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    System.Diagnostics.Debug.WriteLine($"Failed to load conversation from {file}: {ex.Message}");
                }
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose timer first
                    _cacheCleanupTimer?.Dispose();
                    
                    // Clear conversation cache
                    lock (_lockObject)
                    {
                        _conversationCache?.Clear();
                        _conversationCache = null;
                        _cacheAccessTimes?.Clear();
                        _cacheAccessTimes = null;
                    }
                }

                _disposed = true;
            }
        }

        #endregion

        #region Cache Management

        private void AddToCacheWithLRU(string conversationId, ConversationSummary summary)
        {
            lock (_lockObject)
            {
                if (_disposed) return;

                // Update cache and access time
                _conversationCache[conversationId] = summary;
                _cacheAccessTimes[conversationId] = DateTime.UtcNow;

                // Check if cache size exceeds limit
                if (_conversationCache.Count > MAX_CACHE_SIZE)
                {
                    EvictLeastRecentlyUsed();
                }
            }
        }

        private void EvictLeastRecentlyUsed()
        {
            if (_cacheAccessTimes.Count == 0) return;

            // Find the least recently used item
            var lruKey = _cacheAccessTimes.OrderBy(kvp => kvp.Value).First().Key;
            
            // Remove from both caches
            _conversationCache.Remove(lruKey);
            _cacheAccessTimes.Remove(lruKey);
        }

        private void CleanupCache(object state)
        {
            if (_disposed) return;

            try
            {
                lock (_lockObject)
                {
                    if (_disposed) return;

                    var cutoffTime = DateTime.UtcNow.AddHours(-CACHE_EXPIRY_HOURS);
                    var expiredKeys = _cacheAccessTimes
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _conversationCache.Remove(key);
                        _cacheAccessTimes.Remove(key);
                    }

                    // Force garbage collection if we freed significant memory
                    if (expiredKeys.Count > 10)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors to prevent timer issues
            }
        }

        #endregion
    }
}