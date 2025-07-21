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

namespace AiderVSExtension.Tests.Integration
{
    /// <summary>
    /// Integration tests for AI service communication and model management
    /// </summary>
    public class AIServiceIntegrationTests : IDisposable
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly AIModelManager _aiModelManager;
        private readonly AiderService _aiderService;

        public AIServiceIntegrationTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            
            SetupMockServices();
            
            _aiModelManager = new AIModelManager(_mockConfigService.Object);
            _aiderService = new AiderService(_mockErrorHandler.Object, _mockConfigService.Object);
        }

        public void Dispose()
        {
            _aiModelManager?.Dispose();
            _aiderService?.Dispose();
        }

        #region AI Model Manager and Configuration Integration Tests

        [Fact]
        public async Task AIModelManager_WithConfigurationService_LoadsModelsCorrectly()
        {
            // Arrange
            var testModels = CreateTestModelConfigurations();
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(testModels);

            // Act
            await _aiModelManager.InitializeAsync();
            var availableModels = await _aiModelManager.GetAvailableModelsAsync();

            // Assert
            availableModels.Should().HaveCount(3);
            availableModels.Should().Contain(m => m.Provider == AIProvider.ChatGPT);
            availableModels.Should().Contain(m => m.Provider == AIProvider.Claude);
            availableModels.Should().Contain(m => m.Provider == AIProvider.Ollama);
        }

        [Fact]
        public async Task AIModelManager_RealAPICall_ChatGPTIntegration()
        {
            // Arrange
            var chatGptConfig = new AIModelConfiguration
            {
                Id = "chatgpt-test",
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-openai-key", // In real tests, use test API key
                ModelName = "gpt-3.5-turbo",
                TimeoutSeconds = 30,
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { chatGptConfig });

            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-3.5-turbo");

            var request = new CompletionRequest
            {
                Prompt = "Write a simple hello world function in C#",
                MaxTokens = 150,
                Temperature = 0.3
            };

            // Act
            var response = await _aiModelManager.GetCompletionAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("OpenAI"); // Mock response indicates provider
        }

        [Fact]
        public async Task AIModelManager_RealAPICall_ClaudeIntegration()
        {
            // Arrange
            var claudeConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "test-claude-key", // In real tests, use test API key
                ModelName = "claude-3-haiku",
                TimeoutSeconds = 30,
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { claudeConfig });

            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-haiku");

            var request = new CompletionRequest
            {
                Prompt = "Explain what a Visual Studio extension is in one sentence",
                MaxTokens = 100,
                Temperature = 0.2
            };

            // Act
            var response = await _aiModelManager.GetCompletionAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("Visual Studio");
            response.TokensUsed.Should().BeGreaterThan(0);
            response.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task AIModelManager_RealAPICall_OllamaIntegration()
        {
            // Arrange
            var ollamaConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434", // Assumes Ollama is running locally
                ModelName = "llama2",
                TimeoutSeconds = 60,
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { ollamaConfig });

            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, "llama2");

            var request = new CompletionRequest
            {
                Prompt = "What is C# programming language?",
                MaxTokens = 200,
                Temperature = 0.5
            };

            // Act
            var response = await _aiModelManager.GetCompletionAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.IsSuccess.Should().BeTrue();
            response.Content.Should().NotBeNullOrEmpty();
            response.Content.Should().Contain("C#");
            response.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task AIModelManager_SetActiveModel_UpdatesConfiguration()
        {
            // Arrange
            var testModels = CreateTestModelConfigurations();
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(testModels);

            await _aiModelManager.InitializeAsync();

            // Act
            var result = await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-opus");

            // Assert
            result.Should().BeTrue();
            _aiModelManager.ActiveModel.Should().NotBeNull();
            _aiModelManager.ActiveModel.Provider.Should().Be(AIProvider.Claude);
            
            // Verify configuration service was called to save active model
            _mockConfigService.Verify(x => x.SetActiveModelIdAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AIModelManager_TestConnection_ValidatesWithConfiguration()
        {
            // Arrange
            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-test-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                IsEnabled = true
            };

            // Act
            var result = await _aiModelManager.TestConnectionAsync(validConfig);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.ModelVersion.Should().Be("gpt-4");
            result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task AIModelManager_GetCompletion_UsesActiveModelConfiguration()
        {
            // Arrange
            var testModels = CreateTestModelConfigurations();
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(testModels);

            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, "llama2");

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
            response.Content.Should().Contain("Ollama"); // Mock response indicates provider
        }

        #endregion

        #region Aider Service and AI Model Integration Tests

        [Fact]
        public async Task AiderService_SendMessage_IntegratesWithModelConfiguration()
        {
            // Arrange
            var testConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 30
            };

            _mockConfigService.Setup(x => x.GetAIModelConfiguration())
                .Returns(testConfig);

            var message = new ChatMessage
            {
                Content = "Test message for integration",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var response = await _aiderService.SendMessageAsync(message);

            // Assert
            response.Should().NotBeNull();
            response.Type.Should().Be(MessageType.System);
            
            // Verify error handler was used for logging
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.Contains("Message queued")), 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AiderService_WithFileReferences_ProcessesContextCorrectly()
        {
            // Arrange
            var fileReferences = new List<FileReference>
            {
                new FileReference
                {
                    FilePath = "test.cs",
                    StartLine = 1,
                    EndLine = 10,
                    Type = ReferenceType.File,
                    Content = "public class TestClass { }"
                },
                new FileReference
                {
                    FilePath = "helper.cs",
                    StartLine = 5,
                    EndLine = 15,
                    Type = ReferenceType.Selection,
                    Content = "public void HelperMethod() { }"
                }
            };

            var message = "Analyze these files";

            // Act
            await _aiderService.SendMessageAsync(message, fileReferences);

            // Assert
            var history = await _aiderService.GetChatHistoryAsync();
            history.Should().HaveCount(1);
            
            var savedMessage = history.First();
            savedMessage.Content.Should().Be(message);
            savedMessage.References.Should().HaveCount(2);
            savedMessage.References.Should().Contain(r => r.FilePath == "test.cs");
            savedMessage.References.Should().Contain(r => r.FilePath == "helper.cs");
        }

        #endregion

        #region Cross-Service Communication Tests

        [Fact]
        public async Task AIModelManager_And_AiderService_ShareConfigurationState()
        {
            // Arrange
            var sharedConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "shared-key",
                ModelName = "claude-3-sonnet",
                TimeoutSeconds = 45,
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAIModelConfiguration())
                .Returns(sharedConfig);
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { sharedConfig });

            // Act - Initialize both services
            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-sonnet");

            var message = new ChatMessage
            {
                Content = "Test with shared configuration",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                ModelUsed = sharedConfig.ModelName
            };

            var response = await _aiderService.SendMessageAsync(message);

            // Assert - Both services should use the same configuration
            _aiModelManager.ActiveModel.Should().NotBeNull();
            _aiModelManager.ActiveModel.ModelName.Should().Be("claude-3-sonnet");
            
            response.Should().NotBeNull();
            
            // Verify configuration service was accessed by both services
            _mockConfigService.Verify(x => x.GetAIModelConfiguration(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ModelSwitching_UpdatesAllDependentServices()
        {
            // Arrange
            var initialConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "gpt-key",
                ModelName = "gpt-4"
            };

            var newConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434",
                ModelName = "llama2"
            };

            var configs = new[] { initialConfig, newConfig };
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(configs);

            // Setup configuration service to return different configs based on calls
            var configCallCount = 0;
            _mockConfigService.Setup(x => x.GetAIModelConfiguration())
                .Returns(() => configCallCount++ == 0 ? initialConfig : newConfig);

            await _aiModelManager.InitializeAsync();

            // Act - Switch models
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");
            var firstCompletion = await _aiModelManager.GenerateCompletionAsync("Test prompt 1");

            await _aiModelManager.SetActiveModelAsync(AIProvider.Ollama, "llama2");
            var secondCompletion = await _aiModelManager.GenerateCompletionAsync("Test prompt 2");

            // Assert - Verify model switching affected completions
            firstCompletion.Should().Contain("OpenAI"); // Mock response for ChatGPT
            secondCompletion.Should().Contain("Ollama"); // Mock response for Ollama

            _aiModelManager.ActiveModel.Provider.Should().Be(AIProvider.Ollama);
            _aiModelManager.ActiveModel.ModelName.Should().Be("llama2");
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public async Task AIServices_ErrorHandling_MaintainsServiceStability()
        {
            // Arrange
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid
                ModelName = "gpt-4"
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { invalidConfig });

            // Act - Attempt operations with invalid configuration
            await _aiModelManager.InitializeAsync();
            
            var connectionResult = await _aiModelManager.TestConnectionAsync(invalidConfig);
            
            var invalidMessage = new ChatMessage
            {
                Content = "", // Invalid
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Assert - Services should handle errors gracefully
            connectionResult.Should().NotBeNull();
            connectionResult.IsSuccessful.Should().BeFalse();

            // Aider service should throw for invalid message
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _aiderService.SendMessageAsync(invalidMessage));

            // Error handler should have been called
            _mockErrorHandler.Verify(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()), 
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ServiceRecovery_AfterConfigurationFix_RestoresNormalOperation()
        {
            // Arrange - Start with invalid configuration
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "", // Invalid
                ModelName = "claude-3-opus"
            };

            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "valid-claude-key",
                ModelName = "claude-3-opus",
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { invalidConfig });

            await _aiModelManager.InitializeAsync();

            // Act - Fix configuration and retry
            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { validConfig });

            // Reinitialize with valid configuration
            var newModelManager = new AIModelManager(_mockConfigService.Object);
            await newModelManager.InitializeAsync();
            await newModelManager.SetActiveModelAsync(AIProvider.Claude, "claude-3-opus");

            var testResult = await newModelManager.TestConnectionAsync(validConfig);
            var completion = await newModelManager.GenerateCompletionAsync("Test recovery");

            // Assert - Service should recover and work normally
            testResult.Should().NotBeNull();
            testResult.IsSuccessful.Should().BeTrue();
            
            completion.Should().NotBeNullOrEmpty();
            completion.Should().Contain("Claude");

            newModelManager.Dispose();
        }

        #endregion

        #region Performance Integration Tests

        [Fact]
        public async Task ConcurrentAIOperations_MaintainPerformanceAndStability()
        {
            // Arrange
            var testConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "concurrent-test-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            _mockConfigService.Setup(x => x.GetAllModelConfigurationsAsync())
                .ReturnsAsync(new[] { testConfig });

            await _aiModelManager.InitializeAsync();
            await _aiModelManager.SetActiveModelAsync(AIProvider.ChatGPT, "gpt-4");

            const int concurrentOperations = 10;
            var startTime = DateTime.UtcNow;

            // Act - Perform concurrent operations
            var tasks = new List<Task>();
            
            for (int i = 0; i < concurrentOperations; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    // AI Model Manager operations
                    var completion = await _aiModelManager.GenerateCompletionAsync($"Concurrent test {index}");
                    
                    // Aider Service operations
                    var message = new ChatMessage
                    {
                        Content = $"Concurrent message {index}",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    await _aiderService.SendMessageAsync(message);
                    
                    return completion;
                }));
            }

            var results = await Task.WhenAll(tasks.Cast<Task<string>>());
            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Assert - All operations should complete successfully
            totalTime.Should().BeLessThan(TimeSpan.FromSeconds(15));
            results.Should().HaveCount(concurrentOperations);
            results.Should().OnlyContain(r => !string.IsNullOrEmpty(r));

            var chatHistory = await _aiderService.GetChatHistoryAsync();
            chatHistory.Should().HaveCount(concurrentOperations);
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockServices()
        {
            // Setup configuration service
            _mockConfigService.Setup(x => x.GetActiveModelIdAsync())
                .ReturnsAsync((string)null);
            
            _mockConfigService.Setup(x => x.SetActiveModelIdAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockConfigService.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((key, defaultValue) => defaultValue);
            
            _mockConfigService.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<int>()))
                .Returns<string, int>((key, defaultValue) => defaultValue);

            // Setup error handler
            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private List<AIModelConfiguration> CreateTestModelConfigurations()
        {
            return new List<AIModelConfiguration>
            {
                new AIModelConfiguration
                {
                    Id = "chatgpt-test",
                    Provider = AIProvider.ChatGPT,
                    ModelName = "gpt-4",
                    ApiKey = "test-openai-key",
                    IsEnabled = true,
                    TimeoutSeconds = 30,
                    MaxRetries = 3
                },
                new AIModelConfiguration
                {
                    Id = "claude-test",
                    Provider = AIProvider.Claude,
                    ModelName = "claude-3-opus",
                    ApiKey = "test-claude-key",
                    IsEnabled = true,
                    TimeoutSeconds = 45,
                    MaxRetries = 2
                },
                new AIModelConfiguration
                {
                    Id = "ollama-test",
                    Provider = AIProvider.Ollama,
                    ModelName = "llama2",
                    EndpointUrl = "http://localhost:11434",
                    IsEnabled = true,
                    TimeoutSeconds = 60,
                    MaxRetries = 5
                }
            };
        }

        #endregion
    }
}