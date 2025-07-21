using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for Git operations and repository management
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Gets all branches in the current repository
        /// </summary>
        /// <returns>List of branch information</returns>
        Task<IEnumerable<GitBranch>> GetBranchesAsync();

        /// <summary>
        /// Gets the current repository status
        /// </summary>
        /// <returns>Repository status information</returns>
        Task<GitRepositoryStatus> GetRepositoryStatusAsync();

        /// <summary>
        /// Gets recent commits for the current branch
        /// </summary>
        /// <param name="limit">Maximum number of commits to retrieve</param>
        /// <returns>List of commit information</returns>
        Task<IEnumerable<GitCommit>> GetRecentCommitsAsync(int limit = 10);

        /// <summary>
        /// Gets commit history for a specific file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="limit">Maximum number of commits to retrieve</param>
        /// <returns>List of commit information for the file</returns>
        Task<IEnumerable<GitCommit>> GetFileHistoryAsync(string filePath, int limit = 10);

        /// <summary>
        /// Gets the diff for a specific file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cached">Whether to get staged changes</param>
        /// <returns>Diff information</returns>
        Task<GitDiff> GetFileDiffAsync(string filePath, bool cached = false);

        /// <summary>
        /// Gets the diff between two commits
        /// </summary>
        /// <param name="fromCommit">Source commit hash</param>
        /// <param name="toCommit">Target commit hash</param>
        /// <returns>Diff information</returns>
        Task<GitDiff> GetCommitDiffAsync(string fromCommit, string toCommit);

        /// <summary>
        /// Gets the current HEAD commit
        /// </summary>
        /// <returns>Current commit information</returns>
        Task<GitCommit> GetCurrentCommitAsync();

        /// <summary>
        /// Gets remote repository information
        /// </summary>
        /// <returns>List of remote repository information</returns>
        Task<IEnumerable<GitRemote>> GetRemotesAsync();

        /// <summary>
        /// Checks if the current directory is a Git repository
        /// </summary>
        /// <returns>True if in a Git repository</returns>
        Task<bool> IsGitRepositoryAsync();

        /// <summary>
        /// Gets the repository root path
        /// </summary>
        /// <returns>Repository root path or null if not in a Git repository</returns>
        Task<string> GetRepositoryRootAsync();

        /// <summary>
        /// Gets Git configuration information
        /// </summary>
        /// <returns>Git configuration</returns>
        Task<GitConfiguration> GetConfigurationAsync();

        /// <summary>
        /// Gets Git context for chat references
        /// </summary>
        /// <returns>Git context information</returns>
        Task<GitContext> GetGitContextAsync();

        /// <summary>
        /// Gets stash information
        /// </summary>
        /// <returns>List of stash entries</returns>
        Task<IEnumerable<GitStash>> GetStashesAsync();

        /// <summary>
        /// Gets blame information for a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Blame information</returns>
        Task<GitBlame> GetBlameAsync(string filePath);

        /// <summary>
        /// Gets tag information
        /// </summary>
        /// <returns>List of tag information</returns>
        Task<IEnumerable<GitTag>> GetTagsAsync();

        /// <summary>
        /// Event fired when repository status changes
        /// </summary>
        event EventHandler<GitRepositoryStatusChangedEventArgs> RepositoryStatusChanged;

        /// <summary>
        /// Event fired when branch changes
        /// </summary>
        event EventHandler<GitBranchChangedEventArgs> BranchChanged;
    }
}