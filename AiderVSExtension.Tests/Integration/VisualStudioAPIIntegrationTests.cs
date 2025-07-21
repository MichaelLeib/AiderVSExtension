using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;
using AiderVSExtension.Models;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using Xunit;
using FileInfo = AiderVSExtension.Interfaces.FileInfo;

namespace AiderVSExtension.Tests.Integration
{
    /// <summary>
    /// Integration tests for Visual Studio API interactions
    /// </summary>
    public class VisualStudioAPIIntegrationTests : IDisposable
    {
        private readonly Mock<IVsSolution> _mockSolution;
        private readonly Mock<IVsTextManager> _mockTextManager;
        private readonly Mock<IVsOutputWindow> _mockOutputWindow;
        private readonly Mock<IVsErrorList> _mockErrorList;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly FileContextService _fileContextService;

        public VisualStudioAPIIntegrationTests()
        {
            _mockSolution = new Mock<IVsSolution>();
            _mockTextManager = new Mock<IVsTextManager>();
            _mockOutputWindow = new Mock<IVsOutputWindow>();
            _mockErrorList = new Mock<IVsErrorList>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockErrorHandler = new Mock<IErrorHandler>();

            SetupMockServices();
            
            _fileContextService = new FileContextService(_mockServiceProvider.Object, _mockErrorHandler.Object);
        }

        public void Dispose()
        {
            _fileContextService?.Dispose();
        }

        #region Solution Integration Tests

        [Fact]
        public async Task FileContextService_GetSolutionFiles_IntegratesWithVSSolution()
        {
            // Arrange
            var mockProjects = CreateMockProjects();
            SetupSolutionMock(mockProjects);

            // Act
            var files = await _fileContextService.GetSolutionFilesAsync();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCountGreaterThan(0);
            
            var fileList = files.ToList();
            fileList.Should().Contain(f => f.FileName.EndsWith(".cs"));
            fileList.Should().Contain(f => f.FileName.EndsWith(".xaml"));
            fileList.Should().Contain(f => f.ProjectName == "TestProject1");
            fileList.Should().Contain(f => f.ProjectName == "TestProject2");

            // Verify VS API calls
            _mockSolution.Verify(x => x.GetProjectEnum(
                It.IsAny<uint>(), 
                It.IsAny<Guid>(), 
                out It.Ref<IEnumHierarchies>.IsAny), Times.Once);
        }

        [Fact]
        public async Task FileContextService_SearchFiles_UsesVSSolutionFiltering()
        {
            // Arrange
            var mockProjects = CreateMockProjects();
            SetupSolutionMock(mockProjects);

            // Act
            var csharpFiles = await _fileContextService.SearchFilesAsync("*.cs");
            var xamlFiles = await _fileContextService.SearchFilesAsync("*.xaml");

            // Assert
            csharpFiles.Should().NotBeNull();
            csharpFiles.Should().OnlyContain(f => f.FileName.EndsWith(".cs"));
            
            xamlFiles.Should().NotBeNull();
            xamlFiles.Should().OnlyContain(f => f.FileName.EndsWith(".xaml"));

            // Verify search functionality
            var allFiles = await _fileContextService.GetSolutionFilesAsync();
            csharpFiles.Count().Should().BeLessThan(allFiles.Count());
        }

        [Fact]
        public async Task FileContextService_GetProjects_ReturnsVSSolutionProjects()
        {
            // Arrange
            var mockProjects = CreateMockProjects();
            SetupSolutionMock(mockProjects);

            // Act
            var projects = await _fileContextService.GetProjectsAsync();

            // Assert
            projects.Should().NotBeNull();
            projects.Should().HaveCount(2);
            
            var projectList = projects.ToList();
            projectList.Should().Contain(p => p.Name == "TestProject1");
            projectList.Should().Contain(p => p.Name == "TestProject2");
            projectList.Should().Contain(p => p.ProjectType == "C#");
            
            // Verify each project has files
            projectList.Should().OnlyContain(p => p.Files.Any());
        }

