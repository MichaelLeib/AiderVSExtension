using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Message model for AgentAPI communication
    /// </summary>
    public class AgentApiMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        public AgentApiMessage()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Request model for sending messages to AgentAPI
    /// </summary>
    public class AgentApiRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        // AgentAPI expects simple string messages
        // File context should be included in the content itself
    }

    /// <summary>
    /// Response model from AgentAPI
    /// </summary>
    public class AgentApiResponse
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Message list response from /messages endpoint
    /// </summary>
    public class AgentApiMessagesResponse
    {
        [JsonPropertyName("messages")]
        public List<AgentApiMessage> Messages { get; set; } = new List<AgentApiMessage>();
    }

    /// <summary>
    /// Server status response from AgentAPI
    /// </summary>
    public class AgentApiStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("aider_version")]
        public string AiderVersion { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("uptime")]
        public TimeSpan Uptime { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Configuration for AgentAPI server
    /// </summary>
    public class AgentApiConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3284;
        public string Model { get; set; } = "sonnet";
        public string AiderExecutablePath { get; set; }
        public string AgentApiExecutablePath { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Event args for AgentAPI events
    /// </summary>
    public class AgentApiEventArgs : EventArgs
    {
        public string EventType { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    /// <summary>
    /// Aider dependency status
    /// </summary>
    public class AiderDependencyStatus
    {
        public bool IsAiderInstalled { get; set; }
        public string AiderVersion { get; set; }
        public string AiderPath { get; set; }
        public bool IsPythonInstalled { get; set; }
        public string PythonVersion { get; set; }
        public string PythonPath { get; set; }
        public List<string> MissingDependencies { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }
}