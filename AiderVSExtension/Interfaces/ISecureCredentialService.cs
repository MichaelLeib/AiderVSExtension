using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiderVSExtension.Interfaces
{
    /// <summary>
    /// Service for secure storage and retrieval of credentials
    /// </summary>
    public interface ISecureCredentialService
    {
        /// <summary>
        /// Stores a credential securely
        /// </summary>
        /// <param name="key">The credential key</param>
        /// <param name="value">The credential value</param>
        /// <param name="options">Storage options</param>
        Task StoreCredentialAsync(string key, string value, CredentialStorageOptions options = null);

        /// <summary>
        /// Retrieves a credential securely
        /// </summary>
        /// <param name="key">The credential key</param>
        /// <returns>The credential value, or null if not found</returns>
        Task<string> RetrieveCredentialAsync(string key);

        /// <summary>
        /// Removes a credential from secure storage
        /// </summary>
        /// <param name="key">The credential key</param>
        /// <returns>True if the credential was removed</returns>
        Task<bool> RemoveCredentialAsync(string key);

        /// <summary>
        /// Checks if a credential exists in secure storage
        /// </summary>
        /// <param name="key">The credential key</param>
        /// <returns>True if the credential exists</returns>
        Task<bool> CredentialExistsAsync(string key);

        /// <summary>
        /// Lists all credential keys (without values)
        /// </summary>
        /// <returns>Collection of credential keys</returns>
        Task<IEnumerable<string>> ListCredentialKeysAsync();

        /// <summary>
        /// Validates the integrity of stored credentials
        /// </summary>
        /// <returns>Validation result</returns>
        Task<CredentialValidationResult> ValidateCredentialsAsync();

        /// <summary>
        /// Encrypts a value using the service's encryption
        /// </summary>
        /// <param name="plainText">The plain text to encrypt</param>
        /// <returns>The encrypted value</returns>
        Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// Decrypts a value using the service's encryption
        /// </summary>
        /// <param name="encryptedText">The encrypted text to decrypt</param>
        /// <returns>The decrypted value</returns>
        Task<string> DecryptAsync(string encryptedText);

        /// <summary>
        /// Clears all stored credentials
        /// </summary>
        Task ClearAllCredentialsAsync();
    }

    /// <summary>
    /// Options for credential storage
    /// </summary>
    public class CredentialStorageOptions
    {
        /// <summary>
        /// Gets or sets the expiration time for the credential
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets whether the credential should persist across sessions
        /// </summary>
        public bool Persistent { get; set; } = true;

        /// <summary>
        /// Gets or sets additional metadata for the credential
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the encryption level for the credential
        /// </summary>
        public EncryptionLevel EncryptionLevel { get; set; } = EncryptionLevel.Standard;
    }

    /// <summary>
    /// Result of credential validation
    /// </summary>
    public class CredentialValidationResult
    {
        /// <summary>
        /// Gets or sets whether all credentials are valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any validation errors
        /// </summary>
        public IList<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets any validation warnings
        /// </summary>
        public IList<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the number of credentials validated
        /// </summary>
        public int CredentialCount { get; set; }

        /// <summary>
        /// Gets or sets the number of expired credentials
        /// </summary>
        public int ExpiredCount { get; set; }
    }

    /// <summary>
    /// Encryption levels for credential storage
    /// </summary>
    public enum EncryptionLevel
    {
        None,
        Basic,
        Standard,
        High
    }
}
