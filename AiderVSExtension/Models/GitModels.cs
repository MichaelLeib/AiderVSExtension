using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a Git branch
    /// </summary>
    public class GitBranch
    {
        /// <summary>
        /// The branch name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full branch name including remote prefix
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Whether this is the current branch
        /// </summary>
        public bool IsCurrentBranch { get; set; }

        /// <summary>
        /// Whether this is a remote branch
        /// </summary>
        public bool IsRemote { get; set; }

        /// <summary>
        /// The remote name (for remote branches)
        /// </summary>
        public string RemoteName { get; set; }

        /// <summary>
        /// The tip commit hash
        /// </summary>
        public string TipCommitHash { get; set; }

        /// <summary>
        /// The tip commit message
        /// </summary>
        public string TipCommitMessage { get; set; }

        /// <summary>
        /// The tip commit date
        /// </summary>
        public DateTime TipCommitDate { get; set; }

        /// <summary>
        /// The tip commit author
        /// </summary>
        public string TipCommitAuthor { get; set; }

        /// <summary>
        /// Whether the branch is ahead of its tracking branch
        /// </summary>
        public bool IsAhead { get; set; }

        /// <summary>
        /// Whether the branch is behind its tracking branch
        /// </summary>
        public bool IsBehind { get; set; }

        /// <summary>
        /// Number of commits ahead
        /// </summary>
        public int AheadCount { get; set; }

        /// <summary>
        /// Number of commits behind
        /// </summary>
        public int BehindCount { get; set; }

        /// <summary>
        /// The tracking branch name
        /// </summary>
        public string TrackingBranch { get; set; }
    }

    /// <summary>
    /// Represents a Git commit
    /// </summary>
    public class GitCommit
    {
        /// <summary>
        /// The commit hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// The short commit hash
        /// </summary>
        public string ShortHash { get; set; }

        /// <summary>
        /// The commit message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The short commit message
        /// </summary>
        public string ShortMessage { get; set; }

        /// <summary>
        /// The author name
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// The author email
        /// </summary>
        public string AuthorEmail { get; set; }

        /// <summary>
        /// The commit date
        /// </summary>
        public DateTime CommitDate { get; set; }

        /// <summary>
        /// The author date
        /// </summary>
        public DateTime AuthorDate { get; set; }

        /// <summary>
        /// The committer name
        /// </summary>
        public string CommitterName { get; set; }

        /// <summary>
        /// The committer email
        /// </summary>
        public string CommitterEmail { get; set; }

        /// <summary>
        /// The parent commit hashes
        /// </summary>
        public List<string> ParentHashes { get; set; } = new List<string>();

        /// <summary>
        /// Files changed in this commit
        /// </summary>
        public List<GitFileChange> Changes { get; set; } = new List<GitFileChange>();

        /// <summary>
        /// Tags associated with this commit
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Whether this is a merge commit
        /// </summary>
        public bool IsMerge { get; set; }
    }

    /// <summary>
    /// Represents a file change in a commit
    /// </summary>
    public class GitFileChange
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The old file path (for renames)
        /// </summary>
        public string OldFilePath { get; set; }

        /// <summary>
        /// The type of change
        /// </summary>
        public GitChangeType ChangeType { get; set; }

        /// <summary>
        /// Lines added
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        /// Lines deleted
        /// </summary>
        public int LinesDeleted { get; set; }
    }

    /// <summary>
    /// Represents the Git repository status
    /// </summary>
    public class GitRepositoryStatus
    {
        /// <summary>
        /// The current branch name
        /// </summary>
        public string CurrentBranch { get; set; }

        /// <summary>
        /// Whether the repository has uncommitted changes
        /// </summary>
        public bool HasUncommittedChanges { get; set; }

        /// <summary>
        /// Whether the repository is in a detached HEAD state
        /// </summary>
        public bool IsDetachedHead { get; set; }

        /// <summary>
        /// Modified files in the working directory
        /// </summary>
        public List<string> ModifiedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Staged files
        /// </summary>
        public List<string> StagedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Untracked files
        /// </summary>
        public List<string> UntrackedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Deleted files
        /// </summary>
        public List<string> DeletedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Conflicted files
        /// </summary>
        public List<string> ConflictedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Files with changes in the index
        /// </summary>
        public List<string> IndexChanges { get; set; } = new List<string>();

        /// <summary>
        /// Whether the repository is ahead of its tracking branch
        /// </summary>
        public bool IsAhead { get; set; }

        /// <summary>
        /// Whether the repository is behind its tracking branch
        /// </summary>
        public bool IsBehind { get; set; }

        /// <summary>
        /// Number of commits ahead
        /// </summary>
        public int AheadCount { get; set; }

        /// <summary>
        /// Number of commits behind
        /// </summary>
        public int BehindCount { get; set; }

        /// <summary>
        /// The tracking branch name
        /// </summary>
        public string TrackingBranch { get; set; }
    }

    /// <summary>
    /// Represents a Git diff
    /// </summary>
    public class GitDiff
    {
        /// <summary>
        /// The source commit hash
        /// </summary>
        public string FromCommit { get; set; }

        /// <summary>
        /// The target commit hash
        /// </summary>
        public string ToCommit { get; set; }

        /// <summary>
        /// File changes in the diff
        /// </summary>
        public List<GitFileDiff> FileChanges { get; set; } = new List<GitFileDiff>();

        /// <summary>
        /// Total lines added
        /// </summary>
        public int TotalLinesAdded { get; set; }

        /// <summary>
        /// Total lines deleted
        /// </summary>
        public int TotalLinesDeleted { get; set; }

        /// <summary>
        /// Total files changed
        /// </summary>
        public int TotalFilesChanged { get; set; }
    }

    /// <summary>
    /// Represents a diff for a specific file
    /// </summary>
    public class GitFileDiff
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The old file path (for renames)
        /// </summary>
        public string OldFilePath { get; set; }

        /// <summary>
        /// The type of change
        /// </summary>
        public GitChangeType ChangeType { get; set; }

        /// <summary>
        /// Lines added
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        /// Lines deleted
        /// </summary>
        public int LinesDeleted { get; set; }

        /// <summary>
        /// The diff content
        /// </summary>
        public string DiffContent { get; set; }

        /// <summary>
        /// Line-by-line diff hunks
        /// </summary>
        public List<GitDiffHunk> Hunks { get; set; } = new List<GitDiffHunk>();
    }

    /// <summary>
    /// Represents a diff hunk
    /// </summary>
    public class GitDiffHunk
    {
        /// <summary>
        /// The hunk header
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Starting line number in the old file
        /// </summary>
        public int OldStartLine { get; set; }

        /// <summary>
        /// Number of lines in the old file
        /// </summary>
        public int OldLineCount { get; set; }

        /// <summary>
        /// Starting line number in the new file
        /// </summary>
        public int NewStartLine { get; set; }

        /// <summary>
        /// Number of lines in the new file
        /// </summary>
        public int NewLineCount { get; set; }

        /// <summary>
        /// The hunk lines
        /// </summary>
        public List<GitDiffLine> Lines { get; set; } = new List<GitDiffLine>();
    }

    /// <summary>
    /// Represents a line in a diff hunk
    /// </summary>
    public class GitDiffLine
    {
        /// <summary>
        /// The line content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The line type
        /// </summary>
        public GitDiffLineType LineType { get; set; }

        /// <summary>
        /// The line number in the old file
        /// </summary>
        public int? OldLineNumber { get; set; }

        /// <summary>
        /// The line number in the new file
        /// </summary>
        public int? NewLineNumber { get; set; }
    }

    /// <summary>
    /// Represents a Git remote
    /// </summary>
    public class GitRemote
    {
        /// <summary>
        /// The remote name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The remote URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The push URL
        /// </summary>
        public string PushUrl { get; set; }

        /// <summary>
        /// Whether this is the default remote
        /// </summary>
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Represents Git configuration
    /// </summary>
    public class GitConfiguration
    {
        /// <summary>
        /// The user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The user email
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// The default branch name
        /// </summary>
        public string DefaultBranch { get; set; }

        /// <summary>
        /// Whether to automatically push on commit
        /// </summary>
        public bool AutoPush { get; set; }

        /// <summary>
        /// Additional configuration values
        /// </summary>
        public Dictionary<string, string> AdditionalConfig { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents Git context for chat references
    /// </summary>
    public class GitContext
    {
        /// <summary>
        /// The current branch
        /// </summary>
        public GitBranch CurrentBranch { get; set; }

        /// <summary>
        /// Repository status
        /// </summary>
        public GitRepositoryStatus Status { get; set; }

        /// <summary>
        /// Recent commits
        /// </summary>
        public List<GitCommit> RecentCommits { get; set; } = new List<GitCommit>();

        /// <summary>
        /// Available branches
        /// </summary>
        public List<GitBranch> Branches { get; set; } = new List<GitBranch>();

        /// <summary>
        /// Remote repositories
        /// </summary>
        public List<GitRemote> Remotes { get; set; } = new List<GitRemote>();

        /// <summary>
        /// Modified files in the working directory
        /// </summary>
        public List<string> ModifiedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Repository root path
        /// </summary>
        public string RepositoryRoot { get; set; }

        /// <summary>
        /// Whether the repository has uncommitted changes
        /// </summary>
        public bool HasUncommittedChanges { get; set; }
    }

    /// <summary>
    /// Represents a Git stash entry
    /// </summary>
    public class GitStash
    {
        /// <summary>
        /// The stash index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The stash message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The stash author
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The stash date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The branch name when stashed
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// The commit hash
        /// </summary>
        public string CommitHash { get; set; }
    }

    /// <summary>
    /// Represents Git blame information
    /// </summary>
    public class GitBlame
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Blame hunks
        /// </summary>
        public List<GitBlameHunk> Hunks { get; set; } = new List<GitBlameHunk>();
    }

    /// <summary>
    /// Represents a blame hunk
    /// </summary>
    public class GitBlameHunk
    {
        /// <summary>
        /// The commit hash
        /// </summary>
        public string CommitHash { get; set; }

        /// <summary>
        /// The author name
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// The author email
        /// </summary>
        public string AuthorEmail { get; set; }

        /// <summary>
        /// The commit date
        /// </summary>
        public DateTime CommitDate { get; set; }

        /// <summary>
        /// The starting line number
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// The ending line number
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// The line content
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a Git tag
    /// </summary>
    public class GitTag
    {
        /// <summary>
        /// The tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The target commit hash
        /// </summary>
        public string TargetCommitHash { get; set; }

        /// <summary>
        /// The tag date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The tagger name
        /// </summary>
        public string TaggerName { get; set; }

        /// <summary>
        /// The tagger email
        /// </summary>
        public string TaggerEmail { get; set; }

        /// <summary>
        /// Whether this is a lightweight tag
        /// </summary>
        public bool IsLightweight { get; set; }
    }

    /// <summary>
    /// Enumeration of Git change types
    /// </summary>
    public enum GitChangeType
    {
        /// <summary>
        /// File was added
        /// </summary>
        Added,

        /// <summary>
        /// File was modified
        /// </summary>
        Modified,

        /// <summary>
        /// File was deleted
        /// </summary>
        Deleted,

        /// <summary>
        /// File was renamed
        /// </summary>
        Renamed,

        /// <summary>
        /// File was copied
        /// </summary>
        Copied,

        /// <summary>
        /// File type changed
        /// </summary>
        TypeChanged,

        /// <summary>
        /// File is untracked
        /// </summary>
        Untracked,

        /// <summary>
        /// File is conflicted
        /// </summary>
        Conflicted
    }

    /// <summary>
    /// Enumeration of diff line types
    /// </summary>
    public enum GitDiffLineType
    {
        /// <summary>
        /// Context line (unchanged)
        /// </summary>
        Context,

        /// <summary>
        /// Added line
        /// </summary>
        Added,

        /// <summary>
        /// Deleted line
        /// </summary>
        Deleted,

        /// <summary>
        /// No newline at end of file
        /// </summary>
        NoNewline
    }

    /// <summary>
    /// Event arguments for repository status changes
    /// </summary>
    public class GitRepositoryStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new repository status
        /// </summary>
        public GitRepositoryStatus Status { get; set; }

        /// <summary>
        /// The timestamp of the change
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for branch changes
    /// </summary>
    public class GitBranchChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The old branch name
        /// </summary>
        public string OldBranch { get; set; }

        /// <summary>
        /// The new branch name
        /// </summary>
        public string NewBranch { get; set; }

        /// <summary>
        /// The timestamp of the change
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}