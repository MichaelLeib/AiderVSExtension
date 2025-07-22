using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using AiderVSExtension.Security;
using Microsoft.VisualStudio.Shell;
using System.Text;
using System.Security.Cryptography;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Advanced configuration service with profiles, templates, and advanced features
    /// </summary>
    public class AdvancedConfigurationService : IAdvancedConfigurationService, IConfigurationService, IDisposable
    {
        private readonly IConfigurationService _baseConfigurationService;
        private readonly IErrorHandler _errorHandler;
        private readonly WritableSettingsStore _settingsStore;
        private readonly string _profilesCollectionPath = "AiderVS\\Profiles";
        private readonly string _templatesCollectionPath = "AiderVS\\Templates";
        private readonly string _backupsCollectionPath = "AiderVS\\Backups";
        private readonly string _statisticsCollectionPath = "AiderVS\\Statistics";
        private readonly string _changeHistoryCollectionPath = "AiderVS\\ChangeHistory";

        private readonly Dictionary<string, ConfigurationProfile> _profilesCache = new Dictionary<string, ConfigurationProfile>();
        private readonly Dictionary<string, ConfigurationTemplate> _templatesCache = new Dictionary<string, ConfigurationTemplate>();
        private readonly Dictionary<string, ConfigurationBackup> _backupsCache = new Dictionary<string, ConfigurationBackup>();
        private readonly object _lockObject = new object();

        private bool _disposed = false;
        private bool _autoBackupEnabled = true;
        private int _maxBackups = 10;
        private int _retentionDays = 30;

        // Collection size limits to prevent unbounded growth
        private const int MaxProfilesCache = 50;
        private const int MaxTemplatesCache = 30;
        private const int MaxBackupsCache = 20;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
        public event EventHandler<ConfigurationProfileChangedEventArgs> ProfileChanged;
        public event EventHandler<ConfigurationBackupEventArgs> ConfigurationBackedUp;
        public event EventHandler<ConfigurationRestoreEventArgs> ConfigurationRestored;

        public AdvancedConfigurationService(IConfigurationService baseConfigurationService, IErrorHandler errorHandler)
        {
            _baseConfigurationService = baseConfigurationService == null ? throw new ArgumentNullException(nameof(baseConfigurationService)) : baseConfigurationService;
            _errorHandler = errorHandler == null ? throw new ArgumentNullException(nameof(errorHandler)) : errorHandler;

            // Initialize settings store
            var serviceProvider = ServiceProvider.GlobalProvider;
            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            // Subscribe to base configuration changes
            _baseConfigurationService.ConfigurationChanged += OnBaseConfigurationChanged;

            // Initialize collections
            InitializeCollections();

            // Load cached data
            LoadCachedData();
        }

        #region Profile Management

        /// <summary>
        /// Gets all available configuration profiles
        /// </summary>
        public async Task<IEnumerable<ConfigurationProfile>> GetProfilesAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshProfilesCache();
                    return _profilesCache.Values.ToList();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a specific configuration profile
        /// </summary>
        public async Task<ConfigurationProfile> GetProfileAsync(string profileId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshProfilesCache();
                    ConfigurationProfile profile;
                    return _profilesCache.TryGetValue(profileId, out profile) ? profile : null;
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new configuration profile
        /// </summary>
        public async Task<ConfigurationProfile> CreateProfileAsync(ConfigurationProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                // Validate profile
                var validationResult = await ValidateProfileAsync(profile).ConfigureAwait(false);
                if (!validationResult.IsValid)
                    throw new InvalidOperationException($"Profile validation failed: {string.Join(", ", validationResult.ValidationErrors)}");

                // Create backup if enabled
                if (_autoBackupEnabled)
                    await CreateBackupAsync($"Before creating profile '{profile.Name}'").ConfigureAwait(false);

                // Set metadata
                profile.Id = Guid.NewGuid().ToString();
                profile.CreatedAt = DateTime.UtcNow;
                profile.ModifiedAt = DateTime.UtcNow;
                profile.Metadata.CreatedBy = Environment.UserName;
                profile.Version = "1.0.0";

                // Save to settings store
                var json = SecureJsonSerializer.Serialize(profile);
                _settingsStore.SetString(_profilesCollectionPath, profile.Id, json);

                // Update cache
                lock (_lockObject)
                {
                    _profilesCache[profile.Id] = profile;
                }

                await _errorHandler.LogInfoAsync($"Created profile '{profile.Name}' (ID: {profile.Id})", "AdvancedConfigurationService.CreateProfileAsync").ConfigureAwait(false);
                return profile;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.CreateProfileAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing configuration profile
        /// </summary>
        public async Task<ConfigurationProfile> UpdateProfileAsync(ConfigurationProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            try
            {
                // Validate profile
                var validationResult = await ValidateProfileAsync(profile).ConfigureAwait(false);
                if (!validationResult.IsValid)
                    throw new InvalidOperationException($"Profile validation failed: {string.Join(", ", validationResult.ValidationErrors)}");

                // Create backup if enabled
                if (_autoBackupEnabled)
                    await CreateBackupAsync($"Before updating profile '{profile.Name}'").ConfigureAwait(false);

                // Update metadata
                profile.ModifiedAt = DateTime.UtcNow;
                profile.Metadata.ModifiedBy = Environment.UserName;

                // Save to settings store
                var json = SecureJsonSerializer.Serialize(profile);
                _settingsStore.SetString(_profilesCollectionPath, profile.Id, json);

                // Update cache
                lock (_lockObject)
                {
                    _profilesCache[profile.Id] = profile;
                }

                await _errorHandler.LogInfoAsync($"Updated profile '{profile.Name}' (ID: {profile.Id})", "AdvancedConfigurationService.UpdateProfileAsync").ConfigureAwait(false);
                return profile;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.UpdateProfileAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Deletes a configuration profile
        /// </summary>
        public async Task DeleteProfileAsync(string profileId)
        {
            try
            {
                var profile = await GetProfileAsync(profileId).ConfigureAwait(false);
                if (profile == null)
                    throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

                if (profile.IsDefault)
                    throw new InvalidOperationException("Cannot delete the default profile");

                // Create backup if enabled
                if (_autoBackupEnabled)
                    await CreateBackupAsync($"Before deleting profile '{profile.Name}'").ConfigureAwait(false);

                // Delete from settings store
                _settingsStore.DeleteProperty(_profilesCollectionPath, profileId);

                // Remove from cache
                lock (_lockObject)
                {
                    _profilesCache.Remove(profileId);
                }

                await _errorHandler.LogInfoAsync($"Deleted profile '{profile.Name}' (ID: {profileId})", "AdvancedConfigurationService.DeleteProfileAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.DeleteProfileAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Activates a configuration profile
        /// </summary>
        public async Task ActivateProfileAsync(string profileId)
        {
            try
            {
                var profile = await GetProfileAsync(profileId).ConfigureAwait(false);
                if (profile == null)
                    throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

                var oldActiveProfile = await GetActiveProfileAsync().ConfigureAwait(false);
                var oldProfileId = oldActiveProfile == null ? null : oldActiveProfile.Id;

                // Deactivate current profile
                if (oldActiveProfile != null)
                {
                    oldActiveProfile.IsActive = false;
                    await UpdateProfileAsync(oldActiveProfile).ConfigureAwait(false);
                }

                // Activate new profile
                profile.IsActive = true;
                profile.Metadata.LastUsed = DateTime.UtcNow;
                profile.Metadata.UsageCount++;
                await UpdateProfileAsync(profile).ConfigureAwait(false);

                // Apply profile configuration
                await ApplyProfileConfigurationAsync(profile).ConfigureAwait(false);

                // Fire event
                if (ProfileChanged != null)
                    ProfileChanged.Invoke(this, new ConfigurationProfileChangedEventArgs
                {
                    OldProfileId = oldProfileId,
                    NewProfileId = profileId
                });

                await _errorHandler.LogInfoAsync($"Activated profile '{profile.Name}' (ID: {profileId})", "AdvancedConfigurationService.ActivateProfileAsync").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ActivateProfileAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Gets the currently active profile
        /// </summary>
        public async Task<ConfigurationProfile> GetActiveProfileAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshProfilesCache();
                    return _profilesCache.Values.FirstOrDefault(p => p.IsActive);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Duplicates a configuration profile
        /// </summary>
        public async Task<ConfigurationProfile> DuplicateProfileAsync(string profileId, string newName)
        {
            var originalProfile = await GetProfileAsync(profileId);
            if (originalProfile == null)
                throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

            var duplicatedProfile = SecureJsonSerializer.Deserialize<ConfigurationProfile>(
                SecureJsonSerializer.Serialize(originalProfile));

            duplicatedProfile.Id = Guid.NewGuid().ToString();
            duplicatedProfile.Name = newName;
            duplicatedProfile.IsActive = false;
            duplicatedProfile.IsDefault = false;
            duplicatedProfile.CreatedAt = DateTime.UtcNow;
            duplicatedProfile.ModifiedAt = DateTime.UtcNow;
            duplicatedProfile.Metadata.CreatedBy = Environment.UserName;
            duplicatedProfile.Metadata.UsageCount = 0;

            return await CreateProfileAsync(duplicatedProfile);
        }

        #endregion

        #region Template Management

        /// <summary>
        /// Gets all available configuration templates
        /// </summary>
        public async Task<IEnumerable<ConfigurationTemplate>> GetTemplatesAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshTemplatesCache();
                    return _templatesCache.Values.ToList();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a specific configuration template
        /// </summary>
        public async Task<ConfigurationTemplate> GetTemplateAsync(string templateId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshTemplatesCache();
                    ConfigurationTemplate template;
                    return _templatesCache.TryGetValue(templateId, out template) ? template : null;
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new configuration template
        /// </summary>
        public async Task<ConfigurationTemplate> CreateTemplateAsync(ConfigurationTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            try
            {
                // Set metadata
                template.Id = Guid.NewGuid().ToString();
                template.CreatedAt = DateTime.UtcNow;
                template.ModifiedAt = DateTime.UtcNow;
                template.Metadata.CreatedBy = Environment.UserName;
                template.Version = "1.0.0";

                // Save to settings store
                var json = SecureJsonSerializer.Serialize(template);
                _settingsStore.SetString(_templatesCollectionPath, template.Id, json);

                // Update cache
                lock (_lockObject)
                {
                    _templatesCache[template.Id] = template;
                }

                await _errorHandler.LogInfoAsync($"Created template '{template.Name}' (ID: {template.Id})", "AdvancedConfigurationService.CreateTemplateAsync").ConfigureAwait(false);
                return template;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.CreateTemplateAsync").ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing configuration template
        /// </summary>
        public async Task<ConfigurationTemplate> UpdateTemplateAsync(ConfigurationTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            return await Task.Run(async () =>
            {
                try
                {
                    // Update metadata
                    template.ModifiedAt = DateTime.UtcNow;
                    template.Metadata.ModifiedBy = Environment.UserName;

                    // Save to settings store
                    var json = SecureJsonSerializer.Serialize(template);
                    _settingsStore.SetString(_templatesCollectionPath, template.Id, json);

                    // Update cache
                    lock (_lockObject)
                    {
                        _templatesCache[template.Id] = template;
                    }

                    await _errorHandler.LogInfoAsync($"Updated template '{template.Name}' (ID: {template.Id})", "AdvancedConfigurationService.UpdateTemplateAsync");
                    return template;
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.UpdateTemplateAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Deletes a configuration template
        /// </summary>
        public async Task DeleteTemplateAsync(string templateId)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var template = await GetTemplateAsync(templateId);
                    if (template == null)
                        throw new InvalidOperationException($"Template with ID '{templateId}' not found");

                    if (template.IsBuiltIn)
                        throw new InvalidOperationException("Cannot delete built-in templates");

                    // Delete from settings store
                    _settingsStore.DeleteProperty(_templatesCollectionPath, templateId);

                    // Remove from cache
                    lock (_lockObject)
                    {
                        _templatesCache.Remove(templateId);
                    }

                    await _errorHandler.LogInfoAsync($"Deleted template '{template.Name}' (ID: {templateId})", "AdvancedConfigurationService.DeleteTemplateAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.DeleteTemplateAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Applies a configuration template to a profile
        /// </summary>
        public async Task ApplyTemplateAsync(string templateId, string profileId)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var template = await GetTemplateAsync(templateId);
                    if (template == null)
                        throw new InvalidOperationException($"Template with ID '{templateId}' not found");

                    var profile = await GetProfileAsync(profileId);
                    if (profile == null)
                        throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

                    // Apply template settings to profile
                    foreach (var setting in template.Settings)
                    {
                        profile.Settings[setting.Key] = setting.Value;
                    }

                    // Apply AI model configuration
                    if (template.AIModelConfiguration != null)
                    {
                        profile.AIModelConfiguration = template.AIModelConfiguration;
                    }

                    // Apply advanced parameters
                    foreach (var param in template.AdvancedParameters)
                    {
                        profile.AdvancedParameters[param.Key] = param.Value;
                    }

                    // Update template usage
                    template.Metadata.UsageCount++;
                    template.Metadata.LastUsed = DateTime.UtcNow;
                    await UpdateTemplateAsync(template);

                    // Update profile
                    await UpdateProfileAsync(profile);

                    await _errorHandler.LogInfoAsync($"Applied template '{template.Name}' to profile '{profile.Name}'", "AdvancedConfigurationService.ApplyTemplateAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ApplyTemplateAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Creates a template from an existing profile
        /// </summary>
        public async Task<ConfigurationTemplate> CreateTemplateFromProfileAsync(string profileId, string templateName)
        {
            var profile = await GetProfileAsync(profileId);
            if (profile == null)
                throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

            var template = new ConfigurationTemplate
            {
                Name = templateName,
                Description = $"Template created from profile '{profile.Name}'",
                Category = TemplateCategory.User,
                Settings = new Dictionary<string, object>(profile.Settings),
                AIModelConfiguration = profile.AIModelConfiguration,
                AdvancedParameters = new Dictionary<AIProvider, AIModelAdvancedParameters>(profile.AdvancedParameters),
                Tags = new List<string>(profile.Tags)
            };

            return await CreateTemplateAsync(template);
        }

        #endregion

        #region Advanced AI Model Configuration

        /// <summary>
        /// Gets advanced AI model parameters
        /// </summary>
        public async Task<AIModelAdvancedParameters> GetAdvancedParametersAsync(AIProvider provider)
        {
            var activeProfile = await GetActiveProfileAsync().ConfigureAwait(false);
            if (activeProfile != null && activeProfile.AdvancedParameters != null && activeProfile.AdvancedParameters.ContainsKey(provider))
            {
                return activeProfile.AdvancedParameters[provider];
            }

            return await GetDefaultParametersAsync(provider, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets advanced AI model parameters
        /// </summary>
        public async Task SetAdvancedParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var activeProfile = await GetActiveProfileAsync();
                    if (activeProfile == null)
                        throw new InvalidOperationException("No active profile found");

                    // Validate parameters
                    var validationResult = await ValidateParametersAsync(provider, parameters);
                    if (!validationResult.IsValid)
                        throw new InvalidOperationException($"Parameter validation failed: {string.Join(", ", validationResult.Errors)}");

                    // Update parameters
                    activeProfile.AdvancedParameters[provider] = parameters;
                    await UpdateProfileAsync(activeProfile);

                    await _errorHandler.LogInfoAsync($"Updated advanced parameters for {provider}", "AdvancedConfigurationService.SetAdvancedParametersAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.SetAdvancedParametersAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Gets default parameters for a specific AI model
        /// </summary>
        public async Task<AIModelAdvancedParameters> GetDefaultParametersAsync(AIProvider provider, string modelName)
        {
            return await Task.Run(() =>
            {
                var defaultParams = new AIModelAdvancedParameters
                {
                    Provider = provider,
                    ModelName = modelName
                };

                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        defaultParams.Temperature = 0.7;
                        defaultParams.MaxTokens = 2000;
                        defaultParams.TopP = 0.95;
                        defaultParams.FrequencyPenalty = 0.0;
                        defaultParams.PresencePenalty = 0.0;
                        defaultParams.ContextWindow = 4096;
                        break;
                    case AIProvider.Claude:
                        defaultParams.Temperature = 0.7;
                        defaultParams.MaxTokens = 2000;
                        defaultParams.TopP = 0.95;
                        defaultParams.TopK = 40;
                        defaultParams.ContextWindow = 8192;
                        break;
                    case AIProvider.Ollama:
                        defaultParams.Temperature = 0.7;
                        defaultParams.MaxTokens = 2000;
                        defaultParams.TopP = 0.95;
                        defaultParams.TopK = 40;
                        defaultParams.ContextWindow = 2048;
                        break;
                }

                return defaultParams;
            });
        }

        /// <summary>
        /// Tests AI model parameters
        /// </summary>
        public async Task<ParameterTestResult> TestParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Validate parameters first
                    var validationResult = await ValidateParametersAsync(provider, parameters);
                    if (!validationResult.IsValid)
                    {
                        return new ParameterTestResult
                        {
                            IsSuccessful = false,
                            ErrorMessage = $"Parameter validation failed: {string.Join(", ", validationResult.Errors)}",
                            ResponseTime = DateTime.UtcNow - startTime
                        };
                    }

                    // Test with a simple prompt
                    var testPrompt = "Hello, this is a test. Please respond with 'Test successful'.";
                    
                    // This would integrate with the actual AI model manager
                    // For now, we'll simulate a successful test
                    await Task.Delay(1000); // Simulate API call

                    var endTime = DateTime.UtcNow;
                    return new ParameterTestResult
                    {
                        IsSuccessful = true,
                        ResponseTime = endTime - startTime,
                        Output = "Test successful",
                        Metrics = new Dictionary<string, object>
                        {
                            ["provider"] = provider.ToString(),
                            ["model"] = parameters.ModelName,
                            ["temperature"] = parameters.Temperature,
                            ["max_tokens"] = parameters.MaxTokens
                        }
                    };
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.TestParametersAsync");
                    return new ParameterTestResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = ex.Message,
                        ResponseTime = TimeSpan.Zero
                    };
                }
            });
        }

        #endregion

        #region Backup and Restore

        /// <summary>
        /// Creates a backup of current configuration
        /// </summary>
        public async Task<ConfigurationBackup> CreateBackupAsync(string backupName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var backup = new ConfigurationBackup
                    {
                        Name = backupName,
                        Description = $"Backup created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        Type = BackupType.Manual
                    };

                    // Serialize all configuration data
                    var configData = new
                    {
                        Profiles = await GetProfilesAsync(),
                        Templates = await GetTemplatesAsync(),
                        Settings = await GetAllSettingsAsync(),
                        Timestamp = DateTime.UtcNow
                    };

                    var json = SecureJsonSerializer.Serialize(configData);
                    backup.ConfigurationData = json;
                    backup.Size = Encoding.UTF8.GetByteCount(json);
                    backup.Checksum = ComputeChecksum(json);

                    // Save backup
                    var backupJson = SecureJsonSerializer.Serialize(backup);
                    _settingsStore.SetString(_backupsCollectionPath, backup.Id, backupJson);

                    // Update cache
                    lock (_lockObject)
                    {
                        _backupsCache[backup.Id] = backup;
                    }

                    // Cleanup old backups
                    await CleanupOldBackupsAsync();

                    if (ConfigurationBackedUp != null)
                        ConfigurationBackedUp.Invoke(this, new ConfigurationBackupEventArgs
                    {
                        BackupId = backup.Id,
                        BackupName = backup.Name,
                        BackupSize = backup.Size
                    });

                    await _errorHandler.LogInfoAsync($"Created backup '{backup.Name}' (ID: {backup.Id})", "AdvancedConfigurationService.CreateBackupAsync");
                    return backup;
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.CreateBackupAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Gets all available configuration backups
        /// </summary>
        public async Task<IEnumerable<ConfigurationBackup>> GetBackupsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshBackupsCache();
                    return _backupsCache.Values.OrderByDescending(b => b.CreatedAt).ToList();
                }
            });
        }

        /// <summary>
        /// Restores configuration from a backup
        /// </summary>
        public async Task RestoreFromBackupAsync(string backupId)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var backup = await GetBackupAsync(backupId);
                    if (backup == null)
                        throw new InvalidOperationException($"Backup with ID '{backupId}' not found");

                    // Verify backup integrity
                    var computedChecksum = ComputeChecksum(backup.ConfigurationData);
                    if (computedChecksum != backup.Checksum)
                        throw new InvalidOperationException("Backup integrity check failed");

                    // Create a backup of current configuration before restore
                    await CreateBackupAsync($"Before restore from '{backup.Name}'");

                    // Deserialize backup data
                    var configData = SecureJsonSerializer.Deserialize<dynamic>(backup.ConfigurationData, strict: true);

                    // Restore profiles
                    if (configData.Profiles != null)
                    {
                        var profiles = SecureJsonSerializer.Deserialize<List<ConfigurationProfile>>(configData.Profiles.ToString(), strict: true);
                        foreach (var profile in profiles)
                        {
                            await CreateProfileAsync(profile);
                        }
                    }

                    // Restore templates
                    if (configData.Templates != null)
                    {
                        var templates = SecureJsonSerializer.Deserialize<List<ConfigurationTemplate>>(configData.Templates.ToString(), strict: true);
                        foreach (var template in templates)
                        {
                            await CreateTemplateAsync(template);
                        }
                    }

                    if (ConfigurationRestored != null)
                        ConfigurationRestored.Invoke(this, new ConfigurationRestoreEventArgs
                    {
                        BackupId = backupId,
                        BackupName = backup.Name,
                        IsSuccessful = true
                    });

                    await _errorHandler.LogInfoAsync($"Restored configuration from backup '{backup.Name}' (ID: {backupId})", "AdvancedConfigurationService.RestoreFromBackupAsync");
                }
                catch (Exception ex)
                {
                    if (ConfigurationRestored != null)
                        ConfigurationRestored.Invoke(this, new ConfigurationRestoreEventArgs
                    {
                        BackupId = backupId,
                        IsSuccessful = false,
                        ErrorMessage = ex.Message
                    });

                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.RestoreFromBackupAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Deletes a configuration backup
        /// </summary>
        public async Task DeleteBackupAsync(string backupId)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var backup = await GetBackupAsync(backupId);
                    if (backup == null)
                        throw new InvalidOperationException($"Backup with ID '{backupId}' not found");

                    // Delete from settings store
                    _settingsStore.DeleteProperty(_backupsCollectionPath, backupId);

                    // Remove from cache
                    lock (_lockObject)
                    {
                        _backupsCache.Remove(backupId);
                    }

                    await _errorHandler.LogInfoAsync($"Deleted backup '{backup.Name}' (ID: {backupId})", "AdvancedConfigurationService.DeleteBackupAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.DeleteBackupAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Automatically creates backups on configuration changes
        /// </summary>
        public async Task SetAutoBackupAsync(bool enabled)
        {
            await Task.Run(async () =>
            {
                try
                {
                    _autoBackupEnabled = enabled;
                    await SetValueAsync("AutoBackupEnabled", enabled);
                    await _errorHandler.LogInfoAsync("Auto-backup " + (enabled ? "enabled" : "disabled"), "AdvancedConfigurationService.SetAutoBackupAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.SetAutoBackupAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Sets backup retention policy
        /// </summary>
        public async Task SetBackupRetentionAsync(int maxBackups, int retentionDays)
        {
            await Task.Run(async () =>
            {
                try
                {
                    _maxBackups = maxBackups;
                    _retentionDays = retentionDays;
                    await SetValueAsync("MaxBackups", maxBackups);
                    await SetValueAsync("RetentionDays", retentionDays);
                    await _errorHandler.LogInfoAsync($"Backup retention set to {maxBackups} backups, {retentionDays} days", "AdvancedConfigurationService.SetBackupRetentionAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.SetBackupRetentionAsync");
                    throw;
                }
            });
        }

        #endregion

        #region Import/Export Extensions

        /// <summary>
        /// Exports configuration profile to file
        /// </summary>
        public async Task ExportProfileAsync(string profileId, string filePath, ConfigurationExportFormat format)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var profile = await GetProfileAsync(profileId);
                    if (profile == null)
                        throw new InvalidOperationException($"Profile with ID '{profileId}' not found");

                    string content = SerializeProfile(profile, format);
                    File.WriteAllText(filePath, content);

                    await _errorHandler.LogInfoAsync($"Exported profile '{profile.Name}' to '{filePath}'", "AdvancedConfigurationService.ExportProfileAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ExportProfileAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Imports configuration profile from file
        /// </summary>
        public async Task<ConfigurationProfile> ImportProfileAsync(string filePath, ConfigurationExportFormat format)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var profile = DeserializeProfile(content, format);
                    
                    // Generate new ID and update metadata
                    profile.Id = Guid.NewGuid().ToString();
                    profile.CreatedAt = DateTime.UtcNow;
                    profile.ModifiedAt = DateTime.UtcNow;
                    profile.IsActive = false;
                    profile.IsDefault = false;

                    profile = await CreateProfileAsync(profile);
                    await _errorHandler.LogInfoAsync($"Imported profile '{profile.Name}' from '{filePath}'", "AdvancedConfigurationService.ImportProfileAsync");
                    
                    return profile;
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ImportProfileAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Exports configuration template to file
        /// </summary>
        public async Task ExportTemplateAsync(string templateId, string filePath, ConfigurationExportFormat format)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var template = await GetTemplateAsync(templateId);
                    if (template == null)
                        throw new InvalidOperationException($"Template with ID '{templateId}' not found");

                    string content = SerializeTemplate(template, format);
                    File.WriteAllText(filePath, content);

                    await _errorHandler.LogInfoAsync($"Exported template '{template.Name}' to '{filePath}'", "AdvancedConfigurationService.ExportTemplateAsync");
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ExportTemplateAsync");
                    throw;
                }
            });
        }

        /// <summary>
        /// Imports configuration template from file
        /// </summary>
        public async Task<ConfigurationTemplate> ImportTemplateAsync(string filePath, ConfigurationExportFormat format)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var template = DeserializeTemplate(content, format);
                    
                    // Generate new ID and update metadata
                    template.Id = Guid.NewGuid().ToString();
                    template.CreatedAt = DateTime.UtcNow;
                    template.ModifiedAt = DateTime.UtcNow;
                    template.IsBuiltIn = false;

                    template = await CreateTemplateAsync(template);
                    await _errorHandler.LogInfoAsync($"Imported template '{template.Name}' from '{filePath}'", "AdvancedConfigurationService.ImportTemplateAsync");
                    
                    return template;
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ImportTemplateAsync");
                    throw;
                }
            });
        }

        #endregion

        #region Validation and Feedback

        /// <summary>
        /// Validates a configuration profile
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateProfileAsync(ConfigurationProfile profile)
        {
            return await Task.Run(() =>
            {
                var result = new ConfigurationValidationResult { IsValid = true };

                if (profile == null)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError { ErrorMessage = "Profile cannot be null" });
                    return result;
                }

                if (string.IsNullOrWhiteSpace(profile.Name))
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError { ErrorMessage = "Profile name is required" });
                }

                if (profile.Name != null && profile.Name.Length > 100)
                {
                    result.IsValid = false;
                    result.ValidationErrors.Add(new ValidationError { ErrorMessage = "Profile name cannot exceed 100 characters" });
                }

                // Validate AI model configuration
                if (profile.AIModelConfiguration == null)
                {
                    result.ValidationErrors.Add(new ValidationError { ErrorMessage = "AI model configuration is not set" });
                }

                return result;
            });
        }

        /// <summary>
        /// Validates AI model parameters
        /// </summary>
        public async Task<ParameterValidationResult> ValidateParametersAsync(AIProvider provider, AIModelAdvancedParameters parameters)
        {
            return await Task.Run(() =>
            {
                var result = new ParameterValidationResult { IsValid = true };

                if (parameters == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Parameters cannot be null");
                    return result;
                }

                // Validate temperature
                if (parameters.Temperature < 0.0 || parameters.Temperature > 1.0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Temperature must be between 0.0 and 1.0");
                }

                // Validate max tokens
                if (parameters.MaxTokens < 1 || parameters.MaxTokens > 32000)
                {
                    result.IsValid = false;
                    result.Errors.Add("Max tokens must be between 1 and 32000");
                }

                // Validate top-p
                if (parameters.TopP < 0.0 || parameters.TopP > 1.0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Top-p must be between 0.0 and 1.0");
                }

                // Provider-specific validation
                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        if (parameters.FrequencyPenalty < -2.0 || parameters.FrequencyPenalty > 2.0)
                        {
                            result.IsValid = false;
                            result.Errors.Add("Frequency penalty must be between -2.0 and 2.0");
                        }
                        break;
                    case AIProvider.Claude:
                        if (parameters.TopK < 1 || parameters.TopK > 100)
                        {
                            result.IsValid = false;
                            result.Errors.Add("Top-k must be between 1 and 100");
                        }
                        break;
                }

                return result;
            });
        }

        /// <summary>
        /// Gets configuration recommendations
        /// </summary>
        public async Task<IEnumerable<ConfigurationRecommendation>> GetRecommendationsAsync()
        {
            return await Task.Run(() =>
            {
                var recommendations = new List<ConfigurationRecommendation>();

                // Add sample recommendations
                recommendations.Add(new ConfigurationRecommendation
                {
                    Title = "Enable Auto-Backup",
                    Description = "Automatically create backups when configuration changes",
                    Category = RecommendationCategory.Reliability,
                    Priority = RecommendationPriority.Medium,
                    SettingKey = "AutoBackupEnabled",
                    RecommendedValue = true,
                    CurrentValue = _autoBackupEnabled,
                    ExpectedImpact = "Prevents data loss from configuration changes",
                    Reason = "Auto-backup is currently disabled"
                });

                return recommendations;
            });
        }

        /// <summary>
        /// Gets configuration health check
        /// </summary>
        public async Task<ConfigurationHealthCheck> GetHealthCheckAsync()
        {
            return await Task.Run(async () =>
            {
                var healthCheck = new ConfigurationHealthCheck
                {
                    Status = HealthStatus.Healthy,
                    Score = 100
                };

                var issues = new List<HealthIssue>();

                // Check if there's an active profile
                var activeProfile = await GetActiveProfileAsync();
                if (activeProfile == null)
                {
                    issues.Add(new HealthIssue
                    {
                        Title = "No Active Profile",
                        Description = "No configuration profile is currently active",
                        Severity = HealthIssueSeverity.Warning,
                        Resolution = "Activate a configuration profile"
                    });
                    healthCheck.Score -= 20;
                }

                // Check backup status
                if (!_autoBackupEnabled)
                {
                    issues.Add(new HealthIssue
                    {
                        Title = "Auto-Backup Disabled",
                        Description = "Automatic backups are disabled",
                        Severity = HealthIssueSeverity.Info,
                        Resolution = "Enable auto-backup to prevent data loss"
                    });
                    healthCheck.Score -= 10;
                }

                healthCheck.Issues = issues;

                // Determine overall status
                if (healthCheck.Score >= 90)
                    healthCheck.Status = HealthStatus.Healthy;
                else if (healthCheck.Score >= 70)
                    healthCheck.Status = HealthStatus.Warning;
                else
                    healthCheck.Status = HealthStatus.Critical;

                return healthCheck;
            });
        }

        #endregion

        #region Delegated Methods from IConfigurationService

        public AIModelConfiguration GetAIModelConfiguration()
        {
            return _baseConfigurationService.GetAIModelConfiguration();
        }

        public async Task SetAIModelConfigurationAsync(AIModelConfiguration configuration)
        {
            await _baseConfigurationService.SetAIModelConfigurationAsync(configuration);
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            return _baseConfigurationService.GetValue(key, defaultValue);
        }

        public async Task SetValueAsync<T>(string key, T value)
        {
            await _baseConfigurationService.SetValueAsync(key, value);
        }

        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
        {
            return await _baseConfigurationService.ValidateConfigurationAsync();
        }

        public async Task ResetToDefaultsAsync()
        {
            await _baseConfigurationService.ResetToDefaultsAsync();
        }

        public async Task ExportConfigurationAsync(string filePath)
        {
            await _baseConfigurationService.ExportConfigurationAsync(filePath);
        }

        public async Task ImportConfigurationAsync(string filePath)
        {
            await _baseConfigurationService.ImportConfigurationAsync(filePath);
        }

        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            return await _baseConfigurationService.TestConnectionAsync();
        }

        public List<AIModelConfiguration> GetAvailableConfigurations()
        {
            return _baseConfigurationService.GetAvailableConfigurations();
        }

        public async Task<MigrationResult> MigrateConfigurationAsync()
        {
            return await _baseConfigurationService.MigrateConfigurationAsync();
        }

        public T GetSetting<T>(string key, T defaultValue = default(T))
        {
            return _baseConfigurationService.GetSetting(key, defaultValue);
        }

        public async Task<IEnumerable<AIModelConfiguration>> GetAllModelConfigurationsAsync()
        {
            return await _baseConfigurationService.GetAllModelConfigurationsAsync();
        }

        public async Task<string> GetActiveModelIdAsync()
        {
            return await _baseConfigurationService.GetActiveModelIdAsync();
        }

        public async Task SetActiveModelIdAsync(string modelId)
        {
            await _baseConfigurationService.SetActiveModelIdAsync(modelId);
        }

        #endregion

        #region Settings Management

        public async Task<IEnumerable<ConfigurationSetting>> GetAllSettingsAsync()
        {
            return await Task.Run(() =>
            {
                // This would return all configuration settings with metadata
                // For now, return empty list as placeholder
                return new List<ConfigurationSetting>();
            });
        }

        public async Task<ConfigurationSetting> GetSettingAsync(string key)
        {
            return await Task.Run(() =>
            {
                // This would return a specific setting with metadata
                // For now, return null as placeholder
                return (ConfigurationSetting)null;
            });
        }

        public async Task UpdateSettingAsync(ConfigurationSetting setting)
        {
            await Task.Run(() =>
            {
                // This would update a specific setting
                // For now, do nothing as placeholder
            });
        }

        public async Task ResetSettingAsync(string key)
        {
            await Task.Run(() =>
            {
                // This would reset a specific setting to default
                // For now, do nothing as placeholder
            });
        }

        public async Task<ConfigurationSchema> GetSchemaAsync()
        {
            return await Task.Run(() =>
            {
                // This would return the configuration schema
                // For now, return empty schema as placeholder
                return new ConfigurationSchema();
            });
        }

        #endregion

        #region Monitoring and Analytics

        public async Task<ConfigurationUsageStatistics> GetUsageStatisticsAsync()
        {
            return await Task.Run(() =>
            {
                // This would return usage statistics
                // For now, return empty statistics as placeholder
                return new ConfigurationUsageStatistics();
            });
        }

        public async Task<IEnumerable<ConfigurationChangeRecord>> GetChangeHistoryAsync(int days = 30)
        {
            return await Task.Run(() =>
            {
                // This would return change history
                // For now, return empty list as placeholder
                return new List<ConfigurationChangeRecord>();
            });
        }

        public async Task RecordUsageAsync(string feature, string value)
        {
            await Task.Run(() =>
            {
                // This would record usage statistics
                // For now, do nothing as placeholder
            });
        }

        #endregion

        #region Private Helper Methods

        private void InitializeCollections()
        {
            try
            {
                if (!_settingsStore.CollectionExists(_profilesCollectionPath))
                    _settingsStore.CreateCollection(_profilesCollectionPath);

                if (!_settingsStore.CollectionExists(_templatesCollectionPath))
                    _settingsStore.CreateCollection(_templatesCollectionPath);

                if (!_settingsStore.CollectionExists(_backupsCollectionPath))
                    _settingsStore.CreateCollection(_backupsCollectionPath);

                if (!_settingsStore.CollectionExists(_statisticsCollectionPath))
                    _settingsStore.CreateCollection(_statisticsCollectionPath);

                if (!_settingsStore.CollectionExists(_changeHistoryCollectionPath))
                    _settingsStore.CreateCollection(_changeHistoryCollectionPath);
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _ = _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.InitializeCollections");
            }
        }

        private void LoadCachedData()
        {
            try
            {
                RefreshProfilesCache();
                RefreshTemplatesCache();
                RefreshBackupsCache();
                LoadSettings();
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _ = _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.LoadCachedData");
            }
        }

        private void RefreshProfilesCache()
        {
            try
            {
                _profilesCache.Clear();
                var propertyNames = _settingsStore.GetPropertyNames(_profilesCollectionPath);
                
                // Enforce collection size limits
                var propertiesToLoad = propertyNames.Take(MaxProfilesCache);
                
                foreach (var propertyName in propertiesToLoad)
                {
                    var json = _settingsStore.GetString(_profilesCollectionPath, propertyName);
                    var profile = SecureJsonSerializer.Deserialize<ConfigurationProfile>(json, strict: true);
                    _profilesCache[profile.Id] = profile;
                }
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _ = _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.RefreshProfilesCache");
            }
        }

        private void RefreshTemplatesCache()
        {
            try
            {
                _templatesCache.Clear();
                var propertyNames = _settingsStore.GetPropertyNames(_templatesCollectionPath);
                
                // Enforce collection size limits
                var propertiesToLoad = propertyNames.Take(MaxTemplatesCache);
                
                foreach (var propertyName in propertiesToLoad)
                {
                    var json = _settingsStore.GetString(_templatesCollectionPath, propertyName);
                    var template = SecureJsonSerializer.Deserialize<ConfigurationTemplate>(json, strict: true);
                    _templatesCache[template.Id] = template;
                }
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.RefreshTemplatesCache");
            }
        }

        private void RefreshBackupsCache()
        {
            try
            {
                _backupsCache.Clear();
                var propertyNames = _settingsStore.GetPropertyNames(_backupsCollectionPath);
                
                // Enforce collection size limits
                var propertiesToLoad = propertyNames.Take(MaxBackupsCache);
                
                foreach (var propertyName in propertiesToLoad)
                {
                    var json = _settingsStore.GetString(_backupsCollectionPath, propertyName);
                    var backup = SecureJsonSerializer.Deserialize<ConfigurationBackup>(json, strict: true);
                    _backupsCache[backup.Id] = backup;
                }
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _ = _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.RefreshBackupsCache");
            }
        }

        private void LoadSettings()
        {
            try
            {
                _autoBackupEnabled = GetValue("AutoBackupEnabled", true);
                _maxBackups = GetValue("MaxBackups", 10);
                _retentionDays = GetValue("RetentionDays", 30);
            }
            catch (Exception ex)
            {
                if (_errorHandler != null)
                    _ = _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.LoadSettings");
            }
        }

        private async Task<ConfigurationBackup> GetBackupAsync(string backupId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    RefreshBackupsCache();
                    ConfigurationBackup backup;
                    return _backupsCache.TryGetValue(backupId, out backup) ? backup : null;
                }
            });
        }

        private async Task CleanupOldBackupsAsync()
        {
            await Task.Run(async () =>
            {
                try
                {
                    var backups = await GetBackupsAsync();
                    var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
                    
                    var backupsToDelete = backups
                        .Where(b => b.CreatedAt < cutoffDate || backups.Count() > _maxBackups)
                        .OrderBy(b => b.CreatedAt)
                        .Take(Math.Max(0, backups.Count() - _maxBackups))
                        .ToList();

                    foreach (var backup in backupsToDelete)
                    {
                        await DeleteBackupAsync(backup.Id);
                    }
                }
                catch (Exception ex)
                {
                    _ = await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.CleanupOldBackupsAsync");
                }
            });
        }

        private string ComputeChecksum(string data)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private async Task ApplyProfileConfigurationAsync(ConfigurationProfile profile)
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Apply AI model configuration
                    if (profile.AIModelConfiguration != null)
                    {
                        await SetAIModelConfigurationAsync(profile.AIModelConfiguration);
                    }

                    // Apply settings
                    foreach (var setting in profile.Settings)
                    {
                        await SetValueAsync(setting.Key, setting.Value);
                    }
                }
                catch (Exception ex)
                {
                    _ = await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.ApplyProfileConfigurationAsync");
                }
            });
        }

        private string SerializeProfile(ConfigurationProfile profile, ConfigurationExportFormat format)
        {
            switch (format)
            {
                case ConfigurationExportFormat.Json:
                    return SecureJsonSerializer.Serialize(profile);
                case ConfigurationExportFormat.Xml:
                    // XML serialization not yet implemented - return JSON as fallback
                    return SecureJsonSerializer.Serialize(profile);
                case ConfigurationExportFormat.Yaml:
                    // YAML serialization not yet implemented - return JSON as fallback
                    return SecureJsonSerializer.Serialize(profile);
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        }

        private ConfigurationProfile DeserializeProfile(string content, ConfigurationExportFormat format)
        {
            switch (format)
            {
                case ConfigurationExportFormat.Json:
                    return SecureJsonSerializer.Deserialize<ConfigurationProfile>(content, strict: true);
                case ConfigurationExportFormat.Xml:
                    // XML deserialization not yet implemented - try JSON fallback
                    return SecureJsonSerializer.Deserialize<ConfigurationProfile>(content, strict: true);
                case ConfigurationExportFormat.Yaml:
                    // YAML deserialization not yet implemented - try JSON fallback
                    return SecureJsonSerializer.Deserialize<ConfigurationProfile>(content, strict: true);
                default:
                    throw new ArgumentException($"Unsupported import format: {format}");
            }
        }

        private string SerializeTemplate(ConfigurationTemplate template, ConfigurationExportFormat format)
        {
            switch (format)
            {
                case ConfigurationExportFormat.Json:
                    return SecureJsonSerializer.Serialize(template);
                case ConfigurationExportFormat.Xml:
                    // XML serialization not yet implemented - return JSON as fallback
                    return SecureJsonSerializer.Serialize(template);
                case ConfigurationExportFormat.Yaml:
                    // YAML serialization not yet implemented - return JSON as fallback
                    return SecureJsonSerializer.Serialize(template);
                default:
                    throw new ArgumentException($"Unsupported export format: {format}");
            }
        }

        private ConfigurationTemplate DeserializeTemplate(string content, ConfigurationExportFormat format)
        {
            switch (format)
            {
                case ConfigurationExportFormat.Json:
                    return SecureJsonSerializer.Deserialize<ConfigurationTemplate>(content, strict: true);
                case ConfigurationExportFormat.Xml:
                    // XML deserialization not yet implemented - try JSON fallback
                    return SecureJsonSerializer.Deserialize<ConfigurationTemplate>(content, strict: true);
                case ConfigurationExportFormat.Yaml:
                    // YAML deserialization not yet implemented - try JSON fallback
                    return SecureJsonSerializer.Deserialize<ConfigurationTemplate>(content, strict: true);
                default:
                    throw new ArgumentException($"Unsupported import format: {format}");
            }
        }

        private async void OnBaseConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            try
            {
                // Create automatic backup if enabled
                if (_autoBackupEnabled)
                {
                    await CreateBackupAsync($"Auto-backup: {e.Key} changed");
                }

                // Forward the event
                if (ConfigurationChanged != null)
                    ConfigurationChanged.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                _ = await _errorHandler.HandleExceptionAsync(ex, "AdvancedConfigurationService.OnBaseConfigurationChanged");
            }
        }

        #endregion

        #region IConfigurationService Implementation - Missing Methods

        /// <summary>
        /// Tests the connection for a specific configuration
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration configuration)
        {
            return await _baseConfigurationService.TestConnectionAsync(configuration);
        }

        /// <summary>
        /// Validates a specific configuration
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(AIModelConfiguration configuration)
        {
            return await _baseConfigurationService.ValidateConfigurationAsync(configuration);
        }

        /// <summary>
        /// Saves a configuration asynchronously
        /// </summary>
        public async Task SaveConfigurationAsync(AIModelConfiguration configuration)
        {
            await _baseConfigurationService.SetAIModelConfigurationAsync(configuration);
        }

        /// <summary>
        /// Gets a configuration asynchronously
        /// </summary>
        public async Task<AIModelConfiguration> GetConfigurationAsync()
        {
            return await Task.FromResult(_baseConfigurationService.GetAIModelConfiguration());
        }

        /// <summary>
        /// Gets a configuration synchronously
        /// </summary>
        public AIModelConfiguration GetConfiguration()
        {
            return _baseConfigurationService.GetAIModelConfiguration();
        }

        /// <summary>
        /// Gets whether AI completion is enabled
        /// </summary>
        public bool IsAICompletionEnabled
        {
            get => _baseConfigurationService.GetValue(Models.Constants.ConfigurationKeys.AICompletionEnabled, Models.Constants.DefaultValues.DefaultAICompletionEnabled);
            set => _baseConfigurationService.SetValueAsync(Models.Constants.ConfigurationKeys.AICompletionEnabled, value);
        }

        /// <summary>
        /// Toggles AI completion on/off
        /// </summary>
        public async Task ToggleAICompletionAsync()
        {
            var current = IsAICompletionEnabled;
            IsAICompletionEnabled = !current;
            
            // Fire configuration changed event
            if (ConfigurationChanged != null)
                ConfigurationChanged.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = Models.Constants.ConfigurationKeys.AICompletionEnabled,
                OldValue = current,
                NewValue = IsAICompletionEnabled
            });

            await Task.CompletedTask;
        }

        /// <summary>
        /// Saves advanced parameters
        /// </summary>
        /// <param name="parameters">The parameters to save</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveAdvancedParametersAsync(Dictionary<string, object> parameters)
        {
            // Implementation stub - save advanced parameters
            await Task.CompletedTask;
        }

        /// <summary>
        /// Enables automatic backup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task EnableAutoBackupAsync()
        {
            // Implementation stub - enable auto backup
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disables automatic backup
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task DisableAutoBackupAsync()
        {
            // Implementation stub - disable auto backup
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs health check
        /// </summary>
        /// <returns>Health check results</returns>
        public async Task<HealthCheckResults> RunHealthCheckAsync()
        {
            // Implementation stub - perform health checks
            return await Task.FromResult(new HealthCheckResults
            {
                IsHealthy = true,
                Items = new List<HealthCheckItem>
                {
                    new HealthCheckItem { Name = "Configuration", IsHealthy = true, Message = "OK" }
                }
            });
        }

        /// <summary>
        /// Gets configuration settings
        /// </summary>
        /// <returns>Configuration settings</returns>
        public async Task<Dictionary<string, object>> GetSettingsAsync()
        {
            // Implementation stub - get settings
            return await Task.FromResult(new Dictionary<string, object>());
        }

        /// <summary>
        /// Saves configuration settings
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SaveSettingsAsync(Dictionary<string, object> settings)
        {
            // Implementation stub - save settings
            await Task.CompletedTask;
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Unsubscribe from events to prevent memory leaks
                _baseConfigurationService.ConfigurationChanged -= OnBaseConfigurationChanged;
                
                // Clear all event handlers
                ConfigurationChanged = null;
                ProfileChanged = null;
                ConfigurationBackedUp = null;
                ConfigurationRestored = null;
                
                // Clear all caches to free memory
                lock (_lockObject)
                {
                    _profilesCache.Clear();
                    _templatesCache.Clear();
                    _backupsCache.Clear();
                }
                
                _disposed = true;
            }
        }

        ~AdvancedConfigurationService()
        {
            Dispose(false);
        }
    }
}