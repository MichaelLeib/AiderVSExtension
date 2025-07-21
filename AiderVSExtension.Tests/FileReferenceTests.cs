using System;
using System.Text.Json;
using AiderVSExtension.Models;
using FluentAssertions;
using Xunit;

namespace AiderVSExtension.Tests
{
    public class FileReferenceTests
    {
        [Fact]
        public void IsValid_WithValidReference_ReturnsTrue()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

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
            var reference = new FileReference
            {
                FilePath = invalidPath,
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsValid_WithInvalidStartLine_ReturnsFalse(int invalidStartLine)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = invalidStartLine,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsValid_WithInvalidEndLine_ReturnsFalse(int invalidEndLine)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = invalidEndLine,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithEndLineLessThanStartLine_ReturnsFalse()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidPathFormat_ReturnsFalse()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "\\invalid:\\path",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValidationErrors_WithValidReference_ReturnsEmptyList()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var errors = reference.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithAllIssues_ReturnsAllErrors()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "",
                StartLine = 0,
                EndLine = 0,
                Type = ReferenceType.File
            };

            // Act
            var errors = reference.GetValidationErrors();

            // Assert
            errors.Should().HaveCount(4);
            errors.Should().Contain("File path is required");
            errors.Should().Contain("Start line must be greater than 0");
            errors.Should().Contain("End line must be greater than 0");
            errors.Should().Contain("End line must be greater than or equal to start line");
        }

