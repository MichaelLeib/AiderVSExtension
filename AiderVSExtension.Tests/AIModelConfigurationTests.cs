using System;
using System.Collections.Generic;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class AIModelConfigurationTests
    {
        [Fact]
        public void IsValid_WithValidConfiguration_ReturnsTrue()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(301)]
        [InlineData(500)]
        public void IsValid_WithInvalidTimeout_ReturnsFalse(int invalidTimeout)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = invalidTimeout,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(11)]
        public void IsValid_WithInvalidMaxRetries_ReturnsFalse(int invalidMaxRetries)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 30,
                MaxRetries = invalidMaxRetries
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithMissingApiKeyForChatGPT_ReturnsFalse()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = null,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithMissingEndpointUrlForOllama_ReturnsFalse()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = null,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidEndpointUrlFormat_ReturnsFalse()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "invalid-url",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrors_WithValidConfiguration_ReturnsEmptyList()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithMissingApiKey_ReturnsApiKeyError()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = null,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("API key is required for ChatGPT");
        }

        [Fact]
        public void GetValidationErrors_WithInvalidEndpoint_ReturnsEndpointErrors()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "invalid-url",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain("Invalid endpoint URL format");
        }

        [Fact]
        public void JsonSerialization_WithValidConfiguration_SerializesCorrectly()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 60,
                MaxRetries = 5
            };

            // Act
            var json = JsonSerializer.Serialize(config);
            var deserializedConfig = JsonSerializer.Deserialize<AIModelConfiguration>(json);

            // Assert
            deserializedConfig.Should().NotBeNull();
            deserializedConfig.Provider.Should().Be(AIProvider.Claude);
            deserializedConfig.ApiKey.Should().Be("valid-api-key");
            deserializedConfig.TimeoutSeconds.Should().Be(60);
            deserializedConfig.MaxRetries.Should().Be(5);
        }

        [Theory]
        [InlineData(AIProvider.ChatGPT)]
        [InlineData(AIProvider.Claude)]
        public void IsValid_WithValidApiProviders_ReturnsTrue(AIProvider provider)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = provider,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("http://localhost:11434")]
        [InlineData("https://ollama.example.com")]
        [InlineData("http://192.168.1.100:8080")]
        public void IsValid_WithValidOllamaEndpoints_ReturnsTrue(string endpointUrl)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = endpointUrl,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("ftp://localhost")]
        [InlineData("ws://localhost")]
        [InlineData("file://localhost")]
        public void IsValid_WithInvalidProtocol_ReturnsFalse(string endpointUrl)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = endpointUrl,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(300)]
        public void IsValid_WithValidTimeoutRange_ReturnsTrue(int timeoutSeconds)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = timeoutSeconds,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void IsValid_WithValidMaxRetriesRange_ReturnsTrue(int maxRetries)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                TimeoutSeconds = 30,
                MaxRetries = maxRetries
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleIssues_ReturnsAllErrors()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = null,
                TimeoutSeconds = 0,
                MaxRetries = -1
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(3);
            errors.Should().Contain("API key is required for ChatGPT");
            errors.Should().Contain("Timeout must be between 1 and 300 seconds");
            errors.Should().Contain("Max retries must be between 0 and 10");
        }

        [Fact]
        public void GetValidationErrors_WithOllamaMissingEndpoint_ReturnsEndpointError()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = null,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Endpoint URL is required for Ollama");
        }

        [Fact]
        public void GetValidationErrors_WithOllamaInvalidProtocol_ReturnsProtocolError()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "ftp://localhost",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Endpoint URL must use HTTP or HTTPS protocol");
        }

        [Fact]
        public void GetDisplayName_WithModelName_ReturnsProviderAndModel()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ModelName = "gpt-4",
                ApiKey = "valid-api-key"
            };

            // Act
            var displayName = config.GetDisplayName();

            // Assert
            displayName.Should().Be("ChatGPT (gpt-4)");
        }

        [Fact]
        public void GetDisplayName_WithoutModelName_ReturnsProviderOnly()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "valid-api-key"
            };

            // Act
            var displayName = config.GetDisplayName();

            // Assert
            displayName.Should().Be("Claude");
        }

        [Fact]
        public void RequiresApiKey_WithChatGPT_ReturnsTrue()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT
            };

            // Act
            var result = config.RequiresApiKey();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void RequiresApiKey_WithOllama_ReturnsFalse()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama
            };

            // Act
            var result = config.RequiresApiKey();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void SupportsCustomEndpoint_WithOllama_ReturnsTrue()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama
            };

            // Act
            var result = config.SupportsCustomEndpoint();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void SupportsCustomEndpoint_WithChatGPT_ReturnsFalse()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT
            };

            // Act
            var result = config.SupportsCustomEndpoint();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 5,
                AdditionalSettings = new Dictionary<string, object> { { "temperature", 0.7 } }
            };

            // Act
            var cloned = config.Clone();

            // Assert
            cloned.Should().NotBeSameAs(config);
            cloned.Provider.Should().Be(config.Provider);
            cloned.ApiKey.Should().Be(config.ApiKey);
            cloned.ModelName.Should().Be(config.ModelName);
            cloned.TimeoutSeconds.Should().Be(config.TimeoutSeconds);
            cloned.MaxRetries.Should().Be(config.MaxRetries);
            cloned.AdditionalSettings.Should().NotBeSameAs(config.AdditionalSettings);
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var config = new AIModelConfiguration();

            // Assert
            config.IsEnabled.Should().BeTrue();
            config.TimeoutSeconds.Should().Be(30);
            config.MaxRetries.Should().Be(3);
            config.AdditionalSettings.Should().NotBeNull();
            config.AdditionalSettings.Should().BeEmpty();
            config.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void JsonSerialization_WithProviderEnum_SerializesAsString()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434"
            };

            // Act
            var json = JsonSerializer.Serialize(config);

            // Assert
            json.Should().Contain("\"provider\":\"Ollama\"");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void IsValid_WithEmptyApiKey_ReturnsFalse(string emptyApiKey)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = emptyApiKey,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void IsValid_WithEmptyEndpointUrl_ReturnsFalse(string emptyEndpointUrl)
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = emptyEndpointUrl,
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            result.Should().BeFalse();
        }
    }
}