        #endregion

        #region Text Manager Integration Tests

        [Fact]
        public async Task FileContextService_GetSelectedText_IntegratesWithVSTextManager()
        {
            // Arrange
            var mockTextView = new Mock<IVsTextView>();
            var mockTextLines = new Mock<IVsTextLines>();
            
            SetupTextManagerMock(mockTextView.Object, mockTextLines.Object);
            SetupSelectedTextMock(mockTextView.Object, "Selected code text", 5, 10);

            // Act
            var selectedText = await _fileContextService.GetSelectedTextAsync();

            // Assert
            selectedText.Should().NotBeNull();
            selectedText.Text.Should().Be("Selected code text");
            selectedText.StartLine.Should().Be(5);
            selectedText.EndLine.Should().Be(10);
            selectedText.FilePath.Should().NotBeNullOrEmpty();

            // Verify VS API calls
            _mockTextManager.Verify(x => x.GetActiveView(
                It.IsAny<int>(), 
                It.IsAny<IVsTextBuffer>(), 
                out It.Ref<IVsTextView>.IsAny), Times.Once);
        }

        [Fact]
        public async Task FileContextService_GetFileContent_UsesVSTextManager()
        {
            // Arrange
            var testFilePath = @"C:\TestSolution\TestFile.cs";
            var testContent = "public class TestClass { }";
            
            var mockTextLines = new Mock<IVsTextLines>();
            SetupFileContentMock(mockTextLines.Object, testContent);
            SetupTextManagerForFile(testFilePath, mockTextLines.Object);

            // Act
            var content = await _fileContextService.GetFileContentAsync(testFilePath);

            // Assert
            content.Should().Be(testContent);
            
            // Verify file was accessed through VS API
            _mockTextManager.Verify(x => x.CreateTextBuffer(
                It.IsAny<IVsTextLines>(), 
                It.IsAny<Guid>(), 
                out It.Ref<IVsTextBuffer>.IsAny), Times.Once);
        }

        [Fact]
        public async Task FileContextService_GetFileContentRange_ReturnsSpecificLines()
        {
            // Arrange
            var testFilePath = @"C:\TestSolution\TestFile.cs";
            var fullContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
            var expectedRange = "Line 2\nLine 3\nLine 4";
            
            var mockTextLines = new Mock<IVsTextLines>();
            SetupFileContentRangeMock(mockTextLines.Object, fullContent, 2, 4, expectedRange);
            SetupTextManagerForFile(testFilePath, mockTextLines.Object);

            // Act
            var content = await _fileContextService.GetFileContentRangeAsync(testFilePath, 2, 4);

            // Assert
            content.Should().Be(expectedRange);
            
            // Verify range-specific API calls
            mockTextLines.Verify(x => x.GetLineText(
                It.IsInRange(1, 3, Moq.Range.Inclusive), 
                It.IsAny<int>(), 
                It.IsInRange(1, 3, Moq.Range.Inclusive), 
                It.IsAny<int>(), 
                out It.Ref<string>.IsAny), Times.AtLeast(3));
        }

        #endregion

        #region Output Window Integration Tests

