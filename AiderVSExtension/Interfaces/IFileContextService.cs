using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing file and solution context
    /// </summary>
    public interface IFileContextService
    {
        /// <summary>
        /// Gets all files in the current solution
        /// </summary>
        /// <returns>List of file information</returns>
        Task<IEnumerable<FileInfo>> GetSolutionFilesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for files matching the specified pattern
        /// </summary>
        /// <param name="searchPattern">The search pattern</param>
        /// <returns>List of matching files</returns>
        Task<IEnumerable<FileInfo>> SearchFilesAsync(string searchPattern);

        /// <summary>
        /// Gets the content of a specific file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>The file content</returns>
        Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content of a specific file range
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="startLine">The start line number</param>
        /// <param name="endLine">The end line number</param>
        /// <returns>The file content for the specified range</returns>
        Task<string> GetFileContentRangeAsync(string filePath, int startLine, int endLine);

        /// <summary>
        /// Gets Git branches for the current repository
        /// </summary>
        /// <returns>List of Git branch information</returns>
        Task<IEnumerable<GitBranchInfo>> GetGitBranchesAsync();

        /// <summary>
        /// Gets Git status for the current repository
        /// </summary>
        /// <returns>Git status information</returns>
        Task<GitStatusInfo> GetGitStatusAsync();

        /// <summary>
        /// Gets Git context for chat references
        /// </summary>
        /// <returns>Git context information</returns>
        Task<GitContext> GetGitContextAsync();

        /// <summary>
        /// Gets Git diff for a specific file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="cached">Whether to get staged changes</param>
        /// <returns>Git diff information</returns>
        Task<GitDiff> GetGitDiffAsync(string filePath, bool cached = false);

        /// <summary>
        /// Gets Git blame information for a file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>Git blame information</returns>
        Task<GitBlame> GetGitBlameAsync(string filePath);

        /// <summary>
        /// Gets recent Git commits
        /// </summary>
        /// <param name="limit">Maximum number of commits to retrieve</param>
        /// <returns>List of recent commits</returns>
        Task<IEnumerable<GitCommit>> GetRecentCommitsAsync(int limit = 10);

        /// <summary>
        /// Gets commit history for a specific file
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="limit">Maximum number of commits to retrieve</param>
        /// <returns>List of commits affecting the file</returns>
        Task<IEnumerable<GitCommit>> GetFileCommitHistoryAsync(string filePath, int limit = 10);

        /// <summary>
        /// Gets the current clipboard content
        /// </summary>
        /// <returns>The clipboard content</returns>
        string GetClipboardContent();

        /// <summary>
        /// Gets the currently selected text in the active editor
        /// </summary>
        /// <returns>Selected text information</returns>
        Task<SelectedTextInfo> GetSelectedTextAsync();

        /// <summary>
        /// Gets project information for the current solution
        /// </summary>
        /// <returns>List of project information</returns>
        Task<IEnumerable<ProjectInfo>> GetProjectsAsync();
    }

    /// <summary>
    /// Represents file information
    /// </summary>
    public class FileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ProjectName { get; set; }
        public string RelativePath { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public string FileType { get; set; }
    }

    /// <summary>
    /// Represents Git branch information
    /// </summary>
    public class GitBranchInfo
    {
        public string Name { get; set; }
        public bool IsCurrentBranch { get; set; }
        public bool IsRemote { get; set; }
        public string LastCommitHash { get; set; }
        public DateTime LastCommitDate { get; set; }
        public string LastCommitMessage { get; set; }
    }

    /// <summary>
    /// Represents Git status information
    /// </summary>
    public class GitStatusInfo
    {
        public string CurrentBranch { get; set; }
        public List<string> ModifiedFiles { get; set; }
        public List<string> AddedFiles { get; set; }
        public List<string> DeletedFiles { get; set; }
        public List<string> UntrackedFiles { get; set; }
        public bool HasUncommittedChanges { get; set; }

        public GitStatusInfo()
        {
            ModifiedFiles = new List<string>();
            AddedFiles = new List<string>();
            DeletedFiles = new List<string>();
            UntrackedFiles = new List<string>();
        }
    }

    /// <summary>
    /// Represents selected text information
    /// </summary>
    public class SelectedTextInfo
    {
        public string Text { get; set; }
        public string FilePath { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
    }

    /// <summary>
    /// Represents project information
    /// </summary>
    public class ProjectInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string ProjectType { get; set; }
        public List<string> References { get; set; }
        public List<FileInfo> Files { get; set; }

        public ProjectInfo()
        {
            References = new List<string>();
            Files = new List<FileInfo>();
        }
    }
}