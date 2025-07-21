using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the type of a chat message
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageType
    {
        /// <summary>
        /// Message sent by the user
        /// </summary>
        User = 0,

        /// <summary>
        /// Message sent by the AI assistant
        /// </summary>
        Assistant = 1,

        /// <summary>
        /// System message (notifications, status updates, etc.)
        /// </summary>
        System = 2
    }
}