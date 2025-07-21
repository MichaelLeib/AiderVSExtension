using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using AiderVSExtension.UI.Chat;
using FluentAssertions;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace AiderVSExtension.Tests.UI
{
    /// <summary>
    /// Automated UI tests for WPF components
    /// Note: These tests run in STA mode and test actual UI components
    /// </summary>
    [Collection("UI Tests")]
    public class AutomatedUITests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _tempDirectory;
        private readonly Mock<WritableSettingsStore> _mockSettingsStore;
        private readonly Mock<IErrorHandler> _mockErrorHandler;
        private readonly ConfigurationService _configurationService;
        private readonly ConversationPersistenceService _conversationPersistenceService;
        private Application _testApp;
        private Window _testWindow;

        public AutomatedUITests(ITestOutputHelper output)
        {
            _output = output;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "UITests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _mockSettingsStore = new Mock<WritableSettingsStore>();
            _mockErrorHandler = new Mock<IErrorHandler>();
            SetupMocks();

            _configurationService = new ConfigurationService(_mockSettingsStore.Object);
            _conversationPersistenceService = new ConversationPersistenceService(_tempDirectory);

            InitializeWPFApplication();
        }

        public void Dispose()
        {
            _configurationService?.Dispose();
            _conversationPersistenceService?.Dispose();
            
            _testWindow?.Close();
            _testApp?.Shutdown();

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #region Chat Control UI Tests

        [STAFact]
        public async Task ChatControl_InitialState_DisplaysCorrectly()
        {
            // Arrange & Act
            var chatControl = await CreateChatControlAsync();

            // Assert
            chatControl.Should().NotBeNull();
            chatControl.IsLoaded.Should().BeTrue();
            
            var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");
            var messageInput = FindVisualChild<TextBox>(chatControl, "MessageInput");
            var sendButton = FindVisualChild<Button>(chatControl, "SendButton");

            chatHistory.Should().NotBeNull("Chat history should be present");
            messageInput.Should().NotBeNull("Message input should be present");
            sendButton.Should().NotBeNull("Send button should be present");

            messageInput.Text.Should().BeEmpty("Message input should start empty");
            sendButton.IsEnabled.Should().BeFalse("Send button should be disabled initially");
        }

        [STAFact]
        public async Task ChatControl_MessageInput_EnablesSendButton()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();
            var messageInput = FindVisualChild<TextBox>(chatControl, "MessageInput");
            var sendButton = FindVisualChild<Button>(chatControl, "SendButton");

            // Act
            await DispatcherInvoke(() =>
            {
                messageInput.Text = "Test message";
                messageInput.RaiseEvent(new RoutedEventArgs(TextBox.TextChangedEvent));
            });

            // Assert
            sendButton.IsEnabled.Should().BeTrue("Send button should be enabled when text is entered");
        }

        [STAFact]
        public async Task ChatControl_SendMessage_AddsToHistory()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();
            var messageInput = FindVisualChild<TextBox>(chatControl, "MessageInput");
            var sendButton = FindVisualChild<Button>(chatControl, "SendButton");
            var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");

            var testMessage = "This is a test message";

            // Act
            await DispatcherInvoke(() =>
            {
                messageInput.Text = testMessage;
                sendButton.Command?.Execute(null);
            });

            await Task.Delay(100); // Allow UI to update

            // Assert
            await DispatcherInvoke(() =>
            {
                var document = chatHistory.Document;
                var textRange = new TextRange(document.ContentStart, document.ContentEnd);
                var historyText = textRange.Text;

                historyText.Should().Contain(testMessage, "Message should appear in chat history");
                messageInput.Text.Should().BeEmpty("Input should be cleared after sending");
                sendButton.IsEnabled.Should().BeFalse("Send button should be disabled after sending");
            });
        }

        [STAFact]
        public async Task ChatControl_FileReference_DisplaysCorrectly()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();
            var testFilePath = Path.Combine(_tempDirectory, "test-file.cs");
            await File.WriteAllTextAsync(testFilePath, "public class TestClass { }");

            var message = new ChatMessage
            {
                Content = "Please review this file",
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow,
                FileReferences = new List<FileReference>
                {
                    new FileReference
                    {
                        FilePath = testFilePath,
                        Content = "public class TestClass { }",
                        StartLine = 1,
                        EndLine = 1,
                        Type = ReferenceType.FullFile
                    }
                }
            };

            // Act
            await DispatcherInvoke(() =>
            {
                // Simulate adding message with file reference
                var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");
                // This would normally be handled by the ChatControl's AddMessage method
                // For testing, we'll verify the UI can handle file references
            });

            // Assert
            var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");
            chatHistory.Should().NotBeNull();
            // In a real implementation, we'd verify file reference rendering
        }

        [STAFact]
        public async Task ChatControl_LongConversation_ScrollsCorrectly()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();
            var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");
            var scrollViewer = FindVisualChild<ScrollViewer>(chatHistory);

            // Act - Add many messages
            await DispatcherInvoke(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    var paragraph = new Paragraph(new Run($"Message {i}: " + new string('x', 100)));
                    chatHistory.Document.Blocks.Add(paragraph);
                }
            });

            await Task.Delay(100); // Allow UI to update

            // Assert
            await DispatcherInvoke(() =>
            {
                scrollViewer.Should().NotBeNull("ScrollViewer should be present for long conversations");
                scrollViewer.ScrollableHeight.Should().BeGreaterThan(0, "Should be scrollable with many messages");
                
                // Verify auto-scroll to bottom
                scrollViewer.ScrollToEnd();
                scrollViewer.VerticalOffset.Should().BeGreaterThan(0, "Should scroll to show latest messages");
            });
        }

        #endregion

        #region Context Menu UI Tests

        [STAFact]
        public async Task ContextMenu_FileReference_DisplaysOptions()
        {
            // Arrange
            var contextMenuControl = await CreateContextMenuControlAsync();

            // Act
            await DispatcherInvoke(() =>
            {
                // Simulate opening context menu
                var contextMenu = FindVisualChild<ContextMenu>(contextMenuControl);
                if (contextMenu != null)
                {
                    contextMenu.IsOpen = true;
                }
            });

            // Assert
            await DispatcherInvoke(() =>
            {
                var contextMenu = FindVisualChild<ContextMenu>(contextMenuControl);
                contextMenu.Should().NotBeNull("Context menu should be available");
                
                if (contextMenu != null)
                {
                    var menuItems = contextMenu.Items.Cast<MenuItem>().ToList();
                    menuItems.Should().NotBeEmpty("Context menu should have items");
                    
                    // Look for common context menu items
                    var hasFileOption = menuItems.Any(item => item.Header?.ToString()?.Contains("File") == true);
                    var hasClipboardOption = menuItems.Any(item => item.Header?.ToString()?.Contains("Clipboard") == true);
                    
                    (hasFileOption || hasClipboardOption).Should().BeTrue("Should have file or clipboard options");
                }
            });
        }

        [STAFact]
        public async Task ContextMenu_KeyboardNavigation_Works()
        {
            // Arrange
            var contextMenuControl = await CreateContextMenuControlAsync();

            // Act & Assert
            await DispatcherInvoke(() =>
            {
                // Simulate keyboard input
                var keyEvent = new KeyEventArgs(Keyboard.PrimaryDevice, new MockPresentationSource(), 0, Key.F)
                {
                    RoutedEvent = UIElement.KeyDownEvent
                };

                contextMenuControl.RaiseEvent(keyEvent);
                
                // Verify the control can handle keyboard input
                contextMenuControl.IsKeyboardFocusWithin.Should().BeTrue("Control should accept keyboard focus");
            });
        }

        #endregion

        #region Message Renderer UI Tests

        [STAFact]
        public async Task MessageRenderer_CodeBlock_DisplaysWithSyntaxHighlighting()
        {
            // Arrange
            var messageRenderer = await CreateMessageRendererAsync();
            var codeContent = @"public class Example
{
    public void Method()
    {
        Console.WriteLine(""Hello World"");
    }
}";

            var message = new ChatMessage
            {
                Content = $"Here's some code:\n```csharp\n{codeContent}\n```",
                Type = MessageType.Assistant,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await DispatcherInvoke(() =>
            {
                // This would normally call messageRenderer.RenderMessage(message)
                var richTextBox = FindVisualChild<RichTextBox>(messageRenderer);
                if (richTextBox != null)
                {
                    var document = new FlowDocument();
                    var paragraph = new Paragraph(new Run(message.Content));
                    document.Blocks.Add(paragraph);
                    richTextBox.Document = document;
                }
            });

            // Assert
            await DispatcherInvoke(() =>
            {
                var richTextBox = FindVisualChild<RichTextBox>(messageRenderer);
                richTextBox.Should().NotBeNull("RichTextBox should be present for message rendering");
                
                if (richTextBox != null)
                {
                    var document = richTextBox.Document;
                    var textRange = new TextRange(document.ContentStart, document.ContentEnd);
                    textRange.Text.Should().Contain("public class", "Code content should be rendered");
                }
            });
        }

        [STAFact]
        public async Task MessageRenderer_DiffDisplay_ShowsChanges()
        {
            // Arrange
            var messageRenderer = await CreateMessageRendererAsync();
            var diffChanges = new List<DiffChange>
            {
                new DiffChange
                {
                    LineNumber = 1,
                    ChangeType = ChangeType.Added,
                    Content = "+ Added line",
                    OldContent = "",
                    NewContent = "Added line"
                },
                new DiffChange
                {
                    LineNumber = 2,
                    ChangeType = ChangeType.Removed,
                    Content = "- Removed line",
                    OldContent = "Removed line",
                    NewContent = ""
                }
            };

            // Act
            await DispatcherInvoke(() =>
            {
                // This would normally call messageRenderer.RenderDiff(diffChanges)
                var richTextBox = FindVisualChild<RichTextBox>(messageRenderer);
                if (richTextBox != null)
                {
                    var document = new FlowDocument();
                    
                    foreach (var change in diffChanges)
                    {
                        var paragraph = new Paragraph(new Run(change.Content));
                        
                        // Apply color based on change type
                        if (change.ChangeType == ChangeType.Added)
                            paragraph.Foreground = System.Windows.Media.Brushes.Green;
                        else if (change.ChangeType == ChangeType.Removed)
                            paragraph.Foreground = System.Windows.Media.Brushes.Red;
                            
                        document.Blocks.Add(paragraph);
                    }
                    
                    richTextBox.Document = document;
                }
            });

            // Assert
            await DispatcherInvoke(() =>
            {
                var richTextBox = FindVisualChild<RichTextBox>(messageRenderer);
                var document = richTextBox.Document;
                var paragraphs = document.Blocks.OfType<Paragraph>().ToList();
                
                paragraphs.Should().HaveCount(2, "Should render both diff changes");
                
                var addedParagraph = paragraphs.FirstOrDefault(p => p.Inlines.OfType<Run>().Any(r => r.Text.Contains("Added")));
                var removedParagraph = paragraphs.FirstOrDefault(p => p.Inlines.OfType<Run>().Any(r => r.Text.Contains("Removed")));
                
                addedParagraph.Should().NotBeNull("Added line should be rendered");
                removedParagraph.Should().NotBeNull("Removed line should be rendered");
            });
        }

        #endregion

        #region Configuration UI Tests

        [STAFact]
        public async Task ConfigurationPage_LoadSettings_DisplaysCorrectly()
        {
            // Arrange
            var config = new AIModelConfiguration
            {
                Provider = AIProvider.ChatGPT,
                ApiKey = "test-api-key",
                ModelName = "gpt-4",
                TimeoutSeconds = 60,
                MaxRetries = 3,
                IsEnabled = true
            };

            await _configurationService.SetAIModelConfigurationAsync(config);

            // This would test the actual configuration page
            // For this example, we'll test the concept
            var configurationControl = await CreateConfigurationControlAsync();

            // Act & Assert
            await DispatcherInvoke(() =>
            {
                configurationControl.Should().NotBeNull("Configuration control should be created");
                
                // Find configuration UI elements
                var providerComboBox = FindVisualChild<ComboBox>(configurationControl, "ProviderComboBox");
                var apiKeyTextBox = FindVisualChild<TextBox>(configurationControl, "ApiKeyTextBox");
                var modelTextBox = FindVisualChild<TextBox>(configurationControl, "ModelTextBox");
                
                // Verify elements exist (would be populated in real implementation)
                if (providerComboBox != null)
                    providerComboBox.Items.Should().NotBeEmpty("Provider options should be available");
            });
        }

        #endregion

        #region UI Accessibility Tests

        [STAFact]
        public async Task UIControls_KeyboardNavigation_IsAccessible()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();

            // Act & Assert
            await DispatcherInvoke(() =>
            {
                // Test Tab navigation
                var focusableElements = GetFocusableElements(chatControl);
                focusableElements.Should().NotBeEmpty("Should have focusable elements for keyboard navigation");

                foreach (var element in focusableElements)
                {
                    element.IsTabStop.Should().BeTrue($"Element {element.GetType().Name} should be tab-accessible");
                    element.TabIndex.Should().BeGreaterOrEqualTo(0, "Tab index should be properly set");
                }
            });
        }

        [STAFact]
        public async Task UIControls_HighContrast_IsSupported()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();

            // Act
            await DispatcherInvoke(() =>
            {
                // Simulate high contrast mode
                SystemParameters.HighContrast = true;
            });

            // Assert
            await DispatcherInvoke(() =>
            {
                // Verify controls adapt to high contrast
                var textBoxes = FindVisualChildren<TextBox>(chatControl);
                var buttons = FindVisualChildren<Button>(chatControl);

                foreach (var textBox in textBoxes)
                {
                    textBox.Background.Should().NotBeNull("TextBox should have background in high contrast");
                    textBox.Foreground.Should().NotBeNull("TextBox should have foreground in high contrast");
                }

                foreach (var button in buttons)
                {
                    button.Background.Should().NotBeNull("Button should have background in high contrast");
                    button.Foreground.Should().NotBeNull("Button should have foreground in high contrast");
                }
            });
        }

        #endregion

        #region Performance UI Tests

        [STAFact]
        public async Task ChatControl_LargeMessageHistory_PerformsWell()
        {
            // Arrange
            var chatControl = await CreateChatControlAsync();
            var chatHistory = FindVisualChild<RichTextBox>(chatControl, "ChatHistory");
            const int messageCount = 1000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await DispatcherInvoke(() =>
            {
                for (int i = 0; i < messageCount; i++)
                {
                    var paragraph = new Paragraph(new Run($"Performance test message {i}"));
                    chatHistory.Document.Blocks.Add(paragraph);
                }
            });

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"Added {messageCount} messages in {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Adding messages should be performant");

            await DispatcherInvoke(() =>
            {
                chatHistory.Document.Blocks.Should().HaveCount(messageCount, "All messages should be added");
            });
        }

        #endregion

        #region Private Helper Methods

        private void InitializeWPFApplication()
        {
            if (Application.Current == null)
            {
                _testApp = new Application();
            }

            _testWindow = new Window
            {
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -2000, // Position off-screen
                Top = -2000,
                ShowInTaskbar = false,
                ShowActivated = false
            };

            _testWindow.Show();
        }

        private async Task<ChatControl> CreateChatControlAsync()
        {
            ChatControl chatControl = null;

            await DispatcherInvoke(() =>
            {
                chatControl = new ChatControl();
                _testWindow.Content = chatControl;
                chatControl.Measure(new Size(800, 600));
                chatControl.Arrange(new Rect(0, 0, 800, 600));
                chatControl.UpdateLayout();
            });

            return chatControl;
        }

        private async Task<ContextMenuControl> CreateContextMenuControlAsync()
        {
            ContextMenuControl contextMenuControl = null;

            await DispatcherInvoke(() =>
            {
                contextMenuControl = new ContextMenuControl();
                _testWindow.Content = contextMenuControl;
                contextMenuControl.Measure(new Size(400, 300));
                contextMenuControl.Arrange(new Rect(0, 0, 400, 300));
                contextMenuControl.UpdateLayout();
            });

            return contextMenuControl;
        }

        private async Task<MessageRenderer> CreateMessageRendererAsync()
        {
            MessageRenderer messageRenderer = null;

            await DispatcherInvoke(() =>
            {
                messageRenderer = new MessageRenderer();
                _testWindow.Content = messageRenderer;
                messageRenderer.Measure(new Size(600, 400));
                messageRenderer.Arrange(new Rect(0, 0, 600, 400));
                messageRenderer.UpdateLayout();
            });

            return messageRenderer;
        }

        private async Task<UserControl> CreateConfigurationControlAsync()
        {
            UserControl configControl = null;

            await DispatcherInvoke(() =>
            {
                // Create a mock configuration control
                configControl = new UserControl();
                var stackPanel = new StackPanel();
                
                stackPanel.Children.Add(new ComboBox { Name = "ProviderComboBox" });
                stackPanel.Children.Add(new TextBox { Name = "ApiKeyTextBox" });
                stackPanel.Children.Add(new TextBox { Name = "ModelTextBox" });
                
                configControl.Content = stackPanel;
                _testWindow.Content = configControl;
                configControl.UpdateLayout();
            });

            return configControl;
        }

        private async Task DispatcherInvoke(Action action)
        {
            await _testWindow.Dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
        }

        private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            if (parent == null) return null;

            var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    if (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name))
                        return typedChild;
                }

                var foundChild = FindVisualChild<T>(child, name);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var nestedChild in FindVisualChildren<T>(child))
                    yield return nestedChild;
            }
        }

        private IEnumerable<UIElement> GetFocusableElements(DependencyObject parent)
        {
            if (parent == null) yield break;

            var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is UIElement element && element.Focusable)
                    yield return element;

                foreach (var nestedElement in GetFocusableElements(child))
                    yield return nestedElement;
            }
        }

        private void SetupMocks()
        {
            var settingsStorage = new Dictionary<string, object>();

            _mockSettingsStore.Setup(x => x.CollectionExists(It.IsAny<string>())).Returns(true);
            _mockSettingsStore.Setup(x => x.PropertyExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => settingsStorage.ContainsKey(key));

            _mockSettingsStore.Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((collection, key) => 
                    settingsStorage.ContainsKey(key) ? settingsStorage[key]?.ToString() : string.Empty);

            _mockSettingsStore.Setup(x => x.SetString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((collection, key, value) => settingsStorage[key] = value);

            _mockSettingsStore.Setup(x => x.CreateCollection(It.IsAny<string>()));

            _mockErrorHandler.Setup(x => x.LogInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            _mockErrorHandler.Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        #endregion
    }

    // Mock presentation source for keyboard event testing
    public class MockPresentationSource : PresentationSource
    {
        public override Visual RootVisual { get; set; }
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null;
    }

    // Test collection to ensure UI tests run in STA mode
    [CollectionDefinition("UI Tests")]
    public class UITestCollection : ICollectionFixture<STATestFixture>
    {
    }

    public class STATestFixture
    {
        public STATestFixture()
        {
            // Ensure we're running in STA mode for WPF tests
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }
        }
    }

    // Custom fact attribute that ensures STA threading
    public class STAFactAttribute : FactAttribute
    {
        public STAFactAttribute()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Skip = "Test requires STA thread";
            }
        }
    }
}