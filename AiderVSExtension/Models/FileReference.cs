using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a reference to a file or code selection in the chat context
    /// </summary>
    public class FileReference
    {
        /// <summary>
        /// Full path to the referenced file
        /// </summary>
        [JsonPropertyName("filePath")]
        [Required]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Starting line number (1-based) for the reference
        /// </summary>
        [JsonPropertyName("startLine")]
        [Range(1, int.MaxValue, ErrorMessage = "Start line must be greater than 0")]
        public int StartLine { get; set; } = 1;

        /// <summary>
        /// Ending line number (1-based) for the reference
        /// </summary>
        [JsonPropertyName("endLine")]
        [Range(1, int.MaxValue, ErrorMessage = "End line must be greater than 0")]
        public int EndLine { get; set; } = 1;

        /// <summary>
        /// Content of the referenced file or selection
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type of the file reference
        /// </summary>
        [JsonPropertyName("type")]
        [Required]
        public ReferenceType Type { get; set; }

        /// <summary>
        /// Timestamp when the reference was created
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the icon for the reference type
        /// </summary>
        [JsonIgnore]
        public string TypeIcon => Type switch
        {
            ReferenceType.File => "📄",
            ReferenceType.Selection => "📝",
            ReferenceType.Error => "❌",
            ReferenceType.Clipboard => "📋",
            ReferenceType.GitBranch => "🌿",
            ReferenceType.WebSearch => "🔍",
            ReferenceType.Documentation => "📚",
            _ => "📄"
        };

        /// <summary>
        /// Gets whether this reference has a file path
        /// </summary>
        [JsonIgnore]
        public bool HasFilePath => !string.IsNullOrEmpty(FilePath);

        /// <summary>
        /// Gets the display name for UI binding
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                switch (Type)
                {
                    case ReferenceType.Clipboard:
                        return "Clipboard Content";
                    
                    case ReferenceType.GitBranch:
                        return $"Git Branch: {FilePath}";
                    
                    case ReferenceType.Selection:
                        var fileName = !string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : "Unknown File";
                        if (StartLine > 0 && EndLine > 0)
                        {
                            return StartLine == EndLine 
                                ? $"{fileName} (Line {StartLine})" 
                                : $"{fileName} (Lines {StartLine}-{EndLine})";
                        }
                        return $"{fileName} (Selection)";
                    
                    case ReferenceType.Error:
                        var errorFileName = !string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : "Unknown File";
                        return $"{errorFileName} (Error)";
                    
                    default:
                        if (!string.IsNullOrEmpty(FilePath))
                        {
                            return Path.GetFileName(FilePath);
                        }
                        return "Unknown Reference";
                }
            }
        }

        /// <summary>
        /// Validates the file reference properties
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                return false;

            if (StartLine < 1)
                return false;

            if (EndLine < 1)
                return false;

            if (EndLine < StartLine)
                return false;

            // Validate file path format (basic check)
            try
            {
                Path.GetFullPath(FilePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets validation errors for the file reference
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(FilePath))
                errors.Add("File path is required");

            if (StartLine < 1)
                errors.Add("Start line must be greater than 0");

            if (EndLine < 1)
                errors.Add("End line must be greater than 0");

            if (EndLine < StartLine)
                errors.Add("End line must be greater than or equal to start line");

            // Validate file path format
            if (!string.IsNullOrWhiteSpace(FilePath))
            {
                try
                {
                    Path.GetFullPath(FilePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Invalid file path format: {ex.Message}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Gets the display name for the file reference
        /// </summary>
        /// <returns>Formatted display name</returns>
        public string GetDisplayName()
        {
            var fileName = Path.GetFileName(FilePath);
            if (StartLine == EndLine)
            {
                return $"{fileName}:{StartLine}";
            }
            return $"{fileName}:{StartLine}-{EndLine}";
        }

        /// <summary>
        /// Checks if this reference represents a single line
        /// </summary>
        /// <returns>True if single line, false otherwise</returns>
        public bool IsSingleLine()
        {
            return StartLine == EndLine;
        }

        /// <summary>
        /// Gets the number of lines in this reference
        /// </summary>
        /// <returns>Number of lines</returns>
        public int GetLineCount()
        {
            return EndLine - StartLine + 1;
        }

        /// <summary>
        /// Creates a sanitized copy of this file reference for logging
        /// </summary>
        /// <returns>Sanitized file reference copy</returns>
        public FileReference CreateSanitizedCopy()
        {
            return new FileReference
            {
                FilePath = FilePath,
                StartLine = StartLine,
                EndLine = EndLine,
                Content = Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content,
                Type = Type,
                Timestamp = Timestamp
            };
        }

        /// <summary>
        /// Gets the file size in bytes if the file exists
        /// </summary>
        /// <returns>File size in bytes, or -1 if file doesn't exist</returns>
        public long GetFileSize()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return new FileInfo(FilePath).Length;
                }
            }
            catch
            {
                // Ignore errors
            }
            return -1;
        }

        /// <summary>
        /// Gets the file extension from the file path
        /// </summary>
        /// <returns>File extension including the dot, or empty string if no extension</returns>
        public string GetFileExtension()
        {
            try
            {
                return Path.GetExtension(FilePath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if this file reference represents a code file
        /// </summary>
        /// <returns>True if it's a code file, false otherwise</returns>
        public bool IsCodeFile()
        {
            var extension = GetFileExtension().ToLowerInvariant();
            return Constants.Files.SupportedCodeExtensions.Contains(extension);
        }
    }
}