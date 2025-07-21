using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Optimized file context extraction service with caching and background processing
    /// </summary>
    public class OptimizedFileContextService : IFileContextService, IDisposable
    {
        private readonly IFileContextService _baseService;
        private readonly IPerformanceMonitoringService _performanceMonitor;
        private readonly IErrorHandler _errorHandler;
        private readonly ILazyComponent<FileIndexCache> _fileIndexCache;
        private readonly ILazyComponent<ContentAnalyzer> _contentAnalyzer;
        
        // Caching infrastructure
        private readonly ConcurrentDictionary<string, CachedFileContent> _contentCache = new ConcurrentDictionary<string, CachedFileContent>();
        private readonly ConcurrentDictionary<string, CachedFileInfo> _fileInfoCache = new ConcurrentDictionary<string, CachedFileInfo>();
        private readonly ConcurrentDictionary<string, DateTime> _lastAccessTimes = new ConcurrentDictionary<string, DateTime>();
        
        // Background processing
        private readonly Timer _cacheCleanupTimer;
        private readonly Timer _indexRefreshTimer;
        private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);
        
        // Configuration
        private readonly FileContextOptions _options;
        private bool _disposed = false;

        // Collection size limits to prevent unbounded growth
        private const int MaxContentCacheSize = 500;
        private const int MaxFileInfoCacheSize = 200;
        private const int MaxAccessTimesSize = 700;

        public OptimizedFileContextService(
            IFileContextService baseService,
            IPerformanceMonitoringService performanceMonitor,
            IErrorHandler errorHandler,
            LazyComponentManager lazyComponentManager,
            FileContextOptions options = null)
        {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _options = options ?? new FileContextOptions();

            // Initialize lazy components
            _fileIndexCache = lazyComponentManager.CreateLazyComponent(
                () => Task.FromResult(new FileIndexCache(_options.MaxCacheSize)),
                LazyLoadingStrategy.Background,
                priority: 80);

            _contentAnalyzer = lazyComponentManager.CreateLazyComponent(
                () => Task.FromResult(new ContentAnalyzer(_options)),
                LazyLoadingStrategy.OnDemand,
                priority: 60);

            // Initialize timers
            _cacheCleanupTimer = new Timer(CleanupCacheCallback, null, 
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            
            _indexRefreshTimer = new Timer(RefreshIndexCallback, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        #region IFileContextService Implementation

        public async Task<IEnumerable<Interfaces.FileInfo>> GetSolutionFilesAsync(CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("GetSolutionFiles", "FileContext");
            
            try
            {
                var cacheKey = "solution_files";
                var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
                
                if (cached != null && !cached.IsExpired(_options.CacheTimeoutMinutes))
                {
                    tracker.AddMetadata("CacheHit", true);
                    _lastAccessTimes[cacheKey] = DateTime.UtcNow;
                    tracker.Complete();
                    return cached.Files;
                }

                // Get from base service with background refresh
                var files = await _baseService.GetSolutionFilesAsync(cancellationToken).ConfigureAwait(false);
                var fileList = files.ToList();

                // Enforce collection size limits before caching
                if (_fileInfoCache.Count >= MaxFileInfoCacheSize)
                {
                    // Remove oldest entries (20% of cache)
                    var entriesToRemove = _fileInfoCache.Values
                        .OrderBy(f => f.CachedAt)
                        .Take(MaxFileInfoCacheSize / 5)
                        .Select(f => f.CachedAt.ToString()) // Use timestamp as key since we don't have a direct key
                        .ToList();

                    var keysToRemove = _fileInfoCache.Where(kvp => entriesToRemove.Contains(kvp.Value.CachedAt.ToString()))
                        .Take(MaxFileInfoCacheSize / 5)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _fileInfoCache.TryRemove(key, out _);
                        _lastAccessTimes.TryRemove(key, out _);
                    }
                }

                // Cache the results
                _fileInfoCache[cacheKey] = new CachedFileInfo
                {
                    Files = fileList,
                    CachedAt = DateTime.UtcNow
                };
                _lastAccessTimes[cacheKey] = DateTime.UtcNow;

                // Start background indexing with proper exception handling
                _ = IndexFilesInBackgroundSafelyAsync(fileList);

                tracker.AddMetadata("CacheHit", false);
                tracker.AddMetadata("FileCount", fileList.Count);
                tracker.Complete();
                return fileList;
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.GetSolutionFilesAsync").ConfigureAwait(false);
                throw;
            }
        }

        public async Task<IEnumerable<Interfaces.FileInfo>> SearchFilesAsync(string searchPattern)
        {
            using var tracker = _performanceMonitor.StartOperation("SearchFiles", "FileContext");
            
            try
            {
                if (string.IsNullOrWhiteSpace(searchPattern))
                {
                    tracker.Complete();
                    return Enumerable.Empty<Interfaces.FileInfo>();
                }

                var cacheKey = $"search_{searchPattern.ToLowerInvariant()}";
                var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
                
                if (cached != null && !cached.IsExpired(_options.CacheTimeoutMinutes))
                {
                    tracker.AddMetadata("CacheHit", true);
                    _lastAccessTimes[cacheKey] = DateTime.UtcNow;
                    tracker.Complete();
                    return cached.Files;
                }

                // Try optimized search using index first
                var indexCache = await _fileIndexCache.GetValueAsync().ConfigureAwait(false);
                var indexResults = await indexCache.SearchAsync(searchPattern).ConfigureAwait(false);
                
                if (indexResults.Any())
                {
                    _fileInfoCache[cacheKey] = new CachedFileInfo
                    {
                        Files = indexResults.ToList(),
                        CachedAt = DateTime.UtcNow
                    };
                    _lastAccessTimes[cacheKey] = DateTime.UtcNow;
                    
                    tracker.AddMetadata("CacheHit", false);
                    tracker.AddMetadata("IndexSearch", true);
                    tracker.AddMetadata("ResultCount", indexResults.Count());
                    tracker.Complete();
                    return indexResults;
                }

                // Fall back to base service search
                var results = await _baseService.SearchFilesAsync(searchPattern, cancellationToken).ConfigureAwait(false);
                var resultList = results.ToList();

                _fileInfoCache[cacheKey] = new CachedFileInfo
                {
                    Files = resultList,
                    CachedAt = DateTime.UtcNow
                };
                _lastAccessTimes[cacheKey] = DateTime.UtcNow;

                tracker.AddMetadata("CacheHit", false);
                tracker.AddMetadata("IndexSearch", false);
                tracker.AddMetadata("ResultCount", resultList.Count);
                tracker.Complete();
                return resultList;
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.SearchFilesAsync").ConfigureAwait(false);
                throw;
            }
        }

        public async Task<string> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using var tracker = _performanceMonitor.StartOperation("GetFileContent", "FileContext");
            
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    tracker.Complete();
                    return string.Empty;
                }

                var cached = _contentCache.GetValueOrDefault(filePath);
                
                // Check if cached content is still valid
                if (cached != null && await cached.IsValidAsync(_options.CacheTimeoutMinutes).ConfigureAwait(false))
                {
                    tracker.AddMetadata("CacheHit", true);
                    _lastAccessTimes[filePath] = DateTime.UtcNow;
                    EnforceAccessTimesLimit();
                    tracker.Complete();
                    return cached.Content;
                }

                // Read content optimally
                var content = await ReadFileContentOptimallyAsync(filePath).ConfigureAwait(false);
                
                // Enforce collection size limits before caching
                if (_contentCache.Count >= MaxContentCacheSize)
                {
                    // Remove oldest entries (20% of cache)
                    var entriesToRemove = _contentCache.Values
                        .OrderBy(c => c.CachedAt)
                        .Take(MaxContentCacheSize / 5)
                        .Select(c => c.FilePath)
                        .ToList();

                    foreach (var key in entriesToRemove)
                    {
                        _contentCache.TryRemove(key, out _);
                        _lastAccessTimes.TryRemove(key, out _);
                    }
                }

                // Cache the content
                _contentCache[filePath] = new CachedFileContent
                {
                    Content = content,
                    FilePath = filePath,
                    CachedAt = DateTime.UtcNow,
                    LastModified = File.Exists(filePath) ? File.GetLastWriteTime(filePath) : DateTime.MinValue
                };
                _lastAccessTimes[filePath] = DateTime.UtcNow;

                tracker.AddMetadata("CacheHit", false);
                tracker.AddMetadata("ContentLength", content.Length);
                tracker.Complete();
                return content;
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.GetFileContentAsync").ConfigureAwait(false);
                throw;
            }
        }

        public async Task<string> GetFileContentRangeAsync(string filePath, int startLine, int endLine)
        {
            using var tracker = _performanceMonitor.StartOperation("GetFileContentRange", "FileContext");
            
            try
            {
                if (startLine < 1 || endLine < 1 || endLine < startLine)
                {
                    tracker.Complete();
                    return string.Empty;
                }

                var cacheKey = $"{filePath}:{startLine}-{endLine}";
                var cached = _contentCache.GetValueOrDefault(cacheKey);
                
                if (cached != null && await cached.IsValidAsync(_options.CacheTimeoutMinutes).ConfigureAwait(false))
                {
                    tracker.AddMetadata("CacheHit", true);
                    _lastAccessTimes[cacheKey] = DateTime.UtcNow;
                    EnforceAccessTimesLimit();
                    tracker.Complete();
                    return cached.Content;
                }

                // Optimize range reading for large files
                string content;
                if (await IsLargeFileAsync(filePath).ConfigureAwait(false))
                {
                    content = await ReadFileRangeOptimallyAsync(filePath, startLine, endLine).ConfigureAwait(false);
                }
                else
                {
                    // For small files, read entire content and extract range
                    var fullContent = await GetFileContentAsync(filePath).ConfigureAwait(false);
                    content = ExtractContentRange(fullContent, startLine, endLine);
                }

                // Enforce collection size limits before caching
                if (_contentCache.Count >= MaxContentCacheSize)
                {
                    // Remove oldest entries (20% of cache)
                    var entriesToRemove = _contentCache.Values
                        .OrderBy(c => c.CachedAt)
                        .Take(MaxContentCacheSize / 5)
                        .Select(c => c.FilePath)
                        .ToList();

                    foreach (var key in entriesToRemove)
                    {
                        _contentCache.TryRemove(key, out _);
                        _lastAccessTimes.TryRemove(key, out _);
                    }
                }

                // Cache the range content
                _contentCache[cacheKey] = new CachedFileContent
                {
                    Content = content,
                    FilePath = cacheKey,
                    CachedAt = DateTime.UtcNow,
                    LastModified = File.Exists(filePath) ? File.GetLastWriteTime(filePath) : DateTime.MinValue
                };
                _lastAccessTimes[cacheKey] = DateTime.UtcNow;

                tracker.AddMetadata("CacheHit", false);
                tracker.AddMetadata("ContentLength", content.Length);
                tracker.AddMetadata("LineRange", $"{startLine}-{endLine}");
                tracker.Complete();
                return content;
            }
            catch (Exception ex)
            {
                tracker.Fail(ex);
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.GetFileContentRangeAsync").ConfigureAwait(false);
                throw;
            }
        }

        // Delegate other methods to base service with caching where appropriate
        public async Task<IEnumerable<GitBranchInfo>> GetGitBranchesAsync()
        {
            var cacheKey = "git_branches";
            var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
            
            if (cached != null && !cached.IsExpired(_options.CacheTimeoutMinutes))
            {
                return cached.GitBranches;
            }

            var branches = await _baseService.GetGitBranchesAsync().ConfigureAwait(false);
            var branchList = branches.ToList();
            
            _fileInfoCache[cacheKey] = new CachedFileInfo
            {
                GitBranches = branchList,
                CachedAt = DateTime.UtcNow
            };
            
            return branchList;
        }

        public async Task<GitStatusInfo> GetGitStatusAsync()
        {
            // Git status changes frequently, cache for shorter time
            var cacheKey = "git_status";
            var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
            
            if (cached != null && !cached.IsExpired(1)) // 1 minute cache
            {
                return cached.GitStatus;
            }

            var status = await _baseService.GetGitStatusAsync().ConfigureAwait(false);
            
            _fileInfoCache[cacheKey] = new CachedFileInfo
            {
                GitStatus = status,
                CachedAt = DateTime.UtcNow
            };
            
            return status;
        }

        public async Task<GitContext> GetGitContextAsync()
        {
            return await _baseService.GetGitContextAsync().ConfigureAwait(false);
        }

        public async Task<GitDiff> GetGitDiffAsync(string filePath, bool cached = false)
        {
            return await _baseService.GetGitDiffAsync(filePath, cached).ConfigureAwait(false);
        }

        public async Task<GitBlame> GetGitBlameAsync(string filePath)
        {
            return await _baseService.GetGitBlameAsync(filePath).ConfigureAwait(false);
        }

        public async Task<IEnumerable<GitCommit>> GetRecentCommitsAsync(int limit = 10)
        {
            var cacheKey = $"recent_commits_{limit}";
            var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
            
            if (cached != null && !cached.IsExpired(_options.CacheTimeoutMinutes))
            {
                return cached.GitCommits;
            }

            var commits = await _baseService.GetRecentCommitsAsync(limit).ConfigureAwait(false);
            var commitList = commits.ToList();
            
            _fileInfoCache[cacheKey] = new CachedFileInfo
            {
                GitCommits = commitList,
                CachedAt = DateTime.UtcNow
            };
            
            return commitList;
        }

        public async Task<IEnumerable<GitCommit>> GetFileCommitHistoryAsync(string filePath, int limit = 10)
        {
            return await _baseService.GetFileCommitHistoryAsync(filePath, limit).ConfigureAwait(false);
        }

        public string GetClipboardContent()
        {
            return _baseService.GetClipboardContent();
        }

        public async Task<SelectedTextInfo> GetSelectedTextAsync()
        {
            return await _baseService.GetSelectedTextAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<ProjectInfo>> GetProjectsAsync()
        {
            var cacheKey = "projects";
            var cached = _fileInfoCache.GetValueOrDefault(cacheKey);
            
            if (cached != null && !cached.IsExpired(_options.CacheTimeoutMinutes))
            {
                return cached.Projects;
            }

            var projects = await _baseService.GetProjectsAsync().ConfigureAwait(false);
            var projectList = projects.ToList();
            
            _fileInfoCache[cacheKey] = new CachedFileInfo
            {
                Projects = projectList,
                CachedAt = DateTime.UtcNow
            };
            
            return projectList;
        }

        #endregion

        #region Performance Optimization Methods

        private async Task<string> ReadFileContentOptimallyAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            var fileInfo = new System.IO.FileInfo(filePath);
            
            // For large files, read in chunks
            if (fileInfo.Length > _options.LargeFileThreshold)
            {
                return await ReadLargeFileAsync(filePath);
            }

            // For small files, read directly
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        private async Task<string> ReadLargeFileAsync(string filePath)
        {
            var chunks = new List<string>();
            const int bufferSize = 64 * 1024; // 64KB chunks
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize);
            
            var buffer = new char[bufferSize];
            int charactersRead;
            
            while ((charactersRead = await reader.ReadAsync(buffer, 0, bufferSize).ConfigureAwait(false)) > 0)
            {
                chunks.Add(new string(buffer, 0, charactersRead));
                
                // Prevent memory issues with extremely large files
                if (chunks.Count > 100) // ~6.4MB limit
                {
                    break;
                }
            }
            
            return string.Concat(chunks);
        }

        private async Task<string> ReadFileRangeOptimallyAsync(string filePath, int startLine, int endLine)
        {
            if (!File.Exists(filePath))
                return string.Empty;

            var lines = new List<string>();
            var currentLine = 1;
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (currentLine >= startLine && currentLine <= endLine)
                {
                    lines.Add(line);
                }
                
                currentLine++;
                
                if (currentLine > endLine)
                    break;
            }
            
            return string.Join(Environment.NewLine, lines);
        }

        private async Task<bool> IsLargeFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileInfo = new System.IO.FileInfo(filePath);
                return fileInfo.Length > _options.LargeFileThreshold;
            }
            catch
            {
                return false;
            }
        }

        private string ExtractContentRange(string content, int startLine, int endLine)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var startIndex = Math.Max(0, startLine - 1);
            var endIndex = Math.Min(lines.Length - 1, endLine - 1);

            if (startIndex >= lines.Length)
                return string.Empty;

            return string.Join(Environment.NewLine, lines.Skip(startIndex).Take(endIndex - startIndex + 1));
        }

        private async Task IndexFilesInBackgroundAsync(IEnumerable<Interfaces.FileInfo> files)
        {
            try
            {
                var indexCache = await _fileIndexCache.GetValueAsync().ConfigureAwait(false);
                await indexCache.IndexFilesAsync(files).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.IndexFilesInBackgroundAsync").ConfigureAwait(false);
            }
        }

        private async Task IndexFilesInBackgroundSafelyAsync(IEnumerable<Interfaces.FileInfo> files)
        {
            try
            {
                await IndexFilesInBackgroundAsync(files).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.IndexFilesInBackgroundSafelyAsync").ConfigureAwait(false);
            }
        }

        #endregion

        #region Cache Management

        private void CleanupCacheCallback(object state)
        {
            _ = CleanupCacheAsync();
        }

        private async Task CleanupCacheAsync()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-_options.CacheTimeoutMinutes);
                var keysToRemove = new List<string>();

                // Remove expired content cache entries
                foreach (var kvp in _contentCache)
                {
                    if (kvp.Value.CachedAt < cutoffTime ||
                        !_lastAccessTimes.ContainsKey(kvp.Key) ||
                        _lastAccessTimes[kvp.Key] < cutoffTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _contentCache.TryRemove(key, out _);
                    _lastAccessTimes.TryRemove(key, out _);
                }

                // Remove expired file info cache entries
                keysToRemove.Clear();
                foreach (var kvp in _fileInfoCache)
                {
                    if (kvp.Value.IsExpired(_options.CacheTimeoutMinutes))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _fileInfoCache.TryRemove(key, out _);
                    _lastAccessTimes.TryRemove(key, out _);
                }

                // Force garbage collection if we removed a lot of items
                if (keysToRemove.Count > 100)
                {
                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.CleanupCacheAsync").ConfigureAwait(false);
            }
        }

        private void RefreshIndexCallback(object state)
        {
            _ = RefreshIndexAsync();
        }

        private async Task RefreshIndexAsync()
        {
            if (!await _refreshSemaphore.WaitAsync(100).ConfigureAwait(false))
                return;

            try
            {
                if (_fileIndexCache.IsLoaded)
                {
                    var indexCache = await _fileIndexCache.GetValueAsync().ConfigureAwait(false);
                    await indexCache.RefreshAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "OptimizedFileContextService.RefreshIndexAsync").ConfigureAwait(false);
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Dispose timers first to stop background operations
                _cacheCleanupTimer?.Dispose();
                _indexRefreshTimer?.Dispose();
                
                // Dispose semaphore
                _refreshSemaphore?.Dispose();
                
                // Dispose lazy components
                _fileIndexCache?.Dispose();
                _contentAnalyzer?.Dispose();
                
                // Clear all caches to free memory
                _contentCache.Clear();
                _fileInfoCache.Clear();
                _lastAccessTimes.Clear();
                
                _disposed = true;
            }
        }

        ~OptimizedFileContextService()
        {
            Dispose(false);
        }

        private void EnforceAccessTimesLimit()
        {
            if (_lastAccessTimes.Count >= MaxAccessTimesSize)
            {
                // Remove oldest access time entries (20% of collection)
                var entriesToRemove = _lastAccessTimes
                    .OrderBy(kvp => kvp.Value)
                    .Take(MaxAccessTimesSize / 5)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in entriesToRemove)
                {
                    _lastAccessTimes.TryRemove(key, out _);
                }
            }
        }
    }

    #region Supporting Classes

    public class FileContextOptions
    {
        public int CacheTimeoutMinutes { get; set; } = 15;
        public long LargeFileThreshold { get; set; } = 1024 * 1024; // 1MB
        public int MaxCacheSize { get; set; } = 1000;
        public bool EnableBackgroundIndexing { get; set; } = true;
        public bool EnableContentAnalysis { get; set; } = true;
        public string[] ExcludedExtensions { get; set; } = { ".exe", ".dll", ".bin", ".obj", ".pdb" };
        public string[] ExcludedDirectories { get; set; } = { "bin", "obj", ".git", ".vs", "node_modules" };
    }

    internal class CachedFileContent
    {
        public string Content { get; set; }
        public string FilePath { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime LastModified { get; set; }

        public async Task<bool> IsValidAsync(int timeoutMinutes)
        {
            if (DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(timeoutMinutes))
                return false;

            try
            {
                if (File.Exists(FilePath))
                {
                    var currentModified = File.GetLastWriteTime(FilePath);
                    return currentModified <= LastModified;
                }
            }
            catch
            {
                // If we can't check the file, assume invalid
                return false;
            }

            return false;
        }
    }

    internal class CachedFileInfo
    {
        public List<Interfaces.FileInfo> Files { get; set; } = new List<Interfaces.FileInfo>();
        public List<GitBranchInfo> GitBranches { get; set; } = new List<GitBranchInfo>();
        public GitStatusInfo GitStatus { get; set; }
        public List<GitCommit> GitCommits { get; set; } = new List<GitCommit>();
        public List<ProjectInfo> Projects { get; set; } = new List<ProjectInfo>();
        public DateTime CachedAt { get; set; }

        public bool IsExpired(int timeoutMinutes)
        {
            return DateTime.UtcNow - CachedAt > TimeSpan.FromMinutes(timeoutMinutes);
        }
    }

    internal class FileIndexCache
    {
        private readonly ConcurrentDictionary<string, FileIndexEntry> _index = new ConcurrentDictionary<string, FileIndexEntry>();
        private readonly int _maxSize;
        private readonly object _indexLock = new object();

        public FileIndexCache(int maxSize)
        {
            _maxSize = maxSize;
        }

        public async Task IndexFilesAsync(IEnumerable<Interfaces.FileInfo> files)
        {
            await Task.Run(() =>
            {
                lock (_indexLock)
                {
                    foreach (var file in files.Take(_maxSize))
                    {
                        var entry = new FileIndexEntry
                        {
                            File = file,
                            SearchTokens = GenerateSearchTokens(file),
                            IndexedAt = DateTime.UtcNow
                        };
                        
                        _index.AddOrUpdate(file.FilePath, entry, (key, old) => entry);
                    }

                    // Remove excess entries if needed
                    if (_index.Count > _maxSize)
                    {
                        var keysToRemove = _index.Keys.Take(_index.Count - _maxSize).ToList();
                        foreach (var key in keysToRemove)
                        {
                            _index.TryRemove(key, out _);
                        }
                    }
                }
            });
        }

        public async Task<IEnumerable<Interfaces.FileInfo>> SearchAsync(string searchPattern)
        {
            return await Task.Run(() =>
            {
                var pattern = searchPattern.ToLowerInvariant();
                var results = new List<Interfaces.FileInfo>();

                lock (_indexLock)
                {
                    foreach (var entry in _index.Values)
                    {
                        if (entry.SearchTokens.Any(token => token.Contains(pattern)))
                        {
                            results.Add(entry.File);
                        }
                    }
                }

                return results.AsEnumerable();
            });
        }

        public async Task RefreshAsync()
        {
            await Task.Run(() =>
            {
                lock (_indexLock)
                {
                    var cutoffTime = DateTime.UtcNow.AddHours(-1);
                    var keysToRemove = _index.Values
                        .Where(entry => entry.IndexedAt < cutoffTime)
                        .Select(entry => entry.File.FilePath)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _index.TryRemove(key, out _);
                    }
                }
            });
        }

        private List<string> GenerateSearchTokens(Interfaces.FileInfo file)
        {
            var tokens = new List<string>
            {
                file.FileName.ToLowerInvariant(),
                file.RelativePath.ToLowerInvariant(),
                file.ProjectName.ToLowerInvariant(),
                Path.GetFileNameWithoutExtension(file.FileName).ToLowerInvariant()
            };

            // Add path segments
            var pathSegments = file.RelativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var segment in pathSegments)
            {
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    tokens.Add(segment.ToLowerInvariant());
                }
            }

            return tokens.Distinct().ToList();
        }
    }

    internal class FileIndexEntry
    {
        public Interfaces.FileInfo File { get; set; }
        public List<string> SearchTokens { get; set; }
        public DateTime IndexedAt { get; set; }
    }

    internal class ContentAnalyzer
    {
        private readonly FileContextOptions _options;

        public ContentAnalyzer(FileContextOptions options)
        {
            _options = options;
        }

        public async Task<ContentAnalysisResult> AnalyzeAsync(string content, string filePath)
        {
            return await Task.Run(() =>
            {
                var result = new ContentAnalysisResult
                {
                    FilePath = filePath,
                    LineCount = content.Split('\n').Length,
                    CharacterCount = content.Length,
                    FileType = Path.GetExtension(filePath),
                    HasBinaryContent = ContainsBinaryContent(content),
                    EstimatedReadingTime = EstimateReadingTime(content)
                };

                return result;
            });
        }

        private bool ContainsBinaryContent(string content)
        {
            return content.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t');
        }

        private TimeSpan EstimateReadingTime(string content)
        {
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var readingTimeMinutes = wordCount / 200.0; // Average reading speed
            return TimeSpan.FromMinutes(readingTimeMinutes);
        }
    }

    internal class ContentAnalysisResult
    {
        public string FilePath { get; set; }
        public int LineCount { get; set; }
        public int CharacterCount { get; set; }
        public string FileType { get; set; }
        public bool HasBinaryContent { get; set; }
        public TimeSpan EstimatedReadingTime { get; set; }
    }

    #endregion
}