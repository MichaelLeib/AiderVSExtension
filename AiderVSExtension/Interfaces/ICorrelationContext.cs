using System;
using System.Collections.Generic;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for managing correlation context data
    /// </summary>
    public interface ICorrelationContext
    {
        /// <summary>
        /// Gets the correlation ID for the current context
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the timestamp when the context was created
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets additional context properties
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Sets a property in the correlation context
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        void SetProperty(string key, object value);

        /// <summary>
        /// Gets a property from the correlation context
        /// </summary>
        /// <typeparam name="T">The expected type of the property</typeparam>
        /// <param name="key">The property key</param>
        /// <returns>The property value, or default if not found</returns>
        T GetProperty<T>(string key);

        /// <summary>
        /// Checks if a property exists in the context
        /// </summary>
        /// <param name="key">The property key</param>
        /// <returns>True if the property exists</returns>
        bool HasProperty(string key);

        /// <summary>
        /// Removes a property from the context
        /// </summary>
        /// <param name="key">The property key</param>
        /// <returns>True if the property was removed</returns>
        bool RemoveProperty(string key);
    }
}
