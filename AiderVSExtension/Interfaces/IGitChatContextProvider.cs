using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for Git chat context provider
    /// </summary>
    public interface IGitChatContextProvider
    {
        /// <summary>
        /// Gets Git context formatted for chat references
        /// </summary>
        Task<string> GetGitContextForChatAsync(GitContextType contextType, string additionalFilter = null);
    }

    /// <summary>
    /// Types of Git context for chat references
    /// </summary>
    public enum GitContextType
    {
        /// <summary>
        /// Current branch information
        /// </summary>
        CurrentBranch,

        /// <summary>
        /// Recent commits
        /// </summary>
        RecentCommits,

        /// <summary>
        /// Modified files
        /// </summary>
        ModifiedFiles,

        /// <summary>
        /// List of branches
        /// </summary>
        BranchList,

        /// <summary>
        /// Repository status
        /// </summary>
        RepositoryStatus,

        /// <summary>
        /// File history
        /// </summary>
        FileHistory,

        /// <summary>
        /// Diff information
        /// </summary>
        Diff,

        /// <summary>
        /// Blame information
        /// </summary>
        Blame,

        /// <summary>
        /// Stashes
        /// </summary>
        Stashes,

        /// <summary>
        /// Tags
        /// </summary>
        Tags,

        /// <summary>
        /// Remotes
        /// </summary>
        Remotes,

        /// <summary>
        /// Full Git context
        /// </summary>
        Full
    }
}