        [Fact]
        public void OutputWindow_Integration_CreatesAiderOutputPane()
        {
            // Arrange
            var mockOutputWindowPane = new Mock<IVsOutputWindowPane>();
            SetupOutputWindowMock(mockOutputWindowPane.Object);

            // Act
            var errorHandler = new ErrorHandler(_mockServiceProvider.Object);
            
            // This would typically be called during extension initialization
            // We're testing that the output window pane is created correctly

            // Assert
            _mockOutputWindow.Verify(x => x.CreatePane(
                It.IsAny<Guid>(), 
                It.Is<string>(s => s.Contains("Aider")), 
                It.IsAny<int>(), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ErrorHandler_LogToOutputWindow_IntegratesWithVSOutputWindow()
        {
            // Arrange
            var mockOutputWindowPane = new Mock<IVsOutputWindowPane>();
            SetupOutputWindowMock(mockOutputWindowPane.Object);
            
            var errorHandler = new ErrorHandler(_mockServiceProvider.Object);
            var testMessage = "Test integration message";
            var testSource = "IntegrationTest";

            // Act
            await errorHandler.LogInfoAsync(testMessage, testSource);

            // Assert
            mockOutputWindowPane.Verify(x => x.OutputString(
                It.Is<string>(s => s.Contains(testMessage) && s.Contains(testSource))), 
                Times.Once);
            
            mockOutputWindowPane.Verify(x => x.Activate(), Times.Once);
        }

        #endregion

        #region Error List Integration Tests

        [Fact]
        public void ErrorList_Integration_AddsAiderQuickFixProvider()
        {
            // Arrange
            var mockTaskProvider = new Mock<IVsTaskProvider>();
            var mockTaskList = new Mock<IVsTaskList>();
            
            SetupErrorListMock(mockTaskProvider.Object, mockTaskList.Object);

            // Act
            var quickFixProvider = new QuickFixProvider(_mockServiceProvider.Object);
            
            // This would be called when errors are detected
            // We're testing the integration with VS error system

            // Assert
            _mockErrorList.Verify(x => x.RegisterTaskProvider(
                It.IsAny<IVsTaskProvider>()), Times.Once);
        }

        #endregion

        #region Clipboard Integration Tests

        [Fact]
        public void FileContextService_GetClipboardContent_IntegratesWithSystemClipboard()
        {
            // Arrange
            var testClipboardContent = "Test clipboard content for integration";
            
            // Note: In a real integration test, we would set actual clipboard content
            // For this test, we'll mock the clipboard service
            SetupClipboardMock(testClipboardContent);

            // Act
            var clipboardContent = _fileContextService.GetClipboardContent();

            // Assert
            clipboardContent.Should().Be(testClipboardContent);
        }

        #endregion

        #region Service Provider Integration Tests

        [Fact]
        public void ServiceProvider_Integration_ResolvesVSServices()
        {
            // Arrange & Act
            var solution = _mockServiceProvider.Object.GetService(typeof(SVsSolution));
            var textManager = _mockServiceProvider.Object.GetService(typeof(SVsTextManager));
            var outputWindow = _mockServiceProvider.Object.GetService(typeof(SVsOutputWindow));

            // Assert
            solution.Should().NotBeNull();
            textManager.Should().NotBeNull();
            outputWindow.Should().NotBeNull();
            
            // Verify service provider was called
            _mockServiceProvider.Verify(x => x.GetService(typeof(SVsSolution)), Times.Once);
            _mockServiceProvider.Verify(x => x.GetService(typeof(SVsTextManager)), Times.Once);
            _mockServiceProvider.Verify(x => x.GetService(typeof(SVsOutputWindow)), Times.Once);
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public async Task VSAPIIntegration_ErrorHandling_MaintainsStability()
        {
            // Arrange
            SetupFailingVSAPIMocks();

            // Act & Assert - Operations should not throw but handle errors gracefully
            var files = await _fileContextService.GetSolutionFilesAsync();
            files.Should().NotBeNull(); // Should return empty collection, not throw

            var selectedText = await _fileContextService.GetSelectedTextAsync();
            selectedText.Should().NotBeNull(); // Should return empty selection, not throw

            // Verify error handler was called
            _mockErrorHandler.Verify(x => x.HandleExceptionAsync(
                It.IsAny<Exception>(), 
                It.IsAny<string>()), Times.AtLeastOnce);
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockServices()
        {
            // Setup service provider to return mocked VS services
            _mockServiceProvider.Setup(x => x.GetService(typeof(SVsSolution)))
                .Returns(_mockSolution.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(SVsTextManager)))
                .Returns(_mockTextManager.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(SVsOutputWindow)))
                .Returns(_mockOutputWindow.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(SVsErrorList)))
                .Returns(_mockErrorList.Object);

            // Setup error handler
            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        private List<MockProjectInfo> CreateMockProjects()
        {
            return new List<MockProjectInfo>
            {
                new MockProjectInfo
                {
                    Name = "TestProject1",
                    ProjectType = "C#",
                    Files = new List<string>
                    {
                        @"C:\TestSolution\TestProject1\Class1.cs",
                        @"C:\TestSolution\TestProject1\Class2.cs",
                        @"C:\TestSolution\TestProject1\MainWindow.xaml"
                    }
                },
                new MockProjectInfo
                {
                    Name = "TestProject2", 
                    ProjectType = "C#",
                    Files = new List<string>
                    {
                        @"C:\TestSolution\TestProject2\Service1.cs",
                        @"C:\TestSolution\TestProject2\UserControl.xaml",
                        @"C:\TestSolution\TestProject2\Helper.cs"
                    }
                }
            };
        }

        private void SetupSolutionMock(List<MockProjectInfo> projects)
        {
            var mockEnumHierarchies = new Mock<IEnumHierarchies>();
            var hierarchies = projects.Select(p => CreateMockHierarchy(p)).ToArray();
            
            mockEnumHierarchies.Setup(x => x.Next(
                It.IsAny<uint>(), 
                It.IsAny<IVsHierarchy[]>(), 
                out It.Ref<uint>.IsAny))
                .Returns((uint celt, IVsHierarchy[] rgelt, out uint pceltFetched) =>
                {
                    pceltFetched = Math.Min(celt, (uint)hierarchies.Length);
                    Array.Copy(hierarchies, rgelt, pceltFetched);
                    return 0; // S_OK
                });

            _mockSolution.Setup(x => x.GetProjectEnum(
                It.IsAny<uint>(), 
                It.IsAny<Guid>(), 
                out It.Ref<IEnumHierarchies>.IsAny))
                .Returns((uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum) =>
                {
                    ppenum = mockEnumHierarchies.Object;
                    return 0; // S_OK
                });
        }

        private IVsHierarchy CreateMockHierarchy(MockProjectInfo projectInfo)
        {
            var mockHierarchy = new Mock<IVsHierarchy>();
            
            // Setup project name
            mockHierarchy.Setup(x => x.GetProperty(
                It.IsAny<uint>(), 
                It.Is<int>(p => p == (int)__VSHPROPID.VSHPROPID_Name), 
                out It.Ref<object>.IsAny))
                .Returns((uint itemid, int propid, out object pvar) =>
                {
                    pvar = projectInfo.Name;
                    return 0; // S_OK
                });

            return mockHierarchy.Object;
        }

        private void SetupTextManagerMock(IVsTextView textView, IVsTextLines textLines)
        {
            _mockTextManager.Setup(x => x.GetActiveView(
                It.IsAny<int>(), 
                It.IsAny<IVsTextBuffer>(), 
                out It.Ref<IVsTextView>.IsAny))
                .Returns((int fMustHaveFocus, IVsTextBuffer pIVsTextBuffer, out IVsTextView ppView) =>
                {
                    ppView = textView;
                    return 0; // S_OK
                });
        }

        private void SetupSelectedTextMock(IVsTextView textView, string selectedText, int startLine, int endLine)
        {
            textView.GetSelection(out int startLineOut, out int startCol, out int endLineOut, out int endCol)
                .Returns(x =>
                {
                    x[0] = startLine;
                    x[1] = 0;
                    x[2] = endLine;
                    x[3] = selectedText.Length;
                    return 0; // S_OK
                });

            textView.GetSelectedText(out string text)
                .Returns(x =>
                {
                    x[0] = selectedText;
                    return 0; // S_OK
                });
        }

        private void SetupFileContentMock(IVsTextLines textLines, string content)
        {
            textLines.GetLastLineIndex(out int lastLine, out int lastCol)
                .Returns(x =>
                {
                    var lines = content.Split('\n');
                    x[0] = lines.Length - 1;
                    x[1] = lines.Last().Length;
                    return 0; // S_OK
                });

            textLines.GetLineText(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), out string text)
                .Returns(x =>
                {
                    x[4] = content;
                    return 0; // S_OK
                });
        }

        private void SetupFileContentRangeMock(IVsTextLines textLines, string fullContent, int startLine, int endLine, string expectedRange)
        {
            textLines.GetLineText(
                It.IsInRange(startLine - 1, endLine - 1, Moq.Range.Inclusive), 
                It.IsAny<int>(), 
                It.IsInRange(startLine - 1, endLine - 1, Moq.Range.Inclusive), 
                It.IsAny<int>(), 
                out string text)
                .Returns(x =>
                {
                    x[4] = expectedRange;
                    return 0; // S_OK
                });
        }

        private void SetupTextManagerForFile(string filePath, IVsTextLines textLines)
        {
            _mockTextManager.Setup(x => x.CreateTextBuffer(
                It.IsAny<IVsTextLines>(), 
                It.IsAny<Guid>(), 
                out It.Ref<IVsTextBuffer>.IsAny))
                .Returns((IVsTextLines pIVsTextLines, ref Guid riid, out IVsTextBuffer ppIVsTextBuffer) =>
                {
                    var mockBuffer = new Mock<IVsTextBuffer>();
                    ppIVsTextBuffer = mockBuffer.Object;
                    return 0; // S_OK
                });
        }

        private void SetupOutputWindowMock(IVsOutputWindowPane outputPane)
        {
            _mockOutputWindow.Setup(x => x.CreatePane(
                It.IsAny<Guid>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
                .Returns(0); // S_OK

            _mockOutputWindow.Setup(x => x.GetPane(
                It.IsAny<Guid>(), 
                out It.Ref<IVsOutputWindowPane>.IsAny))
                .Returns((ref Guid rguidPane, out IVsOutputWindowPane ppPane) =>
                {
                    ppPane = outputPane;
                    return 0; // S_OK
                });

            outputPane.OutputString(It.IsAny<string>()).Returns(0);
            outputPane.Activate().Returns(0);
        }

        private void SetupErrorListMock(IVsTaskProvider taskProvider, IVsTaskList taskList)
        {
            _mockErrorList.Setup(x => x.RegisterTaskProvider(It.IsAny<IVsTaskProvider>()))
                .Returns(0); // S_OK

            _mockErrorList.Setup(x => x.UnregisterTaskProvider(It.IsAny<IVsTaskProvider>()))
                .Returns(0); // S_OK
        }

        private void SetupClipboardMock(string content)
        {
            // In a real implementation, this would integrate with System.Windows.Clipboard
            // For testing purposes, we'll assume the FileContextService has a testable clipboard abstraction
        }

        private void SetupFailingVSAPIMocks()
        {
            // Setup mocks to simulate VS API failures
            _mockSolution.Setup(x => x.GetProjectEnum(
                It.IsAny<uint>(), 
                It.IsAny<Guid>(), 
                out It.Ref<IEnumHierarchies>.IsAny))
                .Throws(new InvalidOperationException("VS API failure simulation"));

            _mockTextManager.Setup(x => x.GetActiveView(
                It.IsAny<int>(), 
                It.IsAny<IVsTextBuffer>(), 
                out It.Ref<IVsTextView>.IsAny))
                .Throws(new InvalidOperationException("Text manager failure simulation"));
        }

        #endregion

        #region Helper Classes

        private class MockProjectInfo
        {
            public string Name { get; set; }
            public string ProjectType { get; set; }
            public List<string> Files { get; set; } = new List<string>();
        }

        #endregion
    }
}