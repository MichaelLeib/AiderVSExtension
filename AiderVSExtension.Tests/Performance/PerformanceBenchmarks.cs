using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace AiderVSExtension.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks for critical system components
    /// </summary>
    public class PerformanceBenchmarks : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDirectory;
        private readonly Mock<WritableSettingsStore> _mockSettingsStore;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly ConfigurationService _configurationService;
        private readonly ApplicationStateService _applicationStateService;
        private readonly ConversationPersistenceService _conversationPersistenceService;
        private readonly FileContextService _fileContextService;

        public PerformanceBenchmarks(ITestOutputHelper output)
        {
            _output = output;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PerformanceBenchmarks", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _mockSettingsStore = new Mock<WritableSettingsStore>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            SetupMocks();

            _configurationService = new ConfigurationService(_mockSettingsStore.Object);
            _applicationStateService = new ApplicationStateService(Path.Combine(_tempDirectory, "state.json"));
            _conversationPersistenceService = new ConversationPersistenceService(_tempDirectory);
            _fileContextService = new FileContextService(_mockErrorHandler.Object);
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

        #region Configuration Service Benchmarks

        [Fact]
        public async Task Benchmark_ConfigurationService_GetSetOperations()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "benchmark-test-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 3,
                IsEnabled = true
            };

            const int iterations = 1000;
            var stopwatch = Stopwatch.StartNew();

            // Act - Benchmark Set operations
            var setTimes = new List<long>();
            for (int i = 0; i < iterations; i++)
            {
                var iterationStopwatch = Stopwatch.StartNew();
                config.ApiKey = $"benchmark-test-key-{i}";
                await _configurationService.SetAIModelConfigurationAsync(config);
                iterationStopwatch.Stop();
                setTimes.Add(iterationStopwatch.ElapsedTicks);
            }

            // Benchmark Get operations
            var getTimes = new List<long>();
            for (int i = 0; i < iterations; i++)
            {
                var iterationStopwatch = Stopwatch.StartNew();
                var retrievedConfig = _configurationService.GetAIModelConfiguration();
                iterationStopwatch.Stop();
                getTimes.Add(iterationStopwatch.ElapsedTicks);
            }

            stopwatch.Stop();

            // Assert and Report
            var avgSetTime = setTimes.Average() / TimeSpan.TicksPerMillisecond;
            var avgGetTime = getTimes.Average() / TimeSpan.TicksPerMillisecond;
            var totalTime = stopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Configuration Service Benchmark Results:");
            _output.WriteLine($"  Total Time: {totalTime}ms for {iterations * 2} operations");
            _output.WriteLine($"  Average Set Time: {avgSetTime:F3}ms");
            _output.WriteLine($"  Average Get Time: {avgGetTime:F3}ms");
            _output.WriteLine($"  Set Operations/sec: {1000 / avgSetTime:F0}");
            _output.WriteLine($"  Get Operations/sec: {1000 / avgGetTime:F0}");

            // Performance assertions
            avgSetTime.Should().BeLessThan(5.0, "Set operations should be fast");
            avgGetTime.Should().BeLessThan(1.0, "Get operations should be very fast");
            totalTime.Should().BeLessThan(10000, "Total benchmark should complete quickly");
        }

        [Fact]
        public async Task Benchmark_ConfigurationService_ValidationPerformance()
        {
            // Arrange
            var configs = new[]
            {
                new AIModelConfiguration { Provider = AIProvider.ChatGPT, ApiKey = "valid-key", ModelName = "gpt-4", IsEnabled = true },
                new AIModelConfiguration { Provider = AIProvider.Claude, ApiKey = "valid-key", ModelName = "claude-3", IsEnabled = true },
                new AIModelConfiguration { Provider = AIProvider.Ollama, EndpointUrl = "http://localhost:11434", ModelName = "llama2", IsEnabled = true },
                new AIModelConfiguration { Provider = AIProvider.ChatGPT, ApiKey = "", ModelName = "gpt-4", IsEnabled = true }, // Invalid
                new AIModelConfiguration { Provider = AIProvider.Claude, ApiKey = "key", ModelName = "", IsEnabled = true } // Invalid
            };

            const int iterations = 500;
            var validationTimes = new List<long>();

            // Act
            foreach (var config in configs)
            {
                await _configurationService.SetAIModelConfigurationAsync(config);
                
                for (int i = 0; i < iterations; i++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var result = await _configurationService.ValidateConfigurationAsync();
                    stopwatch.Stop();
                    validationTimes.Add(stopwatch.ElapsedTicks);
                }
            }

            // Assert and Report
            var avgValidationTime = validationTimes.Average() / TimeSpan.TicksPerMillisecond;
            var maxValidationTime = validationTimes.Max() / TimeSpan.TicksPerMillisecond;
            var minValidationTime = validationTimes.Min() / TimeSpan.TicksPerMillisecond;

            _output.WriteLine($"Configuration Validation Benchmark Results:");
            _output.WriteLine($"  Average Validation Time: {avgValidationTime:F3}ms");
            _output.WriteLine($"  Min Validation Time: {minValidationTime:F3}ms");
            _output.WriteLine($"  Max Validation Time: {maxValidationTime:F3}ms");
            _output.WriteLine($"  Validations/sec: {1000 / avgValidationTime:F0}");

            avgValidationTime.Should().BeLessThan(10.0, "Validation should be fast");
            maxValidationTime.Should().BeLessThan(50.0, "No validation should be extremely slow");
        }

        #endregion

        #region Application State Service Benchmarks

        [Fact]
        public async Task Benchmark_ApplicationStateService_StateOperations()
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            const int iterations = 1000;
            var setTimes = new List<long>();
            var getTimes = new List<long>();

            // Act - Benchmark Set operations
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                await _applicationStateService.SetStateAsync($"BenchmarkKey_{i}", $"BenchmarkValue_{i}");
                stopwatch.Stop();
                setTimes.Add(stopwatch.ElapsedTicks);
            }

            // Benchmark Get operations
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var value = await _applicationStateService.GetStateAsync<string>($"BenchmarkKey_{i}");
                stopwatch.Stop();
                getTimes.Add(stopwatch.ElapsedTicks);
            }

            // Benchmark GetAllKeys operation
            var getAllKeysStopwatch = Stopwatch.StartNew();
            var allKeys = await _applicationStateService.GetAllKeysAsync();
            getAllKeysStopwatch.Stop();

            // Assert and Report
            var avgSetTime = setTimes.Average() / TimeSpan.TicksPerMillisecond;
            var avgGetTime = getTimes.Average() / TimeSpan.TicksPerMillisecond;
            var getAllKeysTime = getAllKeysStopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Application State Service Benchmark Results:");
            _output.WriteLine($"  Average Set Time: {avgSetTime:F3}ms");
            _output.WriteLine($"  Average Get Time: {avgGetTime:F3}ms");
            _output.WriteLine($"  GetAllKeys Time: {getAllKeysTime}ms for {allKeys.Count()} keys");
            _output.WriteLine($"  Set Operations/sec: {1000 / avgSetTime:F0}");
            _output.WriteLine($"  Get Operations/sec: {1000 / avgGetTime:F0}");

            avgSetTime.Should().BeLessThan(5.0, "State set operations should be fast");
            avgGetTime.Should().BeLessThan(2.0, "State get operations should be fast");
            getAllKeysTime.Should().BeLessThan(100, "GetAllKeys should be fast even with many keys");
            allKeys.Should().HaveCount(iterations);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task Benchmark_ApplicationStateService_ConcurrentAccess(int concurrencyLevel)
        {
            // Arrange
            await _applicationStateService.InitializeAsync();
            var tasks = new List<Task>();
            var operationTimes = new List<long>();
            var lockObject = new object();

            // Act - Concurrent operations
            var globalStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < concurrencyLevel; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    await _applicationStateService.SetStateAsync($"ConcurrentKey_{index}", $"ConcurrentValue_{index}");
                    var retrievedValue = await _applicationStateService.GetStateAsync<string>($"ConcurrentKey_{index}");
                    
                    stopwatch.Stop();
                    
                    lock (lockObject)
                    {
                        operationTimes.Add(stopwatch.ElapsedTicks);
                    }
                    
                    retrievedValue.Should().Be($"ConcurrentValue_{index}");
                }));
            }

            await Task.WhenAll(tasks);
            globalStopwatch.Stop();

            // Assert and Report
            var avgOperationTime = operationTimes.Average() / TimeSpan.TicksPerMillisecond;
            var totalTime = globalStopwatch.ElapsedMilliseconds;
            var operationsPerSecond = (concurrencyLevel * 1000.0) / totalTime;

            _output.WriteLine($"Concurrent Access Benchmark Results (Concurrency: {concurrencyLevel}):");
            _output.WriteLine($"  Total Time: {totalTime}ms");
            _output.WriteLine($"  Average Operation Time: {avgOperationTime:F3}ms");
            _output.WriteLine($"  Operations/sec: {operationsPerSecond:F0}");
            _output.WriteLine($"  Throughput: {(concurrencyLevel * 2 * 1000.0) / totalTime:F0} operations/sec");

            avgOperationTime.Should().BeLessThan(50.0, "Concurrent operations should not be significantly slower");
            totalTime.Should().BeLessThan(10000, "Concurrent operations should complete in reasonable time");
        }

        #endregion

        #region Conversation Persistence Service Benchmarks

        [Fact]
        public async Task Benchmark_ConversationPersistence_SaveLoadOperations()
        {
            // Arrange
            const int conversationCount = 100;
            const int messagesPerConversation = 10;
            var conversations = new List<Conversation>();
            
            for (int i = 0; i < conversationCount; i++)
            {
                var conversation = new Conversation
                {
                    Id = $"benchmark-conversation-{i}",
                    Title = $"Benchmark Conversation {i}",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Messages = Enumerable.Range(0, messagesPerConversation).Select(j => new ChatMessage
                    {
                        Content = $"Message {j} in conversation {i} - Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                        Type = j % 2 == 0 ? MessageType.User : MessageType.Assistant,
                        Timestamp = DateTime.UtcNow.AddMinutes(-messagesPerConversation + j),
                        ModelUsed = "gpt-4"
                    }).ToList()
                };
                conversations.Add(conversation);
            }

            var saveTimes = new List<long>();
            var loadTimes = new List<long>();

            // Act - Benchmark Save operations
            foreach (var conversation in conversations)
            {
                var stopwatch = Stopwatch.StartNew();
                await _conversationPersistenceService.SaveConversationAsync(conversation);
                stopwatch.Stop();
                saveTimes.Add(stopwatch.ElapsedTicks);
            }

            // Benchmark Load operations
            foreach (var conversation in conversations)
            {
                var stopwatch = Stopwatch.StartNew();
                var loadedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
                stopwatch.Stop();
                loadTimes.Add(stopwatch.ElapsedTicks);
                loadedConversation.Should().NotBeNull();
            }

            // Benchmark GetConversationSummaries operation
            var summariesStopwatch = Stopwatch.StartNew();
            var summaries = await _conversationPersistenceService.GetConversationSummariesAsync();
            summariesStopwatch.Stop();

            // Assert and Report
            var avgSaveTime = saveTimes.Average() / TimeSpan.TicksPerMillisecond;
            var avgLoadTime = loadTimes.Average() / TimeSpan.TicksPerMillisecond;
            var summariesTime = summariesStopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Conversation Persistence Benchmark Results:");
            _output.WriteLine($"  Average Save Time: {avgSaveTime:F3}ms per conversation ({messagesPerConversation} messages)");
            _output.WriteLine($"  Average Load Time: {avgLoadTime:F3}ms per conversation");
            _output.WriteLine($"  Get Summaries Time: {summariesTime}ms for {conversationCount} conversations");
            _output.WriteLine($"  Save Operations/sec: {1000 / avgSaveTime:F0}");
            _output.WriteLine($"  Load Operations/sec: {1000 / avgLoadTime:F0}");

            avgSaveTime.Should().BeLessThan(50.0, "Save operations should be reasonably fast");
            avgLoadTime.Should().BeLessThan(30.0, "Load operations should be fast");
            summariesTime.Should().BeLessThan(500, "Getting summaries should be fast");
            summaries.Should().HaveCount(conversationCount);
        }

        [Fact]
        public async Task Benchmark_ConversationPersistence_LargeConversations()
        {
            // Arrange - Create conversations of varying sizes
            var conversationSizes = new[] { 10, 50, 100, 500, 1000 };
            var results = new Dictionary<int, (double saveTime, double loadTime)>();

            foreach (var size in conversationSizes)
            {
                var conversation = new Conversation
                {
                    Id = $"large-conversation-{size}",
                    Title = $"Large Conversation with {size} messages",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Messages = Enumerable.Range(0, size).Select(i => new ChatMessage
                    {
                        Content = $"Message {i} - " + new string('x', 100), // 100-character content
                        Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
                        Timestamp = DateTime.UtcNow.AddMinutes(-size + i),
                        ModelUsed = "gpt-4"
                    }).ToList()
                };

                // Benchmark save
                var saveStopwatch = Stopwatch.StartNew();
                await _conversationPersistenceService.SaveConversationAsync(conversation);
                saveStopwatch.Stop();
                var saveTime = saveStopwatch.Elapsed.TotalMilliseconds;

                // Benchmark load
                var loadStopwatch = Stopwatch.StartNew();
                var loadedConversation = await _conversationPersistenceService.LoadConversationAsync(conversation.Id);
                loadStopwatch.Stop();
                var loadTime = loadStopwatch.Elapsed.TotalMilliseconds;

                results[size] = (saveTime, loadTime);
                loadedConversation.Messages.Should().HaveCount(size);
            }

            // Assert and Report
            _output.WriteLine($"Large Conversation Performance Results:");
            foreach (var kvp in results)
            {
                var size = kvp.Key;
                var (saveTime, loadTime) = kvp.Value;
                _output.WriteLine($"  {size} messages: Save={saveTime:F1}ms, Load={loadTime:F1}ms");
            }

            // Performance should scale reasonably
            results[10].saveTime.Should().BeLessThan(50);
            results[100].saveTime.Should().BeLessThan(500);
            results[1000].saveTime.Should().BeLessThan(5000);
            
            results[10].loadTime.Should().BeLessThan(30);
            results[100].loadTime.Should().BeLessThan(300);
            results[1000].loadTime.Should().BeLessThan(3000);
        }

        #endregion

        #region File Context Service Benchmarks

        [Fact]
        public async Task Benchmark_FileContextService_FileProcessing()
        {
            // Arrange - Create test files of various sizes
            var fileSizes = new[] { 1, 10, 50, 100 }; // KB
            var testFiles = new List<string>();
            var processingTimes = new Dictionary<int, double>();

            foreach (var sizeKb in fileSizes)
            {
                var filePath = Path.Combine(_tempDirectory, $"test-file-{sizeKb}kb.cs");
                var content = GenerateTestFileContent(sizeKb * 1024);
                await File.WriteAllTextAsync(filePath, content);
                testFiles.Add(filePath);
            }

            // Act - Benchmark file processing
            for (int i = 0; i < fileSizes.Length; i++)
            {
                var sizeKb = fileSizes[i];
                var filePath = testFiles[i];
                
                const int iterations = 10;
                var times = new List<long>();

                for (int j = 0; j < iterations; j++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var context = await _fileContextService.GetFileContextAsync(filePath);
                    stopwatch.Stop();
                    times.Add(stopwatch.ElapsedTicks);
                    
                    context.Should().NotBeNull();
                    context.Content.Should().NotBeEmpty();
                }

                var avgTime = times.Average() / TimeSpan.TicksPerMillisecond;
                processingTimes[sizeKb] = avgTime;
            }

            // Assert and Report
            _output.WriteLine($"File Context Processing Benchmark Results:");
            foreach (var kvp in processingTimes)
            {
                var sizeKb = kvp.Key;
                var avgTime = kvp.Value;
                var throughput = sizeKb / avgTime * 1000; // KB/s
                _output.WriteLine($"  {sizeKb}KB file: {avgTime:F3}ms (Throughput: {throughput:F1} KB/s)");
            }

            // Performance assertions
            processingTimes[1].Should().BeLessThan(10, "Small files should process quickly");
            processingTimes[10].Should().BeLessThan(50, "Medium files should process reasonably");
            processingTimes[100].Should().BeLessThan(500, "Large files should still process in reasonable time");

            // Clean up
            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }

        [Fact]
        public async Task Benchmark_FileContextService_ConcurrentFileProcessing()
        {
            // Arrange
            const int fileCount = 20;
            const int fileSizeKb = 10;
            var testFiles = new List<string>();

            for (int i = 0; i < fileCount; i++)
            {
                var filePath = Path.Combine(_tempDirectory, $"concurrent-test-{i}.cs");
                var content = GenerateTestFileContent(fileSizeKb * 1024);
                await File.WriteAllTextAsync(filePath, content);
                testFiles.Add(filePath);
            }

            // Act - Concurrent processing
            var globalStopwatch = Stopwatch.StartNew();
            var tasks = testFiles.Select(async filePath =>
            {
                var stopwatch = Stopwatch.StartNew();
                var context = await _fileContextService.GetFileContextAsync(filePath);
                stopwatch.Stop();
                return new { FilePath = filePath, Time = stopwatch.ElapsedMilliseconds, Context = context };
            });

            var results = await Task.WhenAll(tasks);
            globalStopwatch.Stop();

            // Assert and Report
            var avgProcessingTime = results.Average(r => r.Time);
            var totalTime = globalStopwatch.ElapsedMilliseconds;
            var throughput = (fileCount * fileSizeKb * 1000.0) / totalTime; // KB/s

            _output.WriteLine($"Concurrent File Processing Benchmark Results:");
            _output.WriteLine($"  Files Processed: {fileCount}");
            _output.WriteLine($"  Total Time: {totalTime}ms");
            _output.WriteLine($"  Average Processing Time: {avgProcessingTime:F1}ms per file");
            _output.WriteLine($"  Throughput: {throughput:F1} KB/s");
            _output.WriteLine($"  Concurrency Efficiency: {(fileCount * avgProcessingTime / totalTime):F2}x");

            results.Should().AllSatisfy(r => r.Context.Should().NotBeNull());
            avgProcessingTime.Should().BeLessThan(100, "Average processing time should be reasonable");
            totalTime.Should().BeLessThan(5000, "Total concurrent processing should be fast");

            // Clean up
            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }

        #endregion

        #region Memory Usage Benchmarks

        [Fact]
        public async Task Benchmark_MemoryUsage_ServiceLifecycle()
        {
            // Arrange
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Create and use services
            const int iterations = 100;
            for (int i = 0; i < iterations; i++)
            {
                var config = new AIModelConfiguration
                {
                    Provider = AIProvider.ChatGPT,
                    ApiKey = $"memory-test-{i}",
                    ModelName = "gpt-4",
                    IsEnabled = true
                };

                await _configurationService.SetAIModelConfigurationAsync(config);
                await _applicationStateService.SetStateAsync($"MemoryTest_{i}", DateTime.UtcNow);
                
                var conversation = new Conversation
                {
                    Id = $"memory-test-{i}",
                    Title = $"Memory Test {i}",
                    CreatedAt = DateTime.UtcNow,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage
                        {
                            Content = $"Memory test message {i}",
                            Type = MessageType.User,
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };

                await _conversationPersistenceService.SaveConversationAsync(conversation);
            }

            var afterOperationsMemory = GC.GetTotalMemory(false);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterGCMemory = GC.GetTotalMemory(false);

            // Assert and Report
            var memoryUsedMB = (afterOperationsMemory - initialMemory) / (1024.0 * 1024.0);
            var memoryAfterGCMB = (afterGCMemory - initialMemory) / (1024.0 * 1024.0);
            var memoryPerOperationKB = (afterGCMemory - initialMemory) / (1024.0 * iterations);

            _output.WriteLine($"Memory Usage Benchmark Results:");
            _output.WriteLine($"  Initial Memory: {initialMemory / (1024.0 * 1024.0):F2} MB");
            _output.WriteLine($"  After Operations: {afterOperationsMemory / (1024.0 * 1024.0):F2} MB");
            _output.WriteLine($"  After GC: {afterGCMemory / (1024.0 * 1024.0):F2} MB");
            _output.WriteLine($"  Memory Used: {memoryUsedMB:F2} MB");
            _output.WriteLine($"  Memory After GC: {memoryAfterGCMB:F2} MB");
            _output.WriteLine($"  Memory Per Operation: {memoryPerOperationKB:F2} KB");

            memoryUsedMB.Should().BeLessThan(50, "Memory usage should be reasonable");
            memoryAfterGCMB.Should().BeLessThan(10, "Memory should be freed after GC");
            memoryPerOperationKB.Should().BeLessThan(100, "Memory per operation should be minimal");
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

        private string GenerateTestFileContent(int targetSizeBytes)
        {
            var baseContent = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class TestClass
    {
        public async Task<string> TestMethod(int parameter)
        {
            var result = ""Test result with parameter: "" + parameter;
            await Task.Delay(100);
            return result;
        }
    }
}";

            var content = baseContent;
            while (content.Length < targetSizeBytes)
            {
                content += Environment.NewLine + $"// Additional line {content.Split('\n').Length}: " + new string('x', 50);
            }

            return content.Substring(0, Math.Min(content.Length, targetSizeBytes));
        }

        #endregion
    }
}