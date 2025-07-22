using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the type of change made to a file
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChangeType
    {
        /// <summary>
        /// Content was added to the file
        /// </summary>
        Added = 0,

        /// <summary>
        /// Content was removed from the file
        /// </summary>
        Removed = 1,

        /// <summary>
        /// Content was modified in the file
        /// </summary>
        Modified = 2,

        /// <summary>
        /// Content was deleted from the file
        /// </summary>
        Deleted = 3
    }
}