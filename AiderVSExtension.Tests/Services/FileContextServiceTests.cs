using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;
using EnvDTE;
using EnvDTE80;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Services
{
    public class FileContextServiceTests : IDisposable
    {
        private readonly Mock<DTE2> _mockDte;
        private readonly Mock<IVsOutputWindowPane> _mockOutputPane;
        private readonly Mock<Solution> _mockSolution;
        private readonly Mock<Projects> _mockProjects;
        private readonly FileContextService _fileContextService;
        private readonly string _tempDirectory;

        public FileContextServiceTests()
        {
            _mockDte = new Mock<DTE2>();
            _mockOutputPane = new Mock<IVsOutputWindowPane>();
            _mockSolution = new Mock<Solution>();
            _mockProjects = new Mock<Projects>();
            
            _tempDirectory = Path.Combine(Path.GetTempPath(), "FileContextServiceTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            SetupMockDte();
            
            _fileContextService = new FileContextService(_mockDte.Object, _mockOutputPane.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullDte_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FileContextService(null, _mockOutputPane.Object));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new FileContextService(_mockDte.Object, _mockOutputPane.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullOutputPane_CreatesInstance()
        {
            // Act
            var service = new FileContextService(_mockDte.Object, null);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region GetSolutionFilesAsync Tests

        [Fact]
        public async Task GetSolutionFilesAsync_WithNullSolution_ReturnsEmptyCollection()
        {
            // Arrange
            _mockDte.Setup(x => x.Solution).Returns((Solution)null);

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSolutionFilesAsync_WithNullProjects_ReturnsEmptyCollection()
        {
            // Arrange
            _mockSolution.Setup(x => x.Projects).Returns((Projects)null);

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSolutionFilesAsync_WithValidProjects_ReturnsFiles()
        {
            // Arrange
            var testFile = CreateTestFile("TestFile.cs", "public class TestClass { }");
            var mockProject = CreateMockProject("TestProject", new[] { testFile });
            SetupMockProjectsCollection(new[] { mockProject });

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(1);
            
            var file = files.First();
            file.FileName.Should().Be("TestFile.cs");
            file.ProjectName.Should().Be("TestProject");
            file.FilePath.Should().Be(testFile);
        }

        [Fact]
        public async Task GetSolutionFilesAsync_WithMultipleProjects_ReturnsAllFiles()
        {
            // Arrange
            var testFile1 = CreateTestFile("File1.cs", "class File1 { }");
            var testFile2 = CreateTestFile("File2.cs", "class File2 { }");
            var testFile3 = CreateTestFile("File3.cs", "class File3 { }");
            
            var mockProject1 = CreateMockProject("Project1", new[] { testFile1, testFile2 });
            var mockProject2 = CreateMockProject("Project2", new[] { testFile3 });
            
            SetupMockProjectsCollection(new[] { mockProject1, mockProject2 });

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().HaveCount(3);
            files.Should().Contain(f => f.FileName == "File1.cs" && f.ProjectName == "Project1");
            files.Should().Contain(f => f.FileName == "File2.cs" && f.ProjectName == "Project1");
            files.Should().Contain(f => f.FileName == "File3.cs" && f.ProjectName == "Project2");
        }

        #endregion

        #region SearchFilesAsync Tests

        [Fact]
        public async Task SearchFilesAsync_WithNullPattern_ReturnsEmptyCollection()
        {
            // Act
            var files = await _fileContextService.SearchFilesAsync(null);

            // Assert
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchFilesAsync_WithEmptyPattern_ReturnsEmptyCollection()
        {
            // Act
            var files = await _fileContextService.SearchFilesAsync("");

            // Assert
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchFilesAsync_WithWhitespacePattern_ReturnsEmptyCollection()
        {
            // Act
            var files = await _fileContextService.SearchFilesAsync("   ");

            // Assert
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchFilesAsync_WithMatchingPattern_ReturnsMatchingFiles()
        {
            // Arrange
            var testFile1 = CreateTestFile("TestService.cs", "class TestService { }");
            var testFile2 = CreateTestFile("UserService.cs", "class UserService { }");
            var testFile3 = CreateTestFile("TestController.cs", "class TestController { }");
            
            var mockProject = CreateMockProject("TestProject", new[] { testFile1, testFile2, testFile3 });
            SetupMockProjectsCollection(new[] { mockProject });

            // Act
            var files = await _fileContextService.SearchFilesAsync("Service");

            // Assert
            files.Should().HaveCount(2);
            files.Should().Contain(f => f.FileName == "TestService.cs");
            files.Should().Contain(f => f.FileName == "UserService.cs");
            files.Should().NotContain(f => f.FileName == "TestController.cs");
        }

        [Fact]
        public async Task SearchFilesAsync_CaseInsensitive_ReturnsMatchingFiles()
        {
            // Arrange
            var testFile = CreateTestFile("TestService.cs", "class TestService { }");
            var mockProject = CreateMockProject("TestProject", new[] { testFile });
            SetupMockProjectsCollection(new[] { mockProject });

            // Act
            var files = await _fileContextService.SearchFilesAsync("service");

            // Assert
            files.Should().HaveCount(1);
            files.First().FileName.Should().Be("TestService.cs");
        }

        #endregion

        #region GetFileContentAsync Tests

        [Fact]
        public async Task GetFileContentAsync_WithNullFilePath_ReturnsEmptyString()
        {
            // Act
            var content = await _fileContextService.GetFileContentAsync(null);

            // Assert
            content.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileContentAsync_WithEmptyFilePath_ReturnsEmptyString()
        {
            // Act
            var content = await _fileContextService.GetFileContentAsync("");

            // Assert
            content.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileContentAsync_WithNonExistentFile_ReturnsEmptyString()
        {
            // Act
            var content = await _fileContextService.GetFileContentAsync("non-existent-file.cs");

            // Assert
            content.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileContentAsync_WithValidFile_ReturnsFileContent()
        {
            // Arrange
            var expectedContent = "public class TestClass { public void TestMethod() { } }";
            var testFile = CreateTestFile("TestClass.cs", expectedContent);

            // Act
            var content = await _fileContextService.GetFileContentAsync(testFile);

            // Assert
            content.Should().Be(expectedContent);
        }

        [Fact]
        public async Task GetFileContentAsync_CalledTwice_UsesCaching()
        {
            // Arrange
            var expectedContent = "public class CachedClass { }";
            var testFile = CreateTestFile("CachedClass.cs", expectedContent);

            // Act
            var content1 = await _fileContextService.GetFileContentAsync(testFile);
            var content2 = await _fileContextService.GetFileContentAsync(testFile);

            // Assert
            content1.Should().Be(expectedContent);
            content2.Should().Be(expectedContent);
            content1.Should().BeSameAs(content2); // Should be cached
        }

        #endregion

        #region GetFileContentRangeAsync Tests

        [Fact]
        public async Task GetFileContentRangeAsync_WithInvalidLineNumbers_ReturnsEmptyString()
        {
            // Arrange
            var testFile = CreateTestFile("TestFile.cs", "Line 1\nLine 2\nLine 3");

            // Act & Assert
            var result1 = await _fileContextService.GetFileContentRangeAsync(testFile, 0, 2); // startLine < 1
            var result2 = await _fileContextService.GetFileContentRangeAsync(testFile, 2, 1); // endLine < startLine
            var result3 = await _fileContextService.GetFileContentRangeAsync(testFile, -1, -1); // both negative

            result1.Should().BeEmpty();
            result2.Should().BeEmpty();
            result3.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileContentRangeAsync_WithValidRange_ReturnsSpecifiedLines()
        {
            // Arrange
            var fileContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
            var testFile = CreateTestFile("TestFile.cs", fileContent);

            // Act
            var result = await _fileContextService.GetFileContentRangeAsync(testFile, 2, 4);

            // Assert
            result.Should().Be("Line 2\nLine 3\nLine 4");
        }

        [Fact]
        public async Task GetFileContentRangeAsync_WithRangeBeyondFileEnd_ReturnsAvailableLines()
        {
            // Arrange
            var fileContent = "Line 1\nLine 2\nLine 3";
            var testFile = CreateTestFile("TestFile.cs", fileContent);

            // Act
            var result = await _fileContextService.GetFileContentRangeAsync(testFile, 2, 10);

            // Assert
            result.Should().Be("Line 2\nLine 3");
        }

        [Fact]
        public async Task GetFileContentRangeAsync_WithSingleLine_ReturnsSingleLine()
        {
            // Arrange
            var fileContent = "Line 1\nLine 2\nLine 3";
            var testFile = CreateTestFile("TestFile.cs", fileContent);

            // Act
            var result = await _fileContextService.GetFileContentRangeAsync(testFile, 2, 2);

            // Assert
            result.Should().Be("Line 2");
        }

        #endregion

        #region GetGitBranchesAsync Tests

        [Fact]
        public async Task GetGitBranchesAsync_WithNoSolution_ReturnsEmptyCollection()
        {
            // Arrange
            _mockDte.Setup(x => x.Solution).Returns((Solution)null);

            // Act
            var branches = await _fileContextService.GetGitBranchesAsync();

            // Assert
            branches.Should().NotBeNull();
            branches.Should().BeEmpty();
        }

        [Fact]
        public async Task GetGitBranchesAsync_WithNoGitRepository_ReturnsEmptyCollection()
        {
            // Arrange
            var solutionPath = Path.Combine(_tempDirectory, "TestSolution.sln");
            File.WriteAllText(solutionPath, "Microsoft Visual Studio Solution File");
            _mockSolution.Setup(x => x.FullName).Returns(solutionPath);

            // Act
            var branches = await _fileContextService.GetGitBranchesAsync();

            // Assert
            branches.Should().NotBeNull();
            branches.Should().BeEmpty();
        }

        // Note: Testing actual Git operations would require LibGit2Sharp and a real Git repository
        // For comprehensive testing, we would need to create a test Git repository
        // This is beyond the scope of unit tests and would be better suited for integration tests

        #endregion

        #region GetGitStatusAsync Tests

        [Fact]
        public async Task GetGitStatusAsync_WithNoSolution_ReturnsEmptyStatus()
        {
            // Arrange
            _mockDte.Setup(x => x.Solution).Returns((Solution)null);

            // Act
            var status = await _fileContextService.GetGitStatusAsync();

            // Assert
            status.Should().NotBeNull();
            status.CurrentBranch.Should().BeNull();
            status.HasUncommittedChanges.Should().BeFalse();
            status.ModifiedFiles.Should().BeEmpty();
            status.AddedFiles.Should().BeEmpty();
            status.DeletedFiles.Should().BeEmpty();
            status.UntrackedFiles.Should().BeEmpty();
        }

        #endregion

        #region GetClipboardContent Tests

        [Fact]
        public void GetClipboardContent_ReturnsStringOrEmpty()
        {
            // Act
            var content = _fileContextService.GetClipboardContent();

            // Assert
            content.Should().NotBeNull();
            // Content can be empty or contain actual clipboard data
        }

        #endregion

        #region GetSelectedTextAsync Tests

        [Fact]
        public async Task GetSelectedTextAsync_WithNoActiveView_ReturnsEmptySelectedText()
        {
            // Act
            var selectedText = await _fileContextService.GetSelectedTextAsync();

            // Assert
            selectedText.Should().NotBeNull();
            selectedText.Text.Should().BeEmpty();
            selectedText.FilePath.Should().BeNull();
            selectedText.StartLine.Should().Be(0);
            selectedText.EndLine.Should().Be(0);
        }

        // Note: Testing actual text selection would require Visual Studio text manager mocking
        // This is complex and would be better suited for integration tests

        #endregion

        #region GetProjectsAsync Tests

        [Fact]
        public async Task GetProjectsAsync_WithNullSolution_ReturnsEmptyCollection()
        {
            // Arrange
            _mockDte.Setup(x => x.Solution).Returns((Solution)null);

            // Act
            var projects = await _fileContextService.GetProjectsAsync();

            // Assert
            projects.Should().NotBeNull();
            projects.Should().BeEmpty();
        }

        [Fact]
        public async Task GetProjectsAsync_WithNullProjects_ReturnsEmptyCollection()
        {
            // Arrange
            _mockSolution.Setup(x => x.Projects).Returns((Projects)null);

            // Act
            var projects = await _fileContextService.GetProjectsAsync();

            // Assert
            projects.Should().NotBeNull();
            projects.Should().BeEmpty();
        }

        [Fact]
        public async Task GetProjectsAsync_WithValidProjects_ReturnsProjectInfo()
        {
            // Arrange
            var testFile = CreateTestFile("TestFile.cs", "class Test { }");
            var mockProject = CreateMockProject("TestProject", new[] { testFile });
            SetupMockProjectsCollection(new[] { mockProject });

            // Act
            var projects = await _fileContextService.GetProjectsAsync();

            // Assert
            projects.Should().HaveCount(1);
            
            var project = projects.First();
            project.Name.Should().Be("TestProject");
            project.FilePath.Should().Be("TestProject.csproj");
            project.ProjectType.Should().Be("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            project.Files.Should().HaveCount(1);
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockDte()
        {
            _mockDte.Setup(x => x.Solution).Returns(_mockSolution.Object);
            _mockSolution.Setup(x => x.Projects).Returns(_mockProjects.Object);
            _mockSolution.Setup(x => x.FullName).Returns(Path.Combine(_tempDirectory, "TestSolution.sln"));
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_tempDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private Mock<Project> CreateMockProject(string projectName, string[] filePaths)
        {
            var mockProject = new Mock<Project>();
            var mockProjectItems = new Mock<ProjectItems>();
            
            mockProject.Setup(x => x.Name).Returns(projectName);
            mockProject.Setup(x => x.FullName).Returns($"{projectName}.csproj");
            mockProject.Setup(x => x.Kind).Returns("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"); // C# project GUID
            mockProject.Setup(x => x.ProjectItems).Returns(mockProjectItems.Object);

            // Setup project items
            var projectItemsList = new List<Mock<ProjectItem>>();
            foreach (var filePath in filePaths)
            {
                var mockProjectItem = new Mock<ProjectItem>();
                mockProjectItem.Setup(x => x.Name).Returns(Path.GetFileName(filePath));
                mockProjectItem.Setup(x => x.FileCount).Returns((short)1);
                mockProjectItem.Setup(x => x.FileNames[1]).Returns(filePath);
                mockProjectItem.Setup(x => x.ProjectItems).Returns((ProjectItems)null);
                
                projectItemsList.Add(mockProjectItem);
            }

            // Setup enumeration for ProjectItems
            var projectItemsEnumerator = projectItemsList.Select(x => x.Object).GetEnumerator();
            mockProjectItems.Setup(x => x.GetEnumerator()).Returns(projectItemsEnumerator);

            return mockProject;
        }

        private void SetupMockProjectsCollection(Mock<Project>[] mockProjects)
        {
            var projectsList = mockProjects.Select(x => x.Object).ToList();
            var projectsEnumerator = projectsList.GetEnumerator();
            
            _mockProjects.Setup(x => x.GetEnumerator()).Returns(projectsEnumerator);
        }

        #endregion
    }
}