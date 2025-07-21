using System;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class ConversationSummaryTests
    {
        [Fact]
        public void IsValid_WithValidSummary_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

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
            var summary = new ConversationSummary
            {
                Id = invalidId,
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

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
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = invalidTitle,
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void IsValid_WithNegativeMessageCount_ReturnsFalse(int invalidCount)
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = invalidCount,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithZeroMessageCount_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 0,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithDefaultCreatedAt_ReturnsFalse()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = default(DateTime),
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithDefaultLastModified_ReturnsFalse()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = default(DateTime)
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrors_WithValidSummary_ReturnsEmptyList()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var errors = summary.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleIssues_ReturnsAllErrors()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "",
                Title = "",
                MessageCount = -1,
                CreatedAt = default(DateTime),
                LastModified = default(DateTime)
            };

            // Act
            var errors = summary.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(5);
            errors.Should().Contain("Summary ID is required");
            errors.Should().Contain("Summary title is required");
            errors.Should().Contain("Message count cannot be negative");
            errors.Should().Contain("Created date is required");
            errors.Should().Contain("Last modified date is required");
        }

        [Fact]
        public void GetValidationErrors_WithTitleTooLong_ReturnsTitleError()
        {
            // Arrange
            var longTitle = new string('a', 501); // Exceeds 500 character limit
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = longTitle,
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var errors = summary.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Summary title cannot exceed 500 characters");
        }

        [Fact]
        public void IsValid_WithTitleAtMaxLength_ReturnsTrue()
        {
            // Arrange
            var maxTitle = new string('a', 500); // Exactly 500 characters
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = maxTitle,
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetDisplayName_ReturnsCorrectFormat()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Title = "Test Conversation",
                MessageCount = 5
            };

            // Act
            var displayName = summary.GetDisplayName();

            // Assert
            displayName.Should().Be("Test Conversation (5 messages)");
        }

        [Fact]
        public void GetDisplayName_WithSingleMessage_ReturnsCorrectFormat()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Title = "Test Conversation",
                MessageCount = 1
            };

            // Act
            var displayName = summary.GetDisplayName();

            // Assert
            displayName.Should().Be("Test Conversation (1 message)");
        }

        [Fact]
        public void GetDisplayName_WithZeroMessages_ReturnsCorrectFormat()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Title = "Empty Conversation",
                MessageCount = 0
            };

            // Act
            var displayName = summary.GetDisplayName();

            // Assert
            displayName.Should().Be("Empty Conversation (0 messages)");
        }

        [Fact]
        public void GetAgeDescription_WithRecentConversation_ReturnsCorrectDescription()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                LastModified = DateTime.UtcNow.AddMinutes(-30)
            };

            // Act
            var ageDescription = summary.GetAgeDescription();

            // Assert
            ageDescription.Should().Be("30 minutes ago");
        }

        [Fact]
        public void GetAgeDescription_WithOldConversation_ReturnsCorrectDescription()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                LastModified = DateTime.UtcNow.AddDays(-2)
            };

            // Act
            var ageDescription = summary.GetAgeDescription();

            // Assert
            ageDescription.Should().Be("2 days ago");
        }

        [Fact]
        public void GetAgeDescription_WithVeryOldConversation_ReturnsCorrectDescription()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                LastModified = DateTime.UtcNow.AddDays(-365)
            };

            // Act
            var ageDescription = summary.GetAgeDescription();

            // Assert
            ageDescription.Should().Be("1 year ago");
        }

        [Fact]
        public void IsRecent_WithRecentConversation_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                LastModified = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var isRecent = summary.IsRecent(TimeSpan.FromHours(2));

            // Assert
            isRecent.Should().BeTrue();
        }

        [Fact]
        public void IsRecent_WithOldConversation_ReturnsFalse()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                LastModified = DateTime.UtcNow.AddHours(-3)
            };

            // Act
            var isRecent = summary.IsRecent(TimeSpan.FromHours(2));

            // Assert
            isRecent.Should().BeFalse();
        }

        [Fact]
        public void IsEmpty_WithZeroMessages_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                MessageCount = 0
            };

            // Act
            var isEmpty = summary.IsEmpty();

            // Assert
            isEmpty.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_WithMessages_ReturnsFalse()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                MessageCount = 5
            };

            // Act
            var isEmpty = summary.IsEmpty();

            // Assert
            isEmpty.Should().BeFalse();
        }

        [Fact]
        public void JsonSerialization_WithValidSummary_SerializesCorrectly()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                LastModified = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc),
                LastMessagePreview = "Hello world"
            };

            // Act
            var json = JsonSerializer.Serialize(summary);
            var deserializedSummary = JsonSerializer.Deserialize<ConversationSummary>(json);

            // Assert
            deserializedSummary.Should().NotBeNull();
            deserializedSummary.Id.Should().Be("test-id");
            deserializedSummary.Title.Should().Be("Test Summary");
            deserializedSummary.MessageCount.Should().Be(5);
            deserializedSummary.LastMessagePreview.Should().Be("Hello world");
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var summary = new ConversationSummary();

            // Assert
            summary.Id.Should().NotBeNullOrEmpty();
            summary.Title.Should().Be(string.Empty);
            summary.MessageCount.Should().Be(0);
            summary.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            summary.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            summary.LastMessagePreview.Should().Be(string.Empty);
        }

        [Fact]
        public void JsonDeserialization_WithMissingOptionalFields_SetsDefaults()
        {
            // Arrange
            var json = """
            {
                "id": "test-id",
                "title": "Test Summary",
                "messageCount": 5,
                "createdAt": "2023-01-01T12:00:00Z",
                "lastModified": "2023-01-01T13:00:00Z"
            }
            """;

            // Act
            var summary = JsonSerializer.Deserialize<ConversationSummary>(json);

            // Assert
            summary.Should().NotBeNull();
            summary.LastMessagePreview.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void IsValid_WithValidMessageCounts_ReturnsTrue(int messageCount)
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = messageCount,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetDisplayName_WithLongTitle_TruncatesCorrectly()
        {
            // Arrange
            var longTitle = new string('a', 100);
            var summary = new ConversationSummary
            {
                Title = longTitle,
                MessageCount = 5
            };

            // Act
            var displayName = summary.GetDisplayName();

            // Assert
            displayName.Should().StartWith(longTitle.Substring(0, 50));
            displayName.Should().EndWith("... (5 messages)");
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new ConversationSummary
            {
                Id = "test-id",
                Title = "Test Summary",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                LastMessagePreview = "Preview text"
            };

            // Act
            var cloned = original.Clone();

            // Assert
            cloned.Should().NotBeSameAs(original);
            cloned.Id.Should().Be(original.Id);
            cloned.Title.Should().Be(original.Title);
            cloned.MessageCount.Should().Be(original.MessageCount);
            cloned.CreatedAt.Should().Be(original.CreatedAt);
            cloned.LastModified.Should().Be(original.LastModified);
            cloned.LastMessagePreview.Should().Be(original.LastMessagePreview);
        }

        [Fact]
        public void Equals_WithSameId_ReturnsTrue()
        {
            // Arrange
            var summary1 = new ConversationSummary { Id = "same-id", Title = "Title 1" };
            var summary2 = new ConversationSummary { Id = "same-id", Title = "Title 2" };

            // Act
            var result = summary1.Equals(summary2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentId_ReturnsFalse()
        {
            // Arrange
            var summary1 = new ConversationSummary { Id = "id-1", Title = "Same Title" };
            var summary2 = new ConversationSummary { Id = "id-2", Title = "Same Title" };

            // Act
            var result = summary1.Equals(summary2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_WithSameId_ReturnsSameHashCode()
        {
            // Arrange
            var summary1 = new ConversationSummary { Id = "same-id", Title = "Title 1" };
            var summary2 = new ConversationSummary { Id = "same-id", Title = "Title 2" };

            // Act
            var hash1 = summary1.GetHashCode();
            var hash2 = summary2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void IsValid_WithWhitespaceTitle_ReturnsFalse(string whitespaceTitle)
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = whitespaceTitle,
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithSpecialCharactersInTitle_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Title with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithUnicodeTitle_ReturnsTrue()
        {
            // Arrange
            var summary = new ConversationSummary
            {
                Id = "test-id",
                Title = "Unicode title: 你好世界 🌍 café naïve résumé",
                MessageCount = 5,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = summary.IsValid();

            // Assert
            result.Should().BeTrue();
        }
    }
}