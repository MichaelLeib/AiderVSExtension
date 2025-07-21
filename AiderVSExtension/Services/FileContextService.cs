using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using EnvDTE;
using EnvDTE80;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for managing file and solution context
    /// </summary>
    public class FileContextService : IFileContextService, IDisposable
    {
        private readonly DTE2 _dte;
        private readonly IVsOutputWindowPane _outputPane;
        private readonly IGitService _gitService;
        private readonly IVsTextManager _textManager;
        private readonly Dictionary<string, string> _fileContentCache;
        private readonly Dictionary<string, DateTime> _fileCacheTimestamps;
        private readonly Dictionary<string, long> _fileCacheSizes;
        private readonly Dictionary<string, DateTime> _fileAccessTimes;
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers;
        private readonly object _cacheLock = new object();
        private readonly object _watcherLock = new object();
        private readonly System.Threading.Timer _cacheCleanupTimer;

        // Memory optimization constants
        private const int MAX_CACHE_FILES = 100;
        private const long MAX_CACHE_SIZE_BYTES = 50 * 1024 * 1024; // 50MB
        private const int MAX_FILE_SIZE_BYTES = 1024 * 1024; // 1MB per file
        private const int CACHE_CLEANUP_INTERVAL_MINUTES = 15;
        private const int CACHE_EXPIRY_MINUTES = 30;

        private long _currentCacheSize = 0;
        private bool _disposed = false;

        public FileContextService(DTE2 dte, IVsOutputWindowPane outputPane, IGitService gitService, IVsTextManager textManager = null)
        {
            _dte = dte ?? throw new ArgumentNullException(nameof(dte));
            _outputPane = outputPane;
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _textManager = textManager; // Optional - will fall back to GetGlobalService if null
            _fileContentCache = new Dictionary<string, string>();
            _fileCacheTimestamps = new Dictionary<string, DateTime>();
            _fileCacheSizes = new Dictionary<string, long>();
            _fileAccessTimes = new Dictionary<string, DateTime>();
            _fileWatchers = new Dictionary<string, FileSystemWatcher>();
            
            // Initialize cache cleanup timer
            _cacheCleanupTimer = new System.Threading.Timer(CleanupCache, null,
                TimeSpan.FromMinutes(CACHE_CLEANUP_INTERVAL_MINUTES),
                TimeSpan.FromMinutes(CACHE_CLEANUP_INTERVAL_MINUTES));
        }

        /// <summary>
        /// Gets all files in the current solution
        /// </summary>
        public async Task<IEnumerable<Interfaces.FileInfo>> GetSolutionFilesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var files = new List<Interfaces.FileInfo>();

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (_dte.Solution?.Projects == null)
                        return files;

                    foreach (Project project in _dte.Solution.Projects)
                    {
                        if (project.ProjectItems != null)
                        {
                            CollectProjectFiles(project.ProjectItems, project.Name, files);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error retrieving solution files: {ex.Message}");
                }

                return files;
            });
        }

        /// <summary>
        /// Searches for files matching the specified pattern
        /// </summary>
        public async Task<IEnumerable<Interfaces.FileInfo>> SearchFilesAsync(string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
                return Enumerable.Empty<Interfaces.FileInfo>();

            var allFiles = await GetSolutionFilesAsync().ConfigureAwait(false);
            var pattern = searchPattern.ToLowerInvariant();

            return allFiles.Where(f => 
                f.FileName.ToLowerInvariant().Contains(pattern) ||
                f.RelativePath.ToLowerInvariant().Contains(pattern) ||
                f.ProjectName.ToLowerInvariant().Contains(pattern));
        }

        /// <summary>
        /// Gets the content of a specific file
        /// </summary>
        public async Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Check cache first
                lock (_cacheLock)
                {
                    if (_fileContentCache.ContainsKey(filePath) && _fileCacheTimestamps.ContainsKey(filePath))
                    {
                        var fileInfo = new System.IO.FileInfo(filePath);
                        var cacheTimestamp = _fileCacheTimestamps[filePath];
                        
                        // Check if file has been modified since cached
                        if (fileInfo.Exists && fileInfo.LastWriteTime <= cacheTimestamp)
                        {
                            // Update access time for LRU tracking
                            _fileAccessTimes[filePath] = DateTime.UtcNow;
                            return _fileContentCache[filePath];
                        }
                        else
                        {
                            // File has been modified, remove from cache
                            RemoveFromCache(filePath);
                            RemoveFileWatcher(filePath);
                        }
                    }
                }

                // Read from file system
                if (File.Exists(filePath))
                {
                    var fileInfo = new System.IO.FileInfo(filePath);
                    
                    // Check file size before reading
                    if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                    {
                        // File too large, don't cache, read in chunks if needed
                        return await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                    }
                    
                    var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                    
                    // Add to cache with memory management
                    AddToCacheWithMemoryManagement(filePath, content, fileInfo);
                    
                    // Setup file watcher for automatic cache invalidation
                    SetupFileWatcher(filePath);
                    
                    return content;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                LogError($"Error reading file content from {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the content of a specific file range
        /// </summary>
        public async Task<string> GetFileContentRangeAsync(string filePath, int startLine, int endLine)
        {
            if (startLine < 1 || endLine < 1 || endLine < startLine)
                return string.Empty;

            var fullContent = await GetFileContentAsync(filePath).ConfigureAwait(false);
            if (string.IsNullOrEmpty(fullContent))
                return string.Empty;

            var lines = fullContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            
            // Convert to 0-based indexing
            var startIndex = Math.Max(0, startLine - 1);
            var endIndex = Math.Min(lines.Length - 1, endLine - 1);

            if (startIndex >= lines.Length)
                return string.Empty;

            var selectedLines = lines.Skip(startIndex).Take(endIndex - startIndex + 1);
            return string.Join(Environment.NewLine, selectedLines);
        }

        /// <summary>
        /// Gets Git branches for the current repository
        /// </summary>
        public async Task<IEnumerable<GitBranchInfo>> GetGitBranchesAsync()
        {
            return await Task.Run(() =>
            {
                var branches = new List<GitBranchInfo>();

                try
                {
                    var solutionDir = Path.GetDirectoryName(_dte.Solution?.FullName);
                    if (string.IsNullOrEmpty(solutionDir))
                        return branches;

                    // Find Git repository
                    var gitDir = FindGitRepository(solutionDir);
                    if (gitDir == null)
                        return branches;

                    using (var repo = new Repository(gitDir))
                    {
                        var currentBranch = repo.Head?.FriendlyName;

                        foreach (var branch in repo.Branches)
                        {
                            branches.Add(new GitBranchInfo
                            {
                                Name = branch.FriendlyName,
                                IsCurrentBranch = branch.FriendlyName == currentBranch,
                                IsRemote = branch.IsRemote,
                                LastCommitHash = branch.Tip?.Sha,
                                LastCommitDate = branch.Tip?.Author?.When.DateTime ?? DateTime.MinValue,
                                LastCommitMessage = branch.Tip?.MessageShort
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error retrieving Git branches: {ex.Message}");
                }

                return branches;
            });
        }

        /// <summary>
        /// Gets Git status for the current repository
        /// </summary>
        public async Task<GitStatusInfo> GetGitStatusAsync()
        {
            return await Task.Run(() =>
            {
                var statusInfo = new GitStatusInfo();

                try
                {
                    var solutionDir = Path.GetDirectoryName(_dte.Solution?.FullName);
                    if (string.IsNullOrEmpty(solutionDir))
                        return statusInfo;

                    var gitDir = FindGitRepository(solutionDir);
                    if (gitDir == null)
                        return statusInfo;

                    using (var repo = new Repository(gitDir))
                    {
                        statusInfo.CurrentBranch = repo.Head?.FriendlyName;
                        
                        var status = repo.RetrieveStatus();
                        statusInfo.HasUncommittedChanges = status.IsDirty;

                        foreach (var item in status)
                        {
                            switch (item.State)
                            {
                                case FileStatus.ModifiedInWorkdir:
                                case FileStatus.ModifiedInIndex:
                                    statusInfo.ModifiedFiles.Add(item.FilePath);
                                    break;
                                case FileStatus.NewInWorkdir:
                                case FileStatus.NewInIndex:
                                    statusInfo.AddedFiles.Add(item.FilePath);
                                    break;
                                case FileStatus.DeletedFromWorkdir:
                                case FileStatus.DeletedFromIndex:
                                    statusInfo.DeletedFiles.Add(item.FilePath);
                                    break;
                                case FileStatus.Untracked:
                                    statusInfo.UntrackedFiles.Add(item.FilePath);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error retrieving Git status: {ex.Message}");
                }

                return statusInfo;
            });
        }

        /// <summary>
        /// Gets Git context for chat references
        /// </summary>
        public async Task<GitContext> GetGitContextAsync()
        {
            try
            {
                if (await _gitService.IsGitRepositoryAsync().ConfigureAwait(false))
                {
                    return await _gitService.GetGitContextAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving Git context: {ex.Message}");
            }

            return new GitContext();
        }

        /// <summary>
        /// Gets Git diff for a specific file
        /// </summary>
        public async Task<GitDiff> GetGitDiffAsync(string filePath, bool cached = false)
        {
            try
            {
                if (await _gitService.IsGitRepositoryAsync().ConfigureAwait(false))
                {
                    return await _gitService.GetFileDiffAsync(filePath, cached).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving Git diff for {filePath}: {ex.Message}");
            }

            return new GitDiff();
        }

        /// <summary>
        /// Gets Git blame information for a file
        /// </summary>
        public async Task<GitBlame> GetGitBlameAsync(string filePath)
        {
            try
            {
                if (await _gitService.IsGitRepositoryAsync().ConfigureAwait(false))
                {
                    return await _gitService.GetBlameAsync(filePath).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving Git blame for {filePath}: {ex.Message}");
            }

            return new GitBlame();
        }

        /// <summary>
        /// Gets recent Git commits
        /// </summary>
        public async Task<IEnumerable<GitCommit>> GetRecentCommitsAsync(int limit = 10)
        {
            try
            {
                if (await _gitService.IsGitRepositoryAsync().ConfigureAwait(false))
                {
                    return await _gitService.GetRecentCommitsAsync(limit).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving recent commits: {ex.Message}");
            }

            return Enumerable.Empty<GitCommit>();
        }

        /// <summary>
        /// Gets commit history for a specific file
        /// </summary>
        public async Task<IEnumerable<GitCommit>> GetFileCommitHistoryAsync(string filePath, int limit = 10)
        {
            try
            {
                if (await _gitService.IsGitRepositoryAsync().ConfigureAwait(false))
                {
                    return await _gitService.GetFileHistoryAsync(filePath, limit).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving file commit history for {filePath}: {ex.Message}");
            }

            return Enumerable.Empty<GitCommit>();
        }

        /// <summary>
        /// Gets the current clipboard content
        /// </summary>
        public string GetClipboardContent()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    return Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving clipboard content: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the currently selected text in the active editor
        /// </summary>
        public async Task<SelectedTextInfo> GetSelectedTextAsync()
        {
            return await Task.Run(() =>
            {
                var selectedText = new SelectedTextInfo();

                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    // Use injected service or fall back to global service
                    var textManager = _textManager ?? Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
                    if (textManager != null)
                    {
                        textManager.GetActiveView(1, null, out IVsTextView view);
                        if (view != null)
                        {
                            view.GetSelection(out int startLine, out int startColumn, out int endLine, out int endColumn);
                            
                            if (startLine != endLine || startColumn != endColumn)
                            {
                                view.GetSelectedText(out string text);
                                view.GetBuffer(out IVsTextLines buffer);
                                
                                if (buffer != null)
                                {
                                    buffer.GetLineText(startLine, startColumn, endLine, endColumn, out string selectedTextContent);
                                    
                                    selectedText.Text = selectedTextContent ?? string.Empty;
                                    selectedText.StartLine = startLine + 1; // Convert to 1-based
                                    selectedText.EndLine = endLine + 1; // Convert to 1-based
                                    selectedText.StartColumn = startColumn + 1; // Convert to 1-based
                                    selectedText.EndColumn = endColumn + 1; // Convert to 1-based
                                    
                                    // Get file path from active document
                                    if (_dte.ActiveDocument != null)
                                    {
                                        selectedText.FilePath = _dte.ActiveDocument.FullName;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error retrieving selected text: {ex.Message}");
                }

                return selectedText;
            });
        }

        /// <summary>
        /// Gets project information for the current solution
        /// </summary>
        public async Task<IEnumerable<ProjectInfo>> GetProjectsAsync()
        {
            return await Task.Run(() =>
            {
                var projects = new List<ProjectInfo>();

                try
                {
                    if (_dte.Solution?.Projects == null)
                        return projects;

                    foreach (Project project in _dte.Solution.Projects)
                    {
                        var projectInfo = new ProjectInfo
                        {
                            Name = project.Name,
                            FilePath = project.FullName,
                            ProjectType = project.Kind
                        };

                        // Get project references
                        if (project.Object is VSProject vsProject)
                        {
                            foreach (Reference reference in vsProject.References)
                            {
                                projectInfo.References.Add(reference.Name);
                            }
                        }

                        // Get project files
                        if (project.ProjectItems != null)
                        {
                            CollectProjectFiles(project.ProjectItems, project.Name, projectInfo.Files);
                        }

                        projects.Add(projectInfo);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error retrieving project information: {ex.Message}");
                }

                return projects;
            });
        }

        #region Private Helper Methods

        private void CollectProjectFiles(ProjectItems projectItems, string projectName, List<Interfaces.FileInfo> files)
        {
            if (projectItems == null)
                return;

            foreach (ProjectItem item in projectItems)
            {
                try
                {
                    for (short i = 1; i <= item.FileCount; i++)
                    {
                        var filePath = item.FileNames[i];
                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            var fileInfo = new System.IO.FileInfo(filePath);
                            files.Add(new FileInfo
                            {
                                FilePath = filePath,
                                FileName = fileInfo.Name,
                                ProjectName = projectName,
                                RelativePath = GetRelativePath(filePath, projectName),
                                LastModified = fileInfo.LastWriteTime,
                                Size = fileInfo.Length,
                                FileType = fileInfo.Extension
                            });
                        }
                    }

                    // Recursively process sub-items
                    if (item.ProjectItems != null)
                    {
                        CollectProjectFiles(item.ProjectItems, projectName, files);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error processing project item {item.Name}: {ex.Message}");
                }
            }
        }

        private string GetRelativePath(string filePath, string projectName)
        {
            try
            {
                var solutionDir = Path.GetDirectoryName(_dte.Solution?.FullName);
                if (!string.IsNullOrEmpty(solutionDir))
                {
                    var uri = new Uri(solutionDir + Path.DirectorySeparatorChar);
                    var relativePath = uri.MakeRelativeUri(new Uri(filePath)).ToString();
                    return relativePath.Replace('/', Path.DirectorySeparatorChar);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error creating relative path for {filePath}: {ex.Message}");
            }

            return Path.GetFileName(filePath);
        }

        private string FindGitRepository(string startPath)
        {
            var currentPath = startPath;
            
            while (!string.IsNullOrEmpty(currentPath))
            {
                var gitPath = Path.Combine(currentPath, ".git");
                if (Directory.Exists(gitPath))
                {
                    return currentPath;
                }

                var parentPath = Path.GetDirectoryName(currentPath);
                if (parentPath == currentPath)
                    break;
                
                currentPath = parentPath;
            }

            return null;
        }

        private void LogError(string message)
        {
            try
            {
                _outputPane?.OutputString($"[FileContextService] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }

        #endregion

        #region File Monitoring Methods

        /// <summary>
        /// Sets up a file watcher for automatic cache invalidation
        /// </summary>
        private void SetupFileWatcher(string filePath)
        {
            try
            {
                lock (_watcherLock)
                {
                    // Don't create duplicate watchers
                    if (_fileWatchers.ContainsKey(filePath))
                        return;

                    var fileInfo = new System.IO.FileInfo(filePath);
                    if (!fileInfo.Exists || fileInfo.Directory == null)
                        return;

                    var watcher = new FileSystemWatcher
                    {
                        Path = fileInfo.Directory.FullName,
                        Filter = fileInfo.Name,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += (sender, e) =>
                    {
                        if (e.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            InvalidateFileCache(filePath);
                        }
                    };

                    watcher.Deleted += (sender, e) =>
                    {
                        if (e.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            InvalidateFileCache(filePath);
                            RemoveFileWatcher(filePath);
                        }
                    };

                    _fileWatchers[filePath] = watcher;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to setup file watcher for {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a file watcher
        /// </summary>
        private void RemoveFileWatcher(string filePath)
        {
            try
            {
                lock (_watcherLock)
                {
                    if (_fileWatchers.TryGetValue(filePath, out var watcher))
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                        _fileWatchers.Remove(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to remove file watcher for {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Invalidates the cache for a specific file
        /// </summary>
        private void InvalidateFileCache(string filePath)
        {
            try
            {
                lock (_cacheLock)
                {
                    _fileContentCache.Remove(filePath);
                    _fileCacheTimestamps.Remove(filePath);
                }
                
                LogError($"File cache invalidated for: {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to invalidate cache for {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all file caches and watchers
        /// </summary>
        public void ClearAllCaches()
        {
            try
            {
                lock (_cacheLock)
                {
                    _fileContentCache.Clear();
                    _fileCacheTimestamps.Clear();
                }

                lock (_watcherLock)
                {
                    foreach (var watcher in _fileWatchers.Values)
                    {
                        try
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors
                        }
                    }
                    _fileWatchers.Clear();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to clear caches: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes resources and cleans up file watchers
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
                    
                    // Dispose managed resources
                    ClearAllCaches();
                }

                _disposed = true;
            }
        }

        #region Cache Management

        private void AddToCacheWithMemoryManagement(string filePath, string content, System.IO.FileInfo fileInfo)
        {
            lock (_cacheLock)
            {
                if (_disposed) return;

                var contentSize = System.Text.Encoding.UTF8.GetByteCount(content);
                
                // Check if adding this file would exceed total cache size
                if (_currentCacheSize + contentSize > MAX_CACHE_SIZE_BYTES)
                {
                    EvictFilesToFreeSpace(contentSize);
                }
                
                // Check if we have too many files
                if (_fileContentCache.Count >= MAX_CACHE_FILES)
                {
                    EvictLeastRecentlyUsedFile();
                }

                // Add to cache
                _fileContentCache[filePath] = content;
                _fileCacheTimestamps[filePath] = fileInfo.LastWriteTime;
                _fileCacheSizes[filePath] = contentSize;
                _fileAccessTimes[filePath] = DateTime.UtcNow;
                _currentCacheSize += contentSize;
            }
        }

        private void RemoveFromCache(string filePath)
        {
            if (_fileContentCache.ContainsKey(filePath))
            {
                var size = _fileCacheSizes.GetValueOrDefault(filePath, 0);
                _currentCacheSize -= size;
                
                _fileContentCache.Remove(filePath);
                _fileCacheTimestamps.Remove(filePath);
                _fileCacheSizes.Remove(filePath);
                _fileAccessTimes.Remove(filePath);
            }
        }

        private void EvictFilesToFreeSpace(long neededSpace)
        {
            var targetFreeSpace = neededSpace + (MAX_CACHE_SIZE_BYTES / 10); // Extra 10% buffer
            var freedSpace = 0L;

            var filesToEvict = _fileAccessTimes
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var filePath in filesToEvict)
            {
                var fileSize = _fileCacheSizes.GetValueOrDefault(filePath, 0);
                RemoveFromCache(filePath);
                freedSpace += fileSize;

                if (freedSpace >= targetFreeSpace)
                    break;
            }
        }

        private void EvictLeastRecentlyUsedFile()
        {
            if (_fileAccessTimes.Count == 0) return;

            var lruFile = _fileAccessTimes.OrderBy(kvp => kvp.Value).First().Key;
            RemoveFromCache(lruFile);
        }

        private void CleanupCache(object state)
        {
            if (_disposed) return;

            try
            {
                lock (_cacheLock)
                {
                    if (_disposed) return;

                    var cutoffTime = DateTime.UtcNow.AddMinutes(-CACHE_EXPIRY_MINUTES);
                    var expiredFiles = _fileAccessTimes
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var filePath in expiredFiles)
                    {
                        RemoveFromCache(filePath);
                    }

                    // Force garbage collection if we freed significant memory
                    if (expiredFiles.Count > 10)
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

        #endregion
    }
}