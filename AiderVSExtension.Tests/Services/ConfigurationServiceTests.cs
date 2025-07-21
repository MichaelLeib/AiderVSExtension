using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Services
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly Mock<WritableSettingsStore> _mockSettingsStore;
        private readonly ConfigurationService _configurationService;
        private readonly Dictionary<string, object> _settingsStorage;

        public ConfigurationServiceTests()
        {
            _settingsStorage = new Dictionary<string, object>();
            _mockSettingsStore = new Mock<WritableSettingsStore>();
            
            // Setup mock behavior
            SetupMockSettingsStore();
            
            _configurationService = new ConfigurationService(_mockSettingsStore.Object);
        }

        public void Dispose()
        {
            _configurationService?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullSettingsStore_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConfigurationService(null));
        }

        [Fact]
        public void Constructor_WithValidSettingsStore_CreatesInstance()
        {
            // Act
            var service = new ConfigurationService(_mockSettingsStore.Object);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region GetAIModelConfiguration Tests

        [Fact]
        public void GetAIModelConfiguration_WhenNoConfigurationExists_ReturnsDefaultConfiguration()
        {
            // Arrange
            _settingsStorage.Clear();

            // Act
            var config = _configurationService.GetAIModelConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Provider.Should().Be(Constants.DefaultValues.DefaultProvider);
            config.TimeoutSeconds.Should().Be(Constants.DefaultValues.DefaultTimeoutSeconds);
            config.MaxRetries.Should().Be(Constants.DefaultValues.DefaultMaxRetries);
            config.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void GetAIModelConfiguration_WhenValidConfigurationExists_ReturnsStoredConfiguration()
        {
            // Arrange
            var expectedConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-api-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 5,
                IsEnabled = true
            };
            
            var configJson = System.Text.Json.JsonSerializer.Serialize(expectedConfig);
            _settingsStorage[Constants.ConfigurationKeys.SelectedProvider] = configJson;

            // Act
            var config = _configurationService.GetAIModelConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Provider.Should().Be(AIProvider.ChatGPT);
            config.ApiKey.Should().Be("test-api-key");
            config.ModelName.Should().Be("gpt-4");
            config.TimeoutSeconds.Should().Be(60);
            config.MaxRetries.Should().Be(5);
            config.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void GetAIModelConfiguration_WhenInvalidJsonExists_ReturnsDefaultConfiguration()
        {
            // Arrange
            _settingsStorage[Constants.ConfigurationKeys.SelectedProvider] = "invalid-json";

            // Act
            var config = _configurationService.GetAIModelConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Provider.Should().Be(Constants.DefaultValues.DefaultProvider);
        }

        [Fact]
        public void GetAIModelConfiguration_CalledMultipleTimes_ReturnsCachedConfiguration()
        {
            // Arrange
            var expectedConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "claude-key"
            };
            
            var configJson = System.Text.Json.JsonSerializer.Serialize(expectedConfig);
            _settingsStorage[Constants.ConfigurationKeys.SelectedProvider] = configJson;

            // Act
            var config1 = _configurationService.GetAIModelConfiguration();
            var config2 = _configurationService.GetAIModelConfiguration();

            // Assert
            config1.Should().BeSameAs(config2);
            _mockSettingsStore.Verify(x => x.GetString(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region SetAIModelConfigurationAsync Tests

        [Fact]
        public async Task SetAIModelConfigurationAsync_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _configurationService.SetAIModelConfigurationAsync(null));
        }

        [Fact]
        public async Task SetAIModelConfigurationAsync_WithValidConfiguration_SavesConfiguration()
        {
            // Arrange
            var configuration = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434",
                ModelName = "llama2",
                IsEnabled = true
            };

            // Act
            await _configurationService.SetAIModelConfigurationAsync(configuration);

            // Assert
            var savedConfig = _configurationService.GetAIModelConfiguration();
            savedConfig.Provider.Should().Be(AIProvider.Ollama);
            savedConfig.EndpointUrl.Should().Be("http://localhost:11434");
            savedConfig.ModelName.Should().Be("llama2");
            savedConfig.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task SetAIModelConfigurationAsync_WithValidConfiguration_FiresConfigurationChangedEvent()
        {
            // Arrange
            var configuration = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "new-key"
            };

            ConfigurationChangedEventArgs eventArgs = null;
            _configurationService.ConfigurationChanged += (sender, args) => eventArgs = args;

            // Act
            await _configurationService.SetAIModelConfigurationAsync(configuration);

            // Assert
            eventArgs.Should().NotBeNull();
            eventArgs.Key.Should().Be(Constants.ConfigurationKeys.SelectedProvider);
            eventArgs.NewValue.Should().Be(configuration);
        }

        #endregion

        #region GetValue/SetValueAsync Tests

        [Fact]
        public void GetValue_WithNullOrEmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _configurationService.GetValue<string>(null));
            Assert.Throws<ArgumentException>(() => _configurationService.GetValue<string>(""));
        }

        [Fact]
        public void GetValue_WithNonExistentKey_ReturnsDefaultValue()
        {
            // Act
            var result = _configurationService.GetValue("non-existent-key", "default-value");

            // Assert
            result.Should().Be("default-value");
        }

        [Fact]
        public void GetValue_WithExistingStringKey_ReturnsStoredValue()
        {
            // Arrange
            _settingsStorage["test-key"] = "test-value";

            // Act
            var result = _configurationService.GetValue("test-key", "default");

            // Assert
            result.Should().Be("test-value");
        }

        [Fact]
        public void GetValue_WithExistingIntKey_ReturnsStoredValue()
        {
            // Arrange
            _settingsStorage["int-key"] = 42;

            // Act
            var result = _configurationService.GetValue("int-key", 0);

            // Assert
            result.Should().Be(42);
        }

        [Fact]
        public void GetValue_WithExistingBoolKey_ReturnsStoredValue()
        {
            // Arrange
            _settingsStorage["bool-key"] = true;

            // Act
            var result = _configurationService.GetValue("bool-key", false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task SetValueAsync_WithNullOrEmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.SetValueAsync<string>(null, "value"));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.SetValueAsync<string>("", "value"));
        }

        [Fact]
        public async Task SetValueAsync_WithStringValue_StoresValue()
        {
            // Act
            await _configurationService.SetValueAsync("string-key", "test-value");

            // Assert
            _settingsStorage["string-key"].Should().Be("test-value");
        }

        [Fact]
        public async Task SetValueAsync_WithIntValue_StoresValue()
        {
            // Act
            await _configurationService.SetValueAsync("int-key", 123);

            // Assert
            _settingsStorage["int-key"].Should().Be(123);
        }

        [Fact]
        public async Task SetValueAsync_WithBoolValue_StoresValue()
        {
            // Act
            await _configurationService.SetValueAsync("bool-key", true);

            // Assert
            _settingsStorage["bool-key"].Should().Be(true);
        }

        [Fact]
        public async Task SetValueAsync_WithComplexObject_SerializesToJson()
        {
            // Arrange
            var complexObject = new { Name = "Test", Value = 42 };

            // Act
            await _configurationService.SetValueAsync("complex-key", complexObject);

            // Assert
            var storedValue = _settingsStorage["complex-key"] as string;
            storedValue.Should().NotBeNullOrEmpty();
            storedValue.Should().Contain("Test");
            storedValue.Should().Contain("42");
        }

        [Fact]
        public async Task SetValueAsync_FiresConfigurationChangedEvent()
        {
            // Arrange
            ConfigurationChangedEventArgs eventArgs = null;
            _configurationService.ConfigurationChanged += (sender, args) => eventArgs = args;

            // Act
            await _configurationService.SetValueAsync("test-key", "test-value");

            // Assert
            eventArgs.Should().NotBeNull();
            eventArgs.Key.Should().Be("test-key");
            eventArgs.NewValue.Should().Be("test-value");
        }

        #endregion

        #region ValidateConfigurationAsync Tests

        [Fact]
        public async Task ValidateConfigurationAsync_WithValidConfiguration_ReturnsValidResult()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                IsEnabled = true
            };
            
            await _configurationService.SetAIModelConfigurationAsync(validConfig);

            // Act
            var result = await _configurationService.ValidateConfigurationAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateConfigurationAsync_WithInvalidConfiguration_ReturnsInvalidResult()
        {
            // Arrange - Create invalid configuration (missing API key for ChatGPT)
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid - empty API key
                TimeoutSeconds = 30,
                MaxRetries = 3,
                IsEnabled = true
            };
            
            await _configurationService.SetAIModelConfigurationAsync(invalidConfig);

            // Act
            var result = await _configurationService.ValidateConfigurationAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        #endregion

        #region ResetToDefaultsAsync Tests

        [Fact]
        public async Task ResetToDefaultsAsync_ClearsExistingConfiguration()
        {
            // Arrange
            await _configurationService.SetValueAsync("test-key", "test-value");
            _settingsStorage.Should().ContainKey("test-key");

            // Act
            await _configurationService.ResetToDefaultsAsync();

            // Assert
            var config = _configurationService.GetAIModelConfiguration();
            config.Provider.Should().Be(Constants.DefaultValues.DefaultProvider);
            config.TimeoutSeconds.Should().Be(Constants.DefaultValues.DefaultTimeoutSeconds);
            config.MaxRetries.Should().Be(Constants.DefaultValues.DefaultMaxRetries);
        }

        #endregion

        #region Export/Import Tests

        [Fact]
        public async Task ExportConfigurationAsync_WithNullOrEmptyFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.ExportConfigurationAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.ExportConfigurationAsync(""));
        }

        [Fact]
        public async Task ExportConfigurationAsync_WithValidFilePath_ExportsConfiguration()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "export-test-key",
                ModelName = "claude-3-opus"
            };
            
            await _configurationService.SetAIModelConfigurationAsync(config);

            try
            {
                // Act
                await _configurationService.ExportConfigurationAsync(tempFile);

                // Assert
                File.Exists(tempFile).Should().BeTrue();
                var exportedContent = await File.ReadAllTextAsync(tempFile);
                exportedContent.Should().Contain("Claude");
                exportedContent.Should().Contain("export-test-key");
                exportedContent.Should().Contain("claude-3-opus");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task ImportConfigurationAsync_WithNullOrEmptyFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.ImportConfigurationAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _configurationService.ImportConfigurationAsync(""));
        }

        [Fact]
        public async Task ImportConfigurationAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _configurationService.ImportConfigurationAsync("non-existent-file.json"));
        }

        [Fact]
        public async Task ImportConfigurationAsync_WithValidFile_ImportsConfiguration()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var configToImport = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434",
                ModelName = "imported-model",
                TimeoutSeconds = 45,
                MaxRetries = 2,
                IsEnabled = false
            };
            
            var configJson = System.Text.Json.JsonSerializer.Serialize(configToImport, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(tempFile, configJson);

            try
            {
                // Act
                await _configurationService.ImportConfigurationAsync(tempFile);

                // Assert
                var importedConfig = _configurationService.GetAIModelConfiguration();
                importedConfig.Provider.Should().Be(AIProvider.Ollama);
                importedConfig.EndpointUrl.Should().Be("http://localhost:11434");
                importedConfig.ModelName.Should().Be("imported-model");
                importedConfig.TimeoutSeconds.Should().Be(45);
                importedConfig.MaxRetries.Should().Be(2);
                importedConfig.IsEnabled.Should().BeFalse();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        #endregion

        #region TestConnectionAsync Tests

        [Fact]
        public async Task TestConnectionAsync_WithValidConfiguration_ReturnsSuccessfulResult()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };
            
            await _configurationService.SetAIModelConfigurationAsync(validConfig);

            // Act
            var result = await _configurationService.TestConnectionAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.AdditionalInfo.Should().ContainKey("Provider");
            result.AdditionalInfo["Provider"].Should().Be("ChatGPT");
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidConfiguration_ReturnsFailedResult()
        {
            // Arrange
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid
                IsEnabled = true
            };
            
            await _configurationService.SetAIModelConfigurationAsync(invalidConfig);

            // Act
            var result = await _configurationService.TestConnectionAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().Be("Configuration validation failed");
        }

        #endregion

        #region GetAvailableConfigurations Tests

        [Fact]
        public void GetAvailableConfigurations_ReturnsConfigurationsForAllProviders()
        {
            // Act
            var configurations = _configurationService.GetAvailableConfigurations();

            // Assert
            configurations.Should().NotBeNull();
            configurations.Should().HaveCount(3);
            configurations.Should().Contain(c => c.Provider == AIProvider.ChatGPT);
            configurations.Should().Contain(c => c.Provider == AIProvider.Claude);
            configurations.Should().Contain(c => c.Provider == AIProvider.Ollama);
        }

        [Fact]
        public void GetAvailableConfigurations_AllConfigurationsAreEnabled()
        {
            // Act
            var configurations = _configurationService.GetAvailableConfigurations();

            // Assert
            configurations.Should().OnlyContain(c => c.IsEnabled);
        }

        #endregion

        #region MigrateConfigurationAsync Tests

        [Fact]
        public async Task MigrateConfigurationAsync_WhenAlreadyLatestVersion_ReturnsNoMigrationNeeded()
        {
            // Arrange
            await _configurationService.SetValueAsync("ConfigurationVersion", "1.0.0");

            // Act
            var result = await _configurationService.MigrateConfigurationAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.FromVersion.Should().Be("1.0.0");
            result.ToVersion.Should().Be("1.0.0");
            result.MigrationSteps.Should().Contain("No migration needed - already at latest version");
        }

        [Fact]
        public async Task MigrateConfigurationAsync_WhenOlderVersion_PerformsMigration()
        {
            // Arrange
            await _configurationService.SetValueAsync("ConfigurationVersion", "0.5.0");

            // Act
            var result = await _configurationService.MigrateConfigurationAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.FromVersion.Should().Be("0.5.0");
            result.ToVersion.Should().Be("1.0.0");
            result.MigrationSteps.Should().Contain("Migrating from version 0.5.0 to 1.0.0");
            result.MigrationSteps.Should().Contain("Updated configuration version");
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockSettingsStore()
        {
            // Setup collection existence check
            _mockSettingsStore.Setup(x => x.CollectionExists(It.IsAny<string>()))
                .Returns(true);

            // Setup property existence check
            _mockSettingsStore.Setup(x => x.PropertyExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => _settingsStorage.ContainsKey(key));

            // Setup string getter
            _mockSettingsStore.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => 
                    _settingsStorage.ContainsKey(key) ? _settingsStorage[key]?.ToString() : string.Empty);

            // Setup int getter
            _mockSettingsStore.Setup(x => x.GetInt32(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => 
                    _settingsStorage.ContainsKey(key) && _settingsStorage[key] is int intValue ? intValue : 0);

            // Setup bool getter
            _mockSettingsStore.Setup(x => x.GetBoolean(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => 
                    _settingsStorage.ContainsKey(key) && _settingsStorage[key] is bool boolValue ? boolValue : false);

            // Setup string setter
            _mockSettingsStore.Setup(x => x.SetString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((collection, key, value) => _settingsStorage[key] = value);

            // Setup int setter
            _mockSettingsStore.Setup(x => x.SetInt32(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback<string, string, int>((collection, key, value) => _settingsStorage[key] = value);

            // Setup bool setter
            _mockSettingsStore.Setup(x => x.SetBoolean(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((collection, key, value) => _settingsStorage[key] = value);

            // Setup collection creation
            _mockSettingsStore.Setup(x => x.CreateCollection(It.IsAny<string>()));

            // Setup collection deletion
            _mockSettingsStore.Setup(x => x.DeleteCollection(It.IsAny<string>()))
                .Callback<string>(collection => _settingsStorage.Clear());
        }

        #endregion
    }
}