using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security;

namespace AiderVSExtension.Security
{
    /// <summary>
    /// Provides secure process execution utilities to prevent command injection
    /// </summary>
    public static class ProcessSecurity
    {
        // Dangerous characters that could be used for command injection
        private static readonly char[] DangerousChars = { '|', '&', ';', '$', '`', '\\', '"', '\'', '<', '>', '\n', '\r', '\0' };
        
        // Regex for valid API key formats
        private static readonly Regex ValidApiKeyPattern = new Regex(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);
        
        // Regex for valid provider names
        private static readonly Regex ValidProviderPattern = new Regex(@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);
        
        // Regex for valid model names
        private static readonly Regex ValidModelNamePattern = new Regex(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);
        
        /// <summary>
        /// Sanitizes a command line argument to prevent injection attacks
        /// </summary>
        /// <param name="argument">The argument to sanitize</param>
        /// <returns>Sanitized argument</returns>
        public static string SanitizeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
                return string.Empty;

            // Remove dangerous characters
            foreach (var dangerChar in DangerousChars)
            {
                argument = argument.Replace(dangerChar.ToString(), "");
            }

            // Trim and limit length
            argument = argument.Trim();
            if (argument.Length > 500) // Reasonable limit for arguments
            {
                throw new SecurityException("Argument too long - potential security risk");
            }

            return argument;
        }

        /// <summary>
        /// Validates an API key format for security
        /// </summary>
        /// <param name="apiKey">The API key to validate</param>
        /// <returns>True if the API key is safe to use</returns>
        public static bool IsValidApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            // Check length (reasonable bounds for API keys)
            if (apiKey.Length < 10 || apiKey.Length > 500)
                return false;

            // Check for dangerous characters
            if (apiKey.IndexOfAny(DangerousChars) != -1)
                return false;

            // Validate pattern
            return ValidApiKeyPattern.IsMatch(apiKey);
        }

        /// <summary>
        /// Validates a provider name for security
        /// </summary>
        /// <param name="provider">The provider name to validate</param>
        /// <returns>True if the provider name is safe to use</returns>
        public static bool IsValidProvider(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return false;

            // Check length
            if (provider.Length > 50)
                return false;

            // Check for dangerous characters
            if (provider.IndexOfAny(DangerousChars) != -1)
                return false;

            // Validate pattern
            return ValidProviderPattern.IsMatch(provider);
        }

        /// <summary>
        /// Validates a model name for security
        /// </summary>
        /// <param name="modelName">The model name to validate</param>
        /// <returns>True if the model name is safe to use</returns>
        public static bool IsValidModelName(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return false;

            // Check length
            if (modelName.Length > 100)
                return false;

            // Check for dangerous characters
            if (modelName.IndexOfAny(DangerousChars) != -1)
                return false;

            // Validate pattern
            return ValidModelNamePattern.IsMatch(modelName);
        }

        /// <summary>
        /// Creates a secure process argument list
        /// </summary>
        /// <param name="arguments">Dictionary of argument names and values</param>
        /// <returns>Validated and sanitized argument list</returns>
        public static List<string> CreateSecureArgumentList(Dictionary<string, string> arguments)
        {
            var args = new List<string>();

            foreach (var kvp in arguments)
            {
                var key = SanitizeArgument(kvp.Key);
                var value = SanitizeArgument(kvp.Value);

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                // For key-value pairs, use separate arguments to avoid injection
                args.Add($"--{key}");
                args.Add(value);
            }

            return args;
        }

        /// <summary>
        /// Validates that a command is in the allowlist of safe commands
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <returns>True if the command is allowed</returns>
        public static bool IsAllowedCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return false;

            var allowedCommands = new[]
            {
                "pip", "pip3", "python", "python3", "agentapi", "aider"
            };

            var commandName = System.IO.Path.GetFileNameWithoutExtension(command.ToLowerInvariant());
            return allowedCommands.Contains(commandName);
        }

        /// <summary>
        /// Validates provider-API key combination for additional security
        /// </summary>
        /// <param name="provider">The provider name</param>
        /// <param name="apiKey">The API key</param>
        /// <returns>True if the combination is valid and secure</returns>
        public static bool IsValidProviderApiKeyCombination(string provider, string apiKey)
        {
            if (!IsValidProvider(provider) || !IsValidApiKey(apiKey))
                return false;

            // Provider-specific API key validation
            return provider.ToLowerInvariant() switch
            {
                "openai" or "chatgpt" => apiKey.StartsWith("sk-") && apiKey.Length >= 40,
                "claude" or "anthropic" => apiKey.StartsWith("sk-ant-") && apiKey.Length >= 50,
                "ollama" => apiKey.Length <= 100, // Ollama keys can be shorter or empty
                _ => apiKey.Length >= 10 // Generic validation
            };
        }
    }
}