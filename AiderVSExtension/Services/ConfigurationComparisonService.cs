using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AiderVSExtension.Security;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for comparing and analyzing configuration profiles
    /// </summary>
    public class ConfigurationComparisonService : IConfigurationComparisonService, IDisposable
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private bool _disposed = false;

        public event EventHandler<ComparisonCompletedEventArgs> ComparisonCompleted;

        public ConfigurationComparisonService(
            IAdvancedConfigurationService configurationService,
            IErrorHandler errorHandler)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Compares two configuration profiles
        /// </summary>
        /// <param name="profile1">First profile to compare</param>
        /// <param name="profile2">Second profile to compare</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Comparison result</returns>
        public async Task<ConfigurationComparisonResult> CompareProfilesAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ComparisonOptions options = null)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                options = options ?? new ComparisonOptions();

                var result = new ConfigurationComparisonResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Profile1 = profile1,
                    Profile2 = profile2,
                    Options = options,
                    ComparedAt = startTime
                };

                // Compare basic properties
                await CompareBasicPropertiesAsync(profile1, profile2, result, options);

                // Compare AI model configuration
                await CompareAIModelConfigurationAsync(profile1, profile2, result, options);

                // Compare settings
                await CompareSettingsAsync(profile1, profile2, result, options);

                // Compare advanced parameters
                await CompareAdvancedParametersAsync(profile1, profile2, result, options);

                // Calculate similarity score
                result.SimilarityScore = CalculateSimilarityScore(result, options);
                result.AreIdentical = result.Differences.Count == 0;

                // Generate difference summary
                GenerateDifferenceSummary(result);

                // Calculate duration
                result.Duration = DateTime.UtcNow - startTime;

                // Fire completion event
                ComparisonCompleted?.Invoke(this, new ComparisonCompletedEventArgs
                {
                    ComparisonResult = result,
                    DifferencesFound = result.Differences.Count
                });

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.CompareProfilesAsync");
                throw;
            }
        }

        /// <summary>
        /// Compares multiple configuration profiles
        /// </summary>
        /// <param name="profiles">Profiles to compare</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Multi-comparison result</returns>
        public async Task<ConfigurationMultiComparisonResult> CompareMultipleProfilesAsync(List<ConfigurationProfile> profiles, ComparisonOptions options = null)
        {
            try
            {
                if (profiles == null || profiles.Count < 2)
                {
                    throw new ArgumentException("At least two profiles are required for comparison");
                }

                options = options ?? new ComparisonOptions();

                var result = new ConfigurationMultiComparisonResult
                {
                    Id = Guid.NewGuid().ToString(),
                    Profiles = profiles,
                    ComparedAt = DateTime.UtcNow
                };

                // Perform pairwise comparisons
                var similarities = new List<double>();
                for (int i = 0; i < profiles.Count; i++)
                {
                    result.SimilarityMatrix[profiles[i].Id] = new Dictionary<string, double>();
                    
                    for (int j = i + 1; j < profiles.Count; j++)
                    {
                        var pairwiseResult = await CompareProfilesAsync(profiles[i], profiles[j], options);
                        result.PairwiseComparisons.Add(pairwiseResult);
                        
                        result.SimilarityMatrix[profiles[i].Id][profiles[j].Id] = pairwiseResult.SimilarityScore;
                        similarities.Add(pairwiseResult.SimilarityScore);

                        // Track most similar and different pairs
                        if (result.MostSimilarPair == null || pairwiseResult.SimilarityScore > result.MostSimilarPair.SimilarityScore)
                        {
                            result.MostSimilarPair = new ProfilePair
                            {
                                Profile1 = profiles[i],
                                Profile2 = profiles[j],
                                SimilarityScore = pairwiseResult.SimilarityScore,
                                Relationship = "Most Similar"
                            };
                        }

                        if (result.MostDifferentPair == null || pairwiseResult.SimilarityScore < result.MostDifferentPair.SimilarityScore)
                        {
                            result.MostDifferentPair = new ProfilePair
                            {
                                Profile1 = profiles[i],
                                Profile2 = profiles[j],
                                SimilarityScore = pairwiseResult.SimilarityScore,
                                Relationship = "Most Different"
                            };
                        }
                    }
                }

                // Calculate average similarity
                result.AverageSimilarity = similarities.Any() ? similarities.Average() : 0;

                // Find common settings across all profiles
                await FindCommonSettingsAsync(profiles, result);

                // Find unique settings for each profile
                await FindUniqueSettingsAsync(profiles, result);

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.CompareMultipleProfilesAsync");
                throw;
            }
        }

        /// <summary>
        /// Compares configuration profile with template
        /// </summary>
        /// <param name="profile">Profile to compare</param>
        /// <param name="template">Template to compare against</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Comparison result</returns>
        public async Task<ConfigurationComparisonResult> CompareWithTemplateAsync(ConfigurationProfile profile, ConfigurationTemplate template, ComparisonOptions options = null)
        {
            try
            {
                // Create a virtual profile from template for comparison
                var templateProfile = new ConfigurationProfile
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Version = template.Version,
                    Settings = template.ExpectedProperties
                };

                var result = await CompareProfilesAsync(profile, templateProfile, options);
                
                // Add template-specific analysis
                await AnalyzeTemplateComplianceAsync(profile, template, result);

                return result;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.CompareWithTemplateAsync");
                throw;
            }
        }

        /// <summary>
        /// Generates comparison report
        /// </summary>
        /// <param name="comparisonResult">Comparison result</param>
        /// <param name="format">Report format</param>
        /// <returns>Comparison report</returns>
        public async Task<ComparisonReport> GenerateComparisonReportAsync(ConfigurationComparisonResult comparisonResult, ReportFormat format = ReportFormat.Text)
        {
            try
            {
                var report = new ComparisonReport
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"Configuration Comparison: {comparisonResult.Profile1.Name} vs {comparisonResult.Profile2.Name}",
                    Format = format,
                    GeneratedBy = Environment.UserName
                };

                switch (format)
                {
                    case ReportFormat.Text:
                        report.Content = GenerateTextReport(comparisonResult);
                        break;
                    case ReportFormat.Html:
                        report.Content = GenerateHtmlReport(comparisonResult);
                        break;
                    case ReportFormat.Markdown:
                        report.Content = GenerateMarkdownReport(comparisonResult);
                        break;
                    case ReportFormat.Json:
                        report.Content = SecureJsonSerializer.Serialize(comparisonResult);
                        break;
                    default:
                        report.Content = GenerateTextReport(comparisonResult);
                        break;
                }

                return report;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.GenerateComparisonReportAsync");
                throw;
            }
        }

        /// <summary>
        /// Gets difference summary between two profiles
        /// </summary>
        /// <param name="profile1">First profile</param>
        /// <param name="profile2">Second profile</param>
        /// <returns>Difference summary</returns>
        public async Task<DifferenceSummary> GetDifferenceSummaryAsync(ConfigurationProfile profile1, ConfigurationProfile profile2)
        {
            try
            {
                var comparisonResult = await CompareProfilesAsync(profile1, profile2);
                
                var summary = new DifferenceSummary
                {
                    TotalDifferences = comparisonResult.Differences.Count,
                    AddedProperties = comparisonResult.Differences.Count(d => d.Type == DifferenceType.Added),
                    RemovedProperties = comparisonResult.Differences.Count(d => d.Type == DifferenceType.Removed),
                    ModifiedProperties = comparisonResult.Differences.Count(d => d.Type == DifferenceType.Modified),
                    DifferencesByCategory = comparisonResult.DifferenceSummary,
                    DifferencesBySeverity = comparisonResult.Differences
                        .GroupBy(d => d.Severity)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                // Assess overall impact
                summary.OverallImpact = AssessOverallImpact(summary);
                summary.CompatibilityLevel = AssessCompatibilityLevel(summary);

                return summary;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.GetDifferenceSummaryAsync");
                throw;
            }
        }

        /// <summary>
        /// Merges configurations based on comparison
        /// </summary>
        /// <param name="comparisonResult">Comparison result</param>
        /// <param name="mergeStrategy">Merge strategy</param>
        /// <returns>Merged configuration profile</returns>
        public async Task<ConfigurationProfile> MergeConfigurationsAsync(ConfigurationComparisonResult comparisonResult, MergeStrategy mergeStrategy)
        {
            try
            {
                var mergedProfile = new ConfigurationProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Merged: {comparisonResult.Profile1.Name} + {comparisonResult.Profile2.Name}",
                    Description = $"Merged configuration using {mergeStrategy} strategy",
                    Version = GetHighestVersion(comparisonResult.Profile1.Version, comparisonResult.Profile2.Version),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Environment.UserName,
                    Settings = new Dictionary<string, object>(),
                    IsActive = false
                };

                switch (mergeStrategy)
                {
                    case MergeStrategy.PreferFirst:
                        await MergePreferFirst(comparisonResult, mergedProfile);
                        break;
                    case MergeStrategy.PreferSecond:
                        await MergePreferSecond(comparisonResult, mergedProfile);
                        break;
                    case MergeStrategy.MergeNonConflicting:
                        await MergeNonConflicting(comparisonResult, mergedProfile);
                        break;
                    case MergeStrategy.Smart:
                        await MergeSmartStrategy(comparisonResult, mergedProfile);
                        break;
                    default:
                        throw new NotSupportedException($"Merge strategy {mergeStrategy} is not supported");
                }

                return mergedProfile;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.MergeConfigurationsAsync");
                throw;
            }
        }

        /// <summary>
        /// Finds similar profiles in a collection
        /// </summary>
        /// <param name="targetProfile">Profile to find similarities for</param>
        /// <param name="profiles">Collection of profiles to search</param>
        /// <param name="similarityThreshold">Similarity threshold (0-100)</param>
        /// <returns>List of similar profiles with similarity scores</returns>
        public async Task<List<ProfileSimilarity>> FindSimilarProfilesAsync(ConfigurationProfile targetProfile, List<ConfigurationProfile> profiles, double similarityThreshold = 80.0)
        {
            try
            {
                var similarities = new List<ProfileSimilarity>();

                foreach (var profile in profiles.Where(p => p.Id != targetProfile.Id))
                {
                    var comparison = await CompareProfilesAsync(targetProfile, profile);
                    
                    if (comparison.SimilarityScore >= similarityThreshold)
                    {
                        var similarity = new ProfileSimilarity
                        {
                            Profile = profile,
                            SimilarityScore = comparison.SimilarityScore,
                            CommonPropertiesCount = comparison.CommonSettings.Count,
                            DifferentPropertiesCount = comparison.Differences.Count,
                            KeySimilarities = ExtractKeySimilarities(comparison),
                            KeyDifferences = ExtractKeyDifferences(comparison)
                        };

                        similarities.Add(similarity);
                    }
                }

                return similarities.OrderByDescending(s => s.SimilarityScore).ToList();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.FindSimilarProfilesAsync");
                throw;
            }
        }

        #region Private Methods

        private async Task CompareBasicPropertiesAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ConfigurationComparisonResult result, ComparisonOptions options)
        {
            CompareProperty("Name", profile1.Name, profile2.Name, result, "Basic", DifferenceSeverity.Low);
            CompareProperty("Description", profile1.Description, profile2.Description, result, "Basic", DifferenceSeverity.Info);
            CompareProperty("Version", profile1.Version, profile2.Version, result, "Basic", DifferenceSeverity.Medium);
            CompareProperty("IsActive", profile1.IsActive, profile2.IsActive, result, "Basic", DifferenceSeverity.Medium);
            CompareProperty("IsDefault", profile1.IsDefault, profile2.IsDefault, result, "Basic", DifferenceSeverity.Low);
        }

        private async Task CompareAIModelConfigurationAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ConfigurationComparisonResult result, ComparisonOptions options)
        {
            var ai1 = profile1.AIModelConfiguration;
            var ai2 = profile2.AIModelConfiguration;

            if (ai1 == null && ai2 == null) return;

            if (ai1 == null || ai2 == null)
            {
                result.Differences.Add(new ConfigurationDifference
                {
                    PropertyPath = "AIModelConfiguration",
                    Type = ai1 == null ? DifferenceType.Removed : DifferenceType.Added,
                    Value1 = ai1,
                    Value2 = ai2,
                    Category = "AI Model",
                    Severity = DifferenceSeverity.Critical,
                    Description = ai1 == null ? "AI model configuration is missing in first profile" : "AI model configuration is missing in second profile",
                    Impact = "AI functionality may not work properly"
                });
                return;
            }

            CompareProperty("AIModelConfiguration.Provider", ai1.Provider, ai2.Provider, result, "AI Model", DifferenceSeverity.High);
            CompareProperty("AIModelConfiguration.ModelName", ai1.ModelName, ai2.ModelName, result, "AI Model", DifferenceSeverity.High);
            CompareProperty("AIModelConfiguration.BaseUrl", ai1.BaseUrl, ai2.BaseUrl, result, "AI Model", DifferenceSeverity.Medium);
            
            if (!options.IncludeSensitiveData)
            {
                // Compare API key presence but not value
                var hasKey1 = !string.IsNullOrEmpty(ai1.ApiKey);
                var hasKey2 = !string.IsNullOrEmpty(ai2.ApiKey);
                CompareProperty("AIModelConfiguration.HasApiKey", hasKey1, hasKey2, result, "AI Model", DifferenceSeverity.Critical);
            }
            else
            {
                CompareProperty("AIModelConfiguration.ApiKey", ai1.ApiKey, ai2.ApiKey, result, "AI Model", DifferenceSeverity.Critical);
            }
        }

        private async Task CompareSettingsAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ConfigurationComparisonResult result, ComparisonOptions options)
        {
            var settings1 = profile1.Settings ?? new Dictionary<string, object>();
            var settings2 = profile2.Settings ?? new Dictionary<string, object>();

            var allKeys = settings1.Keys.Union(settings2.Keys).ToList();

            foreach (var key in allKeys)
            {
                if (options.ExcludeProperties.Contains($"Settings.{key}")) continue;
                if (options.FocusProperties.Any() && !options.FocusProperties.Contains($"Settings.{key}")) continue;

                var hasKey1 = settings1.ContainsKey(key);
                var hasKey2 = settings2.ContainsKey(key);

                if (hasKey1 && hasKey2)
                {
                    CompareProperty($"Settings.{key}", settings1[key], settings2[key], result, "Settings", DifferenceSeverity.Medium);
                    
                    // Add to common settings if values are equal
                    if (AreValuesEqual(settings1[key], settings2[key], options))
                    {
                        result.CommonSettings[key] = settings1[key];
                    }
                }
                else if (hasKey1)
                {
                    result.Differences.Add(new ConfigurationDifference
                    {
                        PropertyPath = $"Settings.{key}",
                        Type = DifferenceType.Removed,
                        Value1 = settings1[key],
                        Value2 = null,
                        Category = "Settings",
                        Severity = DifferenceSeverity.Medium,
                        Description = $"Setting '{key}' exists in first profile but not in second"
                    });
                    result.UniqueToProfile1[key] = settings1[key];
                }
                else
                {
                    result.Differences.Add(new ConfigurationDifference
                    {
                        PropertyPath = $"Settings.{key}",
                        Type = DifferenceType.Added,
                        Value1 = null,
                        Value2 = settings2[key],
                        Category = "Settings",
                        Severity = DifferenceSeverity.Medium,
                        Description = $"Setting '{key}' exists in second profile but not in first"
                    });
                    result.UniqueToProfile2[key] = settings2[key];
                }
            }
        }

        private async Task CompareAdvancedParametersAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, ConfigurationComparisonResult result, ComparisonOptions options)
        {
            var params1 = profile1.AdvancedParameters;
            var params2 = profile2.AdvancedParameters;

            if (params1 == null && params2 == null) return;

            if (params1 == null || params2 == null)
            {
                result.Differences.Add(new ConfigurationDifference
                {
                    PropertyPath = "AdvancedParameters",
                    Type = params1 == null ? DifferenceType.Removed : DifferenceType.Added,
                    Value1 = params1,
                    Value2 = params2,
                    Category = "Advanced",
                    Severity = DifferenceSeverity.Medium,
                    Description = params1 == null ? "Advanced parameters missing in first profile" : "Advanced parameters missing in second profile"
                });
                return;
            }

            CompareProperty("AdvancedParameters.Temperature", params1.Temperature, params2.Temperature, result, "Advanced", DifferenceSeverity.Low);
            CompareProperty("AdvancedParameters.MaxTokens", params1.MaxTokens, params2.MaxTokens, result, "Advanced", DifferenceSeverity.Medium);
            CompareProperty("AdvancedParameters.TopP", params1.TopP, params2.TopP, result, "Advanced", DifferenceSeverity.Low);
            CompareProperty("AdvancedParameters.FrequencyPenalty", params1.FrequencyPenalty, params2.FrequencyPenalty, result, "Advanced", DifferenceSeverity.Low);
            CompareProperty("AdvancedParameters.PresencePenalty", params1.PresencePenalty, params2.PresencePenalty, result, "Advanced", DifferenceSeverity.Low);
        }

        private void CompareProperty(string propertyPath, object value1, object value2, ConfigurationComparisonResult result, string category, DifferenceSeverity severity)
        {
            if (!AreValuesEqual(value1, value2, result.Options))
            {
                var differenceType = DifferenceType.Modified;
                if (value1 == null) differenceType = DifferenceType.Added;
                else if (value2 == null) differenceType = DifferenceType.Removed;

                result.Differences.Add(new ConfigurationDifference
                {
                    PropertyPath = propertyPath,
                    Type = differenceType,
                    Value1 = value1,
                    Value2 = value2,
                    Category = category,
                    Severity = severity,
                    Description = $"Property '{propertyPath}' differs between profiles",
                    Impact = GetImpactDescription(propertyPath, severity),
                    SuggestedResolution = GetSuggestedResolution(propertyPath, differenceType)
                });
            }
        }

        private bool AreValuesEqual(object value1, object value2, ComparisonOptions options)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            if (value1.GetType() != value2.GetType()) return false;

            if (value1 is string str1 && value2 is string str2)
            {
                var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (options.IgnoreWhitespace)
                {
                    str1 = str1?.Trim();
                    str2 = str2?.Trim();
                }
                return string.Equals(str1, str2, comparison);
            }

            if (value1 is double d1 && value2 is double d2)
            {
                return Math.Abs(d1 - d2) <= options.NumericTolerance;
            }

            if (value1 is float f1 && value2 is float f2)
            {
                return Math.Abs(f1 - f2) <= options.NumericTolerance;
            }

            return value1.Equals(value2);
        }

        private double CalculateSimilarityScore(ConfigurationComparisonResult result, ComparisonOptions options)
        {
            if (!options.CalculateSimilarity) return 0;

            var totalProperties = GetTotalPropertyCount(result.Profile1) + GetTotalPropertyCount(result.Profile2);
            if (totalProperties == 0) return 100;

            var differenceCount = result.Differences.Count;
            var similarityScore = Math.Max(0, 100 - (differenceCount * 100.0 / totalProperties * 2));

            // Apply weights if specified
            if (options.PropertyWeights.Any())
            {
                var weightedScore = 0.0;
                var totalWeight = 0.0;

                foreach (var difference in result.Differences)
                {
                    var weight = options.PropertyWeights.GetValueOrDefault(difference.Category, 1.0);
                    weightedScore += weight * (100 - GetSeverityPenalty(difference.Severity));
                    totalWeight += weight;
                }

                if (totalWeight > 0)
                {
                    similarityScore = weightedScore / totalWeight;
                }
            }

            return Math.Round(similarityScore, 2);
        }

        private int GetTotalPropertyCount(ConfigurationProfile profile)
        {
            var count = 5; // Basic properties: Name, Description, Version, IsActive, IsDefault
            
            if (profile.AIModelConfiguration != null)
            {
                count += 4; // Provider, ModelName, BaseUrl, ApiKey
            }
            
            if (profile.Settings != null)
            {
                count += profile.Settings.Count;
            }
            
            if (profile.AdvancedParameters != null)
            {
                count += 5; // Temperature, MaxTokens, TopP, FrequencyPenalty, PresencePenalty
            }
            
            return count;
        }

        private double GetSeverityPenalty(DifferenceSeverity severity)
        {
            return severity switch
            {
                DifferenceSeverity.Info => 5,
                DifferenceSeverity.Low => 10,
                DifferenceSeverity.Medium => 20,
                DifferenceSeverity.High => 40,
                DifferenceSeverity.Critical => 80,
                _ => 20
            };
        }

        private void GenerateDifferenceSummary(ConfigurationComparisonResult result)
        {
            result.DifferenceSummary = result.Differences
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private async Task FindCommonSettingsAsync(List<ConfigurationProfile> profiles, ConfigurationMultiComparisonResult result)
        {
            if (!profiles.Any()) return;

            var firstProfile = profiles.First();
            var commonSettings = new Dictionary<string, object>(firstProfile.Settings ?? new Dictionary<string, object>());

            foreach (var profile in profiles.Skip(1))
            {
                var profileSettings = profile.Settings ?? new Dictionary<string, object>();
                var keysToRemove = new List<string>();

                foreach (var kvp in commonSettings.ToList())
                {
                    if (!profileSettings.ContainsKey(kvp.Key) || !profileSettings[kvp.Key].Equals(kvp.Value))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    commonSettings.Remove(key);
                }
            }

            result.CommonSettings = commonSettings;
        }

        private async Task FindUniqueSettingsAsync(List<ConfigurationProfile> profiles, ConfigurationMultiComparisonResult result)
        {
            foreach (var profile in profiles)
            {
                var uniqueSettings = new Dictionary<string, object>();
                var profileSettings = profile.Settings ?? new Dictionary<string, object>();

                foreach (var kvp in profileSettings)
                {
                    var isUnique = true;
                    foreach (var otherProfile in profiles.Where(p => p.Id != profile.Id))
                    {
                        var otherSettings = otherProfile.Settings ?? new Dictionary<string, object>();
                        if (otherSettings.ContainsKey(kvp.Key) && otherSettings[kvp.Key].Equals(kvp.Value))
                        {
                            isUnique = false;
                            break;
                        }
                    }

                    if (isUnique)
                    {
                        uniqueSettings[kvp.Key] = kvp.Value;
                    }
                }

                result.UniqueSettings[profile.Id] = uniqueSettings;
            }
        }

        private async Task AnalyzeTemplateComplianceAsync(ConfigurationProfile profile, ConfigurationTemplate template, ConfigurationComparisonResult result)
        {
            // Check for missing required properties
            foreach (var requiredProperty in template.RequiredProperties)
            {
                if (!profile.Settings?.ContainsKey(requiredProperty) == true)
                {
                    result.Differences.Add(new ConfigurationDifference
                    {
                        PropertyPath = $"Settings.{requiredProperty}",
                        Type = DifferenceType.Removed,
                        Value1 = null,
                        Value2 = template.ExpectedProperties.GetValueOrDefault(requiredProperty),
                        Category = "Template Compliance",
                        Severity = DifferenceSeverity.Critical,
                        Description = $"Required property '{requiredProperty}' is missing",
                        Impact = "Template compliance violation",
                        SuggestedResolution = $"Add the required property '{requiredProperty}'"
                    });
                }
            }
        }

        private string GenerateTextReport(ConfigurationComparisonResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Configuration Comparison Report");
            sb.AppendLine($"Generated: {result.ComparedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duration: {result.Duration.TotalMilliseconds:F0}ms");
            sb.AppendLine();
            
            sb.AppendLine($"Profile 1: {result.Profile1.Name} (Version: {result.Profile1.Version})");
            sb.AppendLine($"Profile 2: {result.Profile2.Name} (Version: {result.Profile2.Version})");
            sb.AppendLine();
            
            sb.AppendLine($"Similarity Score: {result.SimilarityScore:F2}%");
            sb.AppendLine($"Are Identical: {result.AreIdentical}");
            sb.AppendLine($"Total Differences: {result.Differences.Count}");
            sb.AppendLine();

            if (result.Differences.Any())
            {
                sb.AppendLine("Differences:");
                foreach (var diff in result.Differences)
                {
                    sb.AppendLine($"  - {diff.PropertyPath}: {diff.Type} ({diff.Severity})");
                    sb.AppendLine($"    Description: {diff.Description}");
                    if (!string.IsNullOrEmpty(diff.Impact))
                        sb.AppendLine($"    Impact: {diff.Impact}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private string GenerateHtmlReport(ConfigurationComparisonResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine($"<h1>Configuration Comparison Report</h1>");
            sb.AppendLine($"<p><strong>Generated:</strong> {result.ComparedAt:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"<p><strong>Similarity Score:</strong> {result.SimilarityScore:F2}%</p>");
            
            if (result.Differences.Any())
            {
                sb.AppendLine("<h2>Differences</h2>");
                sb.AppendLine("<table border='1'>");
                sb.AppendLine("<tr><th>Property</th><th>Type</th><th>Severity</th><th>Description</th></tr>");
                
                foreach (var diff in result.Differences)
                {
                    sb.AppendLine($"<tr><td>{diff.PropertyPath}</td><td>{diff.Type}</td><td>{diff.Severity}</td><td>{diff.Description}</td></tr>");
                }
                
                sb.AppendLine("</table>");
            }
            
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateMarkdownReport(ConfigurationComparisonResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Configuration Comparison Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {result.ComparedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Similarity Score:** {result.SimilarityScore:F2}%");
            sb.AppendLine($"**Total Differences:** {result.Differences.Count}");
            sb.AppendLine();

            if (result.Differences.Any())
            {
                sb.AppendLine("## Differences");
                sb.AppendLine();
                sb.AppendLine("| Property | Type | Severity | Description |");
                sb.AppendLine("|----------|------|----------|-------------|");
                
                foreach (var diff in result.Differences)
                {
                    sb.AppendLine($"| {diff.PropertyPath} | {diff.Type} | {diff.Severity} | {diff.Description} |");
                }
            }

            return sb.ToString();
        }

        private string AssessOverallImpact(DifferenceSummary summary)
        {
            if (summary.TotalDifferences == 0) return "No impact - profiles are identical";
            
            var criticalCount = summary.DifferencesBySeverity.GetValueOrDefault(DifferenceSeverity.Critical, 0);
            var highCount = summary.DifferencesBySeverity.GetValueOrDefault(DifferenceSeverity.High, 0);
            
            if (criticalCount > 0) return "High impact - critical differences found";
            if (highCount > 3) return "Medium-high impact - multiple high-severity differences";
            if (highCount > 0) return "Medium impact - high-severity differences found";
            
            return "Low impact - mostly minor differences";
        }

        private CompatibilityLevel AssessCompatibilityLevel(DifferenceSummary summary)
        {
            if (summary.TotalDifferences == 0) return CompatibilityLevel.FullyCompatible;
            
            var criticalCount = summary.DifferencesBySeverity.GetValueOrDefault(DifferenceSeverity.Critical, 0);
            var highCount = summary.DifferencesBySeverity.GetValueOrDefault(DifferenceSeverity.High, 0);
            var mediumCount = summary.DifferencesBySeverity.GetValueOrDefault(DifferenceSeverity.Medium, 0);
            
            if (criticalCount > 2) return CompatibilityLevel.Incompatible;
            if (criticalCount > 0 || highCount > 5) return CompatibilityLevel.LimitedCompatibility;
            if (highCount > 2 || mediumCount > 10) return CompatibilityLevel.PartiallyCompatible;
            if (highCount > 0 || mediumCount > 5) return CompatibilityLevel.MostlyCompatible;
            
            return CompatibilityLevel.FullyCompatible;
        }

        private string GetImpactDescription(string propertyPath, DifferenceSeverity severity)
        {
            return severity switch
            {
                DifferenceSeverity.Critical => "May cause functionality to break or behave unexpectedly",
                DifferenceSeverity.High => "May significantly affect behavior or performance",
                DifferenceSeverity.Medium => "May cause minor behavioral differences",
                DifferenceSeverity.Low => "Minimal impact on functionality",
                DifferenceSeverity.Info => "Informational difference with no functional impact",
                _ => "Unknown impact"
            };
        }

        private string GetSuggestedResolution(string propertyPath, DifferenceType differenceType)
        {
            return differenceType switch
            {
                DifferenceType.Added => $"Consider removing '{propertyPath}' from the second profile or adding it to the first",
                DifferenceType.Removed => $"Consider adding '{propertyPath}' to the second profile or removing it from the first",
                DifferenceType.Modified => $"Review the values for '{propertyPath}' and choose the appropriate one",
                DifferenceType.TypeChanged => $"Ensure '{propertyPath}' has the same data type in both profiles",
                _ => "Review and resolve the difference manually"
            };
        }

        private async Task MergePreferFirst(ConfigurationComparisonResult comparisonResult, ConfigurationProfile mergedProfile)
        {
            var profile1 = comparisonResult.Profile1;
            
            mergedProfile.AIModelConfiguration = profile1.AIModelConfiguration;
            mergedProfile.AdvancedParameters = profile1.AdvancedParameters;
            mergedProfile.Settings = new Dictionary<string, object>(profile1.Settings ?? new Dictionary<string, object>());
            
            // Add unique settings from second profile
            var profile2Settings = comparisonResult.Profile2.Settings ?? new Dictionary<string, object>();
            foreach (var kvp in profile2Settings)
            {
                if (!mergedProfile.Settings.ContainsKey(kvp.Key))
                {
                    mergedProfile.Settings[kvp.Key] = kvp.Value;
                }
            }
        }

        private async Task MergePreferSecond(ConfigurationComparisonResult comparisonResult, ConfigurationProfile mergedProfile)
        {
            var profile2 = comparisonResult.Profile2;
            
            mergedProfile.AIModelConfiguration = profile2.AIModelConfiguration;
            mergedProfile.AdvancedParameters = profile2.AdvancedParameters;
            mergedProfile.Settings = new Dictionary<string, object>(profile2.Settings ?? new Dictionary<string, object>());
            
            // Add unique settings from first profile
            var profile1Settings = comparisonResult.Profile1.Settings ?? new Dictionary<string, object>();
            foreach (var kvp in profile1Settings)
            {
                if (!mergedProfile.Settings.ContainsKey(kvp.Key))
                {
                    mergedProfile.Settings[kvp.Key] = kvp.Value;
                }
            }
        }

        private async Task MergeNonConflicting(ConfigurationComparisonResult comparisonResult, ConfigurationProfile mergedProfile)
        {
            // Start with common settings
            mergedProfile.Settings = new Dictionary<string, object>(comparisonResult.CommonSettings);
            
            // Add non-conflicting unique settings
            foreach (var kvp in comparisonResult.UniqueToProfile1)
            {
                mergedProfile.Settings[kvp.Key] = kvp.Value;
            }
            
            foreach (var kvp in comparisonResult.UniqueToProfile2)
            {
                mergedProfile.Settings[kvp.Key] = kvp.Value;
            }
            
            // For AI configuration and advanced parameters, prefer the one that's not null
            mergedProfile.AIModelConfiguration = comparisonResult.Profile1.AIModelConfiguration ?? comparisonResult.Profile2.AIModelConfiguration;
            mergedProfile.AdvancedParameters = comparisonResult.Profile1.AdvancedParameters ?? comparisonResult.Profile2.AdvancedParameters;
        }

        private async Task MergeSmartStrategy(ConfigurationComparisonResult comparisonResult, ConfigurationProfile mergedProfile)
        {
            // Use smart rules to merge configurations
            mergedProfile.Settings = new Dictionary<string, object>(comparisonResult.CommonSettings);
            
            // Smart merge based on property types and values
            foreach (var difference in comparisonResult.Differences)
            {
                if (difference.Type == DifferenceType.Modified)
                {
                    // Choose the non-null/non-empty value
                    var value1 = difference.Value1;
                    var value2 = difference.Value2;
                    
                    if (IsNullOrEmpty(value1) && !IsNullOrEmpty(value2))
                    {
                        SetPropertyValue(mergedProfile, difference.PropertyPath, value2);
                    }
                    else if (!IsNullOrEmpty(value1) && IsNullOrEmpty(value2))
                    {
                        SetPropertyValue(mergedProfile, difference.PropertyPath, value1);
                    }
                    else
                    {
                        // Prefer the "newer" value or use other smart rules
                        SetPropertyValue(mergedProfile, difference.PropertyPath, value2);
                    }
                }
                else if (difference.Type == DifferenceType.Added)
                {
                    SetPropertyValue(mergedProfile, difference.PropertyPath, difference.Value2);
                }
                else if (difference.Type == DifferenceType.Removed)
                {
                    SetPropertyValue(mergedProfile, difference.PropertyPath, difference.Value1);
                }
            }
        }

        private bool IsNullOrEmpty(object value)
        {
            return value == null || 
                   (value is string str && string.IsNullOrWhiteSpace(str)) ||
                   (value is Array arr && arr.Length == 0);
        }

        private void SetPropertyValue(ConfigurationProfile profile, string propertyPath, object value)
        {
            if (propertyPath.StartsWith("Settings."))
            {
                var key = propertyPath.Substring("Settings.".Length);
                profile.Settings[key] = value;
            }
            // Handle other property paths as needed
        }

        private string GetHighestVersion(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1)) return version2 ?? "1.0";
            if (string.IsNullOrEmpty(version2)) return version1;
            
            if (Version.TryParse(version1, out var v1) && Version.TryParse(version2, out var v2))
            {
                return v1 > v2 ? version1 : version2;
            }
            
            return version2; // Prefer second if parsing fails
        }

        private List<string> ExtractKeySimilarities(ConfigurationComparisonResult comparison)
        {
            var similarities = new List<string>();
            
            if (comparison.Profile1.AIModelConfiguration?.Provider == comparison.Profile2.AIModelConfiguration?.Provider)
            {
                similarities.Add($"Same AI Provider: {comparison.Profile1.AIModelConfiguration.Provider}");
            }
            
            if (comparison.CommonSettings.Any())
            {
                similarities.Add($"Common settings: {string.Join(", ", comparison.CommonSettings.Keys.Take(3))}");
            }
            
            return similarities;
        }

        private List<string> ExtractKeyDifferences(ConfigurationComparisonResult comparison)
        {
            return comparison.Differences
                .Where(d => d.Severity >= DifferenceSeverity.Medium)
                .Take(5)
                .Select(d => d.Description)
                .ToList();
        }

        /// <summary>
        /// Gets formatted diff between two configurations
        /// </summary>
        public async Task<string> GetFormattedDiffAsync(ConfigurationProfile profile1, ConfigurationProfile profile2, DiffFormat format = DiffFormat.Unified)
        {
            try
            {
                var comparison = await CompareProfilesAsync(profile1, profile2);
                
                return format switch
                {
                    DiffFormat.Unified => GenerateUnifiedDiff(comparison),
                    DiffFormat.SideBySide => GenerateSideBySideDiff(comparison),
                    DiffFormat.Json => SecureJsonSerializer.Serialize(comparison.Differences),
                    DiffFormat.Html => GenerateHtmlDiff(comparison),
                    DiffFormat.Context => GenerateContextDiff(comparison),
                    _ => GenerateUnifiedDiff(comparison)
                };
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.GetFormattedDiffAsync");
                throw;
            }
        }

        /// <summary>
        /// Finds conflicts between multiple profiles
        /// </summary>
        public async Task<List<ConfigurationConflict>> FindConflictsAsync(List<ConfigurationProfile> profiles)
        {
            try
            {
                var conflicts = new List<ConfigurationConflict>();
                var propertyValues = new Dictionary<string, Dictionary<string, object>>();

                // Collect all property values from all profiles
                foreach (var profile in profiles)
                {
                    var settings = profile.Settings ?? new Dictionary<string, object>();
                    foreach (var kvp in settings)
                    {
                        if (!propertyValues.ContainsKey(kvp.Key))
                            propertyValues[kvp.Key] = new Dictionary<string, object>();

                        propertyValues[kvp.Key][profile.Id] = kvp.Value;
                    }
                }

                // Find properties with conflicting values
                foreach (var property in propertyValues)
                {
                    var uniqueValues = property.Value.Values.Distinct().ToList();
                    if (uniqueValues.Count > 1)
                    {
                        var conflict = new ConfigurationConflict
                        {
                            PropertyPath = $"Settings.{property.Key}",
                            Description = $"Property '{property.Key}' has different values across profiles",
                            ConflictingValues = property.Value,
                            Severity = DetermineConflictSeverity(property.Key),
                            ResolutionStrategies = GetResolutionStrategies(property.Key, uniqueValues),
                            RecommendedResolution = GetRecommendedResolution(property.Key, uniqueValues)
                        };

                        conflicts.Add(conflict);
                    }
                }

                return conflicts.OrderByDescending(c => c.Severity).ToList();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.FindConflictsAsync");
                throw;
            }
        }

        /// <summary>
        /// Exports comparison result to file
        /// </summary>
        public async Task ExportComparisonAsync(ConfigurationComparisonResult comparisonResult, string filePath, ExportFormat format = ExportFormat.Json)
        {
            try
            {
                string content = format switch
                {
                    ExportFormat.Json => SecureJsonSerializer.Serialize(comparisonResult),
                    ExportFormat.Xml => ConvertToXml(comparisonResult),
                    ExportFormat.Csv => ConvertToCsv(comparisonResult),
                    ExportFormat.Yaml => ConvertToYaml(comparisonResult),
                    _ => SecureJsonSerializer.Serialize(comparisonResult)
                };

                await System.IO.File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationComparisonService.ExportComparisonAsync");
                throw;
            }
        }

        private string GenerateUnifiedDiff(ConfigurationComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- {comparison.Profile1.Name}");
            sb.AppendLine($"+++ {comparison.Profile2.Name}");
            sb.AppendLine($"@@ Comparison Results @@");

            foreach (var diff in comparison.Differences)
            {
                switch (diff.Type)
                {
                    case DifferenceType.Removed:
                        sb.AppendLine($"- {diff.PropertyPath}: {diff.Value1}");
                        break;
                    case DifferenceType.Added:
                        sb.AppendLine($"+ {diff.PropertyPath}: {diff.Value2}");
                        break;
                    case DifferenceType.Modified:
                        sb.AppendLine($"- {diff.PropertyPath}: {diff.Value1}");
                        sb.AppendLine($"+ {diff.PropertyPath}: {diff.Value2}");
                        break;
                }
            }

            return sb.ToString();
        }

        private string GenerateSideBySideDiff(ConfigurationComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{comparison.Profile1.Name,-40} | {comparison.Profile2.Name}");
            sb.AppendLine(new string('-', 85));

            foreach (var diff in comparison.Differences)
            {
                var left = diff.Value1?.ToString() ?? "<null>";
                var right = diff.Value2?.ToString() ?? "<null>";
                sb.AppendLine($"{left,-40} | {right}");
            }

            return sb.ToString();
        }

        private string GenerateHtmlDiff(ConfigurationComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div class='diff-container'>");
            sb.AppendLine($"<h3>Comparison: {comparison.Profile1.Name} vs {comparison.Profile2.Name}</h3>");

            foreach (var diff in comparison.Differences)
            {
                var cssClass = diff.Type switch
                {
                    DifferenceType.Added => "diff-added",
                    DifferenceType.Removed => "diff-removed", 
                    DifferenceType.Modified => "diff-modified",
                    _ => "diff-unchanged"
                };

                sb.AppendLine($"<div class='{cssClass}'>");
                sb.AppendLine($"  <strong>{diff.PropertyPath}</strong>: {diff.Value1} → {diff.Value2}");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private string GenerateContextDiff(ConfigurationComparisonResult comparison)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"*** {comparison.Profile1.Name} ***");
            sb.AppendLine($"--- {comparison.Profile2.Name} ---");

            foreach (var diff in comparison.Differences)
            {
                sb.AppendLine($"***************");
                sb.AppendLine($"*** {diff.PropertyPath} ***");
                sb.AppendLine($"! {diff.Value1}");
                sb.AppendLine($"--- {diff.PropertyPath} ---");
                sb.AppendLine($"! {diff.Value2}");
            }

            return sb.ToString();
        }

        private DifferenceSeverity DetermineConflictSeverity(string propertyKey)
        {
            if (propertyKey.Contains("ApiKey") || propertyKey.Contains("Password"))
                return DifferenceSeverity.Critical;
            if (propertyKey.Contains("Model") || propertyKey.Contains("Provider"))
                return DifferenceSeverity.High;
            if (propertyKey.Contains("UI") || propertyKey.Contains("Theme"))
                return DifferenceSeverity.Low;
            return DifferenceSeverity.Medium;
        }

        private List<string> GetResolutionStrategies(string propertyKey, List<object> values)
        {
            var strategies = new List<string>();
            
            if (propertyKey.Contains("ApiKey"))
            {
                strategies.Add("Use the API key that has valid access");
                strategies.Add("Test each API key for connectivity");
            }
            else if (propertyKey.Contains("Model"))
            {
                strategies.Add("Choose the most capable model");
                strategies.Add("Select based on cost considerations");
                strategies.Add("Pick the fastest model for performance");
            }
            else
            {
                strategies.Add("Manual review required");
                strategies.Add("Choose the most recent value");
                strategies.Add("Select based on user preference");
            }

            return strategies;
        }

        private string GetRecommendedResolution(string propertyKey, List<object> values)
        {
            if (propertyKey.Contains("ApiKey"))
                return "Verify API key validity and choose the working one";
            if (propertyKey.Contains("Model"))
                return "Select the model that best fits your use case";
            if (propertyKey.Contains("UI") || propertyKey.Contains("Theme"))
                return "Choose based on user preference";
            
            return "Review values and select manually";
        }

        private string ConvertToXml(ConfigurationComparisonResult result)
        {
            // Simple XML conversion - in a real implementation, use XmlSerializer
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<ComparisonResult>");
            sb.AppendLine($"  <SimilarityScore>{result.SimilarityScore}</SimilarityScore>");
            sb.AppendLine($"  <DifferenceCount>{result.Differences.Count}</DifferenceCount>");
            sb.AppendLine("  <Differences>");
            
            foreach (var diff in result.Differences)
            {
                sb.AppendLine("    <Difference>");
                sb.AppendLine($"      <PropertyPath>{diff.PropertyPath}</PropertyPath>");
                sb.AppendLine($"      <Type>{diff.Type}</Type>");
                sb.AppendLine($"      <Severity>{diff.Severity}</Severity>");
                sb.AppendLine("    </Difference>");
            }
            
            sb.AppendLine("  </Differences>");
            sb.AppendLine("</ComparisonResult>");
            return sb.ToString();
        }

        private string ConvertToCsv(ConfigurationComparisonResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PropertyPath,Type,Severity,Value1,Value2,Description");
            
            foreach (var diff in result.Differences)
            {
                sb.AppendLine($"\"{diff.PropertyPath}\",\"{diff.Type}\",\"{diff.Severity}\",\"{diff.Value1}\",\"{diff.Value2}\",\"{diff.Description}\"");
            }
            
            return sb.ToString();
        }

        private string ConvertToYaml(ConfigurationComparisonResult result)
        {
            // Simple YAML conversion - in a real implementation, use YamlDotNet
            var sb = new StringBuilder();
            sb.AppendLine("comparisonResult:");
            sb.AppendLine($"  similarityScore: {result.SimilarityScore}");
            sb.AppendLine($"  differenceCount: {result.Differences.Count}");
            sb.AppendLine("  differences:");
            
            foreach (var diff in result.Differences)
            {
                sb.AppendLine($"    - propertyPath: \"{diff.PropertyPath}\"");
                sb.AppendLine($"      type: \"{diff.Type}\"");
                sb.AppendLine($"      severity: \"{diff.Severity}\"");
                sb.AppendLine($"      description: \"{diff.Description}\"");
            }
            
            return sb.ToString();
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}