        [Fact]
        public void GetValidationErrors_WithInvalidPathFormat_ReturnsFormatError()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "\\invalid:\\path\\",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var errors = reference.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().StartWith("Invalid file path format:");
        }

        [Fact]
        public void GetDisplayName_WithSingleLine_ReturnsCorrectFormat()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 10
            };

            // Act
            var displayName = reference.GetDisplayName();

            // Assert
            displayName.Should().Be("file.cs:10");
        }

        [Fact]
        public void GetDisplayName_WithMultipleLines_ReturnsCorrectFormat()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5
            };

            // Act
            var displayName = reference.GetDisplayName();

            // Assert
            displayName.Should().Be("file.cs:1-5");
        }

        [Fact]
        public void IsSingleLine_WithSingleLine_ReturnsTrue()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 10
            };

            // Act
            var isSingleLine = reference.IsSingleLine();

            // Assert
            isSingleLine.Should().BeTrue();
        }

        [Fact]
        public void GetLineCount_WithMultipleLines_ReturnsCorrectCount()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5
            };

            // Act
            var lineCount = reference.GetLineCount();

            // Assert
            lineCount.Should().Be(5);
        }

        [Fact]
        public void JsonSerialization_WithValidReference_SerializesCorrectly()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var json = JsonSerializer.Serialize(reference);
            var deserializedReference = JsonSerializer.Deserialize<FileReference>(json);

            // Assert
            deserializedReference.Should().NotBeNull();
            deserializedReference.FilePath.Should().Be("/path/to/file.cs");
            deserializedReference.StartLine.Should().Be(1);
            deserializedReference.EndLine.Should().Be(5);
            deserializedReference.Type.Should().Be(ReferenceType.File);
        }

        [Theory]
        [InlineData(ReferenceType.File)]
        [InlineData(ReferenceType.Selection)]
        [InlineData(ReferenceType.Error)]
        [InlineData(ReferenceType.Clipboard)]
        [InlineData(ReferenceType.GitBranch)]
        [InlineData(ReferenceType.WebSearch)]
        [InlineData(ReferenceType.Documentation)]
        public void IsValid_WithAllReferenceTypes_ReturnsTrue(ReferenceType referenceType)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5,
                Type = referenceType
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 10)]
        [InlineData(5, 5)]
        [InlineData(10, 50)]
        public void IsValid_WithValidLineRanges_ReturnsTrue(int startLine, int endLine)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = startLine,
                EndLine = endLine,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

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
            var reference = new FileReference
            {
                FilePath = filePath,
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithEqualStartAndEndLines_ReturnsTrue()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 10,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsSingleLine_WithMultipleLines_ReturnsFalse()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5
            };

            // Act
            var isSingleLine = reference.IsSingleLine();

            // Assert
            isSingleLine.Should().BeFalse();
        }

        [Fact]
        public void GetLineCount_WithSingleLine_ReturnsOne()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 10
            };

            // Act
            var lineCount = reference.GetLineCount();

            // Assert
            lineCount.Should().Be(1);
        }

        [Fact]
        public void GetValidationErrors_WithEndLineEqualToStartLine_ReturnsNoError()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 5,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var errors = reference.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void GetValidationErrors_WithEndLineLessThanStartLine_ReturnsError()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 10,
                EndLine = 5,
                Type = ReferenceType.File
            };

            // Act
            var errors = reference.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle();
            errors[0].Should().Be("End line must be greater than or equal to start line");
        }

        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var reference = new FileReference();

            // Assert
            reference.FilePath.Should().Be(string.Empty);
            reference.StartLine.Should().Be(1);
            reference.EndLine.Should().Be(1);
            reference.Content.Should().Be(string.Empty);
            reference.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void JsonSerialization_WithReferenceTypeEnum_SerializesAsString()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = 5,
                Type = ReferenceType.Selection
            };

            // Act
            var json = JsonSerializer.Serialize(reference);

            // Assert
            json.Should().Contain("\"type\":\"Selection\"");
        }

        [Theory]
        [InlineData("file.cs", "file.cs:1-5")]
        [InlineData("src/main.py", "main.py:10-15")]
        [InlineData("C:\\Project\\App.xaml", "App.xaml:1")]
        public void GetDisplayName_WithVariousFileNames_ReturnsCorrectFormat(string filePath, string expectedFormat)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = filePath,
                StartLine = expectedFormat.Contains("-") ? 1 : 1,
                EndLine = expectedFormat.Contains("-") ? 5 : 1
            };

            if (expectedFormat.Contains("10-15"))
            {
                reference.StartLine = 10;
                reference.EndLine = 15;
            }

            // Act
            var displayName = reference.GetDisplayName();

            // Assert
            displayName.Should().Be(expectedFormat);
        }

        [Fact]
        public void JsonDeserialization_WithMissingOptionalFields_SetsDefaults()
        {
            // Arrange
            var json = """
            {
                "filePath": "/path/to/file.cs",
                "type": "File"
            }
            """;

            // Act
            var reference = JsonSerializer.Deserialize<FileReference>(json);

            // Assert
            reference.Should().NotBeNull();
            reference.FilePath.Should().Be("/path/to/file.cs");
            reference.Type.Should().Be(ReferenceType.File);
            reference.StartLine.Should().Be(1);
            reference.EndLine.Should().Be(1);
            reference.Content.Should().Be(string.Empty);
        }

        [Fact]
        public void JsonSerialization_WithAllProperties_SerializesCorrectly()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 5,
                EndLine = 10,
                Type = ReferenceType.Selection,
                Content = "some code content",
                Timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var json = JsonSerializer.Serialize(reference);
            var deserializedReference = JsonSerializer.Deserialize<FileReference>(json);

            // Assert
            deserializedReference.Should().NotBeNull();
            deserializedReference.FilePath.Should().Be("/path/to/file.cs");
            deserializedReference.StartLine.Should().Be(5);
            deserializedReference.EndLine.Should().Be(10);
            deserializedReference.Type.Should().Be(ReferenceType.Selection);
            deserializedReference.Content.Should().Be("some code content");
            deserializedReference.Timestamp.Should().Be(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(1000000)]
        [InlineData(999999)]
        public void IsValid_WithLargeValidLineNumbers_ReturnsTrue(int lineNumber)
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = lineNumber,
                EndLine = lineNumber,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValid_WithMaxIntLineNumbers_ReturnsTrue()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 1,
                EndLine = int.MaxValue,
                Type = ReferenceType.File
            };

            // Act
            var result = reference.IsValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetLineCount_WithLargeRange_ReturnsCorrectCount()
        {
            // Arrange
            var reference = new FileReference
            {
                FilePath = "/path/to/file.cs",
                StartLine = 100,
                EndLine = 200
            };

            // Act
            var lineCount = reference.GetLineCount();

            // Assert
            lineCount.Should().Be(101); // 200 - 100 + 1
        }
    }
}
