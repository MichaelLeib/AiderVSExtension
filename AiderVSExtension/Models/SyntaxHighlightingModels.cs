using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Syntax highlighting rule set for a programming language
    /// </summary>
    public class SyntaxRuleSet
    {
        /// <summary>
        /// Language keywords
        /// </summary>
        public string[] Keywords { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Built-in types
        /// </summary>
        public string[] Types { get; set; } = Array.Empty<string>();

        /// <summary>
        /// String delimiters
        /// </summary>
        public string[] StringDelimiters { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Comment patterns
        /// </summary>
        public string[] CommentPatterns { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Regular expression pattern for numbers
        /// </summary>
        public string NumberPattern { get; set; } = @"\b\d+(\.\d+)?\b";

        /// <summary>
        /// Regular expression pattern for operators
        /// </summary>
        public string OperatorPattern { get; set; } = @"[+\-*/%=<>!&|^~?:]+";

        /// <summary>
        /// Regular expression pattern for identifiers
        /// </summary>
        public string IdentifierPattern { get; set; } = @"\b[a-zA-Z_][a-zA-Z0-9_]*\b";

        /// <summary>
        /// Whether the language is case-sensitive
        /// </summary>
        public bool IsCaseSensitive { get; set; } = true;

        /// <summary>
        /// Custom token patterns
        /// </summary>
        public Dictionary<string, TokenType> CustomPatterns { get; set; } = new Dictionary<string, TokenType>();

        /// <summary>
        /// Block comment start/end pairs
        /// </summary>
        public Dictionary<string, string> BlockComments { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Preprocessor directives
        /// </summary>
        public string[] PreprocessorDirectives { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Escape sequences for strings
        /// </summary>
        public Dictionary<string, string> EscapeSequences { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Syntax token types
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Plain text
        /// </summary>
        Text,

        /// <summary>
        /// Language keyword
        /// </summary>
        Keyword,

        /// <summary>
        /// Built-in type
        /// </summary>
        Type,

        /// <summary>
        /// String literal
        /// </summary>
        String,

        /// <summary>
        /// Comment
        /// </summary>
        Comment,

        /// <summary>
        /// Numeric literal
        /// </summary>
        Number,

        /// <summary>
        /// Operator
        /// </summary>
        Operator,

        /// <summary>
        /// Identifier
        /// </summary>
        Identifier,

        /// <summary>
        /// Preprocessor directive
        /// </summary>
        Preprocessor,

        /// <summary>
        /// XML/HTML tag
        /// </summary>
        Tag,

        /// <summary>
        /// Attribute name
        /// </summary>
        Attribute,

        /// <summary>
        /// Attribute value
        /// </summary>
        AttributeValue,

        /// <summary>
        /// Punctuation
        /// </summary>
        Punctuation,

        /// <summary>
        /// Whitespace
        /// </summary>
        Whitespace,

        /// <summary>
        /// Error token
        /// </summary>
        Error
    }

    /// <summary>
    /// Syntax token
    /// </summary>
    public class SyntaxToken
    {
        /// <summary>
        /// Token text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Token type
        /// </summary>
        public TokenType Type { get; set; }

        /// <summary>
        /// Start position in source text
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// End position in source text
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Additional token metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Language detection result
    /// </summary>
    public class LanguageDetectionResult
    {
        /// <summary>
        /// Detected language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Confidence score (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Alternative language candidates
        /// </summary>
        public Dictionary<string, double> Alternatives { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Detection method used
        /// </summary>
        public string DetectionMethod { get; set; }

        /// <summary>
        /// Evidence used for detection
        /// </summary>
        public List<string> Evidence { get; set; } = new List<string>();
    }

    /// <summary>
    /// Syntax highlighting statistics
    /// </summary>
    public class SyntaxHighlightingStats
    {
        /// <summary>
        /// Total number of tokens
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// Token count by type
        /// </summary>
        public Dictionary<TokenType, int> TokenCounts { get; set; } = new Dictionary<TokenType, int>();

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// Language detected
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Theme used
        /// </summary>
        public string Theme { get; set; }

        /// <summary>
        /// Number of lines processed
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Number of characters processed
        /// </summary>
        public int CharacterCount { get; set; }

        /// <summary>
        /// Any errors encountered
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Custom syntax highlighting rule
    /// </summary>
    public class CustomSyntaxRule
    {
        /// <summary>
        /// Rule name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Regular expression pattern
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Token type to assign
        /// </summary>
        public TokenType TokenType { get; set; }

        /// <summary>
        /// Rule priority (higher = applied first)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Languages this rule applies to
        /// </summary>
        public HashSet<string> Languages { get; set; } = new HashSet<string>();

        /// <summary>
        /// Whether to ignore case
        /// </summary>
        public bool IgnoreCase { get; set; } = false;

        /// <summary>
        /// Whether this is a multiline rule
        /// </summary>
        public bool IsMultiline { get; set; } = false;

        /// <summary>
        /// Custom action to execute when rule matches
        /// </summary>
        public Action<SyntaxToken> CustomAction { get; set; }
    }

    /// <summary>
    /// Syntax highlighting performance metrics
    /// </summary>
    public class SyntaxHighlightingMetrics
    {
        /// <summary>
        /// Total highlighting time
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Tokenization time
        /// </summary>
        public TimeSpan TokenizationTime { get; set; }

        /// <summary>
        /// Theme application time
        /// </summary>
        public TimeSpan ThemeApplicationTime { get; set; }

        /// <summary>
        /// Document creation time
        /// </summary>
        public TimeSpan DocumentCreationTime { get; set; }

        /// <summary>
        /// Language detection time
        /// </summary>
        public TimeSpan LanguageDetectionTime { get; set; }

        /// <summary>
        /// Peak memory usage in bytes
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// Cache hit ratio
        /// </summary>
        public double CacheHitRatio { get; set; }

        /// <summary>
        /// Number of cache misses
        /// </summary>
        public int CacheMisses { get; set; }
    }
}