using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Completion
{
    /// <summary>
    /// Cache for AI completion results to improve performance
    /// </summary>
    public class CompletionCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry<IEnumerable<CompletionItem>>> _completionCache;
        private readonly ConcurrentDictionary<string, CacheEntry<CompletionDetails>> _detailsCache;
        private readonly TimeSpan _cacheExpiration;
        private readonly int _maxCacheSize;

        public CompletionCache(TimeSpan cacheExpiration = default, int maxCacheSize = 1000)
        {
            _completionCache = new ConcurrentDictionary<string, CacheEntry<IEnumerable<CompletionItem>>>();
            _detailsCache = new ConcurrentDictionary<string, CacheEntry<CompletionDetails>>();
            _cacheExpiration = cacheExpiration == default ? TimeSpan.FromMinutes(5) : cacheExpiration;
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// Tries to get cached completions for the given key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="completions">Cached completions if found</param>
        /// <returns>True if cache hit, false if cache miss</returns>
        public bool TryGetCompletions(string key, out IEnumerable<CompletionItem> completions)
        {
            completions = null;
            
            if (string.IsNullOrEmpty(key))
                return false;

            if (_completionCache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow - entry.Timestamp < _cacheExpiration)
                {
                    completions = entry.Value;
                    entry.AccessCount++;
                    entry.LastAccessed = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    // Remove expired entry
                    _completionCache.TryRemove(key, out _);
                }
            }

            return false;
        }

        /// <summary>
        /// Caches completions for the given key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="completions">Completions to cache</param>
        public void CacheCompletions(string key, IEnumerable<CompletionItem> completions)
        {
            if (string.IsNullOrEmpty(key) || completions == null)
                return;

            // Ensure cache doesn't exceed maximum size
            if (_completionCache.Count >= _maxCacheSize)
            {
                CleanupExpiredEntries();
                
                // If still at max size, remove least recently used entries
                if (_completionCache.Count >= _maxCacheSize)
                {
                    RemoveLeastRecentlyUsedCompletions();
                }
            }

            var entry = new CacheEntry<IEnumerable<CompletionItem>>
            {
                Value = completions.ToList(), // Create a copy to avoid reference issues
                Timestamp = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 0
            };

            _completionCache.AddOrUpdate(key, entry, (k, old) => entry);
        }

        /// <summary>
        /// Tries to get cached completion details for the given key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="details">Cached details if found</param>
        /// <returns>True if cache hit, false if cache miss</returns>
        public bool TryGetDetails(string key, out CompletionDetails details)
        {
            details = null;
            
            if (string.IsNullOrEmpty(key))
                return false;

            if (_detailsCache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow - entry.Timestamp < _cacheExpiration)
                {
                    details = entry.Value;
                    entry.AccessCount++;
                    entry.LastAccessed = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    // Remove expired entry
                    _detailsCache.TryRemove(key, out _);
                }
            }

            return false;
        }

        /// <summary>
        /// Caches completion details for the given key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="details">Details to cache</param>
        public void CacheDetails(string key, CompletionDetails details)
        {
            if (string.IsNullOrEmpty(key) || details == null)
                return;

            // Ensure cache doesn't exceed maximum size
            if (_detailsCache.Count >= _maxCacheSize)
            {
                CleanupExpiredDetailsEntries();
                
                // If still at max size, remove least recently used entries
                if (_detailsCache.Count >= _maxCacheSize)
                {
                    RemoveLeastRecentlyUsedDetails();
                }
            }

            var entry = new CacheEntry<CompletionDetails>
            {
                Value = details,
                Timestamp = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 0
            };

            _detailsCache.AddOrUpdate(key, entry, (k, old) => entry);
        }

        /// <summary>
        /// Clears all cached entries
        /// </summary>
        public void Clear()
        {
            _completionCache.Clear();
            _detailsCache.Clear();
        }

        /// <summary>
        /// Clears expired entries from the cache
        /// </summary>
        public void CleanupExpiredEntries()
        {
            CleanupExpiredCompletionEntries();
            CleanupExpiredDetailsEntries();
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <returns>Cache statistics</returns>
        public CacheStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var completionEntries = _completionCache.Values.ToList();
            var detailEntries = _detailsCache.Values.ToList();
            
            return new CacheStatistics
            {
                CompletionCacheSize = _completionCache.Count,
                DetailsCacheSize = _detailsCache.Count,
                TotalCacheSize = _completionCache.Count + _detailsCache.Count,
                MaxCacheSize = _maxCacheSize,
                ExpiredCompletionEntries = completionEntries.Count(e => now - e.Timestamp > _cacheExpiration),
                ExpiredDetailsEntries = detailEntries.Count(e => now - e.Timestamp > _cacheExpiration),
                TotalCompletionAccesses = completionEntries.Sum(e => e.AccessCount),
                TotalDetailsAccesses = detailEntries.Sum(e => e.AccessCount),
                CacheExpiration = _cacheExpiration
            };
        }

        private void CleanupExpiredCompletionEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _completionCache
                .Where(kvp => now - kvp.Value.Timestamp > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _completionCache.TryRemove(key, out _);
            }
        }

        private void CleanupExpiredDetailsEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _detailsCache
                .Where(kvp => now - kvp.Value.Timestamp > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _detailsCache.TryRemove(key, out _);
            }
        }

        private void RemoveLeastRecentlyUsedCompletions()
        {
            var entriesToRemove = _completionCache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_maxCacheSize / 4) // Remove 25% of entries
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in entriesToRemove)
            {
                _completionCache.TryRemove(key, out _);
            }
        }

        private void RemoveLeastRecentlyUsedDetails()
        {
            var entriesToRemove = _detailsCache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_maxCacheSize / 4) // Remove 25% of entries
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in entriesToRemove)
            {
                _detailsCache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Represents a cache entry with metadata
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    internal class CacheEntry<T>
    {
        public T Value { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }

    /// <summary>
    /// Cache statistics for monitoring and debugging
    /// </summary>
    public class CacheStatistics
    {
        public int CompletionCacheSize { get; set; }
        public int DetailsCacheSize { get; set; }
        public int TotalCacheSize { get; set; }
        public int MaxCacheSize { get; set; }
        public int ExpiredCompletionEntries { get; set; }
        public int ExpiredDetailsEntries { get; set; }
        public long TotalCompletionAccesses { get; set; }
        public long TotalDetailsAccesses { get; set; }
        public TimeSpan CacheExpiration { get; set; }
        
        public double CompletionCacheUtilization => MaxCacheSize > 0 ? (double)CompletionCacheSize / MaxCacheSize : 0;
        public double DetailsCacheUtilization => MaxCacheSize > 0 ? (double)DetailsCacheSize / MaxCacheSize : 0;
        public double TotalCacheUtilization => MaxCacheSize > 0 ? (double)TotalCacheSize / (MaxCacheSize * 2) : 0;
    }
}