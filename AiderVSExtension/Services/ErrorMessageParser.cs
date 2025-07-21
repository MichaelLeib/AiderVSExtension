using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for parsing error messages from various sources
    /// </summary>
    public class ErrorMessageParser
    {
        private static readonly List<ErrorPattern> ErrorPatterns = new()
        {
            // Visual Studio C# Compiler Errors
            new ErrorPattern(
                @"^(.+?)\((\d+),(\d+)\):\s*(error|warning)\s*([A-Z]{2}\d+):\s*(.+)$",
                (m) => new ErrorContext
                {
                    FilePath = m.Groups[1].Value,
                    LineNumber = int.Parse(m.Groups[2].Value),
                    ColumnNumber = int.Parse(m.Groups[3].Value),
                    ErrorType = m.Groups[4].Value.ToLower() == "error" ? AiderVSExtension.Models.ErrorType.CompilationError : AiderVSExtension.Models.ErrorType.Warning,
                    ErrorCode = m.Groups[5].Value,
                    Message = m.Groups[6].Value,
                    ErrorSpan = m.Groups[0].Value
                }),

            // MSBuild Errors
            new ErrorPattern(
                @"^(.+?)\((\d+)\):\s*(error|warning)\s*([A-Z]{2}\d+)?:?\s*(.+)$",
                (m) => new ErrorContext
                {
                    FilePath = m.Groups[1].Value,
                    LineNumber = int.Parse(m.Groups[2].Value),
                    ColumnNumber = 0,
                    ErrorType = m.Groups[3].Value.ToLower() == "error" ? AiderVSExtension.Models.ErrorType.BuildError : AiderVSExtension.Models.ErrorType.Warning,
                    ErrorCode = m.Groups[4].Value,
                    Message = m.Groups[5].Value,
                    ErrorSpan = m.Groups[0].Value
                }),

            // NuGet Package Errors
            new ErrorPattern(
                @"^.*error\s*(NU\d+):\s*(.+)$",
                (m) => new ErrorContext
                {
                    FilePath = "",
                    LineNumber = 0,
                    ColumnNumber = 0,
                    ErrorType = AiderVSExtension.Models.ErrorType.BuildError,
                    ErrorCode = m.Groups[1].Value,
                    Message = m.Groups[2].Value,
                    ErrorSpan = m.Groups[0].Value
                }),

            // Generic Error Pattern
            new ErrorPattern(
                @"^.*error.*:\s*(.+)$",
                (m) => new ErrorContext
                {
                    FilePath = "",
                    LineNumber = 0,
                    ColumnNumber = 0,
                    ErrorType = AiderVSExtension.Models.ErrorType.Unknown,
                    ErrorCode = "",
                    Message = m.Groups[1].Value,
                    ErrorSpan = m.Groups[0].Value
                }),

            // Warning Pattern
            new ErrorPattern(
                @"^.*warning.*:\s*(.+)$",
                (m) => new ErrorContext
                {
                    FilePath = "",
                    LineNumber = 0,
                    ColumnNumber = 0,
                    ErrorType = AiderVSExtension.Models.ErrorType.Warning,
                    ErrorCode = "",
                    Message = m.Groups[1].Value,
                    ErrorSpan = m.Groups[0].Value
                })
        };

        /// <summary>
        /// Parses a single error line and returns an ErrorContext if successful
        /// </summary>
        public ErrorContext ParseErrorLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            foreach (var pattern in ErrorPatterns)
            {
                var match = Regex.Match(line, pattern.Pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    try
                    {
                        var errorContext = pattern.Parser(match);
                        if (errorContext != null)
                        {
                            EnrichErrorContext(errorContext);
                            return errorContext;
                        }
                    }
                    catch (Exception)
                    {
                        // Continue to next pattern if parsing fails
                        continue;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses multiple error lines and returns a collection of ErrorContexts
        /// </summary>
        public IEnumerable<ErrorContext> ParseErrorLines(string[] lines)
        {
            var errors = new List<ErrorContext>();

            if (lines == null || lines.Length == 0)
            {
                return errors;
            }

            foreach (var line in lines)
            {
                var errorContext = ParseErrorLine(line);
                if (errorContext != null)
                {
                    errors.Add(errorContext);
                }
            }

            return errors;
        }

        /// <summary>
        /// Parses error output text and returns a collection of ErrorContexts
        /// </summary>
        public IEnumerable<ErrorContext> ParseErrorOutput(string outputText)
        {
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return Enumerable.Empty<ErrorContext>();
            }

            var lines = outputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return ParseErrorLines(lines);
        }

        /// <summary>
        /// Categorizes error based on its message content
        /// </summary>
        public ErrorType CategorizeError(string errorMessage, string errorCode = "")
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return AiderVSExtension.Models.ErrorType.Unknown;
            }

            var message = errorMessage.ToLower();
            
            // Check error code patterns
            if (!string.IsNullOrEmpty(errorCode))
            {
                var code = errorCode.ToUpper();
                
                // C# Compiler errors
                if (code.StartsWith("CS"))
                {
                    return AiderVSExtension.Models.ErrorType.CompilationError;
                }
                
                // MSBuild errors
                if (code.StartsWith("MSB"))
                {
                    return AiderVSExtension.Models.ErrorType.BuildError;
                }
                
                // NuGet errors
                if (code.StartsWith("NU"))
                {
                    return AiderVSExtension.Models.ErrorType.BuildError;
                }
            }

            // Check message content patterns
            if (message.Contains("syntax error") || message.Contains("expected") || message.Contains("unexpected"))
            {
                return AiderVSExtension.Models.ErrorType.SyntaxError;
            }
            
            if (message.Contains("type") && (message.Contains("cannot convert") || message.Contains("cannot implicitly")))
            {
                return AiderVSExtension.Models.ErrorType.TypeError;
            }
            
            if (message.Contains("does not exist") || message.Contains("not found") || message.Contains("could not be found"))
            {
                return AiderVSExtension.Models.ErrorType.ReferenceError;
            }
            
            if (message.Contains("build failed") || message.Contains("build error"))
            {
                return AiderVSExtension.Models.ErrorType.BuildError;
            }
            
            if (message.Contains("runtime error") || message.Contains("exception"))
            {
                return AiderVSExtension.Models.ErrorType.RuntimeError;
            }
            
            if (message.Contains("warning"))
            {
                return AiderVSExtension.Models.ErrorType.Warning;
            }

            return AiderVSExtension.Models.ErrorType.CompilationError; // Default for most development errors
        }

        /// <summary>
        /// Extracts the project name from a file path
        /// </summary>
        public string ExtractProjectName(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "";
            }

            try
            {
                var pathParts = filePath.Split('\\', '/');
                
                // Look for project file indicators
                for (int i = pathParts.Length - 1; i >= 0; i--)
                {
                    if (pathParts[i].EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                        pathParts[i].EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ||
                        pathParts[i].EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
                    {
                        return System.IO.Path.GetFileNameWithoutExtension(pathParts[i]);
                    }
                }

                // Fallback: use directory name
                var fileName = System.IO.Path.GetFileName(filePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(fileName);
                }
            }
            catch (Exception)
            {
                // Ignore path parsing errors
            }

            return "";
        }

        /// <summary>
        /// Determines the severity of an error
        /// </summary>
        public ErrorSeverity GetErrorSeverity(ErrorType errorType)
        {
            return errorType switch
            {
                AiderVSExtension.Models.ErrorType.CompilationError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.SyntaxError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.TypeError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.ReferenceError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.BuildError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.RuntimeError => ErrorSeverity.Error,
                AiderVSExtension.Models.ErrorType.Warning => ErrorSeverity.Warning,
                AiderVSExtension.Models.ErrorType.CodeAnalysisWarning => ErrorSeverity.Warning,
                _ => ErrorSeverity.Info
            };
        }

        /// <summary>
        /// Enriches an error context with additional information
        /// </summary>
        private void EnrichErrorContext(ErrorContext errorContext)
        {
            if (errorContext == null)
            {
                return;
            }

            // Set project name if not already set
            if (string.IsNullOrEmpty(errorContext.ProjectName) && !string.IsNullOrEmpty(errorContext.FilePath))
            {
                errorContext.ProjectName = ExtractProjectName(errorContext.FilePath);
            }

            // Re-categorize error type if it's unknown
            if (errorContext.ErrorType == AiderVSExtension.Models.ErrorType.Unknown)
            {
                errorContext.ErrorType = CategorizeError(errorContext.Message, errorContext.ErrorCode);
            }

            // Add additional context to dictionary
            errorContext.AdditionalContext["Severity"] = GetErrorSeverity(errorContext.ErrorType).ToString();
            errorContext.AdditionalContext["ParsedAt"] = DateTime.UtcNow.ToString("O");
        }
    }

    /// <summary>
    /// Represents an error parsing pattern
    /// </summary>
    internal class ErrorPattern
    {
        public string Pattern { get; }
        public Func<Match, ErrorContext> Parser { get; }

        public ErrorPattern(string pattern, Func<Match, ErrorContext> parser)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }
    }

    /// <summary>
    /// Represents error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}