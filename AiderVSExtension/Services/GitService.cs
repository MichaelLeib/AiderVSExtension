using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Git service implementation using LibGit2Sharp
    /// </summary>
    public class GitService : IGitService, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly object _lockObject = new object();
        private Repository _repository;
        private string _repositoryPath;
        private bool _disposed = false;

        public event EventHandler<GitRepositoryStatusChangedEventArgs> RepositoryStatusChanged;
        public event EventHandler<GitBranchChangedEventArgs> BranchChanged;

        public GitService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Gets all branches in the current repository
        /// </summary>
        public async Task<IEnumerable<GitBranch>> GetBranchesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return Enumerable.Empty<GitBranch>();

                    var branches = new List<GitBranch>();
                    var currentBranch = _repository.Head?.FriendlyName;

                    foreach (var branch in _repository.Branches)
                    {
                        var gitBranch = new GitBranch
                        {
                            Name = branch.FriendlyName,
                            FullName = branch.CanonicalName,
                            IsCurrentBranch = branch.FriendlyName == currentBranch,
                            IsRemote = branch.IsRemote,
                            RemoteName = branch.IsRemote ? branch.RemoteName : null,
                            TipCommitHash = branch.Tip?.Sha,
                            TipCommitMessage = branch.Tip?.MessageShort,
                            TipCommitDate = branch.Tip?.Author?.When.DateTime ?? DateTime.MinValue,
                            TipCommitAuthor = branch.Tip?.Author?.Name,
                            TrackingBranch = branch.TrackedBranch?.FriendlyName
                        };

                        // Calculate ahead/behind counts
                        if (branch.TrackedBranch != null)
                        {
                            var filter = new CommitFilter
                            {
                                IncludeReachableFrom = branch.Tip,
                                ExcludeReachableFrom = branch.TrackedBranch.Tip
                            };
                            gitBranch.AheadCount = _repository.Commits.QueryBy(filter).Count();
                            gitBranch.IsAhead = gitBranch.AheadCount > 0;

                            filter = new CommitFilter
                            {
                                IncludeReachableFrom = branch.TrackedBranch.Tip,
                                ExcludeReachableFrom = branch.Tip
                            };
                            gitBranch.BehindCount = _repository.Commits.QueryBy(filter).Count();
                            gitBranch.IsBehind = gitBranch.BehindCount > 0;
                        }

                        branches.Add(gitBranch);
                    }

                    return branches;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetBranchesAsync");
                    return Enumerable.Empty<GitBranch>();
                }
            });
        }

        /// <summary>
        /// Gets the current repository status
        /// </summary>
        public async Task<GitRepositoryStatus> GetRepositoryStatusAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return new GitRepositoryStatus();

                    var status = _repository.RetrieveStatus();
                    var repositoryStatus = new GitRepositoryStatus
                    {
                        CurrentBranch = _repository.Head?.FriendlyName,
                        HasUncommittedChanges = status.IsDirty,
                        IsDetachedHead = _repository.Head?.IsCurrentRepositoryHead == false
                    };

                    foreach (var item in status)
                    {
                        switch (item.State)
                        {
                            case FileStatus.ModifiedInWorkdir:
                                repositoryStatus.ModifiedFiles.Add(item.FilePath);
                                break;
                            case FileStatus.ModifiedInIndex:
                                repositoryStatus.IndexChanges.Add(item.FilePath);
                                break;
                            case FileStatus.NewInIndex:
                                repositoryStatus.StagedFiles.Add(item.FilePath);
                                break;
                            case FileStatus.NewInWorkdir:
                            case FileStatus.Untracked:
                                repositoryStatus.UntrackedFiles.Add(item.FilePath);
                                break;
                            case FileStatus.DeletedFromWorkdir:
                            case FileStatus.DeletedFromIndex:
                                repositoryStatus.DeletedFiles.Add(item.FilePath);
                                break;
                            case FileStatus.Conflicted:
                                repositoryStatus.ConflictedFiles.Add(item.FilePath);
                                break;
                        }
                    }

                    // Calculate ahead/behind counts for current branch
                    var currentBranch = _repository.Head;
                    if (currentBranch?.TrackedBranch != null)
                    {
                        var filter = new CommitFilter
                        {
                            IncludeReachableFrom = currentBranch.Tip,
                            ExcludeReachableFrom = currentBranch.TrackedBranch.Tip
                        };
                        repositoryStatus.AheadCount = _repository.Commits.QueryBy(filter).Count();
                        repositoryStatus.IsAhead = repositoryStatus.AheadCount > 0;

                        filter = new CommitFilter
                        {
                            IncludeReachableFrom = currentBranch.TrackedBranch.Tip,
                            ExcludeReachableFrom = currentBranch.Tip
                        };
                        repositoryStatus.BehindCount = _repository.Commits.QueryBy(filter).Count();
                        repositoryStatus.IsBehind = repositoryStatus.BehindCount > 0;
                        repositoryStatus.TrackingBranch = currentBranch.TrackedBranch.FriendlyName;
                    }

                    return repositoryStatus;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetRepositoryStatusAsync");
                    return new GitRepositoryStatus();
                }
            });
        }

        /// <summary>
        /// Gets recent commits for the current branch
        /// </summary>
        public async Task<IEnumerable<GitCommit>> GetRecentCommitsAsync(int limit = 10)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return Enumerable.Empty<GitCommit>();

                    var commits = new List<GitCommit>();
                    var filter = new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Time,
                        FirstParentOnly = false
                    };

                    foreach (var commit in _repository.Commits.QueryBy(filter).Take(limit))
                    {
                        var gitCommit = MapCommit(commit);
                        commits.Add(gitCommit);
                    }

                    return commits;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetRecentCommitsAsync");
                    return Enumerable.Empty<GitCommit>();
                }
            });
        }

        /// <summary>
        /// Gets commit history for a specific file
        /// </summary>
        public async Task<IEnumerable<GitCommit>> GetFileHistoryAsync(string filePath, int limit = 10)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null || string.IsNullOrEmpty(filePath))
                        return Enumerable.Empty<GitCommit>();

                    var commits = new List<GitCommit>();
                    var filter = new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Time,
                        FirstParentOnly = false
                    };

                    foreach (var commit in _repository.Commits.QueryBy(filter).Take(limit * 10)) // Get more to filter
                    {
                        // Check if this commit affects the file
                        var tree = commit.Tree;
                        var parentTree = commit.Parents.FirstOrDefault()?.Tree;
                        
                        if (parentTree != null)
                        {
                            var patch = _repository.Diff.Compare<Patch>(parentTree, tree);
                            if (patch.Any(p => p.Path == filePath || p.OldPath == filePath))
                            {
                                var gitCommit = MapCommit(commit);
                                commits.Add(gitCommit);
                                
                                if (commits.Count >= limit)
                                    break;
                            }
                        }
                    }

                    return commits;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetFileHistoryAsync");
                    return Enumerable.Empty<GitCommit>();
                }
            });
        }

        /// <summary>
        /// Gets the diff for a specific file
        /// </summary>
        public async Task<GitDiff> GetFileDiffAsync(string filePath, bool cached = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null || string.IsNullOrEmpty(filePath))
                        return new GitDiff();

                    var diff = new GitDiff();
                    Patch patch;

                    if (cached)
                    {
                        // Compare index to HEAD
                        patch = _repository.Diff.Compare<Patch>(_repository.Head.Tip.Tree, DiffTargets.Index);
                    }
                    else
                    {
                        // Compare working directory to index
                        patch = _repository.Diff.Compare<Patch>(DiffTargets.Index, DiffTargets.WorkingDirectory);
                    }

                    var fileEntry = patch.FirstOrDefault(p => p.Path == filePath);
                    if (fileEntry != null)
                    {
                        var fileDiff = MapFileDiff(fileEntry);
                        diff.FileChanges.Add(fileDiff);
                        diff.TotalLinesAdded = fileDiff.LinesAdded;
                        diff.TotalLinesDeleted = fileDiff.LinesDeleted;
                        diff.TotalFilesChanged = 1;
                    }

                    return diff;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetFileDiffAsync");
                    return new GitDiff();
                }
            });
        }

        /// <summary>
        /// Gets the diff between two commits
        /// </summary>
        public async Task<GitDiff> GetCommitDiffAsync(string fromCommit, string toCommit)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null || string.IsNullOrEmpty(fromCommit) || string.IsNullOrEmpty(toCommit))
                        return new GitDiff();

                    var fromCommitObj = _repository.Lookup<Commit>(fromCommit);
                    var toCommitObj = _repository.Lookup<Commit>(toCommit);

                    if (fromCommitObj == null || toCommitObj == null)
                        return new GitDiff();

                    var patch = _repository.Diff.Compare<Patch>(fromCommitObj.Tree, toCommitObj.Tree);
                    var diff = new GitDiff
                    {
                        FromCommit = fromCommit,
                        ToCommit = toCommit
                    };

                    foreach (var fileEntry in patch)
                    {
                        var fileDiff = MapFileDiff(fileEntry);
                        diff.FileChanges.Add(fileDiff);
                        diff.TotalLinesAdded += fileDiff.LinesAdded;
                        diff.TotalLinesDeleted += fileDiff.LinesDeleted;
                    }

                    diff.TotalFilesChanged = diff.FileChanges.Count;
                    return diff;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetCommitDiffAsync");
                    return new GitDiff();
                }
            });
        }

        /// <summary>
        /// Gets the current HEAD commit
        /// </summary>
        public async Task<GitCommit> GetCurrentCommitAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository?.Head?.Tip == null)
                        return null;

                    return MapCommit(_repository.Head.Tip);
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetCurrentCommitAsync");
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets remote repository information
        /// </summary>
        public async Task<IEnumerable<GitRemote>> GetRemotesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return Enumerable.Empty<GitRemote>();

                    var remotes = new List<GitRemote>();
                    foreach (var remote in _repository.Network.Remotes)
                    {
                        remotes.Add(new GitRemote
                        {
                            Name = remote.Name,
                            Url = remote.Url,
                            PushUrl = remote.PushUrl,
                            IsDefault = remote.Name == "origin"
                        });
                    }

                    return remotes;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetRemotesAsync");
                    return Enumerable.Empty<GitRemote>();
                }
            });
        }

        /// <summary>
        /// Checks if the current directory is a Git repository
        /// </summary>
        public async Task<bool> IsGitRepositoryAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    return _repository != null;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets the repository root path
        /// </summary>
        public async Task<string> GetRepositoryRootAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    return _repository?.Info?.WorkingDirectory;
                }
                catch
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets Git configuration information
        /// </summary>
        public async Task<GitConfiguration> GetConfigurationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return new GitConfiguration();

                    var config = _repository.Config;
                    return new GitConfiguration
                    {
                        UserName = config.Get<string>("user.name")?.Value,
                        UserEmail = config.Get<string>("user.email")?.Value,
                        DefaultBranch = config.Get<string>("init.defaultBranch")?.Value ?? "main"
                    };
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetConfigurationAsync");
                    return new GitConfiguration();
                }
            });
        }

        /// <summary>
        /// Gets Git context for chat references
        /// </summary>
        public async Task<GitContext> GetGitContextAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return new GitContext();

                    var context = new GitContext
                    {
                        RepositoryRoot = _repository.Info.WorkingDirectory,
                        Status = await GetRepositoryStatusAsync(),
                        RecentCommits = (await GetRecentCommitsAsync(5)).ToList(),
                        Branches = (await GetBranchesAsync()).ToList(),
                        Remotes = (await GetRemotesAsync()).ToList()
                    };

                    context.CurrentBranch = context.Branches.FirstOrDefault(b => b.IsCurrentBranch);
                    context.HasUncommittedChanges = context.Status.HasUncommittedChanges;
                    context.ModifiedFiles = context.Status.ModifiedFiles;

                    return context;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetGitContextAsync");
                    return new GitContext();
                }
            });
        }

        /// <summary>
        /// Gets stash information
        /// </summary>
        public async Task<IEnumerable<GitStash>> GetStashesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return Enumerable.Empty<GitStash>();

                    var stashes = new List<GitStash>();
                    var stashCollection = _repository.Stashes;

                    for (int i = 0; i < stashCollection.Count(); i++)
                    {
                        var stash = stashCollection.ElementAt(i);
                        stashes.Add(new GitStash
                        {
                            Index = i,
                            Message = stash.Message,
                            Author = stash.Target.Author.Name,
                            Date = stash.Target.Author.When.DateTime,
                            CommitHash = stash.Target.Sha
                        });
                    }

                    return stashes;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetStashesAsync");
                    return Enumerable.Empty<GitStash>();
                }
            });
        }

        /// <summary>
        /// Gets blame information for a file
        /// </summary>
        public async Task<GitBlame> GetBlameAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null || string.IsNullOrEmpty(filePath))
                        return new GitBlame();

                    var blame = _repository.Blame(filePath);
                    var gitBlame = new GitBlame
                    {
                        FilePath = filePath
                    };

                    foreach (var hunk in blame)
                    {
                        var blameHunk = new GitBlameHunk
                        {
                            CommitHash = hunk.FinalCommit.Sha,
                            AuthorName = hunk.FinalCommit.Author.Name,
                            AuthorEmail = hunk.FinalCommit.Author.Email,
                            CommitDate = hunk.FinalCommit.Author.When.DateTime,
                            StartLine = hunk.FinalStartLineNumber,
                            EndLine = hunk.FinalStartLineNumber + hunk.LineCount - 1
                        };

                        gitBlame.Hunks.Add(blameHunk);
                    }

                    return gitBlame;
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetBlameAsync");
                    return new GitBlame();
                }
            });
        }

        /// <summary>
        /// Gets tag information
        /// </summary>
        public async Task<IEnumerable<GitTag>> GetTagsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    EnsureRepository();
                    if (_repository == null)
                        return Enumerable.Empty<GitTag>();

                    var tags = new List<GitTag>();
                    foreach (var tag in _repository.Tags)
                    {
                        var gitTag = new GitTag
                        {
                            Name = tag.FriendlyName,
                            TargetCommitHash = tag.Target.Sha,
                            IsLightweight = !tag.IsAnnotated
                        };

                        if (tag.IsAnnotated)
                        {
                            gitTag.Message = tag.Annotation.Message;
                            gitTag.TaggerName = tag.Annotation.Tagger.Name;
                            gitTag.TaggerEmail = tag.Annotation.Tagger.Email;
                            gitTag.Date = tag.Annotation.Tagger.When.DateTime;
                        }
                        else if (tag.Target is Commit commit)
                        {
                            gitTag.Date = commit.Author.When.DateTime;
                        }

                        tags.Add(gitTag);
                    }

                    return tags.OrderByDescending(t => t.Date);
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.GetTagsAsync");
                    return Enumerable.Empty<GitTag>();
                }
            });
        }

        /// <summary>
        /// Ensures the repository is initialized
        /// </summary>
        private void EnsureRepository()
        {
            lock (_lockObject)
            {
                if (_repository != null)
                    return;

                try
                {
                    // Try to find repository from current working directory
                    var currentPath = Environment.CurrentDirectory;
                    _repositoryPath = Repository.Discover(currentPath);
                    
                    if (!string.IsNullOrEmpty(_repositoryPath))
                    {
                        _repository = new Repository(_repositoryPath);
                    }
                }
                catch (Exception ex)
                {
                    _errorHandler?.HandleExceptionAsync(ex, "GitService.EnsureRepository");
                }
            }
        }

        /// <summary>
        /// Maps a LibGit2Sharp commit to our GitCommit model
        /// </summary>
        private GitCommit MapCommit(Commit commit)
        {
            var gitCommit = new GitCommit
            {
                Hash = commit.Sha,
                ShortHash = commit.Sha.Substring(0, 7),
                Message = commit.Message,
                ShortMessage = commit.MessageShort,
                AuthorName = commit.Author.Name,
                AuthorEmail = commit.Author.Email,
                AuthorDate = commit.Author.When.DateTime,
                CommitDate = commit.Committer.When.DateTime,
                CommitterName = commit.Committer.Name,
                CommitterEmail = commit.Committer.Email,
                IsMerge = commit.Parents.Count() > 1
            };

            gitCommit.ParentHashes.AddRange(commit.Parents.Select(p => p.Sha));

            // Get file changes
            var parent = commit.Parents.FirstOrDefault();
            if (parent != null)
            {
                var patch = _repository.Diff.Compare<Patch>(parent.Tree, commit.Tree);
                foreach (var fileEntry in patch)
                {
                    gitCommit.Changes.Add(new GitFileChange
                    {
                        FilePath = fileEntry.Path,
                        OldFilePath = fileEntry.OldPath,
                        ChangeType = MapChangeType(fileEntry.Status),
                        LinesAdded = fileEntry.LinesAdded,
                        LinesDeleted = fileEntry.LinesDeleted
                    });
                }
            }

            return gitCommit;
        }

        /// <summary>
        /// Maps a LibGit2Sharp patch entry to our GitFileDiff model
        /// </summary>
        private GitFileDiff MapFileDiff(PatchEntryChanges fileEntry)
        {
            var fileDiff = new GitFileDiff
            {
                FilePath = fileEntry.Path,
                OldFilePath = fileEntry.OldPath,
                ChangeType = MapChangeType(fileEntry.Status),
                LinesAdded = fileEntry.LinesAdded,
                LinesDeleted = fileEntry.LinesDeleted,
                DiffContent = fileEntry.Patch
            };

            // Parse hunks
            var lines = fileEntry.Patch.Split('\n');
            GitDiffHunk currentHunk = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    currentHunk = new GitDiffHunk
                    {
                        Header = line
                    };
                    fileDiff.Hunks.Add(currentHunk);
                    
                    // Parse hunk header
                    var parts = line.Split(' ');
                    if (parts.Length >= 3)
                    {
                        var oldInfo = parts[1].TrimStart('-').Split(',');
                        var newInfo = parts[2].TrimStart('+').Split(',');
                        
                        if (oldInfo.Length >= 1 && int.TryParse(oldInfo[0], out int oldStart))
                            currentHunk.OldStartLine = oldStart;
                        if (oldInfo.Length >= 2 && int.TryParse(oldInfo[1], out int oldCount))
                            currentHunk.OldLineCount = oldCount;
                        if (newInfo.Length >= 1 && int.TryParse(newInfo[0], out int newStart))
                            currentHunk.NewStartLine = newStart;
                        if (newInfo.Length >= 2 && int.TryParse(newInfo[1], out int newCount))
                            currentHunk.NewLineCount = newCount;
                    }
                }
                else if (currentHunk != null)
                {
                    var lineType = GitDiffLineType.Context;
                    if (line.StartsWith("+"))
                        lineType = GitDiffLineType.Added;
                    else if (line.StartsWith("-"))
                        lineType = GitDiffLineType.Deleted;
                    else if (line.StartsWith("\\"))
                        lineType = GitDiffLineType.NoNewline;

                    currentHunk.Lines.Add(new GitDiffLine
                    {
                        Content = line,
                        LineType = lineType
                    });
                }
            }

            return fileDiff;
        }

        /// <summary>
        /// Maps LibGit2Sharp change status to our GitChangeType enum
        /// </summary>
        private GitChangeType MapChangeType(ChangeKind changeKind)
        {
            switch (changeKind)
            {
                case ChangeKind.Added:
                    return GitChangeType.Added;
                case ChangeKind.Deleted:
                    return GitChangeType.Deleted;
                case ChangeKind.Modified:
                    return GitChangeType.Modified;
                case ChangeKind.Renamed:
                    return GitChangeType.Renamed;
                case ChangeKind.Copied:
                    return GitChangeType.Copied;
                case ChangeKind.TypeChanged:
                    return GitChangeType.TypeChanged;
                case ChangeKind.Untracked:
                    return GitChangeType.Untracked;
                case ChangeKind.Conflicted:
                    return GitChangeType.Conflicted;
                default:
                    return GitChangeType.Modified;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _repository?.Dispose();
                _disposed = true;
            }
        }
    }
}