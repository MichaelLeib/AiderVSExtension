using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Provides Git context information for chat references
    /// </summary>
    public class GitChatContextProvider : IGitChatContextProvider
    {
        private readonly IGitService _gitService;
        private readonly IErrorHandler _errorHandler;

        public GitChatContextProvider(IGitService gitService, IErrorHandler errorHandler)
        {
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Gets Git context formatted for chat references
        /// </summary>
        public async Task<string> GetGitContextForChatAsync(GitContextType contextType, string additionalFilter = null)
        {
            try
            {
                if (!await _gitService.IsGitRepositoryAsync())
                    return "No Git repository found.";

                switch (contextType)
                {
                    case GitContextType.CurrentBranch:
                        return await GetCurrentBranchContextAsync();
                    case GitContextType.RecentCommits:
                        return await GetRecentCommitsContextAsync(additionalFilter);
                    case GitContextType.ModifiedFiles:
                        return await GetModifiedFilesContextAsync();
                    case GitContextType.BranchList:
                        return await GetBranchListContextAsync();
                    case GitContextType.RepositoryStatus:
                        return await GetRepositoryStatusContextAsync();
                    case GitContextType.FileHistory:
                        return await GetFileHistoryContextAsync(additionalFilter);
                    case GitContextType.Diff:
                        return await GetDiffContextAsync(additionalFilter);
                    case GitContextType.Blame:
                        return await GetBlameContextAsync(additionalFilter);
                    case GitContextType.Stashes:
                        return await GetStashesContextAsync();
                    case GitContextType.Tags:
                        return await GetTagsContextAsync();
                    case GitContextType.Remotes:
                        return await GetRemotesContextAsync();
                    default:
                        return await GetFullGitContextAsync();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "GitChatContextProvider.GetGitContextForChatAsync");
                return $"Error retrieving Git context: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets current branch context
        /// </summary>
        private async Task<string> GetCurrentBranchContextAsync()
        {
            var currentCommit = await _gitService.GetCurrentCommitAsync();
            var branches = await _gitService.GetBranchesAsync();
            var currentBranch = branches.FirstOrDefault(b => b.IsCurrentBranch);

            var sb = new StringBuilder();
            sb.AppendLine("## Current Branch Context");
            sb.AppendLine();

            if (currentBranch != null)
            {
                sb.AppendLine($"**Branch:** {currentBranch.Name}");
                sb.AppendLine($"**Last Commit:** {currentBranch.TipCommitHash?.Substring(0, 7)} - {currentBranch.TipCommitMessage}");
                sb.AppendLine($"**Author:** {currentBranch.TipCommitAuthor}");
                sb.AppendLine($"**Date:** {currentBranch.TipCommitDate:yyyy-MM-dd HH:mm:ss}");

                if (currentBranch.IsAhead)
                    sb.AppendLine($"**Ahead:** {currentBranch.AheadCount} commits");
                if (currentBranch.IsBehind)
                    sb.AppendLine($"**Behind:** {currentBranch.BehindCount} commits");
                if (!string.IsNullOrEmpty(currentBranch.TrackingBranch))
                    sb.AppendLine($"**Tracking:** {currentBranch.TrackingBranch}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets recent commits context
        /// </summary>
        private async Task<string> GetRecentCommitsContextAsync(string limitStr = null)
        {
            var limit = 10;
            if (!string.IsNullOrEmpty(limitStr) && int.TryParse(limitStr, out var parsedLimit))
                limit = parsedLimit;

            var commits = await _gitService.GetRecentCommitsAsync(limit);
            var sb = new StringBuilder();
            sb.AppendLine("## Recent Commits");
            sb.AppendLine();

            foreach (var commit in commits)
            {
                sb.AppendLine($"**{commit.ShortHash}** - {commit.ShortMessage}");
                sb.AppendLine($"  *{commit.AuthorName}* - {commit.AuthorDate:yyyy-MM-dd HH:mm:ss}");
                
                if (commit.Changes.Any())
                {
                    sb.AppendLine($"  Files changed: {commit.Changes.Count}");
                    foreach (var change in commit.Changes.Take(5))
                    {
                        sb.AppendLine($"    {GetChangeSymbol(change.ChangeType)} {change.FilePath}");
                    }
                    if (commit.Changes.Count > 5)
                        sb.AppendLine($"    ... and {commit.Changes.Count - 5} more files");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets modified files context
        /// </summary>
        private async Task<string> GetModifiedFilesContextAsync()
        {
            var status = await _gitService.GetRepositoryStatusAsync();
            var sb = new StringBuilder();
            sb.AppendLine("## Modified Files");
            sb.AppendLine();

            if (!status.HasUncommittedChanges)
            {
                sb.AppendLine("No modified files.");
                return sb.ToString();
            }

            if (status.ModifiedFiles.Any())
            {
                sb.AppendLine("**Modified:**");
                foreach (var file in status.ModifiedFiles)
                    sb.AppendLine($"  📝 {file}");
                sb.AppendLine();
            }

            if (status.StagedFiles.Any())
            {
                sb.AppendLine("**Staged:**");
                foreach (var file in status.StagedFiles)
                    sb.AppendLine($"  ✅ {file}");
                sb.AppendLine();
            }

            if (status.UntrackedFiles.Any())
            {
                sb.AppendLine("**Untracked:**");
                foreach (var file in status.UntrackedFiles.Take(10))
                    sb.AppendLine($"  ❓ {file}");
                if (status.UntrackedFiles.Count > 10)
                    sb.AppendLine($"  ... and {status.UntrackedFiles.Count - 10} more files");
                sb.AppendLine();
            }

            if (status.DeletedFiles.Any())
            {
                sb.AppendLine("**Deleted:**");
                foreach (var file in status.DeletedFiles)
                    sb.AppendLine($"  🗑️ {file}");
                sb.AppendLine();
            }

            if (status.ConflictedFiles.Any())
            {
                sb.AppendLine("**Conflicted:**");
                foreach (var file in status.ConflictedFiles)
                    sb.AppendLine($"  ⚠️ {file}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets branch list context
        /// </summary>
        private async Task<string> GetBranchListContextAsync()
        {
            var branches = await _gitService.GetBranchesAsync();
            var sb = new StringBuilder();
            sb.AppendLine("## Branch List");
            sb.AppendLine();

            var localBranches = branches.Where(b => !b.IsRemote).OrderBy(b => b.Name);
            var remoteBranches = branches.Where(b => b.IsRemote).OrderBy(b => b.Name);

            if (localBranches.Any())
            {
                sb.AppendLine("**Local Branches:**");
                foreach (var branch in localBranches)
                {
                    var indicator = branch.IsCurrentBranch ? "* " : "  ";
                    sb.AppendLine($"{indicator}{branch.Name}");
                    if (branch.IsCurrentBranch)
                        sb.AppendLine($"    └─ {branch.TipCommitHash?.Substring(0, 7)} - {branch.TipCommitMessage}");
                }
                sb.AppendLine();
            }

            if (remoteBranches.Any())
            {
                sb.AppendLine("**Remote Branches:**");
                foreach (var branch in remoteBranches.Take(10))
                {
                    sb.AppendLine($"  {branch.Name}");
                }
                if (remoteBranches.Count() > 10)
                    sb.AppendLine($"  ... and {remoteBranches.Count() - 10} more remote branches");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets repository status context
        /// </summary>
        private async Task<string> GetRepositoryStatusContextAsync()
        {
            var status = await _gitService.GetRepositoryStatusAsync();
            var config = await _gitService.GetConfigurationAsync();
            var remotes = await _gitService.GetRemotesAsync();

            var sb = new StringBuilder();
            sb.AppendLine("## Repository Status");
            sb.AppendLine();

            sb.AppendLine($"**Current Branch:** {status.CurrentBranch}");
            sb.AppendLine($"**Clean:** {(status.HasUncommittedChanges ? "No" : "Yes")}");

            if (status.IsDetachedHead)
                sb.AppendLine("**Status:** Detached HEAD");

            if (status.IsAhead)
                sb.AppendLine($"**Ahead:** {status.AheadCount} commits");
            if (status.IsBehind)
                sb.AppendLine($"**Behind:** {status.BehindCount} commits");

            sb.AppendLine();
            sb.AppendLine($"**User:** {config.UserName} <{config.UserEmail}>");
            
            if (remotes.Any())
            {
                sb.AppendLine($"**Remotes:** {string.Join(", ", remotes.Select(r => r.Name))}");
            }

            var repositoryRoot = await _gitService.GetRepositoryRootAsync();
            if (!string.IsNullOrEmpty(repositoryRoot))
                sb.AppendLine($"**Repository Root:** {repositoryRoot}");

            return sb.ToString();
        }

        /// <summary>
        /// Gets file history context
        /// </summary>
        private async Task<string> GetFileHistoryContextAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "No file path provided for history.";

            var commits = await _gitService.GetFileHistoryAsync(filePath, 10);
            var sb = new StringBuilder();
            sb.AppendLine($"## File History: {filePath}");
            sb.AppendLine();

            if (!commits.Any())
            {
                sb.AppendLine("No commit history found for this file.");
                return sb.ToString();
            }

            foreach (var commit in commits)
            {
                sb.AppendLine($"**{commit.ShortHash}** - {commit.ShortMessage}");
                sb.AppendLine($"  *{commit.AuthorName}* - {commit.AuthorDate:yyyy-MM-dd HH:mm:ss}");
                
                var fileChange = commit.Changes.FirstOrDefault(c => c.FilePath == filePath);
                if (fileChange != null)
                {
                    sb.AppendLine($"  {GetChangeSymbol(fileChange.ChangeType)} +{fileChange.LinesAdded}/-{fileChange.LinesDeleted}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets diff context
        /// </summary>
        private async Task<string> GetDiffContextAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "No file path provided for diff.";

            var diff = await _gitService.GetFileDiffAsync(filePath);
            var sb = new StringBuilder();
            sb.AppendLine($"## Diff: {filePath}");
            sb.AppendLine();

            if (!diff.FileChanges.Any())
            {
                sb.AppendLine("No changes found for this file.");
                return sb.ToString();
            }

            var fileDiff = diff.FileChanges.First();
            sb.AppendLine($"**Changes:** +{fileDiff.LinesAdded}/-{fileDiff.LinesDeleted}");
            sb.AppendLine();

            // Show first few hunks
            foreach (var hunk in fileDiff.Hunks.Take(3))
            {
                sb.AppendLine($"```diff");
                sb.AppendLine(hunk.Header);
                foreach (var line in hunk.Lines.Take(10))
                {
                    sb.AppendLine(line.Content);
                }
                if (hunk.Lines.Count > 10)
                    sb.AppendLine($"... {hunk.Lines.Count - 10} more lines");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (fileDiff.Hunks.Count > 3)
                sb.AppendLine($"... and {fileDiff.Hunks.Count - 3} more hunks");

            return sb.ToString();
        }

        /// <summary>
        /// Gets blame context
        /// </summary>
        private async Task<string> GetBlameContextAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "No file path provided for blame.";

            var blame = await _gitService.GetBlameAsync(filePath);
            var sb = new StringBuilder();
            sb.AppendLine($"## Blame: {filePath}");
            sb.AppendLine();

            if (!blame.Hunks.Any())
            {
                sb.AppendLine("No blame information found for this file.");
                return sb.ToString();
            }

            var recentHunks = blame.Hunks
                .OrderByDescending(h => h.CommitDate)
                .Take(10);

            foreach (var hunk in recentHunks)
            {
                sb.AppendLine($"**{hunk.CommitHash.Substring(0, 7)}** - {hunk.AuthorName}");
                sb.AppendLine($"  Lines {hunk.StartLine}-{hunk.EndLine} - {hunk.CommitDate:yyyy-MM-dd}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets stashes context
        /// </summary>
        private async Task<string> GetStashesContextAsync()
        {
            var stashes = await _gitService.GetStashesAsync();
            var sb = new StringBuilder();
            sb.AppendLine("## Stashes");
            sb.AppendLine();

            if (!stashes.Any())
            {
                sb.AppendLine("No stashes found.");
                return sb.ToString();
            }

            foreach (var stash in stashes)
            {
                sb.AppendLine($"**stash@{{{stash.Index}}}** - {stash.Message}");
                sb.AppendLine($"  *{stash.Author}* - {stash.Date:yyyy-MM-dd HH:mm:ss}");
                if (!string.IsNullOrEmpty(stash.BranchName))
                    sb.AppendLine($"  Branch: {stash.BranchName}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets tags context
        /// </summary>
        private async Task<string> GetTagsContextAsync()
        {
            var tags = await _gitService.GetTagsAsync();
            var sb = new StringBuilder();
            sb.AppendLine("## Tags");
            sb.AppendLine();

            if (!tags.Any())
            {
                sb.AppendLine("No tags found.");
                return sb.ToString();
            }

            foreach (var tag in tags.Take(10))
            {
                sb.AppendLine($"**{tag.Name}** - {tag.TargetCommitHash?.Substring(0, 7)}");
                sb.AppendLine($"  *{tag.TaggerName}* - {tag.Date:yyyy-MM-dd HH:mm:ss}");
                if (!string.IsNullOrEmpty(tag.Message))
                    sb.AppendLine($"  {tag.Message}");
                sb.AppendLine();
            }

            if (tags.Count() > 10)
                sb.AppendLine($"... and {tags.Count() - 10} more tags");

            return sb.ToString();
        }

        /// <summary>
        /// Gets remotes context
        /// </summary>
        private async Task<string> GetRemotesContextAsync()
        {
            var remotes = await _gitService.GetRemotesAsync();
            var sb = new StringBuilder();
            sb.AppendLine("## Remotes");
            sb.AppendLine();

            if (!remotes.Any())
            {
                sb.AppendLine("No remotes configured.");
                return sb.ToString();
            }

            foreach (var remote in remotes)
            {
                sb.AppendLine($"**{remote.Name}** - {remote.Url}");
                if (!string.IsNullOrEmpty(remote.PushUrl) && remote.PushUrl != remote.Url)
                    sb.AppendLine($"  Push URL: {remote.PushUrl}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets full Git context
        /// </summary>
        private async Task<string> GetFullGitContextAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Git Repository Context");
            sb.AppendLine();

            sb.AppendLine(await GetRepositoryStatusContextAsync());
            sb.AppendLine(await GetCurrentBranchContextAsync());
            sb.AppendLine(await GetModifiedFilesContextAsync());
            sb.AppendLine(await GetRecentCommitsContextAsync("5"));

            return sb.ToString();
        }

        /// <summary>
        /// Gets a symbol for a change type
        /// </summary>
        private string GetChangeSymbol(GitChangeType changeType)
        {
            switch (changeType)
            {
                case GitChangeType.Added:
                    return "➕";
                case GitChangeType.Modified:
                    return "📝";
                case GitChangeType.Deleted:
                    return "🗑️";
                case GitChangeType.Renamed:
                    return "📄";
                case GitChangeType.Copied:
                    return "📋";
                case GitChangeType.TypeChanged:
                    return "🔄";
                case GitChangeType.Untracked:
                    return "❓";
                case GitChangeType.Conflicted:
                    return "⚠️";
                default:
                    return "📄";
            }
        }
    }
}