using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;
using FluentAssertions;
using LibGit2Sharp;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Integration
{
    /// <summary>
    /// Integration tests for file system and Git repository access
    /// </summary>
    public class FileSystemIntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _testSolutionPath;
        private readonly string _testGitRepoPath;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly FileContextService _fileContextService;

        public FileSystemIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "FileSystemIntegrationTests", Guid.NewGuid().ToString());
            _testSolutionPath = Path.Combine(_tempDirectory, "TestSolution");
            _testGitRepoPath = Path.Combine(_tempDirectory, "GitRepo");
            
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(_testSolutionPath);
            Directory.CreateDirectory(_testGitRepoPath);

            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            
            SetupMockServices();
            CreateTestFileStructure();
            InitializeTestGitRepository();

            _fileContextService = new FileContextService(_mockServiceProvider.Object, _mockErrorHandler.Object);
        }

        public void Dispose()
        {
            _fileContextService?.Dispose();
            
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region File System Access Tests

        [Fact]
        public async Task FileContextService_GetFileContent_ReadsActualFiles()
        {
            // Arrange
            var testFilePath = Path.Combine(_testSolutionPath, "TestClass.cs");
            var expectedContent = "public class TestClass { }";
            await File.WriteAllTextAsync(testFilePath, expectedContent);

            // Act
            var content = await _fileContextService.GetFileContentAsync(testFilePath);

            // Assert
            content.Should().Be(expectedContent);
        }

        [Fact]
        public async Task FileContextService_GetFileContentRange_ReturnsSpecificLines()
        {
            // Arrange
            var testFilePath = Path.Combine(_testSolutionPath, "MultiLineFile.cs");
            var lines = new[]
            {
                "using System;",
                "namespace TestNamespace",
                "{",
                "    public class TestClass",
                "    {",
                "        public void TestMethod()",
                "        {",
                "            Console.WriteLine(\"Hello World\");",
                "        }",
                "    }",
                "}"
            };
            await File.WriteAllLinesAsync(testFilePath, lines);

            // Act
            var content = await _fileContextService.GetFileContentRangeAsync(testFilePath, 4, 6);

            // Assert
            content.Should().Contain("public class TestClass");
            content.Should().Contain("public void TestMethod()");
            content.Should().NotContain("using System;");
            content.Should().NotContain("Console.WriteLine");
        }

        [Fact]
        public async Task FileContextService_SearchFiles_FindsMatchingFiles()
        {
            // Arrange
            var testFiles = new[]
            {
                Path.Combine(_testSolutionPath, "Class1.cs"),
                Path.Combine(_testSolutionPath, "Class2.cs"),
                Path.Combine(_testSolutionPath, "MainWindow.xaml"),
                Path.Combine(_testSolutionPath, "App.config"),
                Path.Combine(_testSolutionPath, "README.md")
            };

            foreach (var file in testFiles)
            {
                await File.WriteAllTextAsync(file, $"Content of {Path.GetFileName(file)}");
            }

            // Act
            var csharpFiles = await _fileContextService.SearchFilesAsync("*.cs");
            var xamlFiles = await _fileContextService.SearchFilesAsync("*.xaml");
            var allFiles = await _fileContextService.SearchFilesAsync("*.*");

            // Assert
            csharpFiles.Should().HaveCount(2);
            csharpFiles.Should().OnlyContain(f => f.FileName.EndsWith(".cs"));
            
            xamlFiles.Should().HaveCount(1);
            xamlFiles.Should().OnlyContain(f => f.FileName.EndsWith(".xaml"));
            
            allFiles.Should().HaveCount(5);
        }

        [Fact]
        public async Task FileContextService_GetSolutionFiles_ReturnsFileInformation()
        {
            // Arrange
            var testFiles = CreateTestFiles();

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCountGreaterThan(0);
            
            var fileList = files.ToList();
            
            // Verify file information is populated
            fileList.Should().OnlyContain(f => !string.IsNullOrEmpty(f.FileName));
            fileList.Should().OnlyContain(f => !string.IsNullOrEmpty(f.FilePath));
            fileList.Should().OnlyContain(f => f.Size >= 0);
            fileList.Should().OnlyContain(f => f.LastModified != default(DateTime));
        }

        [Fact]
        public async Task FileContextService_LargeFileHandling_PerformsEfficiently()
        {
            // Arrange
            var largeFilePath = Path.Combine(_testSolutionPath, "LargeFile.cs");
            var largeContent = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"// Line {i}"));
            await File.WriteAllTextAsync(largeFilePath, largeContent);

            var startTime = DateTime.UtcNow;

            // Act
            var content = await _fileContextService.GetFileContentAsync(largeFilePath);
            var rangeContent = await _fileContextService.GetFileContentRangeAsync(largeFilePath, 5000, 5010);

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Assert
            content.Should().NotBeNullOrEmpty();
            content.Should().Contain("Line 1");
            content.Should().Contain("Line 10000");
            
            rangeContent.Should().NotBeNullOrEmpty();
            rangeContent.Should().Contain("Line 5000");
            rangeContent.Should().Contain("Line 5010");
            rangeContent.Should().NotContain("Line 1");
            rangeContent.Should().NotContain("Line 10000");

            // Performance assertion - should complete within reasonable time
            totalTime.Should().BeLessThan(TimeSpan.FromSeconds(5));
        }

        #endregion

        #region Git Integration Tests

        [Fact]
        public async Task FileContextService_GetGitBranches_ReturnsRepositoryBranches()
        {
            // Arrange
            CreateGitBranches();

            // Act
            var branches = await _fileContextService.GetGitBranchesAsync();

            // Assert
            branches.Should().NotBeNull();
            branches.Should().HaveCountGreaterThan(0);
            
            var branchList = branches.ToList();
            branchList.Should().Contain(b => b.Name == "main" || b.Name == "master");
            branchList.Should().Contain(b => b.IsCurrentBranch);
            
            // Verify branch information is populated
            branchList.Should().OnlyContain(b => !string.IsNullOrEmpty(b.Name));
            branchList.Should().OnlyContain(b => !string.IsNullOrEmpty(b.LastCommitHash));
            branchList.Should().OnlyContain(b => b.LastCommitDate != default(DateTime));
        }

        [Fact]
        public async Task FileContextService_GetGitStatus_ReturnsRepositoryStatus()
        {
            // Arrange
            CreateGitChanges();

            // Act
            var status = await _fileContextService.GetGitStatusAsync();

            // Assert
            status.Should().NotBeNull();
            status.CurrentBranch.Should().NotBeNullOrEmpty();
            status.HasUncommittedChanges.Should().BeTrue();
            
            // Should detect the changes we made
            status.ModifiedFiles.Should().HaveCountGreaterThan(0);
            status.UntrackedFiles.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task FileContextService_GitOperations_HandleRepositoryErrors()
        {
            // Arrange
            var nonGitDirectory = Path.Combine(_tempDirectory, "NonGitDirectory");
            Directory.CreateDirectory(nonGitDirectory);
            
            // Temporarily change the working directory context
            var originalPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = nonGitDirectory;

            try
            {
                // Act
                var branches = await _fileContextService.GetGitBranchesAsync();
                var status = await _fileContextService.GetGitStatusAsync();

                // Assert - Should handle gracefully when not in a Git repository
                branches.Should().NotBeNull();
                branches.Should().BeEmpty();
                
                status.Should().NotBeNull();
                status.CurrentBranch.Should().BeNullOrEmpty();
                status.HasUncommittedChanges.Should().BeFalse();

                // Verify error handler was called
                _mockErrorHandler.Verify(x => x.LogWarningAsync(
                    It.Is<string>(s => s.Contains("Git")), 
                    It.IsAny<string>()), Times.AtLeastOnce);
            }
            finally
            {
                Environment.CurrentDirectory = originalPath;
            }
        }

        [Fact]
        public async Task FileContextService_GitBranches_IncludeRemoteBranches()
        {
            // Arrange
            CreateRemoteGitBranches();

            // Act
            var branches = await _fileContextService.GetGitBranchesAsync();

            // Assert
            branches.Should().NotBeNull();
            
            var branchList = branches.ToList();
            branchList.Should().Contain(b => !b.IsRemote); // Local branches
            branchList.Should().Contain(b => b.IsRemote);  // Remote branches
            
            var remoteBranches = branchList.Where(b => b.IsRemote).ToList();
            remoteBranches.Should().HaveCountGreaterThan(0);
            remoteBranches.Should().OnlyContain(b => b.Name.Contains("origin/") || b.Name.Contains("remote/"));
        }

        #endregion

        #region File System Monitoring Tests

        [Fact]
        public async Task FileContextService_FileSystemChanges_DetectsModifications()
        {
            // Arrange
            var testFilePath = Path.Combine(_testSolutionPath, "MonitoredFile.cs");
            await File.WriteAllTextAsync(testFilePath, "Original content");

            var originalFiles = await _fileContextService.GetSolutionFilesAsync();
            var originalFile = originalFiles.FirstOrDefault(f => f.FileName == "MonitoredFile.cs");

            // Act - Modify the file
            await Task.Delay(1000); // Ensure timestamp difference
            await File.WriteAllTextAsync(testFilePath, "Modified content");

            var updatedFiles = await _fileContextService.GetSolutionFilesAsync();
            var updatedFile = updatedFiles.FirstOrDefault(f => f.FileName == "MonitoredFile.cs");

            // Assert
            originalFile.Should().NotBeNull();
            updatedFile.Should().NotBeNull();
            updatedFile.LastModified.Should().BeAfter(originalFile.LastModified);
            updatedFile.Size.Should().NotBe(originalFile.Size);
        }

        [Fact]
        public async Task FileContextService_DirectoryStructure_HandlesNestedFolders()
        {
            // Arrange
            var nestedStructure = new[]
            {
                Path.Combine(_testSolutionPath, "Models", "User.cs"),
                Path.Combine(_testSolutionPath, "Models", "Product.cs"),
                Path.Combine(_testSolutionPath, "Services", "UserService.cs"),
                Path.Combine(_testSolutionPath, "Services", "Data", "Repository.cs"),
                Path.Combine(_testSolutionPath, "Views", "MainWindow.xaml"),
                Path.Combine(_testSolutionPath, "Views", "Controls", "UserControl.xaml")
            };

            foreach (var filePath in nestedStructure)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await File.WriteAllTextAsync(filePath, $"Content of {Path.GetFileName(filePath)}");
            }

            // Act
            var allFiles = await _fileContextService.GetSolutionFilesAsync();
            var modelFiles = await _fileContextService.SearchFilesAsync("Models\\*.cs");
            var serviceFiles = await _fileContextService.SearchFilesAsync("Services\\**\\*.cs");

            // Assert
            allFiles.Should().HaveCountGreaterOrEqualTo(6);
            
            modelFiles.Should().HaveCount(2);
            modelFiles.Should().OnlyContain(f => f.FilePath.Contains("Models"));
            
            serviceFiles.Should().HaveCount(2);
            serviceFiles.Should().OnlyContain(f => f.FilePath.Contains("Services"));
            serviceFiles.Should().Contain(f => f.FilePath.Contains("Data"));
        }

        #endregion

        #region Performance and Scalability Tests

        [Fact]
        public async Task FileContextService_LargeSolution_PerformsEfficiently()
        {
            // Arrange
            const int fileCount = 1000;
            var testFiles = new List<string>();
            
            for (int i = 0; i < fileCount; i++)
            {
                var fileName = $"GeneratedFile{i:D4}.cs";
                var filePath = Path.Combine(_testSolutionPath, fileName);
                var content = $"// Generated file {i}\npublic class GeneratedClass{i} {{ }}";
                
                await File.WriteAllTextAsync(filePath, content);
                testFiles.Add(filePath);
            }

            var startTime = DateTime.UtcNow;

            // Act
            var allFiles = await _fileContextService.GetSolutionFilesAsync();
            var searchResults = await _fileContextService.SearchFilesAsync("Generated*.cs");

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Assert
            allFiles.Should().HaveCountGreaterOrEqualTo(fileCount);
            searchResults.Should().HaveCount(fileCount);
            
            // Performance assertion - should handle large solutions efficiently
            totalTime.Should().BeLessThan(TimeSpan.FromSeconds(10));
            
            // Memory usage should be reasonable
            GC.Collect();
            var memoryAfter = GC.GetTotalMemory(false);
            memoryAfter.Should().BeLessThan(100 * 1024 * 1024); // Less than 100MB
        }

        [Fact]
        public async Task FileContextService_ConcurrentAccess_MaintainsThreadSafety()
        {
            // Arrange
            const int concurrentOperations = 20;
            var tasks = new List<Task>();

            // Act - Perform concurrent file operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Different types of operations
                    switch (index % 4)
                    {
                        case 0:
                            await _fileContextService.GetSolutionFilesAsync();
                            break;
                        case 1:
                            await _fileContextService.SearchFilesAsync("*.cs");
                            break;
                        case 2:
                            await _fileContextService.GetGitBranchesAsync();
                            break;
                        case 3:
                            await _fileContextService.GetGitStatusAsync();
                            break;
                    }
                }));
            }

            // Assert - All operations should complete without exceptions
            await Task.WhenAll(tasks);
            
            // Verify no exceptions were thrown
            tasks.Should().OnlyContain(t => t.IsCompletedSuccessfully);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task FileContextService_FileAccessErrors_HandledGracefully()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testSolutionPath, "NonExistentFile.cs");
            var lockedFilePath = Path.Combine(_testSolutionPath, "LockedFile.cs");
            
            // Create and lock a file
            await File.WriteAllTextAsync(lockedFilePath, "Locked content");
            using var fileStream = File.Open(lockedFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            // Act & Assert - Should not throw exceptions
            var nonExistentContent = await _fileContextService.GetFileContentAsync(nonExistentFile);
            var lockedContent = await _fileContextService.GetFileContentAsync(lockedFilePath);

            nonExistentContent.Should().BeNullOrEmpty();
            lockedContent.Should().BeNullOrEmpty();

            // Verify error handler was called
            _mockErrorHandler.Verify(x => x.HandleExceptionAsync(
                It.IsAny<Exception>(), 
                It.IsAny<string>()), Times.AtLeastOnce);
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockServices()
        {
            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private void CreateTestFileStructure()
        {
            var testFiles = new[]
            {
                Path.Combine(_testSolutionPath, "Program.cs"),
                Path.Combine(_testSolutionPath, "MainWindow.xaml"),
                Path.Combine(_testSolutionPath, "App.config"),
                Path.Combine(_testSolutionPath, "README.md")
            };

            foreach (var file in testFiles)
            {
                File.WriteAllText(file, $"Content of {Path.GetFileName(file)}");
            }
        }

        private List<string> CreateTestFiles()
        {
            var files = new List<string>();
            var testFiles = new[]
            {
                "TestClass1.cs",
                "TestClass2.cs", 
                "Interface1.cs",
                "MainWindow.xaml",
                "UserControl.xaml",
                "App.config",
                "packages.config",
                "README.md"
            };

            foreach (var fileName in testFiles)
            {
                var filePath = Path.Combine(_testSolutionPath, fileName);
                File.WriteAllText(filePath, $"// Content of {fileName}\npublic class {Path.GetFileNameWithoutExtension(fileName)} {{ }}");
                files.Add(filePath);
            }

            return files;
        }

        private void InitializeTestGitRepository()
        {
            Repository.Init(_testGitRepoPath);
            
            using var repo = new Repository(_testGitRepoPath);
            
            // Create initial commit
            var testFile = Path.Combine(_testGitRepoPath, "initial.txt");
            File.WriteAllText(testFile, "Initial commit content");
            
            Commands.Stage(repo, "*");
            
            var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", signature, signature);
        }

        private void CreateGitBranches()
        {
            using var repo = new Repository(_testGitRepoPath);
            
            // Create feature branch
            var featureBranch = repo.CreateBranch("feature/test-feature");
            
            // Create development branch
            var devBranch = repo.CreateBranch("development");
            
            // Switch to feature branch and make a commit
            Commands.Checkout(repo, featureBranch);
            
            var featureFile = Path.Combine(_testGitRepoPath, "feature.txt");
            File.WriteAllText(featureFile, "Feature content");
            
            Commands.Stage(repo, "*");
            var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
            repo.Commit("Add feature", signature, signature);
            
            // Switch back to main/master
            Commands.Checkout(repo, repo.Branches.First(b => b.IsCurrentRepositoryHead == false));
        }

        private void CreateGitChanges()
        {
            using var repo = new Repository(_testGitRepoPath);
            
            // Modify existing file
            var existingFile = Path.Combine(_testGitRepoPath, "initial.txt");
            File.WriteAllText(existingFile, "Modified content");
            
            // Add new untracked file
            var newFile = Path.Combine(_testGitRepoPath, "untracked.txt");
            File.WriteAllText(newFile, "New untracked content");
            
            // Stage one file but not the other
            Commands.Stage(repo, "initial.txt");
        }

        private void CreateRemoteGitBranches()
        {
            using var repo = new Repository(_testGitRepoPath);
            
            // Simulate remote branches by creating them locally
            // In a real scenario, these would come from a remote repository
            var remoteBranch1 = repo.CreateBranch("origin/main");
            var remoteBranch2 = repo.CreateBranch("origin/feature/remote-feature");
        }

        #endregion
    }
}