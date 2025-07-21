using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Services
{
    public class AIModelManagerTests : IDisposable
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly AIModelManager _aiModelManager;
        private readonly List<AIModelConfiguration> _testModels;

        public AIModelManagerTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _testModels = CreateTestModels();
            
            SetupMockConfigService();
            
            _aiModelManager = new AIModelManager(_mockConfigService.Object);
        }

        public void Dispose()
        {
            _aiModelManager?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIModelManager(null));
        }

        [Fact]
        public void Constructor_WithValidConfigService_CreatesInstance()
        {
            // Act
            var manager = new AIModelManager(_mockConfigService.Object);

            // Assert
            manager.Should().NotBeNull();
            manager.ActiveModel.Should().BeNull();
        }

        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_LoadsAvailableModels()
        {
            // Act
            await _aiModelManager.InitializeAsync();

            // Assert
            var availableModels = await _aiModelManager.GetAvailableModelsAsync();
            availableModels.Should().HaveCount(3);
            availableModels.Should().Contain(m => m.Provider == AIProvider.ChatGPT);
            availableModels.Should().Contain(m => m.Provider == AIProvider.Claude);
            availableModels.Should().Contain(m => m.Provider == AIProvider.Ollama);
        }

        [Fact]
        public async Task InitializeAsync_WithActiveModelId_SetsActiveModel()
        {
            // Arrange
            var chatGptModel = _testModels.First(m => m.Provider == AIProvider.ChatGPT);
            _mockConfigService.Setup(x => x.GetActiveModelIdAsync())
                .ReturnsAsync(chatGptModel.Id);

            // Act
            await _aiModelManager.InitializeAsync();

            // Assert
            _aiModelManager.ActiveModel.Should().NotBeNull();
            _aiModelManager.ActiveModel.Provider.Should().Be(AIProvider.ChatGPT);
            _aiModelManager.ActiveModel.Id.Should().Be(chatGptModel.Id);
        }

        [Fact]
        public async Task InitializeAsync_WithInvalidActiveModelId_LeavesActiveModelNull()
        {
            // Arrange
            _mockConfigService.Setup(x => x.GetActiveModelIdAsync())
                .ReturnsAsync("invalid-model-id");

            // Act
            await _aiModelManager.InitializeAsync();

            // Assert
            _aiModelManager.ActiveModel.Should().BeNull();
        }

        #endregion

        #region GetAvailableModelsAsync Tests

        [Fact]
        public async Task GetAvailableModelsAsync_ReturnsAllModels()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            var models = await _aiModelManager.GetAvailableModelsAsync();

            // Assert
            models.Should().HaveCount(3);
            models.Should().Contain(m => m.Provider == AIProvider.ChatGPT);
            models.Should().Contain(m => m.Provider == AIProvider.Claude);
            models.Should().Contain(m => m.Provider == AIProvider.Ollama);
        }

        [Fact]
        public async Task GetAvailableModelsAsync_WithProvider_ReturnsFilteredModels()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            var chatGptModels = await _aiModelManager.GetAvailableModelsAsync(AIProvider.ChatGPT);

            // Assert
            chatGptModels.Should().HaveCount(1);
            chatGptModels.First().Provider.Should().Be(AIProvider.ChatGPT);
        }

        [Fact]
        public async Task GetAvailableModelsAsync_WithNonExistentProvider_ReturnsEmptyCollection()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            var models = await _aiModelManager.GetAvailableModelsAsync((AIProvider)999);

            // Assert
            models.Should().BeEmpty();
        }

        #endregion

        #region SetActiveModelAsync Tests

        [Fact]
        public async Task SetActiveModelAsync_WithValidProviderAndModel_SetsActiveModel()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            var targetModel = _testModels.First(m => m.Provider == AIProvider.Claude);

            // Act
            var result = await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, targetModel.ModelName);

            // Assert
            result.Should().BeTrue();
            _aiModelManager.ActiveModel.Should().NotBeNull();
            _aiModelManager.ActiveModel.Provider.Should().Be(AIProvider.Claude);
            _aiModelManager.ActiveModel.ModelName.Should().Be(targetModel.ModelName);
        }

        [Fact]
        public async Task SetActiveModelAsync_WithNonExistentModel_ReturnsFalse()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            var result = await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "non-existent-model");

            // Assert
            result.Should().BeFalse();
            _aiModelManager.ActiveModel.Should().BeNull();
        }

        [Fact]
        public async Task SetActiveModelAsync_WithValidModel_SavesActiveModelId()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            var targetModel = _testModels.First(m => m.Provider == AIProvider.Ollama);

            // Act
            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, targetModel.ModelName);

            // Assert
            _mockConfigService.Verify(x => x.SetActiveModelIdAsync(targetModel.Id), Times.Once);
        }

        [Fact]
        public async Task SetActiveModelAsync_WithValidModel_FiresActiveModelChangedEvent()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            var targetModel = _testModels.First(m => m.Provider == AIProvider.ChatGPT);
            
            ModelChangedEventArgs eventArgs = null;
            _aiModelManager.ActiveModelChanged += (sender, args) => eventArgs = args;

            // Act
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, targetModel.ModelName);

            // Assert
            eventArgs.Should().NotBeNull();
            eventArgs.PreviousModel.Should().BeNull();
            eventArgs.NewModel.Should().NotBeNull();
            eventArgs.NewModel.Provider.Should().Be(AIProvider.ChatGPT);
        }

        [Fact]
        public async Task SetActiveModelAsync_WithProviderOnly_SetsFirstAvailableModel()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude);

            // Assert
            _aiModelManager.ActiveModel.Should().NotBeNull();
            _aiModelManager.ActiveModel.Provider.Should().Be(AIProvider.Claude);
        }

        #endregion

        #region TestConnectionAsync Tests

        [Fact]
        public async Task TestConnectionAsync_WithNullConfiguration_ReturnsFalse()
        {
            // Act
            var result = await _aiModelManager.TestConnectionAsync((AIModelConfiguration)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidConfiguration_ReturnsFalse()
        {
            // Arrange
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid - empty API key
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(invalidConfig);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidChatGPTConfiguration_ReturnsTrue()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-api-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(validConfig);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidClaudeConfiguration_ReturnsTrue()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "valid-claude-key",
                ModelName = "claude-3-opus",
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(validConfig);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidOllamaConfiguration_ReturnsTrue()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434",
                ModelName = "llama2",
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(validConfig);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task TestConnectionAsync_WithConnectionTestResult_ReturnsDetailedResult()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(validConfig);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.ModelVersion.Should().Be("gpt-4");
        }

        #endregion

        #region GetCompletionAsync Tests

        [Fact]
        public async Task GetCompletionAsync_WithNoActiveModel_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CompletionRequest { Prompt = "test prompt" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _aiModelManager.GetCompletionAsync(request));
        }

        [Fact]
        public async Task GetCompletionAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _aiModelManager.GetCompletionAsync(null));
        }

        [Fact]
        public async Task GetCompletionAsync_WithValidRequest_ReturnsCompletion()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");
            var request = new CompletionRequest 
            { 
                Prompt = "Complete this code",
                MaxTokens = 100,
                Temperature = 0.7
            };

            // Act
            var response = await _aiModelManager.GetCompletionAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("completion response");
        }

        [Fact]
        public async Task GetCompletionAsync_WithDifferentProviders_ReturnsProviderSpecificResponse()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            var request = new CompletionRequest { Prompt = "test" };

            // Test ChatGPT
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");
            var chatGptResponse = await _aiModelManager.GetCompletionAsync(request);

            // Test Claude
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-opus");
            var claudeResponse = await _aiModelManager.GetCompletionAsync(request);

            // Test Ollama
            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, "llama2");
            var ollamaResponse = await _aiModelManager.GetCompletionAsync(request);

            // Assert
            chatGptResponse.Content.Should().Contain("OpenAI");
            claudeResponse.Content.Should().Contain("Claude");
            ollamaResponse.Content.Should().Contain("Ollama");
        }

        #endregion

        #region SendChatMessageAsync Tests

        [Fact]
        public async Task SendChatMessageAsync_WithNoActiveModel_ThrowsInvalidOperationException()
        {
            // Arrange
            var message = new ChatMessage { Content = "test message", Type = MessageType.User };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _aiModelManager.SendChatMessageAsync(message));
        }

        [Fact]
        public async Task SendChatMessageAsync_WithNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _aiModelManager.SendChatMessageAsync((ChatMessage)null));
        }

        [Fact]
        public async Task SendChatMessageAsync_WithValidMessage_ReturnsResponse()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-opus");
            var message = new ChatMessage 
            { 
                Content = "Hello AI",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var response = await _aiModelManager.SendChatMessageAsync(message);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SendChatAsync_WithEmptyMessages_ReturnsErrorResponse()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            // Act
            var response = await _aiModelManager.SendChatAsync(Enumerable.Empty<ChatMessage>());

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeFalse();
            response.ErrorMessage.Should().Be("No messages provided");
        }

        [Fact]
        public async Task SendChatAsync_WithValidMessages_ReturnsLastMessageResponse()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, "llama2");
            var messages = new[]
            {
                new ChatMessage { Content = "First message", Type = MessageType.User },
                new ChatMessage { Content = "Second message", Type = MessageType.User }
            };

            // Act
            var response = await _aiModelManager.SendChatAsync(messages);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().Contain("Ollama");
        }

        #endregion

        #region GenerateCompletionAsync Tests

        [Fact]
        public async Task GenerateCompletionAsync_WithNoActiveModel_ThrowsInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _aiModelManager.GenerateCompletionAsync("test prompt"));
        }

        [Fact]
        public async Task GenerateCompletionAsync_WithEmptyPrompt_ReturnsEmptyString()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            // Act
            var result = await _aiModelManager.GenerateCompletionAsync("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GenerateCompletionAsync_WithValidPrompt_ReturnsCompletion()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            // Act
            var result = await _aiModelManager.GenerateCompletionAsync("Complete this code");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("completion response");
        }

        #endregion

        #region GetAvailableModelsForProviderAsync Tests

        [Fact]
        public async Task GetAvailableModelsForProviderAsync_WithChatGPT_ReturnsGPTModels()
        {
            // Act
            var models = await _aiModelManager.GetAvailableModelsForProviderAsync(AIProvider.ChatGPT);

            // Assert
            models.Should().NotBeEmpty();
            models.Should().Contain("gpt-4");
            models.Should().Contain("gpt-4-turbo");
            models.Should().Contain("gpt-3.5-turbo");
        }

        [Fact]
        public async Task GetAvailableModelsForProviderAsync_WithClaude_ReturnsClaudeModels()
        {
            // Act
            var models = await _aiModelManager.GetAvailableModelsForProviderAsync(AIProvider.Claude);

            // Assert
            models.Should().NotBeEmpty();
            models.Should().Contain("claude-3-opus");
            models.Should().Contain("claude-3-sonnet");
            models.Should().Contain("claude-3-haiku");
        }

        [Fact]
        public async Task GetAvailableModelsForProviderAsync_WithOllama_ReturnsOllamaModels()
        {
            // Act
            var models = await _aiModelManager.GetAvailableModelsForProviderAsync(AIProvider.Ollama);

            // Assert
            models.Should().NotBeEmpty();
            models.Should().Contain("llama2");
            models.Should().Contain("codellama");
            models.Should().Contain("mistral");
        }

        [Fact]
        public async Task GetAvailableModelsForProviderAsync_WithInvalidProvider_ReturnsEmpty()
        {
            // Act
            var models = await _aiModelManager.GetAvailableModelsForProviderAsync((AIProvider)999);

            // Assert
            models.Should().BeEmpty();
        }

        #endregion

        #region GetCurrentModel Tests

        [Fact]
        public void GetCurrentModel_WithNoActiveModel_ReturnsNull()
        {
            // Act
            var currentModel = _aiModelManager.GetCurrentModel();

            // Assert
            currentModel.Should().BeNull();
        }

        [Fact]
        public async Task GetCurrentModel_WithActiveModel_ReturnsActiveModel()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-opus");

            // Act
            var currentModel = _aiModelManager.GetCurrentModel();

            // Assert
            currentModel.Should().NotBeNull();
            currentModel.Provider.Should().Be(AIProvider.Claude);
            currentModel.ModelName.Should().Be("claude-3-opus");
        }

        #endregion

        #region GetAvailableModels Tests

        [Fact]
        public async Task GetAvailableModels_ReturnsAllAvailableModels()
        {
            // Arrange
            await _aiModelManager.InitializeAsync();

            // Act
            var models = _aiModelManager.GetAvailableModels();

            // Assert
            models.Should().HaveCount(3);
            models.Should().Contain(m => m.Provider == AIProvider.ChatGPT);
            models.Should().Contain(m => m.Provider == AIProvider.Claude);
            models.Should().Contain(m => m.Provider == AIProvider.Ollama);
        }

        #endregion

        #region Private Helper Methods

        private List<AIModelConfiguration> CreateTestModels()
        {
            return new List<AIModelConfiguration>
            {
                new AIModelConfiguration
                {
                    Id = "chatgpt-1",
                    Provider = AIProvider.ChatGPT,
                    ModelName = "gpt-4",
                    ApiKey = "test-openai-key",
                    IsEnabled = true,
                    TimeoutSeconds = 30,
                    MaxRetries = 3
                },
                new AIModelConfiguration
                {
                    Id = "claude-1",
                    Provider = AIProvider.Claude,
                    ModelName = "claude-3-opus",
                    ApiKey = "test-claude-key",
                    IsEnabled = true,
                    TimeoutSeconds = 45,
                    MaxRetries = 2
                },
                new AIModelConfiguration
                {
                    Id = "ollama-1",
                    Provider = AIProvider.Ollama,
                    ModelName = "llama2",
                    EndpointUrl = "http://localhost:11434",
                    IsEnabled = true,
                    TimeoutSeconds = 60,
                    MaxRetries = 5
                }
            };
        }

        private void SetupMockConfigService()
        {
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(_testModels);

            _mockConfigService.Setup(x => x.GetActiveModelIdAsync())
                .ReturnsAsync((string)null);

            _mockConfigService.Setup(x => x.SetActiveModelIdAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        #endregion
    }
}