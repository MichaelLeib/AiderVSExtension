using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.EndToEnd
{
    /// <summary>
    /// End-to-end integration tests that simulate complete user workflows
    /// </summary>
    public class EndToEndIntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly Mock<WritableSettingsStore> _mockSettingsStore;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly ServiceContainer _serviceContainer;
        private readonly ConfigurationService _configurationService;
        private readonly ApplicationStateService _applicationStateService;
        private readonly ConversationPersistenceService _conversationPersistenceService;
        private readonly AIModelManager _aiModelManager;
        private readonly FileContextService _fileContextService;

        public EndToEndIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "EndToEndTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // Setup mocks
            _mockSettingsStore = new Mock<WritableSettingsStore>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            SetupMocks();

            // Initialize service container
            _serviceContainer = new ServiceContainer();
            
            // Register services
            _configurationService = new ConfigurationService(_mockSettingsStore.Object);
            _applicationStateService = new ApplicationStateService(Path.Combine(_tempDirectory, "state.json"));
            _conversationPersistenceService = new ConversationPersistenceService(_tempDirectory);
            _aiModelManager = new AIModelManager(_configurationService, _mockErrorHandler.Object);
            _fileContextService = new FileContextService(_mockErrorHandler.Object);

            _serviceContainer.RegisterInstance<IConfigurationService>(_configurationService);
            _serviceContainer.RegisterInstance<IApplicationStateService>(_applicationStateService);
            _serviceContainer.RegisterInstance<IConversationPersistenceService>(_conversationPersistenceService);
            _serviceContainer.RegisterInstance<IAIModelManager>(_aiModelManager);
            _serviceContainer.RegisterInstance<IFileContextService>(_fileContextService);
            _serviceContainer.RegisterInstance<IErrorHandler>(_mockErrorHandler.Object);
        }

        public void Dispose()
        {
            _configurationService?.Dispose();
            _applicationStateService?.Dispose();
            _conversationPersistenceService?.Dispose();
            _aiModelManager?.Dispose();
            _serviceContainer?.Dispose();
            
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Complete User Workflow Tests

        [Fact]
        public async Task CompleteUserWorkflow_FirstTimeSetup_WorksEndToEnd()
        {
            // Arrange - Simulate first time user setup
            await _applicationStateService.InitializeAsync();

            // Act - Complete first-time setup workflow
            
            // 1. User configures AI model
            var aiConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "sk-test-api-key-for-integration-testing",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 3,
                IsEnabled = true
            };

            await _configurationService.SetAIModelConfigurationAsync(aiConfig);
            
            // 2. Validate configuration
            var validationResult = await _configurationService.ValidateConfigurationAsync();
            
            // 3. Test AI model connection
            var connectionResult = await _aiModelManager.TestConnectionAsync(aiConfig.Provider);
            
            // 4. Initialize AI model
            await _aiModelManager.InitializeAsync();
            
            // 5. Create first conversation
            var conversation = new Conversation
            {
                Id = "first-conversation",
                Title = "Welcome to Aider",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Hello, I'm setting up Aider for the first time!",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow,
                        ModelUsed = aiConfig.ModelName
                    }
                }
            };

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            
            // 6. Update application state to reflect setup completion
            await _applicationStateService.SetStateAsync("IsFirstTimeSetup", false);
            await _applicationStateService.SetStateAsync("SetupCompletedAt", DateTime.UtcNow);
            await _applicationStateService.SetStateAsync("ActiveModelProvider", aiConfig.Provider.ToString());

            // Assert - Verify complete workflow success
            var savedConfig = _configurationService.GetAIModelConfiguration();
            var isSetupComplete = !(await _applicationStateService.GetStateAsync<bool>("IsFirstTimeSetup"));
            var setupTime = await _applicationStateService.GetStateAsync<DateTime>("SetupCompletedAt");
            var activeProvider = await _applicationStateService.GetStateAsync<string>("ActiveModelProvider");
            var savedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);

            savedConfig.Should().NotBeNull();
            savedConfig.Provider.Should().Be(AIProvider.ChatGPT);
            savedConfig.IsEnabled.Should().BeTrue();
            
            validationResult.IsValid.Should().BeTrue();
            isSetupComplete.Should().BeTrue();
            setupTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            activeProvider.Should().Be("ChatGPT");
            
            savedConversation.Should().NotBeNull();
            savedConversation.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task CompleteUserWorkflow_ModelSwitching_MaintainsConversationHistory()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();

            var chatGptConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "gpt-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            var claudeConfig = new AIModelConfiguration
            {
                Provider = AIProvider.Claude,
                ApiKey = "claude-key", 
                ModelName = "claude-3-opus",
                IsEnabled = true
            };

            var conversation = new Conversation
            {
                Id = "model-switching-test",
                Title = "Model Switching Test",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act - Simulate user switching between models during conversation
            
            // 1. Start with ChatGPT
            await _configurationService.SetAIModelConfigurationAsync(chatGptConfig);
            await _aiModelManager.InitializeAsync();
            
            conversation.AddMessage(new ChatMessage
            {
                Content = "Question for GPT-4",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                ModelUsed = chatGptConfig.ModelName
            });
            
            conversation.AddMessage(new ChatMessage
            {
                Content = "Response from GPT-4",
                Type = MessageType.Assistant,
                Timestamp = DateTime.UtcNow.AddSeconds(5),
                ModelUsed = chatGptConfig.ModelName
            });

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            await _applicationStateService.SetStateAsync("LastUsedModel", chatGptConfig.ModelName);

            // 2. Switch to Claude
            await _configurationService.SetAIModelConfigurationAsync(claudeConfig);
            await _aiModelManager.InitializeAsync();
            
            conversation.AddMessage(new ChatMessage
            {
                Content = "Question for Claude",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                ModelUsed = claudeConfig.ModelName
            });
            
            conversation.AddMessage(new ChatMessage
            {
                Content = "Response from Claude",
                Type = MessageType.Assistant,
                Timestamp = DateTime.UtcNow.AddMinutes(1).AddSeconds(5),
                ModelUsed = claudeConfig.ModelName
            });

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            await _applicationStateService.SetStateAsync("LastUsedModel", claudeConfig.ModelName);
            await _applicationStateService.SetStateAsync("ModelSwitchCount", 1);

            // Assert - Verify model switching preserved conversation history
            var finalConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
            var lastUsedModel = await _applicationStateService.GetStateAsync<string>("LastUsedModel");
            var switchCount = await _applicationStateService.GetStateAsync<int>("ModelSwitchCount");
            var currentConfig = _configurationService.GetAIModelConfiguration();

            finalConversation.Messages.Should().HaveCount(4);
            
            // Verify model usage in conversation history
            finalConversation.Messages.Take(2).All(m => m.ModelUsed == "gpt-4").Should().BeTrue();
            finalConversation.Messages.Skip(2).All(m => m.ModelUsed == "claude-3-opus").Should().BeTrue();
            
            lastUsedModel.Should().Be("claude-3-opus");
            switchCount.Should().Be(1);
            currentConfig.Provider.Should().Be(AIProvider.Claude);
        }

        [Fact]
        public async Task CompleteUserWorkflow_FileContextIntegration_WorksWithConversations()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            
            // Create test files
            var testFile1 = Path.Combine(_tempDirectory, "TestFile1.cs");
            var testFile2 = Path.Combine(_tempDirectory, "TestFile2.cs");
            
            await File.WriteAllTextAsync(testFile1, @"
using System;

namespace TestProject
{
    public class TestClass1
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello from TestClass1"");
        }
    }
}");

            await File.WriteAllTextAsync(testFile2, @"
using System;

namespace TestProject
{
    public class TestClass2
    {
        public void AnotherMethod()
        {
            Console.WriteLine(""Hello from TestClass2"");
        }
    }
}");

            var aiConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            // Act - Complete workflow with file context
            
            // 1. Setup AI configuration
            await _configurationService.SetAIModelConfigurationAsync(aiConfig);
            
            // 2. Extract file context
            var fileContext1 = await _fileContextService.GetFileContextAsync(testFile1);
            var fileContext2 = await _fileContextService.GetFileContextAsync(testFile2);
            
            // 3. Create conversation with file references
            var conversation = new Conversation
            {
                Id = "file-context-test",
                Title = "File Context Integration Test",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Please analyze these files and suggest improvements",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow,
                        ModelUsed = aiConfig.ModelName,
                        FileReferences = new List<FileReference>
                        {
                            new FileReference
                            {
                                FilePath = testFile1,
                                Content = fileContext1.Content,
                                StartLine = 1,
                                EndLine = fileContext1.Content.Split('\n').Length,
                                Type = ReferenceType.FullFile
                            },
                            new FileReference
                            {
                                FilePath = testFile2,
                                Content = fileContext2.Content,
                                StartLine = 1,
                                EndLine = fileContext2.Content.Split('\n').Length,
                                Type = ReferenceType.FullFile
                            }
                        }
                    }
                }
            };

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            
            // 4. Update state with file context statistics
            await _applicationStateService.SetStateAsync("FilesAnalyzed", 2);
            await _applicationStateService.SetStateAsync("LastFileAnalysis", DateTime.UtcNow);
            await _applicationStateService.SetStateAsync("TotalLinesAnalyzed", 
                fileContext1.Content.Split('\n').Length + fileContext2.Content.Split('\n').Length);

            // Assert - Verify file context integration
            var savedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
            var filesAnalyzed = await _applicationStateService.GetStateAsync<int>("FilesAnalyzed");
            var totalLines = await _applicationStateService.GetStateAsync<int>("TotalLinesAnalyzed");
            var lastAnalysis = await _applicationStateService.GetStateAsync<DateTime>("LastFileAnalysis");

            savedConversation.Should().NotBeNull();
            savedConversation.Messages.First().FileReferences.Should().HaveCount(2);
            savedConversation.Messages.First().FileReferences.Should().Contain(fr => fr.FilePath == testFile1);
            savedConversation.Messages.First().FileReferences.Should().Contain(fr => fr.FilePath == testFile2);
            
            filesAnalyzed.Should().Be(2);
            totalLines.Should().BeGreaterThan(0);
            lastAnalysis.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            
            fileContext1.Content.Should().Contain("TestClass1");
            fileContext2.Content.Should().Contain("TestClass2");
        }

        [Fact]
        public async Task CompleteUserWorkflow_ErrorRecovery_MaintainsDataIntegrity()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();

            var validConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "valid-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            var invalidConfig = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "", // Invalid
                ModelName = "gpt-4",
                IsEnabled = true
            };

            // Act - Simulate error scenario and recovery
            
            // 1. Start with valid configuration
            await _configurationService.SetAIModelConfigurationAsync(validConfig);
            var initialValidation = await _configurationService.ValidateConfigurationAsync();
            
            // 2. Create initial conversation
            var conversation = new Conversation
            {
                Id = "error-recovery-test",
                Title = "Error Recovery Test",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Initial message with valid config",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow,
                        ModelUsed = validConfig.ModelName
                    }
                }
            };

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            await _applicationStateService.SetStateAsync("LastSuccessfulOperation", DateTime.UtcNow);

            // 3. Introduce error with invalid configuration
            await _configurationService.SetAIModelConfigurationAsync(invalidConfig);
            var errorValidation = await _configurationService.ValidateConfigurationAsync();
            
            await _applicationStateService.SetStateAsync("LastError", "Invalid API key");
            await _applicationStateService.SetStateAsync("ErrorOccurredAt", DateTime.UtcNow);

            // 4. Recover with valid configuration
            await _configurationService.SetAIModelConfigurationAsync(validConfig);
            var recoveryValidation = await _configurationService.ValidateConfigurationAsync();
            
            // 5. Continue conversation after recovery
            conversation.AddMessage(new ChatMessage
            {
                Content = "Message after error recovery",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow.AddMinutes(1),
                ModelUsed = validConfig.ModelName
            });

            await _conversationPersistenceService.SaveConversationAsync(conversation);
            await _applicationStateService.SetStateAsync("RecoveredAt", DateTime.UtcNow);
            await _applicationStateService.ClearStateAsync("LastError");

            // Assert - Verify error recovery maintained data integrity
            var finalConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
            var finalConfig = _configurationService.GetAIModelConfiguration();
            var lastError = await _applicationStateService.GetStateAsync<string>("LastError");
            var recoveredAt = await _applicationStateService.GetStateAsync<DateTime>("RecoveredAt");
            var lastSuccessful = await _applicationStateService.GetStateAsync<DateTime>("LastSuccessfulOperation");

            // Configuration should be valid after recovery
            initialValidation.IsValid.Should().BeTrue();
            errorValidation.IsValid.Should().BeFalse();
            recoveryValidation.IsValid.Should().BeTrue();
            finalConfig.ApiKey.Should().Be("valid-key");

            // Conversation should maintain integrity
            finalConversation.Messages.Should().HaveCount(2);
            finalConversation.Messages.All(m => m.ModelUsed == "gpt-4").Should().BeTrue();

            // State should reflect recovery
            lastError.Should().BeNullOrEmpty();
            recoveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            lastSuccessful.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Multi-Service Integration Tests

        [Fact]
        public async Task MultiServiceIntegration_ConcurrentOperations_MaintainConsistency()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            const int concurrentOperations = 50;
            var tasks = new List<Task>();

            // Act - Perform concurrent operations across multiple services
            for (int i = 0; i < concurrentOperations; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Configuration operations
                    var config = new AIModelConfiguration
                    {
                        Provider = (AIProvider)(index % 3), // Rotate between providers
                        ApiKey = $"test-key-{index}",
                        ModelName = $"model-{index}",
                        IsEnabled = true
                    };
                    
                    await _configurationService.SetAIModelConfigurationAsync(config);
                    
                    // State operations
                    await _applicationStateService.SetStateAsync($"Operation_{index}", DateTime.UtcNow);
                    await _applicationStateService.SetStateAsync($"Config_{index}", config.ModelName);
                    
                    // Conversation operations
                    var conversation = new Conversation
                    {
                        Id = $"concurrent-test-{index}",
                        Title = $"Concurrent Test {index}",
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage
                            {
                                Content = $"Concurrent message {index}",
                                Type = MessageType.User,
                                Timestamp = DateTime.UtcNow,
                                ModelUsed = config.ModelName
                            }
                        }
                    };
                    
                    await _conversationPersistenceService.SaveConversationAsync(conversation);
                    
                    // Validation
                    var validation = await _configurationService.ValidateConfigurationAsync();
                    await _applicationStateService.SetStateAsync($"Valid_{index}", validation.IsValid);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Verify all operations completed successfully and consistently
            var allConversations = await _conversationPersistenceService.GetConversationSummariesAsync();
            var allKeys = await _applicationStateService.GetAllKeysAsync();
            
            allConversations.Should().HaveCount(concurrentOperations);
            allKeys.Where(k => k.StartsWith("Operation_")).Should().HaveCount(concurrentOperations);
            allKeys.Where(k => k.StartsWith("Config_")).Should().HaveCount(concurrentOperations);
            allKeys.Where(k => k.StartsWith("Valid_")).Should().HaveCount(concurrentOperations);

            // Verify no data corruption
            for (int i = 0; i < concurrentOperations; i++)
            {
                var conversation = await _conversationPersistenceService.LoadConversationAsync($"concurrent-test-{i}");
                var configName = await _applicationStateService.GetStateAsync<string>($"Config_{i}");
                var isValid = await _applicationStateService.GetStateAsync<bool>($"Valid_{i}");

                conversation.Should().NotBeNull();
                conversation.Messages.Should().HaveCount(1);
                conversation.Messages.First().Content.Should().Be($"Concurrent message {i}");
                configName.Should().Be($"model-{i}");
                isValid.Should().BeTrue();
            }
        }

        #endregion

        #region Performance End-to-End Tests

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task EndToEndPerformance_ScalingOperations_MaintainsPerformance(int operationCount)
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            var startTime = DateTime.UtcNow;

            // Act - Perform scaled operations
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "performance-test-key",
                ModelName = "gpt-4",
                IsEnabled = true
            };

            await _configurationService.SetAIModelConfigurationAsync(config);

            var conversations = new List<Conversation>();
            for (int i = 0; i < operationCount; i++)
            {
                var conversation = new Conversation
                {
                    Id = $"perf-test-{i}",
                    Title = $"Performance Test {i}",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Messages = Enumerable.Range(0, 5).Select(j => new ChatMessage
                    {
                        Content = $"Message {j} in conversation {i}",
                        Type = j % 2 == 0 ? MessageType.User : MessageType.Assistant,
                        Timestamp = DateTime.UtcNow.AddSeconds(j),
                        ModelUsed = config.ModelName
                    }).ToList()
                };

                conversations.Add(conversation);
                await _conversationPersistenceService.SaveConversationAsync(conversation);
                await _applicationStateService.SetStateAsync($"Conversation_{i}_Created", DateTime.UtcNow);
            }

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Assert - Performance requirements
            var averageTimePerOperation = totalTime.TotalMilliseconds / operationCount;
            averageTimePerOperation.Should().BeLessThan(100); // Less than 100ms per operation on average

            // Verify data integrity wasn't compromised for performance
            var summaries = await _conversationPersistenceService.GetConversationSummariesAsync();
            summaries.Should().HaveCount(operationCount);
            
            var states = await _applicationStateService.GetAllKeysAsync();
            states.Where(k => k.StartsWith("Conversation_") && k.EndsWith("_Created")).Should().HaveCount(operationCount);

            // Verify memory usage is reasonable (this is a basic check)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memoryBefore = GC.GetTotalMemory(false);
            memoryBefore.Should().BeLessThan(100 * 1024 * 1024); // Less than 100MB for test operations
        }

        #endregion

        #region Private Helper Methods

        private void SetupMocks()
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

            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        #endregion
    }
}