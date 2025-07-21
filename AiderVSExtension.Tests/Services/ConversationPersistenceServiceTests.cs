using System;
using System.Collections.Generic;
using System.IO;
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
    public class ConversationPersistenceServiceTests : IDisposable
    {
        private readonly ConversationPersistenceService _service;
        private readonly string _tempDirectory;

        public ConversationPersistenceServiceTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ConversationPersistenceServiceTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            _service = new ConversationPersistenceService(_tempDirectory);
        }

        public void Dispose()
        {
            _service?.Dispose();
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDirectory_CreatesInstance()
        {
            // Act
            var service = new ConversationPersistenceService(_tempDirectory);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ConversationPersistenceService(null));
        }

        [Fact]
        public void Constructor_WithEmptyDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ConversationPersistenceService(""));
        }

        #endregion

        #region SaveConversationAsync Tests

        [Fact]
        public async Task SaveConversationAsync_WithValidConversation_SavesSuccessfully()
        {
            // Arrange
            var conversation = CreateTestConversation();

            // Act
            await _service.SaveConversationAsync(conversation);

            // Assert
            var savedConversation = await _service.LoadConversationAsync(conversation.Id);
            savedConversation.Should().NotBeNull();
            savedConversation.Id.Should().Be(conversation.Id);
            savedConversation.Title.Should().Be(conversation.Title);
            savedConversation.Messages.Should().HaveCount(conversation.Messages.Count);
        }

        [Fact]
        public async Task SaveConversationAsync_WithNullConversation_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.SaveConversationAsync(null));
        }

        [Fact]
        public async Task SaveConversationAsync_WithInvalidConversation_ThrowsArgumentException()
        {
            // Arrange
            var invalidConversation = new Conversation
            {
                Id = "", // Invalid
                Title = "Test"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.SaveConversationAsync(invalidConversation));
            
            exception.Message.Should().StartWith("Invalid conversation:");
        }

        [Fact]
        public async Task SaveConversationAsync_OverwritesExistingConversation()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Modify conversation
            conversation.Title = "Updated Title";
            conversation.Messages.Add(new ChatMessage
            {
                Content = "New message",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            });

            // Act
            await _service.SaveConversationAsync(conversation);

            // Assert
            var savedConversation = await _service.LoadConversationAsync(conversation.Id);
            savedConversation.Title.Should().Be("Updated Title");
            savedConversation.Messages.Should().HaveCount(3);
        }

        #endregion

        #region LoadConversationAsync Tests

        [Fact]
        public async Task LoadConversationAsync_WithValidId_ReturnsConversation()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Act
            var loadedConversation = await _service.LoadConversationAsync(conversation.Id);

            // Assert
            loadedConversation.Should().NotBeNull();
            loadedConversation.Id.Should().Be(conversation.Id);
            loadedConversation.Title.Should().Be(conversation.Title);
        }

        [Fact]
        public async Task LoadConversationAsync_WithNonExistentId_ReturnsNull()
        {
            // Act
            var result = await _service.LoadConversationAsync("non-existent-id");

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoadConversationAsync_WithInvalidId_ReturnsNull(string invalidId)
        {
            // Act
            var result = await _service.LoadConversationAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region DeleteConversationAsync Tests

        [Fact]
        public async Task DeleteConversationAsync_WithValidId_DeletesConversation()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Act
            var result = await _service.DeleteConversationAsync(conversation.Id);

            // Assert
            result.Should().BeTrue();
            var deletedConversation = await _service.LoadConversationAsync(conversation.Id);
            deletedConversation.Should().BeNull();
        }

        [Fact]
        public async Task DeleteConversationAsync_WithNonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteConversationAsync("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteConversationAsync_WithInvalidId_ReturnsFalse(string invalidId)
        {
            // Act
            var result = await _service.DeleteConversationAsync(invalidId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetConversationSummariesAsync Tests

        [Fact]
        public async Task GetConversationSummariesAsync_WithNoConversations_ReturnsEmptyList()
        {
            // Act
            var summaries = await _service.GetConversationSummariesAsync();

            // Assert
            summaries.Should().NotBeNull();
            summaries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationSummariesAsync_WithConversations_ReturnsSummaries()
        {
            // Arrange
            var conversation1 = CreateTestConversation("conv1", "First Conversation");
            var conversation2 = CreateTestConversation("conv2", "Second Conversation");
            
            await _service.SaveConversationAsync(conversation1);
            await _service.SaveConversationAsync(conversation2);

            // Act
            var summaries = await _service.GetConversationSummariesAsync();

            // Assert
            summaries.Should().HaveCount(2);
            summaries.Should().Contain(s => s.Id == "conv1" && s.Title == "First Conversation");
            summaries.Should().Contain(s => s.Id == "conv2" && s.Title == "Second Conversation");
        }

        [Fact]
        public async Task GetConversationSummariesAsync_OrdersByLastModifiedDescending()
        {
            // Arrange
            var conversation1 = CreateTestConversation("conv1", "First");
            conversation1.LastModified = DateTime.UtcNow.AddHours(-2);
            
            var conversation2 = CreateTestConversation("conv2", "Second");
            conversation2.LastModified = DateTime.UtcNow.AddHours(-1);
            
            await _service.SaveConversationAsync(conversation1);
            await _service.SaveConversationAsync(conversation2);

            // Act
            var summaries = await _service.GetConversationSummariesAsync();

            // Assert
            summaries.Should().HaveCount(2);
            summaries.First().Id.Should().Be("conv2"); // Most recent first
            summaries.Last().Id.Should().Be("conv1");
        }

        #endregion

        #region ArchiveConversationAsync Tests

        [Fact]
        public async Task ArchiveConversationAsync_WithValidId_ArchivesConversation()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Act
            var result = await _service.ArchiveConversationAsync(conversation.Id, "test-archive");

            // Assert
            result.Should().BeTrue();
            
            // Original should be deleted
            var originalConversation = await _service.LoadConversationAsync(conversation.Id);
            originalConversation.Should().BeNull();
            
            // Archive should exist
            var archiveDirectory = Path.Combine(_tempDirectory, "archives");
            Directory.Exists(archiveDirectory).Should().BeTrue();
        }

        [Fact]
        public async Task ArchiveConversationAsync_WithNonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _service.ArchiveConversationAsync("non-existent", "archive");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ArchiveConversationAsync_WithInvalidId_ReturnsFalse(string invalidId)
        {
            // Act
            var result = await _service.ArchiveConversationAsync(invalidId, "archive");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetArchivedConversationsAsync Tests

        [Fact]
        public async Task GetArchivedConversationsAsync_WithNoArchives_ReturnsEmptyList()
        {
            // Act
            var archives = await _service.GetArchivedConversationsAsync();

            // Assert
            archives.Should().NotBeNull();
            archives.Should().BeEmpty();
        }

        [Fact]
        public async Task GetArchivedConversationsAsync_WithArchives_ReturnsArchiveList()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);
            await _service.ArchiveConversationAsync(conversation.Id, "test-archive");

            // Act
            var archives = await _service.GetArchivedConversationsAsync();

            // Assert
            archives.Should().HaveCount(1);
            archives.First().Should().Contain("test-archive");
        }

        #endregion

        #region ExportConversationAsync Tests

        [Fact]
        public async Task ExportConversationAsync_WithValidConversation_ExportsSuccessfully()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);
            var exportPath = Path.Combine(_tempDirectory, "export.json");

            // Act
            var result = await _service.ExportConversationAsync(conversation.Id, exportPath);

            // Assert
            result.Should().BeTrue();
            File.Exists(exportPath).Should().BeTrue();
            
            var exportedContent = await File.ReadAllTextAsync(exportPath);
            exportedContent.Should().Contain(conversation.Title);
        }

        [Fact]
        public async Task ExportConversationAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var exportPath = Path.Combine(_tempDirectory, "export.json");

            // Act
            var result = await _service.ExportConversationAsync("non-existent", exportPath);

            // Assert
            result.Should().BeFalse();
            File.Exists(exportPath).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ExportConversationAsync_WithInvalidPath_ReturnsFalse(string invalidPath)
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Act
            var result = await _service.ExportConversationAsync(conversation.Id, invalidPath);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ImportConversationAsync Tests

        [Fact]
        public async Task ImportConversationAsync_WithValidFile_ImportsSuccessfully()
        {
            // Arrange
            var conversation = CreateTestConversation();
            var exportPath = Path.Combine(_tempDirectory, "export.json");
            
            // Export first
            await _service.SaveConversationAsync(conversation);
            await _service.ExportConversationAsync(conversation.Id, exportPath);
            
            // Delete original
            await _service.DeleteConversationAsync(conversation.Id);

            // Act
            var result = await _service.ImportConversationAsync(exportPath);

            // Assert
            result.Should().BeTrue();
            
            var importedConversation = await _service.LoadConversationAsync(conversation.Id);
            importedConversation.Should().NotBeNull();
            importedConversation.Title.Should().Be(conversation.Title);
        }

        [Fact]
        public async Task ImportConversationAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Act
            var result = await _service.ImportConversationAsync("non-existent.json");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ImportConversationAsync_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJsonPath = Path.Combine(_tempDirectory, "invalid.json");
            await File.WriteAllTextAsync(invalidJsonPath, "invalid json content");

            // Act
            var result = await _service.ImportConversationAsync(invalidJsonPath);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CleanupOldConversationsAsync Tests

        [Fact]
        public async Task CleanupOldConversationsAsync_WithOldConversations_DeletesOldOnes()
        {
            // Arrange
            var oldConversation = CreateTestConversation("old", "Old");
            oldConversation.LastModified = DateTime.UtcNow.AddDays(-40); // Older than 30 days
            
            var newConversation = CreateTestConversation("new", "New");
            newConversation.LastModified = DateTime.UtcNow.AddDays(-10); // Within 30 days
            
            await _service.SaveConversationAsync(oldConversation);
            await _service.SaveConversationAsync(newConversation);

            // Act
            var deletedCount = await _service.CleanupOldConversationsAsync(TimeSpan.FromDays(30));

            // Assert
            deletedCount.Should().Be(1);
            
            var oldExists = await _service.LoadConversationAsync("old");
            var newExists = await _service.LoadConversationAsync("new");
            
            oldExists.Should().BeNull();
            newExists.Should().NotBeNull();
        }

        [Fact]
        public async Task CleanupOldConversationsAsync_WithNoOldConversations_ReturnsZero()
        {
            // Arrange
            var conversation = CreateTestConversation();
            await _service.SaveConversationAsync(conversation);

            // Act
            var deletedCount = await _service.CleanupOldConversationsAsync(TimeSpan.FromDays(30));

            // Assert
            deletedCount.Should().Be(0);
        }

        #endregion

        #region Private Helper Methods

        private Conversation CreateTestConversation(string id = null, string title = null)
        {
            return new Conversation
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Title = title ?? "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    },
                    new ChatMessage
                    {
                        Content = "Hi there!",
                        Type = MessageType.Assistant,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };
        }

        #endregion
    }
}