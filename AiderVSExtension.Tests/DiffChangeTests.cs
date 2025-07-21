using System;
using System.Collections.Generic;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class DiffChangeTests
    {
        [Fact]
        public void IsValid_WithValidAddedChange_ReturnsTrue()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithValidRemovedChange_ReturnsTrue()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Removed,
                OriginalContent = "old code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithValidModifiedChange_ReturnsTrue()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "old code line",
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithInvalidFilePath_ReturnsFalse(string invalidPath)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = invalidPath,
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsValid_WithInvalidLineNumber_ReturnsFalse(int invalidLineNumber)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = invalidLineNumber,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithInvalidId_ReturnsFalse(string invalidId)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = invalidId
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithDefaultTimestamp_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = default(DateTime),
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithAddedChangeAndMissingNewContent_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithRemovedChangeAndMissingOriginalContent_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Removed,
                OriginalContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithModifiedChangeAndMissingOriginalContent_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "",
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithModifiedChangeAndMissingNewContent_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "old code line",
                NewContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithModifiedChangeAndSameContent_ReturnsFalse()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "same content",
                NewContent = "same content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrors_WithValidChange_ReturnsEmptyList()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithMultipleIssues_ReturnsAllErrors()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "",
                LineNumber = 0,
                Type = ChangeType.Added,
                NewContent = "",
                Timestamp = default(DateTime),
                Id = ""
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(4);
            errors.Should().Contain("File path is required");
            errors.Should().Contain("Line number must be greater than 0");
            errors.Should().Contain("Change ID is required");
            errors.Should().Contain("Timestamp is required");
        }

        [Fact]
        public void GetValidationErrors_WithAddedChangeAndMissingNewContent_ReturnsContentError()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("New content is required for added changes");
        }

        [Fact]
        public void GetValidationErrors_WithRemovedChangeAndMissingOriginalContent_ReturnsContentError()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Removed,
                OriginalContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Original content is required for removed changes");
        }

        [Fact]
        public void GetValidationErrors_WithModifiedChangeAndMissingContent_ReturnsContentErrors()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "",
                NewContent = "",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(2);
            errors.Should().Contain("Original content is required for modified changes");
            errors.Should().Contain("New content is required for modified changes");
        }

        [Fact]
        public void GetValidationErrors_WithModifiedChangeAndSameContent_ReturnsContentError()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "same content",
                NewContent = "same content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("Original and new content must be different for modified changes");
        }

        [Fact]
        public void GetValidationErrors_WithInvalidPathFormat_ReturnsFormatError()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "\\invalid:\\path\\",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new code line",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var errors = change.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().StartWith("Invalid file path format:");
        }

        [Fact]
        public void GetDisplayName_ReturnsCorrectFormat()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified
            };

            // Act
            var displayName = change.GetDisplayName();

            // Assert
            displayName.Should().Be("file.cs:10 (modified)");
        }

        [Fact]
        public void GetChangeSummary_WithAddedChange_ReturnsCorrectSummary()
        {
            // Arrange
            var change = new DiffChange
            {
                Type = ChangeType.Added,
                NewContent = "This is a very long line of code that should be truncated"
            };

            // Act
            var summary = change.GetChangeSummary();

            // Assert
            summary.Should().StartWith("Added: This is a very long line of code that should be tru...");
        }

        [Fact]
        public void GetChangeSummary_WithRemovedChange_ReturnsCorrectSummary()
        {
            // Arrange
            var change = new DiffChange
            {
                Type = ChangeType.Removed,
                OriginalContent = "This is a very long line of code that should be truncated"
            };

            // Act
            var summary = change.GetChangeSummary();

            // Assert
            summary.Should().StartWith("Removed: This is a very long line of code that should be tr...");
        }

        [Fact]
        public void GetChangeSummary_WithModifiedChange_ReturnsCorrectSummary()
        {
            // Arrange
            var change = new DiffChange
            {
                Type = ChangeType.Modified,
                OriginalContent = "Old very long line of code",
                NewContent = "New very long line of code"
            };

            // Act
            var summary = change.GetChangeSummary();

            // Assert
            summary.Should().StartWith("Modified: Old very long line of cod... → New very long line of cod...");
        }

        [Fact]
        public void AffectsSameLine_WithSameFileAndLine_ReturnsTrue()
        {
            // Arrange
            var change1 = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10
            };

            var change2 = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10
            };

            // Act
            var result = change1.AffectsSameLine(change2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AffectsSameLine_WithDifferentFile_ReturnsFalse()
        {
            // Arrange
            var change1 = new DiffChange
            {
                FilePath = "/path/to/file1.cs",
                LineNumber = 10
            };

            var change2 = new DiffChange
            {
                FilePath = "/path/to/file2.cs",
                LineNumber = 10
            };

            // Act
            var result = change1.AffectsSameLine(change2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AffectsSameLine_WithDifferentLine_ReturnsFalse()
        {
            // Arrange
            var change1 = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10
            };

            var change2 = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 20
            };

            // Act
            var result = change1.AffectsSameLine(change2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AffectsSameLine_WithCaseInsensitiveFilePath_ReturnsTrue()
        {
            // Arrange
            var change1 = new DiffChange
            {
                FilePath = "/path/to/FILE.CS",
                LineNumber = 10
            };

            var change2 = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10
            };

            // Act
            var result = change1.AffectsSameLine(change2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "old content",
                NewContent = "new content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id",
                ContextBefore = new List<string> { "line1", "line2" },
                ContextAfter = new List<string> { "line3", "line4" },
                IsApplied = true
            };

            // Act
            var cloned = original.Clone();

            // Assert
            cloned.Should().NotBeSameAs(original);
            cloned.FilePath.Should().Be(original.FilePath);
            cloned.LineNumber.Should().Be(original.LineNumber);
            cloned.Type.Should().Be(original.Type);
            cloned.OriginalContent.Should().Be(original.OriginalContent);
            cloned.NewContent.Should().Be(original.NewContent);
            cloned.Timestamp.Should().Be(original.Timestamp);
            cloned.Id.Should().Be(original.Id);
            cloned.ContextBefore.Should().NotBeSameAs(original.ContextBefore);
            cloned.ContextAfter.Should().NotBeSameAs(original.ContextAfter);
            cloned.IsApplied.Should().Be(original.IsApplied);
        }

        [Theory]
        [InlineData(ChangeType.Added)]
        [InlineData(ChangeType.Removed)]
        [InlineData(ChangeType.Modified)]
        public void IsValid_WithAllChangeTypes_ReturnsTrue(ChangeType changeType)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = changeType,
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Set content based on change type
            switch (changeType)
            {
                case ChangeType.Added:
                    change.NewContent = "new content";
                    break;
                case ChangeType.Removed:
                    change.OriginalContent = "old content";
                    break;
                case ChangeType.Modified:
                    change.OriginalContent = "old content";
                    change.NewContent = "new content";
                    break;
            }

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void JsonSerialization_WithValidChange_SerializesCorrectly()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Modified,
                OriginalContent = "old content",
                NewContent = "new content",
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Id = "test-id"
            };

            // Act
            var json = JsonSerializer.Serialize(change);
            var deserializedChange = JsonSerializer.Deserialize<DiffChange>(json);

            // Assert
            deserializedChange.Should().NotBeNull();
            deserializedChange.FilePath.Should().Be("/path/to/file.cs");
            deserializedChange.LineNumber.Should().Be(10);
            deserializedChange.Type.Should().Be(ChangeType.Modified);
            deserializedChange.OriginalContent.Should().Be("old content");
            deserializedChange.NewContent.Should().Be("new content");
            deserializedChange.Id.Should().Be("test-id");
        }

        [Fact]
        public void JsonSerialization_WithChangeTypeEnum_SerializesAsString()
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var json = JsonSerializer.Serialize(change);

            // Assert
            json.Should().Contain("\"type\":\"Added\"");
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var change = new DiffChange();

            // Assert
            change.FilePath.Should().Be(string.Empty);
            change.OriginalContent.Should().Be(string.Empty);
            change.NewContent.Should().Be(string.Empty);
            change.Id.Should().NotBeNullOrEmpty();
            change.ContextBefore.Should().NotBeNull();
            change.ContextBefore.Should().BeEmpty();
            change.ContextAfter.Should().NotBeNull();
            change.ContextAfter.Should().BeEmpty();
            change.IsApplied.Should().BeFalse();
            change.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void JsonDeserialization_WithMissingOptionalFields_SetsDefaults()
        {
            // Arrange
            var json = """
            {
                "filePath": "/path/to/file.cs",
                "lineNumber": 10,
                "type": "Added",
                "newContent": "new content",
                "timestamp": "2023-01-01T12:00:00Z",
                "id": "test-id"
            }
            """;

            // Act
            var change = JsonSerializer.Deserialize<DiffChange>(json);

            // Assert
            change.Should().NotBeNull();
            change.OriginalContent.Should().Be(string.Empty);
            change.ContextBefore.Should().NotBeNull();
            change.ContextBefore.Should().BeEmpty();
            change.ContextAfter.Should().NotBeNull();
            change.ContextAfter.Should().BeEmpty();
            change.IsApplied.Should().BeFalse();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void IsValid_WithValidLineNumbers_ReturnsTrue(int lineNumber)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = "/path/to/file.cs",
                LineNumber = lineNumber,
                Type = ChangeType.Added,
                NewContent = "new content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("C:\\Windows\\System32\\file.txt")]
        [InlineData("/usr/local/bin/script.sh")]
        [InlineData("./relative/path/file.js")]
        [InlineData("..\\parent\\file.py")]
        public void IsValid_WithValidPaths_ReturnsTrue(string filePath)
        {
            // Arrange
            var change = new DiffChange
            {
                FilePath = filePath,
                LineNumber = 10,
                Type = ChangeType.Added,
                NewContent = "new content",
                Timestamp = DateTime.UtcNow,
                Id = "test-id"
            };

            // Act
            var result = change.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetChangeSummary_WithShortContent_DoesNotTruncate()
        {
            // Arrange
            var change = new DiffChange
            {
                Type = ChangeType.Added,
                NewContent = "short"
            };

            // Act
            var summary = change.GetChangeSummary();

            // Assert
            summary.Should().Be("Added: short...");
        }

        [Fact]
        public void GetChangeSummary_WithEmptyContent_HandlesGracefully()
        {
            // Arrange
            var change = new DiffChange
            {
                Type = ChangeType.Added,
                NewContent = ""
            };

            // Act
            var summary = change.GetChangeSummary();

            // Assert
            summary.Should().Be("Added: ...");
        }

        [Fact]
        public void IsApplied_DefaultsToFalse()
        {
            // Arrange
            var change = new DiffChange();

            // Act & Assert
            change.IsApplied.Should().BeFalse();
        }

        [Fact]
        public void IsApplied_CanBeSetToTrue()
        {
            // Arrange
            var change = new DiffChange
            {
                IsApplied = true
            };

            // Act & Assert
            change.IsApplied.Should().BeTrue();
        }
    }
}
