using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    }
}