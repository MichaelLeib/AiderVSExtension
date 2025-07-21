using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Thread-safe priority queue implementation
    /// </summary>
    /// <typeparam name="T">The type of items stored in the queue</typeparam>
    public class ConcurrentPriorityQueue<T>
    {
        private readonly SortedDictionary<int, ConcurrentQueue<T>> _queues;
        private readonly object _lockObject = new object();
        private volatile int _count;

        /// <summary>
        /// Gets the number of items in the queue
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets a value indicating whether the queue is empty
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Initializes a new instance of the ConcurrentPriorityQueue class
        /// </summary>
        public ConcurrentPriorityQueue()
        {
            _queues = new SortedDictionary<int, ConcurrentQueue<T>>(new DescendingComparer());
            _count = 0;
        }

        /// <summary>
        /// Enqueues an item with the specified priority
        /// </summary>
        /// <param name="item">The item to enqueue</param>
        /// <param name="priority">The priority of the item (higher values = higher priority)</param>
        public void Enqueue(T item, int priority)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            lock (_lockObject)
            {
                if (!_queues.TryGetValue(priority, out var queue))
                {
                    queue = new ConcurrentQueue<T>();
                    _queues[priority] = queue;
                }

                queue.Enqueue(item);
                Interlocked.Increment(ref _count);
            }
        }

        /// <summary>
        /// Attempts to dequeue the highest priority item
        /// </summary>
        /// <param name="item">The dequeued item</param>
        /// <returns>True if an item was dequeued, false if the queue is empty</returns>
        public bool TryDequeue(out T item)
        {
            item = default(T);

            lock (_lockObject)
            {
                // Find the highest priority queue with items
                foreach (var kvp in _queues)
                {
                    var queue = kvp.Value;
                    if (queue.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref _count);
                        
                        // Remove empty queue to keep dictionary clean
                        if (queue.IsEmpty)
                        {
                            _queues.Remove(kvp.Key);
                        }
                        
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to peek at the highest priority item without removing it
        /// </summary>
        /// <param name="item">The peeked item</param>
        /// <returns>True if an item was peeked, false if the queue is empty</returns>
        public bool TryPeek(out T item)
        {
            item = default(T);

            lock (_lockObject)
            {
                // Find the highest priority queue with items
                foreach (var kvp in _queues)
                {
                    var queue = kvp.Value;
                    if (queue.TryPeek(out item))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Removes all items from the queue
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _queues.Clear();
                _count = 0;
            }
        }

        /// <summary>
        /// Gets all items in the queue grouped by priority
        /// </summary>
        /// <returns>A dictionary mapping priority to items</returns>
        public Dictionary<int, List<T>> GetItemsByPriority()
        {
            lock (_lockObject)
            {
                var result = new Dictionary<int, List<T>>();
                
                foreach (var kvp in _queues)
                {
                    var items = new List<T>();
                    var queue = kvp.Value;
                    
                    // Convert queue to array to get all items
                    var queueArray = queue.ToArray();
                    items.AddRange(queueArray);
                    
                    if (items.Count > 0)
                    {
                        result[kvp.Key] = items;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Gets all items in the queue in priority order
        /// </summary>
        /// <returns>An enumerable of all items ordered by priority</returns>
        public IEnumerable<T> GetAllItems()
        {
            lock (_lockObject)
            {
                var items = new List<T>();
                
                foreach (var kvp in _queues)
                {
                    var queue = kvp.Value;
                    items.AddRange(queue.ToArray());
                }

                return items;
            }
        }

        /// <summary>
        /// Gets the number of items with the specified priority
        /// </summary>
        /// <param name="priority">The priority level</param>
        /// <returns>The number of items with the specified priority</returns>
        public int GetCountByPriority(int priority)
        {
            lock (_lockObject)
            {
                if (_queues.TryGetValue(priority, out var queue))
                {
                    return queue.Count;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets all priority levels currently in the queue
        /// </summary>
        /// <returns>An enumerable of priority levels</returns>
        public IEnumerable<int> GetPriorityLevels()
        {
            lock (_lockObject)
            {
                return _queues.Keys.ToList();
            }
        }

        /// <summary>
        /// Checks if the queue contains items with the specified priority
        /// </summary>
        /// <param name="priority">The priority level to check</param>
        /// <returns>True if the queue contains items with the specified priority</returns>
        public bool ContainsPriority(int priority)
        {
            lock (_lockObject)
            {
                return _queues.ContainsKey(priority) && !_queues[priority].IsEmpty;
            }
        }

        /// <summary>
        /// Gets statistics about the queue
        /// </summary>
        /// <returns>Queue statistics</returns>
        public PriorityQueueStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                var stats = new PriorityQueueStatistics
                {
                    TotalItems = _count,
                    PriorityLevels = _queues.Keys.ToList(),
                    ItemsByPriority = new Dictionary<int, int>()
                };

                foreach (var kvp in _queues)
                {
                    stats.ItemsByPriority[kvp.Key] = kvp.Value.Count;
                }

                if (_queues.Any())
                {
                    stats.HighestPriority = _queues.Keys.First();
                    stats.LowestPriority = _queues.Keys.Last();
                }

                return stats;
            }
        }

        /// <summary>
        /// Comparer for descending order (highest priority first)
        /// </summary>
        private class DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x); // Reverse order for descending
            }
        }
    }

    /// <summary>
    /// Statistics for the priority queue
    /// </summary>
    public class PriorityQueueStatistics
    {
        /// <summary>
        /// Total number of items in the queue
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// List of all priority levels in the queue
        /// </summary>
        public List<int> PriorityLevels { get; set; } = new List<int>();

        /// <summary>
        /// Number of items per priority level
        /// </summary>
        public Dictionary<int, int> ItemsByPriority { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// Highest priority level in the queue
        /// </summary>
        public int HighestPriority { get; set; }

        /// <summary>
        /// Lowest priority level in the queue
        /// </summary>
        public int LowestPriority { get; set; }

        /// <summary>
        /// Number of different priority levels
        /// </summary>
        public int UniquePriorityLevels => PriorityLevels.Count;

        /// <summary>
        /// Average number of items per priority level
        /// </summary>
        public double AverageItemsPerPriority => UniquePriorityLevels > 0 ? (double)TotalItems / UniquePriorityLevels : 0;
    }
}