using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Provides validation utilities for data models
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates an object using Data Annotations
        /// </summary>
        /// <param name="obj">Object to validate</param>
        /// <returns>Validation results</returns>
        public static IEnumerable<ValidationResult> ValidateObject(object obj)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(obj);
            
            Validator.TryValidateObject(obj, validationContext, validationResults, true);
            
            return validationResults;
        }

        /// <summary>
        /// Validates an object and returns formatted error messages
        /// </summary>
        /// <param name="obj">Object to validate</param>
        /// <returns>List of error messages</returns>
        public static List<string> GetValidationErrors(object obj)
        {
            var validationResults = ValidateObject(obj);
            return validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToList();
        }

        /// <summary>
        /// Validates that a string is not null or empty
        /// </summary>
        /// <param name="value">String value to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return $"{fieldName} is required";
            }
            return null;
        }

        /// <summary>
        /// Validates that a string length is within specified bounds
        /// </summary>
        /// <param name="value">String value to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="minLength">Minimum length (optional)</param>
        /// <param name="maxLength">Maximum length (optional)</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateStringLength(string value, string fieldName, int minLength = -1, int maxLength = -1)
        {
            if (value == null) return null;

            if (minLength != -1 && value.Length < minLength)
            {
                return $"{fieldName} must be at least {minLength} characters long";
            }

            if (maxLength != -1 && value.Length > maxLength)
            {
                return $"{fieldName} cannot exceed {maxLength} characters";
            }

            return null;
        }

        /// <summary>
        /// Validates that a numeric value is within specified range
        /// </summary>
        /// <param name="value">Numeric value to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="min">Minimum value (optional)</param>
        /// <param name="max">Maximum value (optional)</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateRange(int value, string fieldName, int min = int.MinValue, int max = int.MaxValue)
        {
            if (min != int.MinValue && value < min)
            {
                return $"{fieldName} must be at least {min}";
            }

            if (max != int.MaxValue && value > max)
            {
                return $"{fieldName} cannot exceed {max}";
            }

            return null;
        }

        /// <summary>
        /// Validates that a URL is properly formatted
        /// </summary>
        /// <param name="url">URL to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="requireHttps">Whether HTTPS is required</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateUrl(string url, string fieldName, bool requireHttps = false)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return $"{fieldName} is required";
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return $"{fieldName} must be a valid URL";
            }

            if (requireHttps && uri.Scheme != "https")
            {
                return $"{fieldName} must use HTTPS";
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return $"{fieldName} must use HTTP or HTTPS protocol";
            }

            return null;
        }

        /// <summary>
        /// Validates that a file path is properly formatted with comprehensive security checks
        /// </summary>
        /// <param name="filePath">File path to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="mustExist">Whether the file must exist</param>
        /// <param name="allowedExtensions">List of allowed file extensions (null for any)</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateFilePath(string filePath, string fieldName, bool mustExist = false, string[] allowedExtensions = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return $"{fieldName} is required";
            }

            try
            {
                // Basic format checks
                if (filePath.Length > Constants.Validation.MaxFilePathLength)
                {
                    return $"{fieldName} path exceeds maximum length of {Constants.Validation.MaxFilePathLength} characters";
                }

                // Check for dangerous characters and patterns
                var invalidChars = Path.GetInvalidPathChars();
                if (filePath.IndexOfAny(invalidChars) >= 0)
                {
                    return $"{fieldName} contains invalid path characters";
                }

                // Security check: Prevent directory traversal attacks
                if (filePath.Contains("..") || filePath.Contains("~"))
                {
                    return $"{fieldName} cannot contain relative path components (.., ~)";
                }

                // Check for reserved names on Windows
                var fileName = Path.GetFileName(filePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
                    if (reservedNames.Contains(nameWithoutExtension))
                    {
                        return $"{fieldName} cannot use reserved file name '{nameWithoutExtension}'";
                    }
                }

                // Get absolute path for further validation
                var fullPath = Path.GetFullPath(filePath);

                // Security check: Ensure path is rooted and not suspicious
                if (!Path.IsPathRooted(fullPath))
                {
                    return $"{fieldName} must be an absolute path";
                }

                // Check path length limits (Windows has 260 char limit for many operations)
                if (fullPath.Length > 260)
                {
                    return $"{fieldName} full path exceeds Windows path length limit (260 characters)";
                }

                // Extension validation if specified
                if (allowedExtensions != null && allowedExtensions.Length > 0)
                {
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (!allowedExtensions.Any(ext => ext.ToLowerInvariant() == extension))
                    {
                        return $"{fieldName} must have one of the following extensions: {string.Join(", ", allowedExtensions)}";
                    }
                }

                // Existence check
                if (mustExist && !File.Exists(fullPath))
                {
                    return $"{fieldName} must point to an existing file";
                }

                // Security check: Ensure we're not accessing system directories (basic check)
                var systemPaths = new[] { 
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };

                foreach (var systemPath in systemPaths)
                {
                    if (!string.IsNullOrEmpty(systemPath) && fullPath.StartsWith(systemPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Allow read access to system directories but log for security
                        System.Diagnostics.Debug.WriteLine($"Security notice: Accessing system path {fullPath}");
                    }
                }
            }
            catch (ArgumentException ex)
            {
                return $"{fieldName} contains invalid characters: {ex.Message}";
            }
            catch (NotSupportedException ex)
            {
                return $"{fieldName} format is not supported: {ex.Message}";
            }
            catch (PathTooLongException)
            {
                return $"{fieldName} path is too long";
            }
            catch (Exception ex)
            {
                return $"{fieldName} is not a valid file path: {ex.Message}";
            }

            return null;
        }

        /// <summary>
        /// Validates that a directory path is properly formatted with security checks
        /// </summary>
        /// <param name="directoryPath">Directory path to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="mustExist">Whether the directory must exist</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateDirectoryPath(string directoryPath, string fieldName, bool mustExist = false)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return $"{fieldName} is required";
            }

            try
            {
                // Basic format checks
                if (directoryPath.Length > Constants.Validation.MaxFilePathLength)
                {
                    return $"{fieldName} path exceeds maximum length of {Constants.Validation.MaxFilePathLength} characters";
                }

                // Check for dangerous characters and patterns
                var invalidChars = Path.GetInvalidPathChars();
                if (directoryPath.IndexOfAny(invalidChars) >= 0)
                {
                    return $"{fieldName} contains invalid path characters";
                }

                // Security check: Prevent directory traversal attacks
                if (directoryPath.Contains(".."))
                {
                    return $"{fieldName} cannot contain relative path components (..)";
                }

                // Get absolute path for further validation
                var fullPath = Path.GetFullPath(directoryPath);

                // Security check: Ensure path is rooted
                if (!Path.IsPathRooted(fullPath))
                {
                    return $"{fieldName} must be an absolute path";
                }

                // Existence check
                if (mustExist && !Directory.Exists(fullPath))
                {
                    return $"{fieldName} must point to an existing directory";
                }
            }
            catch (ArgumentException ex)
            {
                return $"{fieldName} contains invalid characters: {ex.Message}";
            }
            catch (NotSupportedException ex)
            {
                return $"{fieldName} format is not supported: {ex.Message}";
            }
            catch (PathTooLongException)
            {
                return $"{fieldName} path is too long";
            }
            catch (Exception ex)
            {
                return $"{fieldName} is not a valid directory path: {ex.Message}";
            }

            return null;
        }

        /// <summary>
        /// Validates that an API key has the expected format
        /// </summary>
        /// <param name="apiKey">API key to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="provider">AI provider type for provider-specific validation</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateApiKey(string apiKey, string fieldName, AIProvider provider)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return $"{fieldName} is required";
            }

            // Provider-specific API key validation using constants
            switch (provider)
            {
                case AIProvider.ChatGPT:
                    if (!apiKey.StartsWith(Constants.Validation.OpenAIApiKeyPrefix))
                    {
                        return $"{fieldName} must be a valid OpenAI API key (starts with '{Constants.Validation.OpenAIApiKeyPrefix}')";
                    }
                    if (apiKey.Length < Constants.DefaultValues.MinApiKeyLength)
                    {
                        return $"{fieldName} appears to be too short for a valid OpenAI API key";
                    }
                    break;

                case AIProvider.Claude:
                    if (!apiKey.StartsWith(Constants.Validation.AnthropicApiKeyPrefix))
                    {
                        return $"{fieldName} must be a valid Anthropic API key (starts with '{Constants.Validation.AnthropicApiKeyPrefix}')";
                    }
                    if (apiKey.Length < Constants.DefaultValues.MinApiKeyLength)
                    {
                        return $"{fieldName} appears to be too short for a valid Anthropic API key";
                    }
                    break;

                case AIProvider.Ollama:
                    // Ollama typically doesn't require API keys
                    break;
            }

            return null;
        }

        /// <summary>
        /// Validates that a timestamp is within reasonable bounds
        /// </summary>
        /// <param name="timestamp">Timestamp to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="allowFuture">Whether future timestamps are allowed</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateTimestamp(DateTime timestamp, string fieldName, bool allowFuture = false)
        {
            if (timestamp == default)
            {
                return $"{fieldName} is required";
            }

            var now = DateTime.UtcNow;
            
            // Check if timestamp is too far in the past
            if (timestamp < now.AddYears(-Constants.Validation.MaxTimestampAgeYears))
            {
                return $"{fieldName} cannot be more than {Constants.Validation.MaxTimestampAgeYears} years in the past";
            }

            // Check if timestamp is in the future (if not allowed)
            if (!allowFuture && timestamp > now.AddMinutes(Constants.Validation.ClockSkewToleranceMinutes))
            {
                return $"{fieldName} cannot be in the future";
            }

            return null;
        }

        /// <summary>
        /// Validates that a model name is valid for the specified provider
        /// </summary>
        /// <param name="modelName">Model name to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="provider">AI provider type</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateModelName(string modelName, string fieldName, AIProvider provider)
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return $"{fieldName} is required";
            }

            // Basic format validation using constants
            if (modelName.Length > Constants.Validation.MaxModelNameLength)
            {
                return $"{fieldName} cannot exceed {Constants.Validation.MaxModelNameLength} characters";
            }

            // Check for invalid characters
            if (Regex.IsMatch(modelName, Constants.Validation.InvalidFilenameCharsPattern))
            {
                return $"{fieldName} contains invalid characters";
            }

            // Provider-specific validation using constants
            switch (provider)
            {
                case AIProvider.ChatGPT:
                    if (!Constants.Validation.ValidGptModelPrefixes.Any(m => modelName.StartsWith(m)))
                    {
                        return $"{fieldName} must be a valid OpenAI model name";
                    }
                    break;

                case AIProvider.Claude:
                    if (!modelName.StartsWith(Constants.Validation.ClaudeModelPrefix))
                    {
                        return $"{fieldName} must be a valid Claude model name (starts with '{Constants.Validation.ClaudeModelPrefix}')";
                    }
                    break;

                case AIProvider.Ollama:
                    // Ollama model names are more flexible
                    if (modelName.Length < Constants.Validation.MinModelNameLength)
                    {
                        return $"{fieldName} must be at least {Constants.Validation.MinModelNameLength} characters long";
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Validates a collection of objects
        /// </summary>
        /// <typeparam name="T">Type of objects in the collection</typeparam>
        /// <param name="collection">Collection to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="validator">Validator function for individual items</param>
        /// <param name="maxItems">Maximum number of items allowed</param>
        /// <returns>List of validation error messages</returns>
        public static List<string> ValidateCollection<T>(IEnumerable<T>? collection, string fieldName, 
            Func<T, int, List<string>> validator, int maxItems = -1)
        {
            var errors = new List<string>();

            if (collection == null)
            {
                return errors;
            }

            var items = collection.ToList();
            
            if (maxItems != -1 && items.Count > maxItems)
            {
                errors.Add($"{fieldName} cannot contain more than {maxItems} items");
            }

            for (int i = 0; i < items.Count; i++)
            {
                var itemErrors = validator(items[i], i);
                foreach (var error in itemErrors)
                {
                    errors.Add($"{fieldName}[{i}]: {error}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates that a GUID is not empty
        /// </summary>
        /// <param name="guid">GUID to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateGuid(Guid guid, string fieldName)
        {
            if (guid == Guid.Empty)
            {
                return $"{fieldName} cannot be empty";
            }
            return null;
        }

        /// <summary>
        /// Validates that a string represents a valid GUID
        /// </summary>
        /// <param name="guidString">String to validate as GUID</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateGuidString(string guidString, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(guidString))
            {
                return $"{fieldName} is required";
            }

            if (!Guid.TryParse(guidString, out var guid))
            {
                return $"{fieldName} must be a valid GUID";
            }

            return ValidateGuid(guid, fieldName);
        }

        /// <summary>
        /// Validates AI service responses for security and format compliance
        /// </summary>
        /// <param name="response">The response object to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>List of validation error messages</returns>
        public static List<string> ValidateAIResponse(object response, string fieldName)
        {
            var errors = new List<string>();

            if (response == null)
            {
                errors.Add($"{fieldName} cannot be null");
                return errors;
            }

            // Check for JSON injection in string responses
            if (response is string stringResponse)
            {
                errors.AddRange(ValidateJsonStringForInjection(stringResponse, fieldName));
            }

            return errors;
        }

        /// <summary>
        /// Validates a JSON string for potential injection attacks
        /// </summary>
        /// <param name="jsonString">JSON string to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>List of validation error messages</returns>
        public static List<string> ValidateJsonStringForInjection(string jsonString, string fieldName)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(jsonString))
            {
                return errors;
            }

            // Check for excessive length that could indicate attack
            if (jsonString.Length > Constants.DefaultValues.MaxMessageContentLength * 2)
            {
                errors.Add($"{fieldName} response is excessively long and may be malicious");
            }

            // Check for suspicious patterns that could indicate injection
            var suspiciousPatterns = new[]
            {
                @"<script[^>]*>",           // Script tags
                @"javascript:",            // JavaScript protocol
                @"data:text/html",         // Data URLs with HTML
                @"eval\s*\(",             // Eval function calls
                @"setTimeout\s*\(",       // setTimeout calls
                @"setInterval\s*\(",      // setInterval calls
                @"document\.",            // DOM manipulation
                @"window\.",              // Window object access
                @"location\.",            // Location object access
                @"cookie",                // Cookie access
                @"localStorage",          // Local storage access
                @"sessionStorage"         // Session storage access
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (Regex.IsMatch(jsonString, pattern, RegexOptions.IgnoreCase))
                {
                    errors.Add($"{fieldName} contains potentially malicious content");
                    break; // Only report once
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates content for potential XSS vectors
        /// </summary>
        /// <param name="content">Content to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateContentForXSS(string content, string fieldName)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            // Check for HTML/XML tags that could be dangerous
            var dangerousTags = new[]
            {
                "<script", "<object", "<embed", "<applet", "<meta", "<iframe",
                "<form", "<input", "<link", "<style", "javascript:", "vbscript:",
                "onload=", "onerror=", "onclick=", "onmouseover="
            };

            foreach (var tag in dangerousTags)
            {
                if (content.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return $"{fieldName} contains potentially unsafe HTML/script content";
                }
            }

            return null;
        }

        /// <summary>
        /// Validates that user input doesn't contain command injection vectors
        /// </summary>
        /// <param name="input">User input to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateAgainstCommandInjection(string input, string fieldName)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            // Check for command injection patterns
            var commandPatterns = new[]
            {
                @"[;&|`$]",              // Command separators and variable expansion
                @"\\x[0-9a-fA-F]{2}",    // Hex escape sequences
                @"\\[0-7]{1,3}",         // Octal escape sequences
                @"\$\(",                 // Command substitution
                @"`[^`]*`",              // Backtick command execution
                @">\s*\/",               // Output redirection to files
                @"<\s*\/",               // Input redirection from files
                @"\|\s*\/",              // Pipe to commands
                @"&&",                   // Command chaining
                @"\|\|",                 // OR command chaining
                @"chmod\s+",             // File permission changes
                @"rm\s+",                // File deletion
                @"del\s+",               // Windows file deletion
                @"format\s+",            // Disk formatting
                @"shutdown\s+",          // System shutdown
                @"reboot\s+",            // System reboot
                @"sudo\s+",              // Privilege escalation
                @"su\s+",                // User switching
                @"passwd\s+",            // Password changes
                @"net\s+user",           // Windows user management
                @"reg\s+add",            // Registry modification
                @"reg\s+delete"          // Registry deletion
            };

            foreach (var pattern in commandPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return $"{fieldName} contains potentially dangerous command sequences";
                }
            }

            return null;
        }

        /// <summary>
        /// Validates user input for SQL injection patterns
        /// </summary>
        /// <param name="input">User input to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <returns>Validation error message, or null if valid</returns>
        public static string ValidateAgainstSQLInjection(string input, string fieldName)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            // Check for SQL injection patterns
            var sqlPatterns = new[]
            {
                @"'[^']*'",                    // Single quoted strings
                @"""[^""]*""",                 // Double quoted strings
                @";\s*(DROP|DELETE|INSERT|UPDATE|CREATE|ALTER|EXEC|EXECUTE)\s+",  // Dangerous SQL commands
                @"UNION\s+SELECT",             // Union-based injection
                @"1\s*=\s*1",                 // Always true conditions
                @"1\s*=\s*2",                 // Always false conditions
                @"OR\s+1\s*=\s*1",           // OR-based injection
                @"AND\s+1\s*=\s*1",          // AND-based injection
                @"--",                        // SQL comments
                @"/\*.*\*/",                  // SQL block comments
                @"xp_cmdshell",               // SQL Server command execution
                @"sp_executesql",             // SQL Server dynamic SQL
                @"CAST\s*\(",                 // Type casting
                @"CONVERT\s*\(",              // Type conversion
                @"CHAR\s*\(",                 // Character functions
                @"ASCII\s*\(",                // ASCII functions
                @"SUBSTRING\s*\(",            // String functions
                @"LEN\s*\(",                  // Length functions
                @"WAITFOR\s+DELAY"            // Time-based injection
            };

            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    return $"{fieldName} contains potentially dangerous SQL patterns";
                }
            }

            return null;
        }

        /// <summary>
        /// Comprehensive input sanitization for user content
        /// </summary>
        /// <param name="input">Input to validate</param>
        /// <param name="fieldName">Name of the field being validated</param>
        /// <param name="allowHtml">Whether HTML content is allowed</param>
        /// <returns>List of validation error messages</returns>
        public static List<string> ValidateUserInput(string input, string fieldName, bool allowHtml = false)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(input))
            {
                return errors;
            }

            // Check length
            if (input.Length > Constants.DefaultValues.MaxMessageContentLength)
            {
                errors.Add($"{fieldName} exceeds maximum length of {Constants.DefaultValues.MaxMessageContentLength} characters");
            }

            // Check for XSS if HTML is not allowed
            if (!allowHtml)
            {
                var xssError = ValidateContentForXSS(input, fieldName);
                if (xssError != null)
                {
                    errors.Add(xssError);
                }
            }

            // Check for command injection
            var commandError = ValidateAgainstCommandInjection(input, fieldName);
            if (commandError != null)
            {
                errors.Add(commandError);
            }

            // Check for SQL injection
            var sqlError = ValidateAgainstSQLInjection(input, fieldName);
            if (sqlError != null)
            {
                errors.Add(sqlError);
            }

            return errors;
        }
    }
}