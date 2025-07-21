using System;
using System.Collections.Generic;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class ChatMessageTests
    {
        [Fact]
        public void IsValid_WithValidMessage_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid message content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void IsValid_WithInvalidId_ReturnsFalse(string invalidId)
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = invalidId,
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void IsValid_WithInvalidContent_ReturnsFalse(string invalidContent)
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = invalidContent,
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithContentExceedingMaxLength_ReturnsFalse()
        {
            // Arrange
            var longContent = new string('a', 50001); // Exceeds 50,000 character limit
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = longContent,
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithContentAtMaxLength_ReturnsTrue()
        {
            // Arrange
            var maxContent = new string('a', 50000); // Exactly 50,000 characters
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = maxContent,
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithDefaultTimestamp_ReturnsFalse()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = default(DateTime)
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidFileReference_ReturnsFalse()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = "", // Invalid file path
                        Type = ReferenceType.File
                    }
                }
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithValidFileReference_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = "/path/to/file.cs",
                        Type = ReferenceType.File,
                        StartLine = 1,
                        EndLine = 10
                    }
                }
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValidationErrors_WithValidMessage_ReturnsEmptyList()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleIssues_ReturnsAllErrors()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "",
                Content = "",
                Type = MessageType.User,
                Timestamp = default(DateTime)
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(3);
            errors.Should().Contain("Message ID is required");
            errors.Should().Contain("Message content is required");
            errors.Should().Contain("Message timestamp is required");
        }

        [Fact]
        public void GetValidationErrors_WithContentTooLong_ReturnsContentError()
        {
            // Arrange
            var longContent = new string('a', 50001);
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = longContent,
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Message content cannot exceed 50,000 characters");
        }

        [Fact]
        public void GetValidationErrors_WithInvalidReferences_ReturnsReferenceErrors()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = "",
                        Type = ReferenceType.File,
                        StartLine = 0,
                        EndLine = -1
                    }
                }
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().NotBeEmpty();
            errors.Should().Contain(e => e.StartsWith("Reference 1:"));
        }

        [Theory]
        [InlineData(MessageType.User)]
        [InlineData(MessageType.Assistant)]
        [InlineData(MessageType.System)]
        public void IsValid_WithAllMessageTypes_ReturnsTrue(MessageType messageType)
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = messageType,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void JsonSerialization_WithValidMessage_SerializesCorrectly()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Test message",
                Type = MessageType.User,
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                ModelUsed = "gpt-4"
            };

            // Act
            var json = JsonSerializer.Serialize(message);
            var deserializedMessage = JsonSerializer.Deserialize<ChatMessage>(json);

            // Assert
            deserializedMessage.Should().NotBeNull();
            deserializedMessage.Id.Should().Be("test-id");
            deserializedMessage.Content.Should().Be("Test message");
            deserializedMessage.Type.Should().Be(MessageType.User);
            deserializedMessage.ModelUsed.Should().Be("gpt-4");
        }

        [Fact]
        public void JsonSerialization_WithMessageTypeEnum_SerializesAsString()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Test message",
                Type = MessageType.Assistant,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var json = JsonSerializer.Serialize(message);

            // Assert
            json.Should().Contain("\"type\":\"Assistant\"");
        }

        [Fact]
        public void Constructor_CreatesMessageWithDefaults()
        {
            // Act
            var message = new ChatMessage();

            // Assert
            message.Id.Should().NotBeNullOrEmpty();
            message.Content.Should().Be(string.Empty);
            message.References.Should().NotBeNull();
            message.References.Should().BeEmpty();
            message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void IsValid_WithNullReferences_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = null
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithEmptyReferences_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>()
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValidationErrors_WithNullReferences_ReturnsEmptyList()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = null
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_WithMultipleValidReferences_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = "/path/to/file1.cs",
                        Type = ReferenceType.File,
                        StartLine = 1,
                        EndLine = 10
                    },
                    new FileReference
                    {
                        FilePath = "/path/to/file2.cs",
                        Type = ReferenceType.Selection,
                        StartLine = 5,
                        EndLine = 15
                    }
                }
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleReferences_IndexesCorrectly()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Valid content",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                References = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = "/path/to/file1.cs",
                        Type = ReferenceType.File,
                        StartLine = 1,
                        EndLine = 10
                    },
                    new FileReference
                    {
                        FilePath = "", // Invalid
                        Type = ReferenceType.File,
                        StartLine = 0 // Invalid
                    }
                }
            };

            // Act
            var errors = message.GetValidationErrors();

            // Assert
            errors.Should().Contain(e => e.StartsWith("Reference 2:"));
        }

        [Fact]
        public void IsValid_WithSpecialCharactersInContent_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Content with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithUnicodeContent_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Unicode content: 你好世界 🌍 café naïve résumé",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithMultilineContent_ReturnsTrue()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Line 1\nLine 2\r\nLine 3\n\nLine 5",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var result = message.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void JsonDeserialization_WithMissingOptionalFields_SetsDefaults()
        {
            // Arrange
            var json = """
            {
                "id": "test-id",
                "content": "Test message",
                "type": "User",
                "timestamp": "2023-01-01T12:00:00Z"
            }
            """;

            // Act
            var message = JsonSerializer.Deserialize<ChatMessage>(json);

            // Assert
            message.Should().NotBeNull();
            message.References.Should().NotBeNull();
            message.References.Should().BeEmpty();
            message.ModelUsed.Should().BeNull();
        }

        [Fact]
        public void JsonSerialization_WithNullModelUsed_HandlesCorrectly()
        {
            // Arrange
            var message = new ChatMessage
            {
                Id = "test-id",
                Content = "Test message",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                ModelUsed = null
            };

            // Act
            var json = JsonSerializer.Serialize(message);
            var deserializedMessage = JsonSerializer.Deserialize<ChatMessage>(json);

            // Assert
            deserializedMessage.Should().NotBeNull();
            deserializedMessage.ModelUsed.Should().BeNull();
        }
    }
}
