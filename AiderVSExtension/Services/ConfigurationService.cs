using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Security;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Implementation of configuration service using Visual Studio Settings Store
    /// </summary>
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        private readonly WritableSettingsStore _settingsStore;
        private AIModelConfiguration? _cachedConfiguration;
        private readonly object _lockObject = new object();

        // Event for configuration changes
        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public ConfigurationService(WritableSettingsStore settingsStore)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            EnsureCollectionExists();
        }

        /// <summary>
        /// Gets the current AI model configuration
        /// </summary>
        public AIModelConfiguration GetAIModelConfiguration()
        {
            lock (_lockObject)
            {
                if (_cachedConfiguration != null)
                    return _cachedConfiguration;

                try
                {
                    var configJson = GetValue(AiderVSExtension.Models.Constants.ConfigurationKeys.SelectedProvider, string.Empty);
                    if (!string.IsNullOrEmpty(configJson))
                    {
                        var storedConfig = JsonSerializer.Deserialize<AIModelConfiguration>(configJson);
                        if (storedConfig != null)
                        {
                            // Decrypt the API key for in-memory use
                            _cachedConfiguration = CreateConfigurationFromStorage(storedConfig);
                            return _cachedConfiguration;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and fall back to defaults
                    System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                }

                // Return default configuration
                _cachedConfiguration = CreateDefaultConfiguration();
                return _cachedConfiguration;
            }
        }
        
        /// <summary>
        /// Gets the AI model configuration for a specific provider
        /// </summary>
        public AIModelConfiguration GetAIModelConfiguration(AIProvider provider)
        {
            var config = GetAIModelConfiguration();
            if (config.Provider == provider)
            {
                return config;
            }
            
            // Return a default configuration for the requested provider
            return new AIModelConfiguration
            {
                Provider = provider,
                TimeoutSeconds = Constants.DefaultValues.DefaultTimeoutSeconds,
                MaxRetries = Constants.DefaultValues.DefaultMaxRetries,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Sets the AI model configuration
        /// </summary>
        public async Task SetAIModelConfigurationAsync(AIModelConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResult = await ValidateConfigurationAsync().ConfigureAwait(false);
            if (!validationResult.IsValid)
                throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");

            var oldConfiguration = _cachedConfiguration;

            // Create a copy for storage with encrypted API key outside the lock
            var configForStorage = CreateConfigurationForStorage(configuration);
            var configJson = JsonSerializer.Serialize(configForStorage);
            
            // Set the value asynchronously first
            await SetValueAsync(AiderVSExtension.Models.Constants.ConfigurationKeys.SelectedProvider, configJson).ConfigureAwait(false);
            
            // Then update the cache under lock
            lock (_lockObject)
            {
                _cachedConfiguration = configuration; // Cache the unencrypted version
            }

            // Fire configuration changed event outside the lock
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = AiderVSExtension.Models.Constants.ConfigurationKeys.SelectedProvider,
                OldValue = oldConfiguration,
                NewValue = configuration
            });
        }

        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                if (!_settingsStore.PropertyExists(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key))
                    return defaultValue;

                var value = _settingsStore.GetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key);
                
                if (typeof(T) == typeof(string))
                    return (T)(object)value;
                
                if (typeof(T) == typeof(int))
                    return (T)(object)_settingsStore.GetInt32(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key);
                
                if (typeof(T) == typeof(bool))
                    return (T)(object)_settingsStore.GetBoolean(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key);

                // For complex types, try JSON deserialization
                if (!string.IsNullOrEmpty(value))
                {
                    return JsonSerializer.Deserialize<T>(value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting value for key '{key}': {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a configuration value by key
        /// </summary>
        public async Task SetValueAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            await Task.Run(() =>
            {
                var oldValue = GetValue<T>(key);

                try
                {
                    if (typeof(T) == typeof(string))
                        _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, value?.ToString() ?? string.Empty);
                    else if (typeof(T) == typeof(int))
                        _settingsStore.SetInt32(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, Convert.ToInt32(value));
                    else if (typeof(T) == typeof(bool))
                        _settingsStore.SetBoolean(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, Convert.ToBoolean(value));
                    else
                    {
                        // For complex types, serialize to JSON
                        var jsonValue = JsonSerializer.Serialize(value);
                        _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, jsonValue);
                    }

                    // Fire configuration changed event
                    ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                    {
                        Key = key,
                        OldValue = oldValue,
                        NewValue = value
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting value for key '{key}': {ex.Message}");
                    throw;
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var configuration = GetAIModelConfiguration();
            return await ValidateConfigurationAsync(configuration);
        }
        
        /// <summary>
        /// Validates a specific configuration
        /// </summary>
        public async Task<ConfigurationValidationResult> ValidateConfigurationAsync(AIModelConfiguration configuration)
        {
            return await Task.Run(() =>
            {
                var result = new ConfigurationValidationResult();

                if (configuration == null)
                {
                    result.Errors.Add("Configuration is null");
                    result.IsValid = false;
                    return result;
                }

                // Use the model's built-in validation
                var validationErrors = configuration.GetValidationErrors();
                result.Errors.AddRange(validationErrors);
                result.IsValid = validationErrors.Count == 0;

                // Additional validation logic can be added here
                return result;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets configuration to default values
        /// </summary>
        public async Task ResetToDefaultsAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        // Clear all configuration values
                        if (_settingsStore.CollectionExists(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection))
                        {
                            _settingsStore.DeleteCollection(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection);
                        }

                        // Recreate collection and set defaults
                        EnsureCollectionExists();
                        _cachedConfiguration = null; // Force reload

                        // Set default values
                        var defaultConfig = CreateDefaultConfiguration();
                        
                        // Use sync version to avoid deadlock in sync context
                        var configJson = JsonSerializer.Serialize(defaultConfig);
                        _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection,
                            AiderVSExtension.Models.Constants.ConfigurationKeys.SelectedProvider, configJson);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resetting to defaults: {ex.Message}");
                        throw;
                    }
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Exports configuration to a file
        /// </summary>
        public async Task ExportConfigurationAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            await Task.Run(() =>
            {
                try
                {
                    var configuration = GetAIModelConfiguration();
                    var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error exporting configuration: {ex.Message}");
                    throw;
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports configuration from a file
        /// </summary>
        public async Task ImportConfigurationAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            try
            {
                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var configuration = JsonSerializer.Deserialize<AIModelConfiguration>(json);
                if (configuration != null)
                {
                    await SetAIModelConfigurationAsync(configuration).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the connection to the configured AI provider
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            var configuration = GetAIModelConfiguration();
            return await TestConnectionAsync(configuration);
        }
        
        /// <summary>
        /// Tests the connection for a specific configuration
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync(AIModelConfiguration configuration)
        {
            var result = new ConnectionTestResult();

            if (configuration == null)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = "No configuration found";
                return result;
            }

            try
            {
                var startTime = DateTime.UtcNow;
                
                // Validate configuration first
                if (!configuration.IsValid())
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = "Configuration validation failed";
                    result.ResponseTime = DateTime.UtcNow - startTime;
                    return result;
                }

                // Implement actual connection testing based on provider type
                var connectionResult = await TestProviderConnectionAsync(configuration).ConfigureAwait(false);
                result.IsSuccessful = connectionResult.IsSuccessful;
                result.ErrorMessage = connectionResult.ErrorMessage;
                result.ResponseTime = DateTime.UtcNow - startTime;
                result.AdditionalInfo["Provider"] = configuration.Provider.ToString();
                
                if (connectionResult.IsSuccessful)
                {
                    result.AdditionalInfo["ConnectionTest"] = "Passed";
                }
                else
                {
                    result.AdditionalInfo["ConnectionTest"] = "Failed";
                    result.AdditionalInfo["FailureReason"] = connectionResult.ErrorMessage;
                }
                result.AdditionalInfo["ModelName"] = configuration.ModelName ?? "Default";
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets all available AI model configurations
        /// </summary>
        public List<AIModelConfiguration> GetAvailableConfigurations()
        {
            var configurations = new List<AIModelConfiguration>();

            // Add default configurations for each provider
            configurations.Add(new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ModelName = "gpt-4",
                IsEnabled = true
            });

            configurations.Add(new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ModelName = "claude-3-opus",
                IsEnabled = true
            });

            configurations.Add(new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                ModelName = Constants.DefaultValues.DefaultOllamaModel,
                EndpointUrl = Constants.DefaultValues.DefaultOllamaEndpoint,
                IsEnabled = true
            });

            return configurations;
        }

        /// <summary>
        /// Migrates configuration from older versions
        /// </summary>
        public async Task<MigrationResult> MigrateConfigurationAsync()
        {
            return await Task.Run(() =>
            {
                var result = new MigrationResult
                {
                    IsSuccessful = true,
                    FromVersion = "0.0.0",
                    ToVersion = "1.0.0"
                };

                try
                {
                    // Check if migration is needed
                    var currentVersion = GetValue("ConfigurationVersion", "0.0.0");
                    
                    if (currentVersion == "1.0.0")
                    {
                        result.MigrationSteps.Add("No migration needed - already at latest version");
                        return result;
                    }

                    result.FromVersion = currentVersion;
                    result.MigrationSteps.Add($"Migrating from version {currentVersion} to 1.0.0");

                    // Perform migration steps here
                    // For now, just update the version - use sync to avoid deadlock
                    _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection,
                        "ConfigurationVersion", "1.0.0");
                    result.MigrationSteps.Add("Updated configuration version");
                }
                catch (Exception ex)
                {
                    result.IsSuccessful = false;
                    result.Warnings.Add($"Migration failed: {ex.Message}");
                }

                return result;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures the configuration collection exists in the settings store
        /// </summary>
        private void EnsureCollectionExists()
        {
            if (!_settingsStore.CollectionExists(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection))
            {
                _settingsStore.CreateCollection(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection);
            }
        }

        /// <summary>
        /// Creates a default AI model configuration
        /// </summary>
        private static AIModelConfiguration CreateDefaultConfiguration()
        {
            return new AIModelConfiguration
            {
                Provider = Constants.DefaultValues.DefaultProvider,
                TimeoutSeconds = Constants.DefaultValues.DefaultTimeoutSeconds,
                MaxRetries = Constants.DefaultValues.DefaultMaxRetries,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Creates a configuration for storage with encrypted API key
        /// </summary>
        private AIModelConfiguration CreateConfigurationForStorage(AIModelConfiguration config)
        {
            var storageConfig = new AIModelConfiguration
            {
                Provider = config.Provider,
                EndpointUrl = config.EndpointUrl,
                ModelName = config.ModelName,
                IsEnabled = config.IsEnabled,
                AdditionalSettings = new Dictionary<string, object>(config.AdditionalSettings),
                LastModified = config.LastModified,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };

            // Encrypt the API key if present
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                try
                {
                    storageConfig.ApiKey = EncryptApiKey(config.ApiKey);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error encrypting API key: {ex.Message}");
                    // Store as plain text if encryption fails (should not happen in production)
                    storageConfig.ApiKey = config.ApiKey;
                }
            }

            return storageConfig;
        }

        /// <summary>
        /// Creates a configuration from storage with decrypted API key
        /// </summary>
        private AIModelConfiguration CreateConfigurationFromStorage(AIModelConfiguration storedConfig)
        {
            var config = new AIModelConfiguration
            {
                Provider = storedConfig.Provider,
                EndpointUrl = storedConfig.EndpointUrl,
                ModelName = storedConfig.ModelName,
                IsEnabled = storedConfig.IsEnabled,
                AdditionalSettings = new Dictionary<string, object>(storedConfig.AdditionalSettings),
                LastModified = storedConfig.LastModified,
                TimeoutSeconds = storedConfig.TimeoutSeconds,
                MaxRetries = storedConfig.MaxRetries
            };

            // Decrypt the API key if present
            if (!string.IsNullOrEmpty(storedConfig.ApiKey))
            {
                try
                {
                    config.ApiKey = DecryptApiKey(storedConfig.ApiKey);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error decrypting API key: {ex.Message}");
                    // Assume it's plain text if decryption fails (backward compatibility)
                    config.ApiKey = storedConfig.ApiKey;
                }
            }

            return config;
        }

        /// <summary>
        /// Encrypts an API key using Windows Data Protection API
        /// </summary>
        private string EncryptApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return apiKey;

            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(apiKey);
                var entropy = GetMachineEntropy();
                var encryptedBytes = ProtectedData.Protect(plainBytes, entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to encrypt API key: {ex.Message}");
                throw new InvalidOperationException("Failed to encrypt API key", ex);
            }
        }

        /// <summary>
        /// Decrypts an API key using Windows Data Protection API
        /// </summary>
        private string DecryptApiKey(string encryptedApiKey)
        {
            if (string.IsNullOrEmpty(encryptedApiKey))
                return encryptedApiKey;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedApiKey);
                var entropy = GetMachineEntropy();
                var plainBytes = ProtectedData.Unprotect(encryptedBytes, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException)
            {
                // If it's not base64, assume it's plain text (backward compatibility)
                return encryptedApiKey;
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to decrypt API key: {ex.Message}");
                // Return as-is for backward compatibility
                return encryptedApiKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error decrypting API key: {ex.Message}");
                throw new InvalidOperationException("Failed to decrypt API key", ex);
            }
        }

        /// <summary>
        /// Gets machine-specific entropy for encryption
        /// </summary>
        private byte[] GetMachineEntropy()
        {
            // Combine machine name and user name for entropy
            var entropyString = $"{Environment.MachineName}|{Environment.UserName}|AiderVS";
            return Encoding.UTF8.GetBytes(entropyString);
        }

        /// <summary>
        /// Securely saves an API key for a specific provider
        /// </summary>
        public async Task SaveApiKeyAsync(AIProvider provider, string apiKey)
        {
            var config = GetAIModelConfiguration();
            
            // Create a new configuration with the updated API key
            var updatedConfig = new AIModelConfiguration
            {
                Provider = provider,
                ApiKey = apiKey,
                EndpointUrl = config.EndpointUrl,
                ModelName = config.ModelName,
                IsEnabled = config.IsEnabled,
                AdditionalSettings = new Dictionary<string, object>(config.AdditionalSettings),
                LastModified = DateTime.UtcNow,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };

            await SetAIModelConfigurationAsync(updatedConfig).ConfigureAwait(false);
        }

        /// <summary>
        /// Securely retrieves an API key for a specific provider
        /// </summary>
        public string GetApiKey(AIProvider provider)
        {
            var config = GetAIModelConfiguration();
            return config.Provider == provider ? config.ApiKey : null;
        }

        /// <summary>
        /// Clears the API key for a specific provider
        /// </summary>
        public async Task ClearApiKeyAsync(AIProvider provider)
        {
            await SaveApiKeyAsync(provider, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a configuration setting by key (alias for GetValue for compatibility)
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default(T))
        {
            return GetValue<T>(key, defaultValue);
        }
        
        /// <summary>
        /// Gets a configuration value synchronously
        /// </summary>
        public AIModelConfiguration GetConfiguration()
        {
            return GetAIModelConfiguration();
        }
        
        /// <summary>
        /// Gets a configuration value asynchronously
        /// </summary>
        public async Task<AIModelConfiguration> GetConfigurationAsync()
        {
            return await Task.FromResult(GetAIModelConfiguration());
        }
        
        /// <summary>
        /// Sets a configuration value synchronously
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                    _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, value?.ToString() ?? string.Empty);
                else if (typeof(T) == typeof(int))
                    _settingsStore.SetInt32(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, Convert.ToInt32(value));
                else if (typeof(T) == typeof(bool))
                    _settingsStore.SetBoolean(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, Convert.ToBoolean(value));
                else
                {
                    // For complex types, serialize to JSON
                    var jsonValue = JsonSerializer.Serialize(value);
                    _settingsStore.SetString(AiderVSExtension.Models.Constants.ConfigurationKeys.RootCollection, key, jsonValue);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting value for key '{key}': {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets whether AI completion is enabled
        /// </summary>
        public bool IsAICompletionEnabled
        {
            get => GetValue(Constants.ConfigurationKeys.AICompletionEnabled, Constants.DefaultValues.DefaultAICompletionEnabled);
            set => SetValue(Constants.ConfigurationKeys.AICompletionEnabled, value);
        }
        
        /// <summary>
        /// Toggles AI completion on/off
        /// </summary>
        public async Task ToggleAICompletionAsync()
        {
            var oldValue = IsAICompletionEnabled;
            IsAICompletionEnabled = !IsAICompletionEnabled;
            
            // Fire configuration changed event
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                Key = Constants.ConfigurationKeys.AICompletionEnabled,
                OldValue = oldValue,
                NewValue = IsAICompletionEnabled
            });
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Saves a configuration asynchronously
        /// </summary>
        public async Task SaveConfigurationAsync(AIModelConfiguration configuration)
        {
            await SetAIModelConfigurationAsync(configuration);
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear cached configuration
                    lock (_lockObject)
                    {
                        _cachedConfiguration = null;
                    }
                }

                _disposed = true;
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestProviderConnectionAsync(AIModelConfiguration configuration)
        {
            try
            {
                switch (configuration.Provider)
                {
                    case AIProvider.ChatGPT:
                        return await TestOpenAIConnectionAsync(configuration).ConfigureAwait(false);
                    case AIProvider.Claude:
                        return await TestClaudeConnectionAsync(configuration).ConfigureAwait(false);
                    case AIProvider.Ollama:
                        return await TestOllamaConnectionAsync(configuration).ConfigureAwait(false);
                    default:
                        return (false, "Unknown AI provider");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Connection test failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestOpenAIConnectionAsync(AIModelConfiguration configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration.ApiKey))
                {
                    return (false, "API key is required for OpenAI");
                }

                using var httpClient = CertificatePinning.CreateSecureHttpClient("https://api.openai.com");
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration.ApiKey}");
                
                var response = await httpClient.GetAsync("/v1/models").ConfigureAwait(false);
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "OpenAI connection successful");
                }
                else
                {
                    return (false, $"OpenAI API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return (false, $"Network error connecting to OpenAI: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"OpenAI connection failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestClaudeConnectionAsync(AIModelConfiguration configuration)
        {
            try
            {
                if (string.IsNullOrEmpty(configuration.ApiKey))
                {
                    return (false, "API key is required for Claude");
                }

                using var httpClient = CertificatePinning.CreateSecureHttpClient("https://api.anthropic.com");
                httpClient.DefaultRequestHeaders.Add("x-api-key", configuration.ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
                var response = await httpClient.GetAsync("/v1/models").ConfigureAwait(false);
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "Claude connection successful");
                }
                else
                {
                    return (false, $"Claude API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return (false, $"Network error connecting to Claude: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Claude connection failed: {ex.Message}");
            }
        }

        private async Task<(bool IsSuccessful, string ErrorMessage)> TestOllamaConnectionAsync(AIModelConfiguration configuration)
        {
            try
            {
                var endpoint = !string.IsNullOrEmpty(configuration.Endpoint) 
                    ? SecureUrlBuilder.EnforceHttps(configuration.Endpoint) 
                    : "http://localhost:11434";
                
                // For Ollama, disable certificate pinning since it's typically local
                using var httpClient = CertificatePinning.CreateSecureHttpClient(endpoint, enablePinning: false);
                httpClient.Timeout = TimeSpan.FromSeconds(10); // Shorter timeout for local service
                
                var response = await httpClient.GetAsync("/api/tags").ConfigureAwait(false);
                if (((int)response.StatusCode >= 200 && (int)response.StatusCode < 300))
                {
                    return (true, "Ollama connection successful");
                }
                else
                {
                    return (false, $"Ollama API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return (false, $"Network error connecting to Ollama: {ex.Message}");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                return (false, "Timeout connecting to Ollama. Make sure Ollama is running.");
            }
            catch (Exception ex)
            {
                return (false, $"Ollama connection failed: {ex.Message}");
            }
        }

        #endregion

        #region Additional AI Model Management Methods

        /// <summary>
        /// Gets all AI model configurations asynchronously
        /// </summary>
        public async Task<IEnumerable<AIModelConfiguration>> GetAllModelConfigurationsAsync()
        {
            return await Task.Run(() =>
            {
                var configurations = new List<AIModelConfiguration>();

                foreach (AIProvider provider in Enum.GetValues(typeof(AIProvider)))
                {
                    var config = GetAIModelConfiguration(provider);
                    if (config != null)
                    {
                        configurations.Add(config);
                    }
                }

                return configurations.AsEnumerable();
            });
        }

        /// <summary>
        /// Gets the ID of the currently active AI model
        /// </summary>
        public async Task<string> GetActiveModelIdAsync()
        {
            return await Task.FromResult(GetValue("ActiveAIModel", string.Empty));
        }

        /// <summary>
        /// Sets the ID of the active AI model
        /// </summary>
        public async Task SetActiveModelIdAsync(string modelId)
        {
            await SetValueAsync("ActiveAIModel", modelId ?? string.Empty);
        }

        #endregion
    }
}

