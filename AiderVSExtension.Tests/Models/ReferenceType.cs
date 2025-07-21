using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the type of a file reference in chat context
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReferenceType
    {
        /// <summary>
        /// Reference to an entire file
        /// </summary>
        File = 0,

        /// <summary>
        /// Reference to a selected portion of text in a file
        /// </summary>
        Selection = 1,

        /// <summary>
        /// Reference to an error or diagnostic in a file
        /// </summary>
        Error = 2,

        /// <summary>
        /// Reference to clipboard content
        /// </summary>
        Clipboard = 3,

        /// <summary>
        /// Reference to a Git branch or commit
        /// </summary>
        GitBranch = 4,

        /// <summary>
        /// Reference to web search results
        /// </summary>
        WebSearch = 5,

        /// <summary>
        /// Reference to documentation
        /// </summary>
        Documentation = 6
    }
}