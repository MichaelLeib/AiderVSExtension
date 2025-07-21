using System;
using System.Text.Json;

namespace AiderVSExtension.Security
{
    /// <summary>
    /// Provides secure JSON serialization with protection against common attacks
    /// </summary>
    public static class SecureJsonSerializer
    {
        /// <summary>
        /// Default secure JSON serializer options
        /// </summary>
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 10, // Prevent deep object nesting attacks
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Add converter for enums to prevent string injection
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        /// <summary>
        /// Strict options for untrusted input with additional security
        /// </summary>
        private static readonly JsonSerializerOptions StrictOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 5, // More restrictive depth for untrusted input
            AllowTrailingCommas = false, // Stricter parsing
            ReadCommentHandling = JsonCommentHandling.Disallow, // No comments allowed
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Securely serializes an object to JSON
        /// </summary>
        /// <typeparam name="T">The type to serialize</typeparam>
        /// <param name="value">The value to serialize</param>
        /// <param name="strict">Whether to use strict serialization options</param>
        /// <returns>JSON string representation</returns>
        public static string Serialize<T>(T value, bool strict = false)
        {
            if (value == null)
                return "null";

            try
            {
                var options = strict ? StrictOptions : DefaultOptions;
                return JsonSerializer.Serialize(value, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Securely deserializes JSON to an object with input validation
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <param name="strict">Whether to use strict deserialization options</param>
        /// <returns>Deserialized object</returns>
        public static T Deserialize<T>(string json, bool strict = false)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            // Basic input validation to prevent obviously malicious input
            if (json.Length > 1_000_000) // 1MB limit
            {
                throw new ArgumentException("JSON string too large - potential DoS attack");
            }

            // Check for suspicious patterns that might indicate injection attempts
            if (ContainsSuspiciousPatterns(json))
            {
                throw new System.Security.SecurityException("JSON contains suspicious patterns");
            }

            try
            {
                var options = strict ? StrictOptions : DefaultOptions;
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error during deserialization: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tries to deserialize JSON safely, returning false on failure
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <param name="result">The deserialized object if successful</param>
        /// <param name="strict">Whether to use strict deserialization options</param>
        /// <returns>True if deserialization succeeded</returns>
        public static bool TryDeserialize<T>(string json, out T result, bool strict = false)
        {
            result = default(T);

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                result = Deserialize<T>(json, strict);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for suspicious patterns in JSON that might indicate attacks
        /// </summary>
        /// <param name="json">The JSON string to check</param>
        /// <returns>True if suspicious patterns are found</returns>
        private static bool ContainsSuspiciousPatterns(string json)
        {
            // Check for excessive nesting (count opening braces/brackets)
            int braceCount = 0;
            int maxNesting = 0;
            int currentNesting = 0;

            foreach (char c in json)
            {
                if (c == '{' || c == '[')
                {
                    currentNesting++;
                    maxNesting = Math.Max(maxNesting, currentNesting);
                    braceCount++;
                }
                else if (c == '}' || c == ']')
                {
                    currentNesting--;
                }

                // Prevent extremely deep nesting
                if (maxNesting > 20)
                    return true;

                // Prevent excessive number of objects/arrays
                if (braceCount > 1000)
                    return true;
            }

            // Check for suspicious string patterns
            var suspiciousPatterns = new[]
            {
                "__proto__", "constructor", "prototype",
                "eval(", "function(", "=>", "javascript:",
                "<script", "document.", "window.",
                "process.env", "require(", "import("
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (json.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}