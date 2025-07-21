using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiderVSExtension.Exceptions;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AiderVSExtension.Tests.Services
{
    public class AiderServiceTests : IDisposable
    {
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly AiderService _aiderService;

        public AiderServiceTests()
        {
            _mockErrorHandler = new Mock<IErrorHandler>();
            _mockConfigService = new Mock<IConfigurationService>();
            
            SetupMockServices();
            
            _aiderService = new AiderService(_mockErrorHandler.Object, _mockConfigService.Object);
        }

        public void Dispose()
        {
            _aiderService?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullErrorHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AiderService(null, _mockConfigService.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AiderService(_mockErrorHandler.Object, null));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new AiderService(_mockErrorHandler.Object, _mockConfigService.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsConnected.Should().BeFalse();
        }

        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_LogsInitializationStart()
        {
            // Act
            try
            {
                await _aiderService.InitializeAsync();
            }
            catch
            {
                // Expected to fail due to connection issues in test environment
            }

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                "Initializing Aider service", 
                "AiderService.InitializeAsync"), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenConnectionFails_ThrowsAiderServiceException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<AiderServiceException>(() => 
                _aiderService.InitializeAsync());
            
            exception.Message.Should().Be("Failed to initialize Aider service");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task InitializeAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.InitializeAsync());
        }

        #endregion

        #region SendMessageAsync (String) Tests

        [Fact]
        public async Task SendMessageAsync_WithNullMessage_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _aiderService.SendMessageAsync((string)null));
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyMessage_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _aiderService.SendMessageAsync(""));
        }

        [Fact]
        public async Task SendMessageAsync_WithWhitespaceMessage_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _aiderService.SendMessageAsync("   "));
        }

        [Fact]
        public async Task SendMessageAsync_WithValidMessage_LogsMessageQueued()
        {
            // Arrange
            var message = "Test message for Aider";

            // Act
            await _aiderService.SendMessageAsync(message);

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.StartsWith("Message queued for sending: Test message")), 
                "AiderService.SendMessageAsync"), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithFileReferences_CreatesMessageWithReferences()
        {
            // Arrange
            var message = "Analyze this file";
            var fileReferences = new List<FileReference>
            {
                new FileReference
                {
                    FilePath = "test.cs",
                    StartLine = 1,
                    EndLine = 10,
                    Type = ReferenceType.File
                }
            };

            // Act
            await _aiderService.SendMessageAsync(message, fileReferences);

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.StartsWith("Message queued for sending: Analyze this file")), 
                "AiderService.SendMessageAsync"), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.SendMessageAsync("test message"));
        }

        #endregion

        #region SendMessageAsync (ChatMessage) Tests

        [Fact]
        public async Task SendMessageAsync_WithNullChatMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _aiderService.SendMessageAsync((ChatMessage)null));
        }

        [Fact]
        public async Task SendMessageAsync_WithInvalidChatMessage_ThrowsArgumentException()
        {
            // Arrange
            var invalidMessage = new ChatMessage
            {
                Content = "", // Invalid - empty content
                Type = MessageType.User
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _aiderService.SendMessageAsync(invalidMessage));
            
            exception.Message.Should().StartWith("Invalid message:");
        }

        [Fact]
        public async Task SendMessageAsync_WithValidChatMessage_ReturnsTimeoutResponse()
        {
            // Arrange
            var validMessage = new ChatMessage
            {
                Content = "Hello Aider",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            // Act
            var response = await _aiderService.SendMessageAsync(validMessage);

            // Assert
            response.Should().NotBeNull();
            response.Type.Should().Be(MessageType.System);
            response.Content.Should().Contain("Request timed out");
        }

        [Fact]
        public async Task SendMessageAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var message = new ChatMessage { Content = "test", Type = MessageType.User };
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.SendMessageAsync(message));
        }

        #endregion

        #region GetChatHistoryAsync Tests

        [Fact]
        public async Task GetChatHistoryAsync_InitiallyEmpty_ReturnsEmptyCollection()
        {
            // Act
            var history = await _aiderService.GetChatHistoryAsync();

            // Assert
            history.Should().NotBeNull();
            history.Should().BeEmpty();
        }

        [Fact]
        public async Task GetChatHistoryAsync_AfterSendingMessage_ContainsMessage()
        {
            // Arrange
            var message = "Test message for history";
            await _aiderService.SendMessageAsync(message);

            // Act
            var history = await _aiderService.GetChatHistoryAsync();

            // Assert
            history.Should().NotBeNull();
            history.Should().HaveCount(1);
            history.First().Content.Should().Be(message);
            history.First().Type.Should().Be(MessageType.User);
        }

        [Fact]
        public async Task GetChatHistoryAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.GetChatHistoryAsync());
        }

        #endregion

        #region ClearChatHistoryAsync Tests

        [Fact]
        public async Task ClearChatHistoryAsync_ClearsHistory()
        {
            // Arrange
            await _aiderService.SendMessageAsync("Test message 1");
            await _aiderService.SendMessageAsync("Test message 2");
            
            var historyBeforeClear = await _aiderService.GetChatHistoryAsync();
            historyBeforeClear.Should().HaveCount(2);

            // Act
            await _aiderService.ClearChatHistoryAsync();

            // Assert
            var historyAfterClear = await _aiderService.GetChatHistoryAsync();
            historyAfterClear.Should().BeEmpty();
        }

        [Fact]
        public async Task ClearChatHistoryAsync_LogsClearOperation()
        {
            // Act
            await _aiderService.ClearChatHistoryAsync();

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                "Chat history cleared", 
                "AiderService.ClearChatHistoryAsync"), Times.Once);
        }

        [Fact]
        public async Task ClearChatHistoryAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.ClearChatHistoryAsync());
        }

        #endregion

        #region SaveConversationAsync Tests

        [Fact]
        public async Task SaveConversationAsync_LogsSaveOperation()
        {
            // Act
            await _aiderService.SaveConversationAsync();

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.StartsWith("Conversation saved to")), 
                "AiderService.SaveConversationAsync"), Times.Once);
        }

        [Fact]
        public async Task SaveConversationAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.SaveConversationAsync());
        }

        #endregion

        #region LoadConversationAsync Tests

        [Fact]
        public async Task LoadConversationAsync_WhenNoSavedConversation_LogsNoConversationFound()
        {
            // Act
            await _aiderService.LoadConversationAsync();

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                "No saved conversation found", 
                "AiderService.LoadConversationAsync"), Times.Once);
        }

        [Fact]
        public async Task LoadConversationAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.LoadConversationAsync());
        }

        #endregion

        #region ArchiveConversationAsync Tests

        [Fact]
        public async Task ArchiveConversationAsync_WithoutArchiveName_UsesDefaultName()
        {
            // Act
            await _aiderService.ArchiveConversationAsync();

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.StartsWith("Conversation archived to") && s.Contains("conversation_archive_")), 
                "AiderService.ArchiveConversationAsync"), Times.Once);
        }

        [Fact]
        public async Task ArchiveConversationAsync_WithCustomArchiveName_UsesCustomName()
        {
            // Arrange
            var customName = "my_custom_archive";

            // Act
            await _aiderService.ArchiveConversationAsync(customName);

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                It.Is<string>(s => s.StartsWith("Conversation archived to") && s.Contains(customName)), 
                "AiderService.ArchiveConversationAsync"), Times.Once);
        }

        [Fact]
        public async Task ArchiveConversationAsync_ClearsCurrentHistory()
        {
            // Arrange
            await _aiderService.SendMessageAsync("Test message before archive");
            var historyBeforeArchive = await _aiderService.GetChatHistoryAsync();
            historyBeforeArchive.Should().HaveCount(1);

            // Act
            await _aiderService.ArchiveConversationAsync();

            // Assert
            var historyAfterArchive = await _aiderService.GetChatHistoryAsync();
            historyAfterArchive.Should().BeEmpty();
        }

        [Fact]
        public async Task ArchiveConversationAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _aiderService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _aiderService.ArchiveConversationAsync());
        }

        #endregion

        #region Event Tests

        [Fact]
        public void MessageReceived_EventCanBeSubscribed()
        {
            // Arrange
            MessageReceivedEventArgs receivedArgs = null;
            _aiderService.MessageReceived += (sender, args) => receivedArgs = args;

            // Act
            // Simulate message received (this would normally come from WebSocket)
            // For testing, we'll just verify the event can be subscribed to
            
            // Assert
            receivedArgs.Should().BeNull(); // No message received yet
        }

        [Fact]
        public void ConnectionStatusChanged_EventCanBeSubscribed()
        {
            // Arrange
            ConnectionStatusChangedEventArgs receivedArgs = null;
            _aiderService.ConnectionStatusChanged += (sender, args) => receivedArgs = args;

            // Act
            // Simulate connection status change (this would normally come from connection logic)
            // For testing, we'll just verify the event can be subscribed to
            
            // Assert
            receivedArgs.Should().BeNull(); // No status change yet
        }

        #endregion

        #region IsConnected Tests

        [Fact]
        public void IsConnected_InitiallyFalse()
        {
            // Act & Assert
            _aiderService.IsConnected.Should().BeFalse();
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert - Should not throw
            _aiderService.Dispose();
            _aiderService.Dispose();
        }

        [Fact]
        public void Dispose_LogsDisposalOperation()
        {
            // Act
            _aiderService.Dispose();

            // Assert
            _mockErrorHandler.Verify(x => x.LogInfoAsync(
                "Aider service disposed", 
                "AiderService.Dispose"), Times.Once);
        }

        #endregion

        #region Private Helper Methods

        private void SetupMockServices()
        {
            // Setup error handler
            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup configuration service
            _mockConfigService.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((key, defaultValue) => defaultValue);
            
            _mockConfigService.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<int>()))
                .Returns<string, int>((key, defaultValue) => defaultValue);
        }

        #endregion
    }
}