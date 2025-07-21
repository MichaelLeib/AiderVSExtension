namespace AiderVSExtension.Models
{
    /// <summary>
    /// Contains constant definitions for configuration keys and default values
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Configuration keys for Visual Studio Settings Store
        /// </summary>
        public static class ConfigurationKeys
        {
            /// <summary>
            /// Root collection name for extension settings
            /// </summary>
            public const string RootCollection = "AiderVSExtension";

            /// <summary>
            /// Key for selected AI provider
            /// </summary>
            public const string SelectedProvider = "SelectedProvider";

            /// <summary>
            /// Key for ChatGPT API key
            /// </summary>
            public const string ChatGptApiKey = "ChatGptApiKey";

            /// <summary>
            /// Key for Claude API key
            /// </summary>
            public const string ClaudeApiKey = "ClaudeApiKey";

            /// <summary>
            /// Key for Ollama endpoint URL
            /// </summary>
            public const string OllamaEndpointUrl = "OllamaEndpointUrl";

            /// <summary>
            /// Key for Ollama model name
            /// </summary>
            public const string OllamaModelName = "OllamaModelName";

            /// <summary>
            /// Key for connection timeout setting
            /// </summary>
            public const string ConnectionTimeout = "ConnectionTimeout";

            /// <summary>
            /// Key for maximum retry attempts
            /// </summary>
            public const string MaxRetries = "MaxRetries";

            /// <summary>
            /// Key for chat history persistence setting
            /// </summary>
            public const string PersistChatHistory = "PersistChatHistory";

            /// <summary>
            /// Key for diff visualization enabled setting
            /// </summary>
            public const string DiffVisualizationEnabled = "DiffVisualizationEnabled";

            /// <summary>
            /// Key for AI completion enabled setting
            /// </summary>
            public const string AICompletionEnabled = "AICompletionEnabled";

            /// <summary>
            /// Key for completion fallback to IntelliSense setting
            /// </summary>
            public const string CompletionFallbackEnabled = "CompletionFallbackEnabled";

            /// <summary>
            /// Key for Aider WebSocket endpoint URL
            /// </summary>
            public const string AiderEndpoint = "AiderEndpoint";

            /// <summary>
            /// Key for Aider HTTP endpoint URL
            /// </summary>
            public const string AiderHttpEndpoint = "AiderHttpEndpoint";

            /// <summary>
            /// Key for message timeout setting
            /// </summary>
            public const string MessageTimeout = "MessageTimeout";

            /// <summary>
            /// Key for retry delay setting
            /// </summary>
            public const string RetryDelay = "RetryDelay";

            /// <summary>
            /// Key for heartbeat interval setting
            /// </summary>
            public const string HeartbeatInterval = "HeartbeatInterval";

            /// <summary>
            /// Key for reconnect delay setting
            /// </summary>
            public const string ReconnectDelay = "ReconnectDelay";

            /// <summary>
            /// Key for maximum consecutive failures setting
            /// </summary>
            public const string MaxConsecutiveFailures = "MaxConsecutiveFailures";
        }

        /// <summary>
        /// Default values for configuration settings
        /// </summary>
        public static class DefaultValues
        {
            /// <summary>
            /// Default AI provider
            /// </summary>
            public const AIProvider DefaultProvider = AIProvider.ChatGPT;

            /// <summary>
            /// Default connection timeout in seconds
            /// </summary>
            public const int DefaultTimeoutSeconds = 30;

            /// <summary>
            /// Default maximum retry attempts
            /// </summary>
            public const int DefaultMaxRetries = 3;

            /// <summary>
            /// Default Ollama endpoint URL
            /// </summary>
            public const string DefaultOllamaEndpoint = "http://localhost:11434";

            /// <summary>
            /// Default Ollama model name
            /// </summary>
            public const string DefaultOllamaModel = "llama2";

            /// <summary>
            /// Default chat history persistence setting
            /// </summary>
            public const bool DefaultPersistChatHistory = true;

            /// <summary>
            /// Default diff visualization enabled setting
            /// </summary>
            public const bool DefaultDiffVisualizationEnabled = true;

            /// <summary>
            /// Default AI completion enabled setting
            /// </summary>
            public const bool DefaultAICompletionEnabled = true;

            /// <summary>
            /// Default completion fallback enabled setting
            /// </summary>
            public const bool DefaultCompletionFallbackEnabled = true;

            /// <summary>
            /// Default Aider WebSocket endpoint URL
            /// </summary>
            public const string DefaultAiderEndpoint = "ws://localhost:8765";

            /// <summary>
            /// Default Aider HTTP endpoint URL
            /// </summary>
            public const string DefaultAiderHttpEndpoint = "http://localhost:8766";

            /// <summary>
            /// Default message timeout in milliseconds
            /// </summary>
            public const int DefaultMessageTimeoutMs = 60000;

            /// <summary>
            /// Default retry delay in milliseconds
            /// </summary>
            public const int DefaultRetryDelayMs = 1000;

            /// <summary>
            /// Default heartbeat interval in milliseconds
            /// </summary>
            public const int DefaultHeartbeatIntervalMs = 30000;

            /// <summary>
            /// Default reconnect delay in milliseconds
            /// </summary>
            public const int DefaultReconnectDelayMs = 5000;

            /// <summary>
            /// Default maximum consecutive failures
            /// </summary>
            public const int DefaultMaxConsecutiveFailures = 5;

            /// <summary>
            /// Maximum message content length
            /// </summary>
            public const int MaxMessageContentLength = 50000;

            /// <summary>
            /// Maximum number of context lines for diff changes
            /// </summary>
            public const int MaxDiffContextLines = 5;

            /// <summary>
            /// Maximum number of chat messages to persist
            /// </summary>
            public const int MaxPersistedMessages = 1000;

            /// <summary>
            /// Maximum number of file references per message
            /// </summary>
            public const int MaxFileReferencesPerMessage = 50;

            /// <summary>
            /// Maximum file size for content extraction in bytes
            /// </summary>
            public const long MaxFileContentSize = 1024 * 1024; // 1MB

            /// <summary>
            /// Maximum API key length for validation
            /// </summary>
            public const int MaxApiKeyLength = 500;

            /// <summary>
            /// Minimum API key length for validation
            /// </summary>
            public const int MinApiKeyLength = 10;
        }

        /// <summary>
        /// UI-related constants
        /// </summary>
        public static class UI
        {
            /// <summary>
            /// Chat tool window title
            /// </summary>
            public const string ChatWindowTitle = "Aider AI Chat";

            /// <summary>
            /// Context menu trigger character
            /// </summary>
            public const char ContextMenuTrigger = '#';

            /// <summary>
            /// Default chat input placeholder text
            /// </summary>
            public const string ChatInputPlaceholder = "Type your message... (Press # for context menu)";

            /// <summary>
            /// Error quick fix menu text
            /// </summary>
            public const string FixWithAiderText = "Fix with Aider";

            /// <summary>
            /// Add to chat menu text
            /// </summary>
            public const string AddToChatText = "Add to Aider Chat";

            /// <summary>
            /// Output window pane name
            /// </summary>
            public const string OutputPaneName = "Aider VS Extension";
        }

        /// <summary>
        /// File and path-related constants
        /// </summary>
        public static class Files
        {
            /// <summary>
            /// Chat history file name
            /// </summary>
            public const string ChatHistoryFileName = "chat_history.json";

            /// <summary>
            /// Configuration file name
            /// </summary>
            public const string ConfigurationFileName = "aider_config.json";

            /// <summary>
            /// Extension data folder name
            /// </summary>
            public const string ExtensionDataFolder = "AiderVSExtension";

            /// <summary>
            /// Supported code file extensions for context
            /// </summary>
            public static readonly string[] SupportedCodeExtensions = 
            {
                ".cs", ".vb", ".cpp", ".c", ".h", ".hpp", ".js", ".ts", ".jsx", ".tsx",
                ".py", ".java", ".php", ".rb", ".go", ".rs", ".swift", ".kt", ".scala",
                ".html", ".css", ".scss", ".less", ".xml", ".json", ".yaml", ".yml",
                ".sql", ".md", ".txt", ".config", ".xaml", ".razor"
            };
        }

        /// <summary>
        /// Network and API-related constants
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// OpenAI API base URL
            /// </summary>
            public const string OpenAIApiBaseUrl = "https://api.openai.com/v1";

            /// <summary>
            /// Anthropic API base URL
            /// </summary>
            public const string AnthropicApiBaseUrl = "https://api.anthropic.com/v1";

            /// <summary>
            /// Default user agent for API requests
            /// </summary>
            public const string DefaultUserAgent = "AiderVSExtension/1.0";

            /// <summary>
            /// Maximum request size in bytes
            /// </summary>
            public const int MaxRequestSizeBytes = 1024 * 1024; // 1MB

            /// <summary>
            /// Request retry delay in milliseconds
            /// </summary>
            public const int RetryDelayMilliseconds = 1000;
        }

        /// <summary>
        /// Validation-related constants
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Valid OpenAI API key prefix
            /// </summary>
            public const string OpenAIApiKeyPrefix = "sk-";

            /// <summary>
            /// Valid Anthropic API key prefix
            /// </summary>
            public const string AnthropicApiKeyPrefix = "sk-ant-";

            /// <summary>
            /// Valid Claude model prefix
            /// </summary>
            public const string ClaudeModelPrefix = "claude-";

            /// <summary>
            /// Valid GPT model prefixes
            /// </summary>
            public static readonly string[] ValidGptModelPrefixes = { "gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o" };

            /// <summary>
            /// Maximum age for timestamps in years
            /// </summary>
            public const int MaxTimestampAgeYears = 10;

            /// <summary>
            /// Clock skew tolerance in minutes
            /// </summary>
            public const int ClockSkewToleranceMinutes = 5;

            /// <summary>
            /// Maximum model name length
            /// </summary>
            public const int MaxModelNameLength = 100;

            /// <summary>
            /// Minimum model name length
            /// </summary>
            public const int MinModelNameLength = 2;

            /// <summary>
            /// Invalid filename characters pattern
            /// </summary>
            public const string InvalidFilenameCharsPattern = @"[<>:""/\\|?*]";

            /// <summary>
            /// Maximum validation error message length
            /// </summary>
            public const int MaxValidationErrorLength = 1000;

            /// <summary>
            /// Maximum file path length for security validation
            /// </summary>
            public const int MaxFilePathLength = 2000;
        }
    }
}