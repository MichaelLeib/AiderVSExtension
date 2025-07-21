using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json.Serialization;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents a change made by Aider to a file
    /// </summary>
    public class DiffChange
    {
        /// <summary>
        /// Full path to the file that was changed
        /// </summary>
        [JsonPropertyName("filePath")]
        [Required]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Line number where the change occurred (1-based)
        /// </summary>
        [JsonPropertyName("lineNumber")]
        [Range(1, int.MaxValue, ErrorMessage = "Line number must be greater than 0")]
        public int LineNumber { get; set; }

        /// <summary>
        /// Type of change (Added, Removed, Modified)
        /// </summary>
        [JsonPropertyName("type")]
        [Required]
        public ChangeType Type { get; set; }

        /// <summary>
        /// Original content before the change
        /// </summary>
        [JsonPropertyName("originalContent")]
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>
        /// New content after the change
        /// </summary>
        [JsonPropertyName("newContent")]
        public string NewContent { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the change was made
        /// </summary>
        [JsonPropertyName("timestamp")]
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Unique identifier for this change
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Context lines before the change (for better diff visualization)
        /// </summary>
        [JsonPropertyName("contextBefore")]
        public List<string> ContextBefore { get; set; } = new List<string>();

        /// <summary>
        /// Context lines after the change (for better diff visualization)
        /// </summary>
        [JsonPropertyName("contextAfter")]
        public List<string> ContextAfter { get; set; } = new List<string>();

        /// <summary>
        /// Whether this change has been applied to the file
        /// </summary>
        [JsonPropertyName("isApplied")]
        public bool IsApplied { get; set; } = false;

        /// <summary>
        /// Validates the diff change properties
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                return false;

            if (LineNumber < 1)
                return false;

            if (string.IsNullOrWhiteSpace(Id))
                return false;

            if (Timestamp == default)
                return false;

            // Validate file path format
            try
            {
                Path.GetFullPath(FilePath);
            }
            catch
            {
                return false;
            }

            // Type-specific validation
            switch (Type)
            {
                case ChangeType.Added:
                    if (string.IsNullOrEmpty(NewContent))
                        return false;
                    break;

                case ChangeType.Removed:
                    if (string.IsNullOrEmpty(OriginalContent))
                        return false;
                    break;

                case ChangeType.Modified:
                    if (string.IsNullOrEmpty(OriginalContent) || string.IsNullOrEmpty(NewContent))
                        return false;
                    if (OriginalContent == NewContent)
                        return false;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Gets validation errors for the diff change
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(FilePath))
                errors.Add("File path is required");

            if (LineNumber < 1)
                errors.Add("Line number must be greater than 0");

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Change ID is required");

            if (Timestamp == default)
                errors.Add("Timestamp is required");

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

            // Type-specific validation
            switch (Type)
            {
                case ChangeType.Added:
                    if (string.IsNullOrEmpty(NewContent))
                        errors.Add("New content is required for added changes");
                    break;

                case ChangeType.Removed:
                    if (string.IsNullOrEmpty(OriginalContent))
                        errors.Add("Original content is required for removed changes");
                    break;

                case ChangeType.Modified:
                    if (string.IsNullOrEmpty(OriginalContent))
                        errors.Add("Original content is required for modified changes");
                    if (string.IsNullOrEmpty(NewContent))
                        errors.Add("New content is required for modified changes");
                    if (!string.IsNullOrEmpty(OriginalContent) && !string.IsNullOrEmpty(NewContent) && OriginalContent == NewContent)
                        errors.Add("Original and new content must be different for modified changes");
                    break;
            }

            return errors;
        }

        /// <summary>
        /// Gets the display name for this change
        /// </summary>
        /// <returns>Formatted display name</returns>
        public string GetDisplayName()
        {
            var fileName = Path.GetFileName(FilePath);
            var changeTypeText = Type.ToString().ToLower();
            return $"{fileName}:{LineNumber} ({changeTypeText})";
        }

        /// <summary>
        /// Gets a summary of the change for display purposes
        /// </summary>
        /// <returns>Change summary text</returns>
        public string GetChangeSummary()
        {
            switch (Type)
            {
                case ChangeType.Added:
                    return $"Added: {NewContent.Substring(0, Math.Min(50, NewContent.Length))}...";
                
                case ChangeType.Removed:
                    return $"Removed: {OriginalContent.Substring(0, Math.Min(50, OriginalContent.Length))}...";
                
                case ChangeType.Modified:
                    return $"Modified: {OriginalContent.Substring(0, Math.Min(25, OriginalContent.Length))}... → {NewContent.Substring(0, Math.Min(25, NewContent.Length))}...";
                
                default:
                    return "Unknown change";
            }
        }

        /// <summary>
        /// Checks if this change affects the same line as another change
        /// </summary>
        /// <param name="other">Other diff change to compare</param>
        /// <returns>True if they affect the same line, false otherwise</returns>
        public bool AffectsSameLine(DiffChange other)
        {
            return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase) && 
                   LineNumber == other.LineNumber;
        }

        /// <summary>
        /// Creates a copy of this diff change
        /// </summary>
        /// <returns>Deep copy of the diff change</returns>
        public DiffChange Clone()
        {
            return new DiffChange
            {
                FilePath = FilePath,
                LineNumber = LineNumber,
                Type = Type,
                OriginalContent = OriginalContent,
                NewContent = NewContent,
                Timestamp = Timestamp,
                Id = Id,
                ContextBefore = new List<string>(ContextBefore),
                ContextAfter = new List<string>(ContextAfter),
                IsApplied = IsApplied
            };
        }
    }
}