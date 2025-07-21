using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiderVSExtension.Models;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Interface for configuration comparison and analysis
    /// </summary>
    public interface IConfigurationComparisonService
    {
        /// <summary>
        /// Event fired when comparison is completed
        /// </summary>
        event EventHandler<ComparisonCompletedEventArgs> ComparisonCompleted;

        /// <summary>
        /// Compares two configuration profiles
        /// </summary>
        /// <param name="profile1">First profile to compare</param>
        /// <param name="profile2">Second profile to compare</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Comparison result</returns>
        Task<ConfigurationComparisonResult> CompareProfilesAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ComparisonOptions options = null);

        /// <summary>
        /// Compares multiple configuration profiles
        /// </summary>
        /// <param name="profiles">Profiles to compare</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Multi-comparison result</returns>
        Task<ConfigurationMultiComparisonResult> CompareMultipleProfilesAsync(List<ConfigurationProfile> profiles, ComparisonOptions options = null);

        /// <summary>
        /// Compares configuration profile with template
        /// </summary>
        /// <param name="profile">Profile to compare</param>
        /// <param name="template">Template to compare against</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Comparison result</returns>
        Task<ConfigurationComparisonResult> CompareWithTemplateAsync(ConfigurationProfile profile, ConfigurationTemplate template, ComparisonOptions options = null);

        /// <summary>
        /// Generates comparison report
        /// </summary>
        /// <param name="comparisonResult">Comparison result</param>
        /// <param name="format">Report format</param>
        /// <returns>Comparison report</returns>
        Task<ComparisonReport> GenerateComparisonReportAsync(ConfigurationComparisonResult comparisonResult, ReportFormat format = ReportFormat.Text);

        /// <summary>
        /// Gets difference summary between two profiles
        /// </summary>
        /// <param name="profile1">First profile</param>
        /// <param name="profile2">Second profile</param>
        /// <returns>Difference summary</returns>
        Task<DifferenceSummary> GetDifferenceSummaryAsync(ConfigurationProfile profile1, ConfigurationProfile profile2);

        /// <summary>
        /// Merges configurations based on comparison
        /// </summary>
        /// <param name="comparisonResult">Comparison result</param>
        /// <param name="mergeStrategy">Merge strategy</param>
        /// <returns>Merged configuration profile</returns>
        Task<ConfigurationProfile> MergeConfigurationsAsync(ConfigurationComparisonResult comparisonResult, MergeStrategy mergeStrategy);

        /// <summary>
        /// Finds similar profiles in a collection
        /// </summary>
        /// <param name="targetProfile">Profile to find similarities for</param>
        /// <param name="profiles">Collection of profiles to search</param>
        /// <param name="similarityThreshold">Similarity threshold (0-100)</param>
        /// <returns>List of similar profiles with similarity scores</returns>
        Task<List<ProfileSimilarity>> FindSimilarProfilesAsync(ConfigurationProfile targetProfile, List<ConfigurationProfile> profiles, double similarityThreshold = 80.0);

        /// <summary>
        /// Gets formatted diff between two configurations
        /// </summary>
        /// <param name="profile1">First profile</param>
        /// <param name="profile2">Second profile</param>
        /// <param name="format">Diff format</param>
        /// <returns>Formatted diff string</returns>
        Task<string> GetFormattedDiffAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, DiffFormat format = DiffFormat.Unified);

        /// <summary>
        /// Finds conflicts between multiple profiles
        /// </summary>
        /// <param name="profiles">Profiles to check for conflicts</param>
        /// <returns>List of conflicts</returns>
        Task<List<ConfigurationConflict>> FindConflictsAsync(List<ConfigurationProfile> profiles);

        /// <summary>
        /// Exports comparison result to file
        /// </summary>
        /// <param name="comparisonResult">Comparison result</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="format">Export format</param>
        Task ExportComparisonAsync(ConfigurationComparisonResult comparisonResult, string filePath, ExportFormat format = ExportFormat.Json);
    }
}