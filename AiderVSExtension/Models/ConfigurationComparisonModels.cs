using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Configuration comparison result
    /// </summary>
    public class ConfigurationComparisonResult
    {
        /// <summary>
        /// Comparison ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// First profile in comparison
        /// </summary>
        public ConfigurationProfile Profile1 { get; set; }

        /// <summary>
        /// Second profile in comparison
        /// </summary>
        public ConfigurationProfile Profile2 { get; set; }

        /// <summary>
        /// Comparison timestamp
        /// </summary>
        public DateTime ComparedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Overall similarity score (0-100)
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Whether profiles are identical
        /// </summary>
        public bool AreIdentical { get; set; }

        /// <summary>
        /// List of differences found
        /// </summary>
        public List<ConfigurationDifference> Differences { get; set; } = new List<ConfigurationDifference>();

        /// <summary>
        /// Comparison options used
        /// </summary>
        public ComparisonOptions Options { get; set; }

        /// <summary>
        /// Comparison duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Summary of differences by category
        /// </summary>
        public Dictionary<string, int> DifferenceSummary { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Common settings between profiles
        /// </summary>
        public Dictionary<string, object> CommonSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Unique settings in first profile
        /// </summary>
        public Dictionary<string, object> UniqueToProfile1 { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Unique settings in second profile
        /// </summary>
        public Dictionary<string, object> UniqueToProfile2 { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Multi-profile comparison result
    /// </summary>
    public class ConfigurationMultiComparisonResult
    {
        /// <summary>
        /// Comparison ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Profiles being compared
        /// </summary>
        public List<ConfigurationProfile> Profiles { get; set; } = new List<ConfigurationProfile>();

        /// <summary>
        /// Pairwise comparison results
        /// </summary>
        public List<ConfigurationComparisonResult> PairwiseComparisons { get; set; } = new List<ConfigurationComparisonResult>();

        /// <summary>
        /// Comparison timestamp
        /// </summary>
        public DateTime ComparedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Common settings across all profiles
        /// </summary>
        public Dictionary<string, object> CommonSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Settings unique to each profile
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> UniqueSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Similarity matrix (profile ID to profile ID to similarity score)
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> SimilarityMatrix { get; set; } = new Dictionary<string, Dictionary<string, double>>();

        /// <summary>
        /// Most similar profile pair
        /// </summary>
        public ProfilePair MostSimilarPair { get; set; }

        /// <summary>
        /// Most different profile pair
        /// </summary>
        public ProfilePair MostDifferentPair { get; set; }

        /// <summary>
        /// Average similarity across all pairs
        /// </summary>
        public double AverageSimilarity { get; set; }
    }

    /// <summary>
    /// Configuration difference
    /// </summary>
    public class ConfigurationDifference
    {
        /// <summary>
        /// Property path (e.g., "Settings.APIKey")
        /// </summary>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Difference type
        /// </summary>
        public DifferenceType Type { get; set; }

        /// <summary>
        /// Value in first profile
        /// </summary>
        public object Value1 { get; set; }

        /// <summary>
        /// Value in second profile
        /// </summary>
        public object Value2 { get; set; }

        /// <summary>
        /// Difference category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Difference severity
        /// </summary>
        public DifferenceSeverity Severity { get; set; }

        /// <summary>
        /// Human-readable description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Impact of the difference
        /// </summary>
        public string Impact { get; set; }

        /// <summary>
        /// Suggested resolution
        /// </summary>
        public string SuggestedResolution { get; set; }
    }

    /// <summary>
    /// Comparison options
    /// </summary>
    public class ComparisonOptions
    {
        /// <summary>
        /// Whether to include sensitive data in comparison
        /// </summary>
        public bool IncludeSensitiveData { get; set; } = false;

        /// <summary>
        /// Whether to ignore case differences in string values
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// Whether to ignore whitespace differences
        /// </summary>
        public bool IgnoreWhitespace { get; set; } = true;

        /// <summary>
        /// Properties to exclude from comparison
        /// </summary>
        public List<string> ExcludeProperties { get; set; } = new List<string>();

        /// <summary>
        /// Properties to focus on (if specified, only these will be compared)
        /// </summary>
        public List<string> FocusProperties { get; set; } = new List<string>();

        /// <summary>
        /// Whether to perform deep comparison of nested objects
        /// </summary>
        public bool DeepComparison { get; set; } = true;

        /// <summary>
        /// Tolerance for numeric comparisons
        /// </summary>
        public double NumericTolerance { get; set; } = 0.001;

        /// <summary>
        /// Whether to calculate similarity scores
        /// </summary>
        public bool CalculateSimilarity { get; set; } = true;

        /// <summary>
        /// Custom comparison weights for different property categories
        /// </summary>
        public Dictionary<string, double> PropertyWeights { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Difference summary
    /// </summary>
    public class DifferenceSummary
    {
        /// <summary>
        /// Total number of differences
        /// </summary>
        public int TotalDifferences { get; set; }

        /// <summary>
        /// Number of added properties
        /// </summary>
        public int AddedProperties { get; set; }

        /// <summary>
        /// Number of removed properties
        /// </summary>
        public int RemovedProperties { get; set; }

        /// <summary>
        /// Number of modified properties
        /// </summary>
        public int ModifiedProperties { get; set; }

        /// <summary>
        /// Differences by category
        /// </summary>
        public Dictionary<string, int> DifferencesByCategory { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Differences by severity
        /// </summary>
        public Dictionary<DifferenceSeverity, int> DifferencesBySeverity { get; set; } = new Dictionary<DifferenceSeverity, int>();

        /// <summary>
        /// Overall impact assessment
        /// </summary>
        public string OverallImpact { get; set; }

        /// <summary>
        /// Compatibility assessment
        /// </summary>
        public CompatibilityLevel CompatibilityLevel { get; set; }
    }

    /// <summary>
    /// Profile similarity
    /// </summary>
    public class ProfileSimilarity
    {
        /// <summary>
        /// Target profile
        /// </summary>
        public ConfigurationProfile Profile { get; set; }

        /// <summary>
        /// Similarity score (0-100)
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Common properties count
        /// </summary>
        public int CommonPropertiesCount { get; set; }

        /// <summary>
        /// Different properties count
        /// </summary>
        public int DifferentPropertiesCount { get; set; }

        /// <summary>
        /// Key similarities
        /// </summary>
        public List<string> KeySimilarities { get; set; } = new List<string>();

        /// <summary>
        /// Key differences
        /// </summary>
        public List<string> KeyDifferences { get; set; } = new List<string>();
    }

    /// <summary>
    /// Profile pair
    /// </summary>
    public class ProfilePair
    {
        /// <summary>
        /// First profile
        /// </summary>
        public ConfigurationProfile Profile1 { get; set; }

        /// <summary>
        /// Second profile
        /// </summary>
        public ConfigurationProfile Profile2 { get; set; }

        /// <summary>
        /// Similarity score between the pair
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Relationship description
        /// </summary>
        public string Relationship { get; set; }
    }

    /// <summary>
    /// Comparison report
    /// </summary>
    public class ComparisonReport
    {
        /// <summary>
        /// Report ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Report title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Report content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Report format
        /// </summary>
        public ReportFormat Format { get; set; }

        /// <summary>
        /// Generated timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Generated by
        /// </summary>
        public string GeneratedBy { get; set; }

        /// <summary>
        /// Report metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }


    /// <summary>
    /// Comparison completed event arguments
    /// </summary>
    public class ComparisonCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Comparison result
        /// </summary>
        public ConfigurationComparisonResult ComparisonResult { get; set; }

        /// <summary>
        /// Completion timestamp
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of differences found
        /// </summary>
        public int DifferencesFound { get; set; }
    }

    /// <summary>
    /// Difference types
    /// </summary>
    public enum DifferenceType
    {
        /// <summary>
        /// Property exists in first profile but not second
        /// </summary>
        Added,

        /// <summary>
        /// Property exists in second profile but not first
        /// </summary>
        Removed,

        /// <summary>
        /// Property exists in both but with different values
        /// </summary>
        Modified,

        /// <summary>
        /// Property type differs between profiles
        /// </summary>
        TypeChanged
    }

    /// <summary>
    /// Difference severity levels
    /// </summary>
    public enum DifferenceSeverity
    {
        /// <summary>
        /// Informational difference
        /// </summary>
        Info,

        /// <summary>
        /// Low impact difference
        /// </summary>
        Low,

        /// <summary>
        /// Medium impact difference
        /// </summary>
        Medium,

        /// <summary>
        /// High impact difference
        /// </summary>
        High,

        /// <summary>
        /// Critical difference that may break functionality
        /// </summary>
        Critical
    }

    /// <summary>
    /// Merge strategies
    /// </summary>
    public enum MergeStrategy
    {
        /// <summary>
        /// Prefer values from first profile
        /// </summary>
        PreferFirst,

        /// <summary>
        /// Prefer values from second profile
        /// </summary>
        PreferSecond,

        /// <summary>
        /// Merge non-conflicting values
        /// </summary>
        MergeNonConflicting,

        /// <summary>
        /// Manual merge (requires user input)
        /// </summary>
        Manual,

        /// <summary>
        /// Smart merge based on rules
        /// </summary>
        Smart
    }

    /// <summary>
    /// Report formats
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>
        /// Plain text format
        /// </summary>
        Text,

        /// <summary>
        /// HTML format
        /// </summary>
        Html,

        /// <summary>
        /// Markdown format
        /// </summary>
        Markdown,

        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// CSV format
        /// </summary>
        Csv
    }

    /// <summary>
    /// Compatibility levels
    /// </summary>
    public enum CompatibilityLevel
    {
        /// <summary>
        /// Fully compatible
        /// </summary>
        FullyCompatible,

        /// <summary>
        /// Mostly compatible with minor issues
        /// </summary>
        MostlyCompatible,

        /// <summary>
        /// Partially compatible with some issues
        /// </summary>
        PartiallyCompatible,

        /// <summary>
        /// Limited compatibility with major issues
        /// </summary>
        LimitedCompatibility,

        /// <summary>
        /// Incompatible
        /// </summary>
        Incompatible
    }

    /// <summary>
    /// Diff formats for comparison output
    /// </summary>
    public enum DiffFormat
    {
        /// <summary>
        /// Unified diff format
        /// </summary>
        Unified,

        /// <summary>
        /// Side-by-side diff format
        /// </summary>
        SideBySide,

        /// <summary>
        /// JSON diff format
        /// </summary>
        Json,

        /// <summary>
        /// HTML diff format
        /// </summary>
        Html,

        /// <summary>
        /// Context diff format
        /// </summary>
        Context
    }

    /// <summary>
    /// Export formats for comparison results
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// XML format
        /// </summary>
        Xml,

        /// <summary>
        /// CSV format
        /// </summary>
        Csv,

        /// <summary>
        /// Excel format
        /// </summary>
        Excel,

        /// <summary>
        /// PDF format
        /// </summary>
        Pdf,

        /// <summary>
        /// YAML format
        /// </summary>
        Yaml
    }

    /// <summary>
    /// Configuration conflict
    /// </summary>
    public class ConfigurationConflict
    {
        /// <summary>
        /// Conflict ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Property path where conflict occurs
        /// </summary>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Description of the conflict
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Conflicting values (profile ID to value)
        /// </summary>
        public Dictionary<string, object> ConflictingValues { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Conflict severity
        /// </summary>
        public DifferenceSeverity Severity { get; set; }

        /// <summary>
        /// Suggested resolution strategies
        /// </summary>
        public List<string> ResolutionStrategies { get; set; } = new List<string>();

        /// <summary>
        /// Recommended resolution
        /// </summary>
        public string RecommendedResolution { get; set; }
    }
}