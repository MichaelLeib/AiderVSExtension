using System;
using System.IO;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Integration
{
    /// <summary>
    /// Integration tests that verify the interaction between multiple services
    /// </summary>
    public class ServiceIntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly Mock<WritableSettingsStore> _mockSettingsStore;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly ConfigurationService _configurationService;
        private readonly ApplicationStateService _applicationStateService;
        private readonly ConversationPersistenceService _conversationPersistenceService;

        public ServiceIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ServiceIntegrationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _mockSettingsStore = new Mock<WritableSettingsStore>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            
            SetupMockSettingsStore();
            SetupMockErrorHandler();

            _configurationService = new ConfigurationService(_mockSettingsStore.Object);
            _applicationStateService = new ApplicationStateService(Path.Combine(_tempDirectory, "state.json"));
            _conversationPersistenceService = new ConversationPersistenceService(_tempDirectory);
        }

        public void Dispose()
        {
            _configurationService?.Dispose();
            _applicationStateService?.Dispose();
            _conversationPersistenceService?.Dispose();
            
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Configuration and State Integration Tests

        [Fact]
        public async Task ConfigurationService_And_ApplicationStateService_Integration_WorksTogether()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var aiConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-api-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 3,
                IsEnabled = true
            };

            // Act - Save configuration and track state
            await _configurationService.SetAIModelConfigurationAsync(aiConfig);
            await _applicationStateService.SetStateAsync("LastConfigurationUpdate", DateTime.UtcNow);
            await _applicationStateService.SetStateAsync("ConfigurationProvider", aiConfig.Provider.ToString());

            // Assert - Verify both services maintain consistent data
            var savedConfig = _configurationService.GetAIModelConfiguration();
            var lastUpdate = await _applicationStateService.GetStateAsync<DateTime>("LastConfigurationUpdate");
            var provider = await _applicationStateService.GetStateAsync<string>("ConfigurationProvider");

            savedConfig.Should().NotBeNull();
            savedConfig.Provider.Should().Be(AIProvider.ChatGPT);
            savedConfig.ApiKey.Should().Be("test-api-key");
            
            lastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            provider.Should().Be("ChatGPT");
        }

        [Fact]
        public async Task ConfigurationService_ValidationResult_PersistsInApplicationState()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            // Act
            await _configurationService.SetAIModelConfigurationAsync(invalidConfig);
            var validationResult = await _configurationService.ValidateConfigurationAsync();
            
            await _applicationStateService.SetStateAsync("LastValidationResult", validationResult.IsValid);
            await _applicationStateService.SetStateAsync("ValidationErrors", validationResult.Errors);

            // Assert
            var isValid = await _applicationStateService.GetStateAsync<bool>("LastValidationResult");
            var errors = await _applicationStateService.GetStateAsync<object>("ValidationErrors");

            isValid.Should().BeFalse();
            errors.Should().NotBeNull();
        }

        #endregion

        #region Configuration and Conversation Persistence Integration Tests

        [Fact]
        public async Task ConfigurationService_And_ConversationPersistence_Integration_WorksTogether()
        {
            // Arrange
            var aiConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "claude-api-key",
                ModelName = "claude-3-opus",
                IsEnabled = true
            };

            var conversation = new Conversation
            {
                Id = "test-conversation",
                Title = "Integration Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Test message using Claude",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow,
                        ModelUsed = aiConfig.ModelName
                    }
                }
            };

            // Act
            await _configurationService.SetAIModelConfigurationAsync(aiConfig);
            await _conversationPersistenceService.SaveConversationAsync(conversation);

            // Assert - Verify conversation references the configured model
            var savedConfig = _configurationService.GetAIModelConfiguration();
            var savedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);

            savedConfig.Provider.Should().Be(AIProvider.Claude);
            savedConfig.ModelName.Should().Be("claude-3-opus");
            
            savedConversation.Should().NotBeNull();
            savedConversation.Messages.First().ModelUsed.Should().Be(savedConfig.ModelName);
        }

        [Fact]
        public async Task ConversationPersistence_WithMultipleProviders_MaintainsModelHistory()
        {
            // Arrange
            var chatGptConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "gpt-key",
                ModelName = "gpt-4"
            };

            var claudeConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "claude-key",
                ModelName = "claude-3-opus"
            };

            var conversation = new Conversation
            {
                Id = "multi-model-conversation",
                Title = "Multi-Model Test",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act - Simulate switching between models during conversation
            await _configurationService.SetAIModelConfigurationAsync(chatGptConfig);
            conversation.AddMessage(new ChatMessage
            {
                Content = "Message with GPT-4",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                ModelUsed = chatGptConfig.ModelName
            });

            await _configurationService.SetAIModelConfigurationAsync(claudeConfig);
            conversation.AddMessage(new ChatMessage
            {
                Content = "Message with Claude",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                ModelUsed = claudeConfig.ModelName
            });

            await _conversationPersistenceService.SaveConversationAsync(conversation);

            // Assert
            var savedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
            
            savedConversation.Messages.Should().HaveCount(2);
            savedConversation.Messages.First().ModelUsed.Should().Be("gpt-4");
            savedConversation.Messages.Last().ModelUsed.Should().Be("claude-3-opus");
        }

        #endregion

        #region State and Conversation Persistence Integration Tests

        [Fact]
        public async Task ApplicationState_And_ConversationPersistence_TrackUsageStatistics()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var conversations = new[]
            {
                CreateTestConversation("conv1", "First Conversation", 3),
                CreateTestConversation("conv2", "Second Conversation", 5),
                CreateTestConversation("conv3", "Third Conversation", 2)
            };

            // Act - Save conversations and track statistics
            foreach (var conversation in conversations)
            {
                await _conversationPersistenceService.SaveConversationAsync(conversation);
            }

            var summaries = await _conversationPersistenceService.GetConversationSummariesAsync();
            var totalConversations = summaries.Count();
            var totalMessages = summaries.Sum(s => s.MessageCount);

            await _applicationStateService.SetStateAsync("TotalConversations", totalConversations);
            await _applicationStateService.SetStateAsync("TotalMessages", totalMessages);
            await _applicationStateService.SetStateAsync("LastStatisticsUpdate", DateTime.UtcNow);

            // Assert
            var storedConversationCount = await _applicationStateService.GetStateAsync<int>("TotalConversations");
            var storedMessageCount = await _applicationStateService.GetStateAsync<int>("TotalMessages");
            var lastUpdate = await _applicationStateService.GetStateAsync<DateTime>("LastStatisticsUpdate");

            storedConversationCount.Should().Be(3);
            storedMessageCount.Should().Be(10); // 3 + 5 + 2
            lastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task ConversationPersistence_Cleanup_UpdatesApplicationState()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var oldConversation = CreateTestConversation("old-conv", "Old Conversation", 2);
            oldConversation.LastModified = DateTime.UtcNow.AddDays(-40);
            
            var newConversation = CreateTestConversation("new-conv", "New Conversation", 3);
            
            await _conversationPersistenceService.SaveConversationAsync(oldConversation);
            await _conversationPersistenceService.SaveConversationAsync(newConversation);

            // Act
            var deletedCount = await _conversationPersistenceService.CleanupOldConversationsAsync(TimeSpan.FromDays(30));
            
            await _applicationStateService.SetStateAsync("LastCleanupDate", DateTime.UtcNow);
            await _applicationStateService.SetStateAsync("ConversationsDeleted", deletedCount);

            // Assert
            var cleanupDate = await _applicationStateService.GetStateAsync<DateTime>("LastCleanupDate");
            var deletedCountStored = await _applicationStateService.GetStateAsync<int>("ConversationsDeleted");
            
            deletedCount.Should().Be(1);
            deletedCountStored.Should().Be(1);
            cleanupDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Full Service Chain Integration Tests

        [Fact]
        public async Task FullServiceChain_ConfigurationToConversationToState_WorksEndToEnd()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.Ollama,
                EndpointUrl = "http://localhost:11434",
                ModelName = "llama2",
                IsEnabled = true
            };

            // Act - Full workflow simulation
            // 1. Configure AI model
            await _configurationService.SetAIModelConfigurationAsync(config);
            var validationResult = await _configurationService.ValidateConfigurationAsync();
            
            // 2. Create and save conversation
            var conversation = CreateTestConversation("full-chain-test", "Full Chain Test", 1);
            conversation.Messages.First().ModelUsed = config.ModelName;
            await _conversationPersistenceService.SaveConversationAsync(conversation);
            
            // 3. Update application state
            await _applicationStateService.SetStateAsync("ActiveModel", config.ModelName);
            await _applicationStateService.SetStateAsync("LastConversationId", conversation.Id);
            await _applicationStateService.SetStateAsync("ConfigurationValid", validationResult.IsValid);

            // Assert - Verify entire chain
            var savedConfig = _configurationService.GetAIModelConfiguration();
            var savedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
            var activeModel = await _applicationStateService.GetStateAsync<string>("ActiveModel");
            var lastConversationId = await _applicationStateService.GetStateAsync<string>("LastConversationId");
            var isConfigValid = await _applicationStateService.GetStateAsync<bool>("ConfigurationValid");

            // Configuration assertions
            savedConfig.Provider.Should().Be(AIProvider.Ollama);
            savedConfig.ModelName.Should().Be("llama2");
            
            // Conversation assertions
            savedConversation.Should().NotBeNull();
            savedConversation.Messages.First().ModelUsed.Should().Be("llama2");
            
            // State assertions
            activeModel.Should().Be("llama2");
            lastConversationId.Should().Be(conversation.Id);
            isConfigValid.Should().BeTrue();
        }

        [Fact]
        public async Task ServiceChain_ErrorHandling_MaintainsConsistency()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid
                TimeoutSeconds = 0, // Invalid
                MaxRetries = -1 // Invalid
            };

            // Act - Attempt operations with invalid configuration
            await _configurationService.SetAIModelConfigurationAsync(invalidConfig);
            var validationResult = await _configurationService.ValidateConfigurationAsync();
            
            // State should reflect the error condition
            await _applicationStateService.SetStateAsync("ConfigurationValid", validationResult.IsValid);
            await _applicationStateService.SetStateAsync("ValidationErrors", validationResult.Errors);
            await _applicationStateService.SetStateAsync("LastErrorTime", DateTime.UtcNow);

            // Assert - Verify error state is properly maintained
            var isValid = await _applicationStateService.GetStateAsync<bool>("ConfigurationValid");
            var errors = await _applicationStateService.GetStateAsync<object>("ValidationErrors");
            var errorTime = await _applicationStateService.GetStateAsync<DateTime>("LastErrorTime");

            isValid.Should().BeFalse();
            errors.Should().NotBeNull();
            errorTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            
            // Configuration should still be accessible but invalid
            var config = _configurationService.GetAIModelConfiguration();
            config.IsValid().Should().BeFalse();
        }

        #endregion

        #region Performance Integration Tests

        [Fact]
        public async Task ServiceIntegration_PerformanceUnderLoad_MaintainsResponsiveness()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            const int operationCount = 100;
            var startTime = DateTime.UtcNow;

            // Act - Perform multiple operations concurrently
            var tasks = new List<Task>();
            
            for (int i = 0; i < operationCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Configuration operations
                    var config = new AIModelConfiguration
                    {
                        Provider = AIProvider.ChatGPT,
                        ApiKey = $"test-key-{index}",
                        ModelName = "gpt-4",
                        IsEnabled = true
                    };
                    await _configurationService.SetAIModelConfigurationAsync(config);
                    
                    // State operations
                    await _applicationStateService.SetStateAsync($"Operation_{index}", DateTime.UtcNow);
                    
                    // Conversation operations
                    var conversation = CreateTestConversation($"perf-test-{index}", $"Performance Test {index}", 1);
                    await _conversationPersistenceService.SaveConversationAsync(conversation);
                }));
            }

            await Task.WhenAll(tasks);
            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Assert - Verify operations completed in reasonable time
            totalTime.Should().BeLessThan(TimeSpan.FromSeconds(30)); // Should complete within 30 seconds
            
            // Verify data integrity
            var summaries = await _conversationPersistenceService.GetConversationSummariesAsync();
            summaries.Should().HaveCount(operationCount);
            
            var keys = await _applicationStateService.GetAllKeysAsync();
            keys.Should().Contain(k => k.StartsWith("Operation_"));
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockSettingsStore()
        {
            var settingsStorage = new Dictionary<string, object>();

            _mockSettingsStore.Setup(x => x.CollectionExists(It.IsAny<string>())).Returns(true);
            _mockSettingsStore.Setup(x => x.PropertyExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => settingsStorage.ContainsKey(key));

            _mockSettingsStore.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => 
                    settingsStorage.ContainsKey(key) ? settingsStorage[key]?.ToString() : string.Empty);

            _mockSettingsStore.Setup(x => x.SetString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((collection, key, value) => settingsStorage[key] = value);

            _mockSettingsStore.Setup(x => x.CreateCollection(It.IsAny<string>()));
        }

        private void SetupMockErrorHandler()
        {
            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private Conversation CreateTestConversation(string id, string title, int messageCount)
        {
            var conversation = new Conversation
            {
                Id = id,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            for (int i = 0; i < messageCount; i++)
            {
                conversation.Messages.Add(new ChatMessage
                {
                    Content = $"Test message {i + 1}",
                    Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
                    Timestamp = DateTime.UtcNow.AddMinutes(-messageCount + i)
                });
            }

            return conversation;
        }

        #endregion
    }
}