using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class ConversationTests
    {
        [Fact]
        public void IsValid_WithValidConversation_ReturnsTrue()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_WithInvalidId_ReturnsFalse(string invalidId)
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = invalidId,
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_WithInvalidTitle_ReturnsFalse(string invalidTitle)
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = invalidTitle,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithDefaultCreatedAt_ReturnsFalse()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = default(DateTime),
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithDefaultLastModified_ReturnsFalse()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = default(DateTime),
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithNullMessages_ReturnsFalse()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = null
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidMessage_ReturnsFalse()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Id = "", // Invalid
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithEmptyMessages_ReturnsTrue()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValidationErrors_WithValidConversation_ReturnsEmptyList()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var errors = conversation.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleIssues_ReturnsAllErrors()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "",
                Title = "",
                CreatedAt = default(DateTime),
                LastModified = default(DateTime),
                Messages = null
            };

            // Act
            var errors = conversation.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(5);
            errors.Should().Contain("Conversation ID is required");
            errors.Should().Contain("Conversation title is required");
            errors.Should().Contain("Created date is required");
            errors.Should().Contain("Last modified date is required");
            errors.Should().Contain("Messages collection is required");
        }

        [Fact]
        public void GetValidationErrors_WithInvalidMessages_ReturnsMessageErrors()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Id = "", // Invalid
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Act
            var errors = conversation.GetValidationErrors();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.StartsWith("Message 1:"));
        }

        [Fact]
        public void AddMessage_WithValidMessage_AddsMessage()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            var message = new ChatMessage
            {
                Content = "Hello",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            conversation.AddMessage(message);

            // Assert
            conversation.Messages.Should().HaveCount(1);
            conversation.Messages.First().Should().Be(message);
        }

        [Fact]
        public void AddMessage_WithValidMessage_UpdatesLastModified()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow.AddHours(-1),
                Messages = new List<ChatMessage>()
            };

            var originalLastModified = conversation.LastModified;
            var message = new ChatMessage
            {
                Content = "Hello",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            conversation.AddMessage(message);

            // Assert
            conversation.LastModified.Should().BeAfter(originalLastModified);
        }

        [Fact]
        public void AddMessage_WithNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>()
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => conversation.AddMessage(null));
        }

        [Fact]
        public void AddMessage_WithInvalidMessage_ThrowsArgumentException()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>()
            };

            var invalidMessage = new ChatMessage
            {
                Id = "", // Invalid
                Content = "Hello",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => conversation.AddMessage(invalidMessage));
            exception.Message.Should().StartWith("Invalid message:");
        }

        [Fact]
        public void RemoveMessage_WithExistingMessage_RemovesMessage()
        {
            // Arrange
            var message = new ChatMessage
            {
                Content = "Hello",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage> { message }
            };

            // Act
            var result = conversation.RemoveMessage(message.Id);

            // Assert
            result.Should().BeTrue();
            conversation.Messages.Should().BeEmpty();
        }

        [Fact]
        public void RemoveMessage_WithNonExistentMessage_ReturnsFalse()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.RemoveMessage("non-existent-id");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void RemoveMessage_WithExistingMessage_UpdatesLastModified()
        {
            // Arrange
            var message = new ChatMessage
            {
                Content = "Hello",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow.AddHours(-1),
                Messages = new List<ChatMessage> { message }
            };

            var originalLastModified = conversation.LastModified;

            // Act
            conversation.RemoveMessage(message.Id);

            // Assert
            conversation.LastModified.Should().BeAfter(originalLastModified);
        }

        [Fact]
        public void GetMessageCount_ReturnsCorrectCount()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Content = "Message 1", Type = MessageType.User, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "Message 2", Type = MessageType.Assistant, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "Message 3", Type = MessageType.User, Timestamp = DateTime.UtcNow }
                }
            };

            // Act
            var count = conversation.GetMessageCount();

            // Assert
            count.Should().Be(3);
        }

        [Fact]
        public void GetMessageCount_WithEmptyMessages_ReturnsZero()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>()
            };

            // Act
            var count = conversation.GetMessageCount();

            // Assert
            count.Should().Be(0);
        }

        [Fact]
        public void GetLastMessage_WithMessages_ReturnsLastMessage()
        {
            // Arrange
            var firstMessage = new ChatMessage { Content = "First", Type = MessageType.User, Timestamp = DateTime.UtcNow.AddMinutes(-1) };
            var lastMessage = new ChatMessage { Content = "Last", Type = MessageType.Assistant, Timestamp = DateTime.UtcNow };

            var conversation = new Conversation
            {
                Messages = new List<ChatMessage> { firstMessage, lastMessage }
            };

            // Act
            var result = conversation.GetLastMessage();

            // Assert
            result.Should().Be(lastMessage);
        }

        [Fact]
        public void GetLastMessage_WithEmptyMessages_ReturnsNull()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.GetLastMessage();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetSummary_ReturnsCorrectSummary()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Content = "Message 1", Type = MessageType.User, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "Message 2", Type = MessageType.Assistant, Timestamp = DateTime.UtcNow }
                }
            };

            // Act
            var summary = conversation.GetSummary();

            // Assert
            summary.Should().NotBeNull();
            summary.Id.Should().Be("test-id");
            summary.Title.Should().Be("Test Conversation");
            summary.MessageCount.Should().Be(2);
            summary.CreatedAt.Should().Be(conversation.CreatedAt);
            summary.LastModified.Should().Be(conversation.LastModified);
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Act
            var cloned = original.Clone();

            // Assert
            cloned.Should().NotBeSameAs(original);
            cloned.Id.Should().Be(original.Id);
            cloned.Title.Should().Be(original.Title);
            cloned.CreatedAt.Should().Be(original.CreatedAt);
            cloned.LastModified.Should().Be(original.LastModified);
            cloned.Messages.Should().NotBeSameAs(original.Messages);
            cloned.Messages.Should().HaveCount(original.Messages.Count);
        }

        [Fact]
        public void JsonSerialization_WithValidConversation_SerializesCorrectly()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                LastModified = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "Hello",
                        Type = MessageType.User,
                        Timestamp = new DateTime(2023, 1, 1, 12, 30, 0, DateTimeKind.Utc)
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(conversation);
            var deserializedConversation = JsonSerializer.Deserialize<Conversation>(json);

            // Assert
            deserializedConversation.Should().NotBeNull();
            deserializedConversation.Id.Should().Be("test-id");
            deserializedConversation.Title.Should().Be("Test Conversation");
            deserializedConversation.Messages.Should().HaveCount(1);
            deserializedConversation.Messages.First().Content.Should().Be("Hello");
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var conversation = new Conversation();

            // Assert
            conversation.Id.Should().NotBeNullOrEmpty();
            conversation.Title.Should().Be(string.Empty);
            conversation.Messages.Should().NotBeNull();
            conversation.Messages.Should().BeEmpty();
            conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            conversation.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void JsonDeserialization_WithMissingOptionalFields_SetsDefaults()
        {
            // Arrange
            var json = """
            {
                "id": "test-id",
                "title": "Test Conversation",
                "createdAt": "2023-01-01T12:00:00Z",
                "lastModified": "2023-01-01T13:00:00Z"
            }
            """;

            // Act
            var conversation = JsonSerializer.Deserialize<Conversation>(json);

            // Assert
            conversation.Should().NotBeNull();
            conversation.Messages.Should().NotBeNull();
            conversation.Messages.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_WithTitleExceedingMaxLength_ReturnsFalse()
        {
            // Arrange
            var longTitle = new string('a', 501); // Exceeds 500 character limit
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = longTitle,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrors_WithTitleTooLong_ReturnsTitleError()
        {
            // Arrange
            var longTitle = new string('a', 501);
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = longTitle,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var errors = conversation.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Conversation title cannot exceed 500 characters");
        }

        [Fact]
        public void IsValid_WithTitleAtMaxLength_ReturnsTrue()
        {
            // Arrange
            var maxTitle = new string('a', 500); // Exactly 500 characters
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = maxTitle,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            // Act
            var result = conversation.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetMessagesByType_ReturnsCorrectMessages()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Content = "User 1", Type = MessageType.User, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "Assistant 1", Type = MessageType.Assistant, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "User 2", Type = MessageType.User, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "System 1", Type = MessageType.System, Timestamp = DateTime.UtcNow }
                }
            };

            // Act
            var userMessages = conversation.GetMessagesByType(MessageType.User);

            // Assert
            userMessages.Should().HaveCount(2);
            userMessages.Should().OnlyContain(m => m.Type == MessageType.User);
        }

        [Fact]
        public void GetMessagesByType_WithNoMatchingMessages_ReturnsEmpty()
        {
            // Arrange
            var conversation = new Conversation
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Content = "User 1", Type = MessageType.User, Timestamp = DateTime.UtcNow }
                }
            };

            // Act
            var systemMessages = conversation.GetMessagesByType(MessageType.System);

            // Assert
            systemMessages.Should().BeEmpty();
        }

        [Fact]
        public void ClearMessages_RemovesAllMessages()
        {
            // Arrange
            var conversation = new Conversation
            {
                Id = "test-id",
                Title = "Test Conversation",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow.AddHours(-1),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Content = "Message 1", Type = MessageType.User, Timestamp = DateTime.UtcNow },
                    new ChatMessage { Content = "Message 2", Type = MessageType.Assistant, Timestamp = DateTime.UtcNow }
                }
            };

            var originalLastModified = conversation.LastModified;

            // Act
            conversation.ClearMessages();

            // Assert
            conversation.Messages.Should().BeEmpty();
            conversation.LastModified.Should().BeAfter(originalLastModified);
        }
    }
}