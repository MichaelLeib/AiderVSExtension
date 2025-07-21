using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for configuration analytics and monitoring
    /// </summary>
    public class ConfigurationAnalyticsService : IConfigurationAnalyticsService, IDisposable
    {
        private readonly IAdvancedConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private readonly List<ConfigurationUsageMetric> _usageMetrics = new List<ConfigurationUsageMetric>();
        private readonly List<ConfigurationPerformanceMetric> _performanceMetrics = new List<ConfigurationPerformanceMetric>();
        private readonly Dictionary<string, ConfigurationHealthStatus> _healthStatuses = new Dictionary<string, ConfigurationHealthStatus>();
        private bool _disposed = false;

        public event EventHandler<AnalyticsEventArgs> MetricRecorded;
        public event EventHandler<HealthStatusChangedEventArgs> HealthStatusChanged;
        public event EventHandler<RecommendationGeneratedEventArgs> RecommendationGenerated;

        public ConfigurationAnalyticsService(
            IAdvancedConfigurationService configurationService,
            IErrorHandler errorHandler)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Records configuration usage metric
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="action">Action performed</param>
        /// <param name="metadata">Additional metadata</param>
        public async Task RecordUsageAsync(string profileId, string action, Dictionary<string, object> metadata = null)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    Id = Guid.NewGuid().ToString(),
                    ProfileId = profileId,
                    Action = action,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    SessionId = GetCurrentSessionId(),
                    UserId = Environment.UserName
                };

                _usageMetrics.Add(metric);

                // Fire event
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs
                {
                    MetricType = AnalyticsMetricType.Usage,
                    Metric = metric
                });

                // Cleanup old metrics (keep last 1000)
                if (_usageMetrics.Count > 1000)
                {
                    _usageMetrics.RemoveRange(0, _usageMetrics.Count - 1000);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.RecordUsageAsync");
            }
        }

        /// <summary>
        /// Records configuration performance metric
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="operation">Operation performed</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="success">Whether operation was successful</param>
        /// <param name="metadata">Additional metadata</param>
        public async Task RecordPerformanceAsync(string profileId, string operation, TimeSpan duration, bool success, Dictionary<string, object> metadata = null)
        {
            try
            {
                var metric = new ConfigurationPerformanceMetric
                {
                    Id = Guid.NewGuid().ToString(),
                    ProfileId = profileId,
                    Operation = operation,
                    Duration = duration,
                    Success = success,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    SessionId = GetCurrentSessionId(),
                    MemoryUsage = GC.GetTotalMemory(false)
                };

                _performanceMetrics.Add(metric);

                // Fire event
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs
                {
                    MetricType = AnalyticsMetricType.Performance,
                    Metric = metric
                });

                // Cleanup old metrics (keep last 1000)
                if (_performanceMetrics.Count > 1000)
                {
                    _performanceMetrics.RemoveRange(0, _performanceMetrics.Count - 1000);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.RecordPerformanceAsync");
            }
        }

        /// <summary>
        /// Gets configuration usage analytics
        /// </summary>
        /// <param name="profileId">Profile ID (optional)</param>
        /// <param name="timeRange">Time range for analytics</param>
        /// <returns>Usage analytics</returns>
        public async Task<ConfigurationUsageAnalytics> GetUsageAnalyticsAsync(string profileId = null, TimeRange timeRange = null)
        {
            try
            {
                timeRange = timeRange ?? TimeRange.LastWeek();
                var metrics = _usageMetrics
                    .Where(m => (string.IsNullOrEmpty(profileId) || m.ProfileId == profileId) &&
                               m.Timestamp >= timeRange.StartTime &&
                               m.Timestamp <= timeRange.EndTime)
                    .ToList();

                var analytics = new ConfigurationUsageAnalytics
                {
                    ProfileId = profileId,
                    TimeRange = timeRange,
                    TotalActions = metrics.Count,
                    UniqueProfiles = metrics.Select(m => m.ProfileId).Distinct().Count(),
                    UniqueSessions = metrics.Select(m => m.SessionId).Distinct().Count(),
                    ActionCounts = metrics.GroupBy(m => m.Action)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    DailyUsage = metrics.GroupBy(m => m.Timestamp.Date)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TopActions = metrics.GroupBy(m => m.Action)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    AverageActionsPerSession = metrics.Any() ? 
                        metrics.GroupBy(m => m.SessionId).Average(g => g.Count()) : 0
                };

                return analytics;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.GetUsageAnalyticsAsync");
                return new ConfigurationUsageAnalytics();
            }
        }

        /// <summary>
        /// Gets configuration performance analytics
        /// </summary>
        /// <param name="profileId">Profile ID (optional)</param>
        /// <param name="timeRange">Time range for analytics</param>
        /// <returns>Performance analytics</returns>
        public async Task<ConfigurationPerformanceAnalytics> GetPerformanceAnalyticsAsync(string profileId = null, TimeRange timeRange = null)
        {
            try
            {
                timeRange = timeRange ?? TimeRange.LastWeek();
                var metrics = _performanceMetrics
                    .Where(m => (string.IsNullOrEmpty(profileId) || m.ProfileId == profileId) &&
                               m.Timestamp >= timeRange.StartTime &&
                               m.Timestamp <= timeRange.EndTime)
                    .ToList();

                if (!metrics.Any())
                {
                    return new ConfigurationPerformanceAnalytics
                    {
                        ProfileId = profileId,
                        TimeRange = timeRange
                    };
                }

                var analytics = new ConfigurationPerformanceAnalytics
                {
                    ProfileId = profileId,
                    TimeRange = timeRange,
                    TotalOperations = metrics.Count,
                    SuccessfulOperations = metrics.Count(m => m.Success),
                    FailedOperations = metrics.Count(m => !m.Success),
                    SuccessRate = metrics.Count > 0 ? (double)metrics.Count(m => m.Success) / metrics.Count * 100 : 0,
                    AverageDuration = TimeSpan.FromMilliseconds(metrics.Average(m => m.Duration.TotalMilliseconds)),
                    MedianDuration = GetMedianDuration(metrics),
                    MinDuration = TimeSpan.FromMilliseconds(metrics.Min(m => m.Duration.TotalMilliseconds)),
                    MaxDuration = TimeSpan.FromMilliseconds(metrics.Max(m => m.Duration.TotalMilliseconds)),
                    OperationCounts = metrics.GroupBy(m => m.Operation)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    SlowOperations = metrics
                        .Where(m => m.Duration > TimeSpan.FromSeconds(5))
                        .OrderByDescending(m => m.Duration)
                        .Take(10)
                        .ToList(),
                    ErrorsByOperation = metrics
                        .Where(m => !m.Success)
                        .GroupBy(m => m.Operation)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return analytics;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.GetPerformanceAnalyticsAsync");
                return new ConfigurationPerformanceAnalytics();
            }
        }

        /// <summary>
        /// Gets configuration health status
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns>Health status</returns>
        public async Task<ConfigurationHealthStatus> GetHealthStatusAsync(string profileId)
        {
            try
            {
                if (_healthStatuses.TryGetValue(profileId, out var cachedStatus) &&
                    DateTime.UtcNow - cachedStatus.LastUpdated < TimeSpan.FromMinutes(5))
                {
                    return cachedStatus;
                }

                var profile = await _configurationService.GetProfileAsync(profileId);
                if (profile == null)
                {
                    return new ConfigurationHealthStatus
                    {
                        ProfileId = profileId,
                        OverallHealth = HealthLevel.Critical,
                        Issues = new List<HealthIssue>
                        {
                            new HealthIssue
                            {
                                Severity = IssueSeverity.Critical,
                                Category = "Profile",
                                Description = "Profile not found",
                                Impact = "Configuration cannot be used"
                            }
                        }
                    };
                }

                var status = await AnalyzeConfigurationHealthAsync(profile);
                _healthStatuses[profileId] = status;

                return status;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.GetHealthStatusAsync");
                return new ConfigurationHealthStatus
                {
                    ProfileId = profileId,
                    OverallHealth = HealthLevel.Unknown,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Gets configuration recommendations
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns>List of recommendations</returns>
        public async Task<List<ConfigurationRecommendation>> GetRecommendationsAsync(string profileId)
        {
            try
            {
                var recommendations = new List<ConfigurationRecommendation>();
                
                var profile = await _configurationService.GetProfileAsync(profileId);
                if (profile == null)
                {
                    return recommendations;
                }

                // Analyze usage patterns
                var usageAnalytics = await GetUsageAnalyticsAsync(profileId, TimeRange.LastMonth());
                var performanceAnalytics = await GetPerformanceAnalyticsAsync(profileId, TimeRange.LastMonth());
                
                // Generate usage-based recommendations
                recommendations.AddRange(await GenerateUsageRecommendationsAsync(profile, usageAnalytics));
                
                // Generate performance-based recommendations
                recommendations.AddRange(await GeneratePerformanceRecommendationsAsync(profile, performanceAnalytics));
                
                // Generate configuration-based recommendations
                recommendations.AddRange(await GenerateConfigurationRecommendationsAsync(profile));

                // Sort by priority and impact
                recommendations = recommendations
                    .OrderByDescending(r => r.Priority)
                    .ThenByDescending(r => r.ExpectedImpact)
                    .ToList();

                return recommendations;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.GetRecommendationsAsync");
                return new List<ConfigurationRecommendation>();
            }
        }

        /// <summary>
        /// Generates configuration report
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="timeRange">Time range for report</param>
        /// <returns>Configuration report</returns>
        public async Task<ConfigurationReport> GenerateReportAsync(string profileId, TimeRange timeRange = null)
        {
            try
            {
                timeRange = timeRange ?? TimeRange.LastMonth();
                
                var profile = await _configurationService.GetProfileAsync(profileId);
                var usageAnalytics = await GetUsageAnalyticsAsync(profileId, timeRange);
                var performanceAnalytics = await GetPerformanceAnalyticsAsync(profileId, timeRange);
                var healthStatus = await GetHealthStatusAsync(profileId);
                var recommendations = await GetRecommendationsAsync(profileId);

                var report = new ConfigurationReport
                {
                    ProfileId = profileId,
                    ProfileName = profile?.Name,
                    TimeRange = timeRange,
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedBy = Environment.UserName,
                    UsageAnalytics = usageAnalytics,
                    PerformanceAnalytics = performanceAnalytics,
                    HealthStatus = healthStatus,
                    Recommendations = recommendations,
                    Summary = GenerateReportSummary(usageAnalytics, performanceAnalytics, healthStatus)
                };

                return report;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.GenerateReportAsync");
                return new ConfigurationReport
                {
                    ProfileId = profileId,
                    TimeRange = timeRange ?? TimeRange.LastMonth(),
                    GeneratedAt = DateTime.UtcNow,
                    Summary = "Error generating report"
                };
            }
        }

        /// <summary>
        /// Clears analytics data
        /// </summary>
        /// <param name="olderThan">Clear data older than this date</param>
        public async Task ClearAnalyticsDataAsync(DateTime? olderThan = null)
        {
            try
            {
                var cutoffDate = olderThan ?? DateTime.UtcNow.AddDays(-90);
                
                _usageMetrics.RemoveAll(m => m.Timestamp < cutoffDate);
                _performanceMetrics.RemoveAll(m => m.Timestamp < cutoffDate);
                
                // Clear cached health statuses
                _healthStatuses.Clear();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "ConfigurationAnalyticsService.ClearAnalyticsDataAsync");
            }
        }

        #region Private Methods

        private string GetCurrentSessionId()
        {
            // In a real implementation, this would get the current VS session ID
            return "session-" + DateTime.UtcNow.ToString("yyyyMMdd");
        }

        private TimeSpan GetMedianDuration(List<ConfigurationPerformanceMetric> metrics)
        {
            if (!metrics.Any())
                return TimeSpan.Zero;

            var sortedDurations = metrics.Select(m => m.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
            var count = sortedDurations.Count;
            
            if (count % 2 == 0)
            {
                return TimeSpan.FromMilliseconds((sortedDurations[count / 2 - 1] + sortedDurations[count / 2]) / 2);
            }
            else
            {
                return TimeSpan.FromMilliseconds(sortedDurations[count / 2]);
            }
        }

        private async Task<ConfigurationHealthStatus> AnalyzeConfigurationHealthAsync(ConfigurationProfile profile)
        {
            var issues = new List<HealthIssue>();
            var scores = new List<int>();

            // Check basic configuration
            if (string.IsNullOrEmpty(profile.Name))
            {
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "Configuration",
                    Description = "Profile name is empty",
                    Impact = "Profile may be difficult to identify"
                });
                scores.Add(80);
            }
            else
            {
                scores.Add(100);
            }

            // Check AI model configuration
            if (profile.AIModelConfiguration == null)
            {
                issues.Add(new HealthIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = "AI Model",
                    Description = "No AI model configured",
                    Impact = "AI features will not work"
                });
                scores.Add(0);
            }
            else
            {
                if (string.IsNullOrEmpty(profile.AIModelConfiguration.ApiKey) && 
                    profile.AIModelConfiguration.Provider != AIProvider.Ollama)
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Critical,
                        Category = "AI Model",
                        Description = "API key is missing",
                        Impact = "AI model cannot be accessed"
                    });
                    scores.Add(20);
                }
                else
                {
                    scores.Add(100);
                }

                if (string.IsNullOrEmpty(profile.AIModelConfiguration.ModelName))
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = "AI Model",
                        Description = "Model name is missing",
                        Impact = "Default model will be used"
                    });
                    scores.Add(60);
                }
                else
                {
                    scores.Add(100);
                }
            }

            // Check performance metrics
            var recentMetrics = _performanceMetrics
                .Where(m => m.ProfileId == profile.Id && m.Timestamp > DateTime.UtcNow.AddDays(-7))
                .ToList();

            if (recentMetrics.Any())
            {
                var successRate = (double)recentMetrics.Count(m => m.Success) / recentMetrics.Count;
                if (successRate < 0.8)
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Performance",
                        Description = $"Low success rate: {successRate:P1}",
                        Impact = "Operations may be failing frequently"
                    });
                    scores.Add((int)(successRate * 100));
                }
                else
                {
                    scores.Add(100);
                }

                var avgDuration = recentMetrics.Average(m => m.Duration.TotalMilliseconds);
                if (avgDuration > 5000) // 5 seconds
                {
                    issues.Add(new HealthIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "Performance",
                        Description = $"Slow average response time: {avgDuration:F0}ms",
                        Impact = "Operations may be taking too long"
                    });
                    scores.Add(70);
                }
                else
                {
                    scores.Add(100);
                }
            }

            var overallScore = scores.Any() ? (int)scores.Average() : 50;
            var overallHealth = overallScore switch
            {
                >= 90 => HealthLevel.Excellent,
                >= 80 => HealthLevel.Good,
                >= 60 => HealthLevel.Fair,
                >= 40 => HealthLevel.Poor,
                _ => HealthLevel.Critical
            };

            return new ConfigurationHealthStatus
            {
                ProfileId = profile.Id,
                OverallHealth = overallHealth,
                HealthScore = overallScore,
                Issues = issues,
                LastUpdated = DateTime.UtcNow,
                Checks = new Dictionary<string, bool>
                {
                    ["HasName"] = !string.IsNullOrEmpty(profile.Name),
                    ["HasAIModel"] = profile.AIModelConfiguration != null,
                    ["HasApiKey"] = profile.AIModelConfiguration?.ApiKey != null || profile.AIModelConfiguration?.Provider == AIProvider.Ollama,
                    ["HasModelName"] = !string.IsNullOrEmpty(profile.AIModelConfiguration?.ModelName),
                    ["GoodPerformance"] = recentMetrics.Any() && recentMetrics.Count(m => m.Success) / (double)recentMetrics.Count >= 0.8
                }
            };
        }

        private async Task<List<ConfigurationRecommendation>> GenerateUsageRecommendationsAsync(ConfigurationProfile profile, ConfigurationUsageAnalytics analytics)
        {
            var recommendations = new List<ConfigurationRecommendation>();

            if (analytics.TotalActions < 10)
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Usage,
                    Priority = RecommendationPriority.Low,
                    Title = "Low Usage Detected",
                    Description = "This profile has been used very little. Consider exploring more features or removing if not needed.",
                    ExpectedImpact = "Better organization and performance",
                    Category = "Usage",
                    Actions = new List<string> { "Explore features", "Remove unused profile" }
                });
            }

            if (analytics.ActionCounts.ContainsKey("error") && analytics.ActionCounts["error"] > analytics.TotalActions * 0.1)
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Performance,
                    Priority = RecommendationPriority.High,
                    Title = "High Error Rate",
                    Description = "This profile has a high error rate. Check configuration settings.",
                    ExpectedImpact = "Reduced errors and better reliability",
                    Category = "Reliability",
                    Actions = new List<string> { "Check API key", "Verify model settings", "Test configuration" }
                });
            }

            return recommendations;
        }

        private async Task<List<ConfigurationRecommendation>> GeneratePerformanceRecommendationsAsync(ConfigurationProfile profile, ConfigurationPerformanceAnalytics analytics)
        {
            var recommendations = new List<ConfigurationRecommendation>();

            if (analytics.SuccessRate < 80)
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Performance,
                    Priority = RecommendationPriority.High,
                    Title = "Low Success Rate",
                    Description = $"Success rate is {analytics.SuccessRate:F1}%. Consider reviewing configuration.",
                    ExpectedImpact = "Higher success rate and reliability",
                    Category = "Performance",
                    Actions = new List<string> { "Check network connectivity", "Verify API credentials", "Review timeout settings" }
                });
            }

            if (analytics.AverageDuration > TimeSpan.FromSeconds(10))
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Performance,
                    Priority = RecommendationPriority.Medium,
                    Title = "Slow Response Times",
                    Description = $"Average response time is {analytics.AverageDuration.TotalSeconds:F1} seconds.",
                    ExpectedImpact = "Faster response times",
                    Category = "Performance",
                    Actions = new List<string> { "Reduce max tokens", "Use faster model", "Check network latency" }
                });
            }

            return recommendations;
        }

        private async Task<List<ConfigurationRecommendation>> GenerateConfigurationRecommendationsAsync(ConfigurationProfile profile)
        {
            var recommendations = new List<ConfigurationRecommendation>();

            // Check for missing advanced parameters
            if (profile.AdvancedParameters == null)
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Configuration,
                    Priority = RecommendationPriority.Low,
                    Title = "Consider Advanced Parameters",
                    Description = "Advanced parameters are not configured. These can improve AI responses.",
                    ExpectedImpact = "Better AI response quality",
                    Category = "Configuration",
                    Actions = new List<string> { "Configure temperature", "Set max tokens", "Adjust creativity settings" }
                });
            }

            // Check for outdated version
            if (string.IsNullOrEmpty(profile.Version) || profile.Version == "1.0")
            {
                recommendations.Add(new ConfigurationRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = RecommendationType.Update,
                    Priority = RecommendationPriority.Medium,
                    Title = "Update Configuration",
                    Description = "This profile may be using an older configuration format.",
                    ExpectedImpact = "Access to new features and improvements",
                    Category = "Maintenance",
                    Actions = new List<string> { "Run migration wizard", "Update to latest version" }
                });
            }

            return recommendations;
        }

        private string GenerateReportSummary(ConfigurationUsageAnalytics usage, ConfigurationPerformanceAnalytics performance, ConfigurationHealthStatus health)
        {
            var summary = $"Configuration Health: {health.OverallHealth} ({health.HealthScore}/100)\n";
            summary += $"Total Actions: {usage.TotalActions} over {usage.TimeRange.Days} days\n";
            summary += $"Success Rate: {performance.SuccessRate:F1}%\n";
            summary += $"Average Response Time: {performance.AverageDuration.TotalSeconds:F1} seconds\n";
            
            if (health.Issues.Any())
            {
                summary += $"Issues Found: {health.Issues.Count}\n";
            }

            return summary;
        }

        #endregion

        #region IConfigurationAnalyticsService Implementation

        public async Task TrackConfigurationChangeAsync(string configurationKey, object oldValue, object newValue, string source)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    MetricName = "ConfigurationChange",
                    Value = 1,
                    Unit = "count",
                    Timestamp = DateTime.UtcNow,
                    Category = "Change",
                    ConfigurationId = configurationKey,
                    Properties = new Dictionary<string, object>
                    {
                        ["oldValue"] = oldValue,
                        ["newValue"] = newValue,
                        ["source"] = source
                    }
                };

                _usageMetrics.Add(metric);
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs { Metric = metric });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error tracking configuration change: {ex.Message}", ex, "ConfigurationAnalyticsService.TrackConfigurationChangeAsync");
            }
        }

        public async Task TrackConfigurationAccessAsync(string configurationKey, string accessType)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    MetricName = "ConfigurationAccess",
                    Value = 1,
                    Unit = "count",
                    Timestamp = DateTime.UtcNow,
                    Category = "Access",
                    ConfigurationId = configurationKey,
                    Properties = new Dictionary<string, object>
                    {
                        ["accessType"] = accessType
                    }
                };

                _usageMetrics.Add(metric);
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs { Metric = metric });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error tracking configuration access: {ex.Message}", ex, "ConfigurationAnalyticsService.TrackConfigurationAccessAsync");
            }
        }

        public async Task TrackConfigurationValidationAsync(string configurationKey, bool isValid, IEnumerable<string> validationErrors)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    MetricName = "ConfigurationValidation",
                    Value = isValid ? 1 : 0,
                    Unit = "success",
                    Timestamp = DateTime.UtcNow,
                    Category = "Validation",
                    ConfigurationId = configurationKey,
                    Properties = new Dictionary<string, object>
                    {
                        ["isValid"] = isValid,
                        ["errorCount"] = validationErrors?.Count() ?? 0,
                        ["errors"] = validationErrors?.ToList() ?? new List<string>()
                    }
                };

                _usageMetrics.Add(metric);
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs { Metric = metric });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error tracking configuration validation: {ex.Message}", ex, "ConfigurationAnalyticsService.TrackConfigurationValidationAsync");
            }
        }

        public async Task<Dictionary<string, object>> GetConfigurationUsageStatsAsync(TimeSpan timeRange)
        {
            try
            {
                var startTime = DateTime.UtcNow - timeRange;
                var recentMetrics = _usageMetrics.Where(m => m.Timestamp >= startTime).ToList();

                var stats = new Dictionary<string, object>
                {
                    ["totalMetrics"] = recentMetrics.Count,
                    ["timeRange"] = timeRange,
                    ["startTime"] = startTime,
                    ["endTime"] = DateTime.UtcNow,
                    ["metricsByCategory"] = recentMetrics.GroupBy(m => m.Category)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ["metricsByConfiguration"] = recentMetrics.GroupBy(m => m.ConfigurationId)
                        .ToDictionary(g => g.Key ?? "unknown", g => g.Count()),
                    ["averageValue"] = recentMetrics.Any() ? recentMetrics.Average(m => m.Value) : 0
                };

                return await Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error getting configuration usage stats: {ex.Message}", ex, "ConfigurationAnalyticsService.GetConfigurationUsageStatsAsync");
                return new Dictionary<string, object>();
            }
        }

        public async Task TrackConfigurationMigrationAsync(string fromVersion, string toVersion, IEnumerable<string> migratedKeys)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    MetricName = "ConfigurationMigration",
                    Value = migratedKeys?.Count() ?? 0,
                    Unit = "keys",
                    Timestamp = DateTime.UtcNow,
                    Category = "Migration",
                    Properties = new Dictionary<string, object>
                    {
                        ["fromVersion"] = fromVersion,
                        ["toVersion"] = toVersion,
                        ["migratedKeys"] = migratedKeys?.ToList() ?? new List<string>()
                    }
                };

                _usageMetrics.Add(metric);
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs { Metric = metric });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error tracking configuration migration: {ex.Message}", ex, "ConfigurationAnalyticsService.TrackConfigurationMigrationAsync");
            }
        }

        public async Task TrackConfigurationPortabilityAsync(string operation, string format, int keyCount)
        {
            try
            {
                var metric = new ConfigurationUsageMetric
                {
                    MetricName = "ConfigurationPortability",
                    Value = keyCount,
                    Unit = "keys",
                    Timestamp = DateTime.UtcNow,
                    Category = "Portability",
                    Properties = new Dictionary<string, object>
                    {
                        ["operation"] = operation,
                        ["format"] = format,
                        ["keyCount"] = keyCount
                    }
                };

                _usageMetrics.Add(metric);
                MetricRecorded?.Invoke(this, new AnalyticsEventArgs { Metric = metric });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Error tracking configuration portability: {ex.Message}", ex, "ConfigurationAnalyticsService.TrackConfigurationPortabilityAsync");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _usageMetrics.Clear();
                _performanceMetrics.Clear();
                _healthStatuses.Clear();
                _disposed = true;
            }
        }
    }
}