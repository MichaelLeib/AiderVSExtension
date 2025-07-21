using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using AiderVSExtension.Interfaces;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for secure storage and retrieval of credentials using Visual Studio settings store and Windows DPAPI
    /// </summary>
    public class SecureCredentialService : ISecureCredentialService
    {
        private const string CredentialsCollectionPath = "AiderVSExtension\\Credentials";
        private const string MetadataCollectionPath = "AiderVSExtension\\CredentialMetadata";
        
        private readonly WritableSettingsStore _settingsStore;
        private readonly IErrorHandler _errorHandler;

        public SecureCredentialService(IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            try
            {
                var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
                _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                
                // Ensure the collection exists
                if (!_settingsStore.CollectionExists(CredentialsCollectionPath))
                {
                    _settingsStore.CreateCollection(CredentialsCollectionPath);
                }
                
                if (!_settingsStore.CollectionExists(MetadataCollectionPath))
                {
                    _settingsStore.CreateCollection(MetadataCollectionPath);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.LogErrorAsync("Failed to initialize SecureCredentialService", ex, "SecureCredentialService.Constructor");
                throw;
            }
        }

        /// <summary>
        /// Stores a credential securely using Windows DPAPI encryption
        /// </summary>
        public async Task StoreCredentialAsync(string key, string value, CredentialStorageOptions options = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Credential key cannot be null or empty", nameof(key));

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Credential value cannot be null or empty", nameof(value));

            try
            {
                await Task.Run(() =>
                {
                    options ??= new CredentialStorageOptions();

                    // Create credential envelope with metadata
                    var envelope = new CredentialEnvelope
                    {
                        Value = value,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = options.ExpiresAt,
                        EncryptionLevel = options.EncryptionLevel,
                        Metadata = options.Metadata
                    };

                    // Serialize and encrypt the credential
                    var serialized = JsonSerializer.Serialize(envelope);
                    var encrypted = EncryptString(serialized, options.EncryptionLevel);

                    // Store in VS settings
                    _settingsStore.SetString(CredentialsCollectionPath, key, encrypted);

                    // Store metadata separately (unencrypted for key listing)
                    var metadata = new CredentialMetadata
                    {
                        Key = key,
                        CreatedAt = envelope.CreatedAt,
                        ExpiresAt = envelope.ExpiresAt,
                        EncryptionLevel = options.EncryptionLevel,
                        Persistent = options.Persistent
                    };

                    var metadataJson = JsonSerializer.Serialize(metadata);
                    _settingsStore.SetString(MetadataCollectionPath, key, metadataJson);
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Failed to store credential with key: {key}", ex, "SecureCredentialService.StoreCredentialAsync");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a credential securely
        /// </summary>
        public async Task<string> RetrieveCredentialAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Credential key cannot be null or empty", nameof(key));

            try
            {
                return await Task.Run(() =>
                {
                    if (!_settingsStore.PropertyExists(CredentialsCollectionPath, key))
                        return null;

                    var encrypted = _settingsStore.GetString(CredentialsCollectionPath, key);
                    
                    // Get metadata to determine encryption level
                    var encryptionLevel = EncryptionLevel.Standard;
                    if (_settingsStore.PropertyExists(MetadataCollectionPath, key))
                    {
                        var metadataJson = _settingsStore.GetString(MetadataCollectionPath, key);
                        var metadata = JsonSerializer.Deserialize<CredentialMetadata>(metadataJson);
                        encryptionLevel = metadata.EncryptionLevel;

                        // Check if credential has expired
                        if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
                        {
                            // Remove expired credential
                            _settingsStore.DeleteProperty(CredentialsCollectionPath, key);
                            _settingsStore.DeleteProperty(MetadataCollectionPath, key);
                            return null;
                        }
                    }

                    // Decrypt and deserialize
                    var decrypted = DecryptString(encrypted, encryptionLevel);
                    var envelope = JsonSerializer.Deserialize<CredentialEnvelope>(decrypted);

                    return envelope.Value;
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Failed to retrieve credential with key: {key}", ex, "SecureCredentialService.RetrieveCredentialAsync");
                return null;
            }
        }

        /// <summary>
        /// Removes a credential from secure storage
        /// </summary>
        public async Task<bool> RemoveCredentialAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                return await Task.Run(() =>
                {
                    var exists = _settingsStore.PropertyExists(CredentialsCollectionPath, key);
                    
                    if (exists)
                    {
                        _settingsStore.DeleteProperty(CredentialsCollectionPath, key);
                        
                        if (_settingsStore.PropertyExists(MetadataCollectionPath, key))
                        {
                            _settingsStore.DeleteProperty(MetadataCollectionPath, key);
                        }
                    }

                    return exists;
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Failed to remove credential with key: {key}", ex, "SecureCredentialService.RemoveCredentialAsync");
                return false;
            }
        }

        /// <summary>
        /// Checks if a credential exists in secure storage
        /// </summary>
        public async Task<bool> CredentialExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                return await Task.Run(() => _settingsStore.PropertyExists(CredentialsCollectionPath, key));
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync($"Failed to check credential existence for key: {key}", ex, "SecureCredentialService.CredentialExistsAsync");
                return false;
            }
        }

        /// <summary>
        /// Lists all credential keys (without values)
        /// </summary>
        public async Task<IEnumerable<string>> ListCredentialKeysAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (!_settingsStore.CollectionExists(MetadataCollectionPath))
                        return Enumerable.Empty<string>();

                    var keys = new List<string>();
                    var propertyNames = _settingsStore.GetPropertyNames(MetadataCollectionPath);

                    foreach (var propertyName in propertyNames)
                    {
                        try
                        {
                            var metadataJson = _settingsStore.GetString(MetadataCollectionPath, propertyName);
                            var metadata = JsonSerializer.Deserialize<CredentialMetadata>(metadataJson);

                            // Check if credential has expired
                            if (metadata.ExpiresAt.HasValue && metadata.ExpiresAt.Value < DateTime.UtcNow)
                            {
                                // Remove expired credential
                                _settingsStore.DeleteProperty(CredentialsCollectionPath, propertyName);
                                _settingsStore.DeleteProperty(MetadataCollectionPath, propertyName);
                                continue;
                            }

                            keys.Add(propertyName);
                        }
                        catch
                        {
                            // Skip invalid entries
                        }
                    }

                    return keys;
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Failed to list credential keys", ex, "SecureCredentialService.ListCredentialKeysAsync");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Validates the integrity of stored credentials
        /// </summary>
        public async Task<CredentialValidationResult> ValidateCredentialsAsync()
        {
            var result = new CredentialValidationResult();

            try
            {
                var keys = await ListCredentialKeysAsync();
                result.CredentialCount = keys.Count();

                foreach (var key in keys)
                {
                    try
                    {
                        var value = await RetrieveCredentialAsync(key);
                        if (string.IsNullOrEmpty(value))
                        {
                            result.Errors.Add($"Credential '{key}' could not be decrypted or is empty");
                        }

                        // Check for expiration
                        if (_settingsStore.PropertyExists(MetadataCollectionPath, key))
                        {
                            var metadataJson = _settingsStore.GetString(MetadataCollectionPath, key);
                            var metadata = JsonSerializer.Deserialize<CredentialMetadata>(metadataJson);

                            if (metadata.ExpiresAt.HasValue)
                            {
                                if (metadata.ExpiresAt.Value < DateTime.UtcNow)
                                {
                                    result.ExpiredCount++;
                                }
                                else if (metadata.ExpiresAt.Value < DateTime.UtcNow.AddDays(7))
                                {
                                    result.Warnings.Add($"Credential '{key}' expires within 7 days");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to validate credential '{key}': {ex.Message}");
                    }
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Failed to validate credentials", ex, "SecureCredentialService.ValidateCredentialsAsync");
                result.Errors.Add($"Validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Encrypts a value using the specified encryption level
        /// </summary>
        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                return await Task.Run(() => EncryptString(plainText, EncryptionLevel.Standard));
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Failed to encrypt value", ex, "SecureCredentialService.EncryptAsync");
                throw;
            }
        }

        /// <summary>
        /// Decrypts a value using the service's encryption
        /// </summary>
        public async Task<string> DecryptAsync(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                return await Task.Run(() => DecryptString(encryptedText, EncryptionLevel.Standard));
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Failed to decrypt value", ex, "SecureCredentialService.DecryptAsync");
                throw;
            }
        }

        /// <summary>
        /// Clears all stored credentials
        /// </summary>
        public async Task ClearAllCredentialsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (_settingsStore.CollectionExists(CredentialsCollectionPath))
                    {
                        _settingsStore.DeleteCollection(CredentialsCollectionPath);
                        _settingsStore.CreateCollection(CredentialsCollectionPath);
                    }

                    if (_settingsStore.CollectionExists(MetadataCollectionPath))
                    {
                        _settingsStore.DeleteCollection(MetadataCollectionPath);
                        _settingsStore.CreateCollection(MetadataCollectionPath);
                    }
                });
            }
            catch (Exception ex)
            {
                await _errorHandler.LogErrorAsync("Failed to clear all credentials", ex, "SecureCredentialService.ClearAllCredentialsAsync");
                throw;
            }
        }

        #region Private Helper Methods

        private string EncryptString(string plainText, EncryptionLevel level)
        {
            if (level == EncryptionLevel.None)
                return plainText;

            var data = Encoding.UTF8.GetBytes(plainText);
            
            switch (level)
            {
                case EncryptionLevel.Basic:
                case EncryptionLevel.Standard:
                    var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(encryptedData);
                
                case EncryptionLevel.High:
                    // For high-level encryption, use additional entropy
                    var entropy = Encoding.UTF8.GetBytes("AiderVSExtension_SecureCredentials");
                    var highEncryptedData = ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(highEncryptedData);
                
                default:
                    return plainText;
            }
        }

        private string DecryptString(string encryptedText, EncryptionLevel level)
        {
            if (level == EncryptionLevel.None)
                return encryptedText;

            var data = Convert.FromBase64String(encryptedText);
            
            switch (level)
            {
                case EncryptionLevel.Basic:
                case EncryptionLevel.Standard:
                    var decryptedData = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedData);
                
                case EncryptionLevel.High:
                    var entropy = Encoding.UTF8.GetBytes("AiderVSExtension_SecureCredentials");
                    var highDecryptedData = ProtectedData.Unprotect(data, entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(highDecryptedData);
                
                default:
                    return encryptedText;
            }
        }

        #endregion

        #region Internal Models

        private class CredentialEnvelope
        {
            public string Value { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public EncryptionLevel EncryptionLevel { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
        }

        private class CredentialMetadata
        {
            public string Key { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public EncryptionLevel EncryptionLevel { get; set; }
            public bool Persistent { get; set; }
        }

        #endregion
    }
}