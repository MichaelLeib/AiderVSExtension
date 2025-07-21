using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for conversation persistence and management services
    /// </summary>
    public interface IConversationPersistenceService
    {
        /// <summary>
        /// Saves a conversation to persistent storage
        /// </summary>
        /// <param name="conversation">The conversation to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveConversationAsync(Conversation conversation);

        /// <summary>
        /// Loads a conversation from persistent storage
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to load</param>
        /// <returns>The loaded conversation, or null if not found</returns>
        Task<Conversation?> LoadConversationAsync(string conversationId);

        /// <summary>
        /// Deletes a conversation from persistent storage
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to delete</param>
        /// <returns>True if deleted successfully, false if not found</returns>
        Task<bool> DeleteConversationAsync(string conversationId);

        /// <summary>
        /// Gets summaries of all conversations
        /// </summary>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>List of conversation summaries</returns>
        Task<List<ConversationSummary>> GetConversationSummariesAsync(bool includeArchived = false);

        /// <summary>
        /// Archives a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to archive</param>
        /// <returns>True if archived successfully, false if not found</returns>
        Task<bool> ArchiveConversationAsync(string conversationId);

        /// <summary>
        /// Unarchives a conversation
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to unarchive</param>
        /// <returns>True if unarchived successfully, false if not found</returns>
        Task<bool> UnarchiveConversationAsync(string conversationId);

        /// <summary>
        /// Searches conversations by query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive</param>
        /// <returns>List of matching conversation summaries</returns>
        Task<List<ConversationSummary>> SearchConversationsAsync(string query, bool includeArchived = false, bool caseSensitive = false);

        /// <summary>
        /// Gets conversations by tag
        /// </summary>
        /// <param name="tag">The tag to filter by</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>List of conversation summaries with the specified tag</returns>
        Task<List<ConversationSummary>> GetConversationsByTagAsync(string tag, bool includeArchived = false);

        /// <summary>
        /// Cleans up old conversations based on retention policy
        /// </summary>
        /// <param name="maxAge">Maximum age in days for non-archived conversations</param>
        /// <param name="maxCount">Maximum number of conversations to keep</param>
        /// <returns>Number of conversations cleaned up</returns>
        Task<int> CleanupOldConversationsAsync(int maxAge = 90, int maxCount = 1000);

        /// <summary>
        /// Exports conversations to a backup file
        /// </summary>
        /// <param name="filePath">The path to export to</param>
        /// <param name="includeArchived">Whether to include archived conversations</param>
        /// <returns>Number of conversations exported</returns>
        Task<int> ExportConversationsAsync(string filePath, bool includeArchived = true);

        /// <summary>
        /// Imports conversations from a backup file
        /// </summary>
        /// <param name="filePath">The path to import from</param>
        /// <returns>Number of conversations imported</returns>
        Task<int> ImportConversationsAsync(string filePath);
    }
}