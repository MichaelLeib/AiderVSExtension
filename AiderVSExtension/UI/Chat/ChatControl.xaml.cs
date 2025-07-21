using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.UI.Chat
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    public partial class ChatControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly ObservableCollection<ChatMessage> _messages;
        private readonly ObservableCollection<FileReference> _fileReferences;
        
        // Memory optimization constants
        private const int MAX_MESSAGE_HISTORY = 1000;
        private const int MAX_FILE_REFERENCES = 50;
        private IAiderService _aiderService;
        private IFileContextService _fileContextService;
        private IMessageRenderer _messageRenderer;
        private IAiderSetupManager _setupManager;
        private ServiceContainer _serviceContainer;
        private IVSThemingService _themingService;
        private bool _isInitialized = false;
        private bool _isProcessing = false;
        private DispatcherTimer _typingTimer;

        public ChatControl()
        {
            InitializeComponent();
            
            _messages = new ObservableCollection<ChatMessage>();
            _fileReferences = new ObservableCollection<FileReference>();
            
            DataContext = this;
            
            // Initialize typing timer for context menu
            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _typingTimer.Tick += TypingTimer_Tick;
        }

        #region Properties

        public ObservableCollection<ChatMessage> Messages => _messages;
        public ObservableCollection<FileReference> FileReferences => _fileReferences;
        
        public bool HasFileReferences => _fileReferences.Count > 0;
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }

        #endregion

        #region Initialization

        public void Initialize(IServiceProvider serviceProvider)
        {
            try
            {
                _serviceContainer = serviceProvider.GetService(typeof(ServiceContainer)) as ServiceContainer;
                
                if (_serviceContainer != null)
                {
                    _aiderService = _serviceContainer.GetService<IAiderService>();
                    _fileContextService = _serviceContainer.GetService<IFileContextService>();
                    _messageRenderer = _serviceContainer.GetService<IMessageRenderer>();
                    _setupManager = _serviceContainer.GetService<IAiderSetupManager>();
                    _themingService = _serviceContainer.GetService<IVSThemingService>();
                    
                    // Register for theme changes
                    if (_themingService != null)
                    {
                        _themingService.ThemeChanged += OnThemeChanged;
                    }
                }
                
                _isInitialized = true;
                UpdateStatus();
                
                // Load chat history
                LoadChatHistory();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing chat: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ChatControl initialization error: {ex}");
            }
        }

        private async void LoadChatHistory()
        {
            try
            {
                // Load chat history from Aider service if available
                if (_aiderService != null)
                {
                    var chatHistory = await _aiderService.GetChatHistoryAsync();
                    if (chatHistory != null && chatHistory.Any())
                    {
                        // Ensure UI update happens on UI thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var message in chatHistory)
                            {
                                AddMessageWithMemoryManagement(message);
                            }
                            ScrollToBottom();
                        });
                        return;
                    }
                }
                
                // Add welcome message if no history exists
                var welcomeMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "Welcome to Aider AI Chat! Type your message below or use # to reference files, clipboard, and more.",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    AddMessageWithMemoryManagement(welcomeMessage);
                });
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chat history: {ex.Message}");
                
                // Add fallback welcome message
                var welcomeMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "Welcome to Aider AI Chat! Type your message below or use # to reference files, clipboard, and more.",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    AddMessageWithMemoryManagement(welcomeMessage);
                });
                ScrollToBottom();
            }
        }

        #endregion

        #region Event Handlers

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Enter sends the message
                    SendMessage();
                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    // Enter alone adds a new line (default behavior)
                    return;
                }
            }
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;
            
            var text = textBox.Text;
            var caretIndex = textBox.CaretIndex;
            
            // Check if user typed # and show context menu
            if (text.Length > 0 && caretIndex > 0 && text[caretIndex - 1] == '#')
            {
                _typingTimer.Stop();
                _typingTimer.Start();
            }
            else
            {
                _typingTimer.Stop();
                ContextMenuPopup.IsOpen = false;
            }
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            ShowContextMenu();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            ClearChat();
        }

        private void SaveChatButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChatHistory();
        }

        private void RemoveFileReference_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileReference fileRef)
            {
                _fileReferences.Remove(fileRef);
                OnPropertyChanged(nameof(HasFileReferences));
            }
        }

        private void ContextMenuControl_ItemSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            ContextMenuPopup.IsOpen = false;
            HandleContextMenuSelection(e.SelectedItem);
        }

        #endregion

        #region Message Handling

        private async void SendMessage()
        {
            if (IsProcessing || !_isInitialized) return;
            
            var messageText = InputTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(messageText)) return;
            
            try
            {
                IsProcessing = true;
                
                // Create user message
                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = messageText,
                    Type = MessageType.User,
                    Timestamp = DateTime.Now,
                    References = new List<FileReference>(_fileReferences)
                };
                
                // Add to messages
                AddMessageWithMemoryManagement(userMessage);
                
                // Clear input
                InputTextBox.Text = string.Empty;
                _fileReferences.Clear();
                OnPropertyChanged(nameof(HasFileReferences));
                
                ScrollToBottom();
                
                // Send to Aider service with dependency validation
                if (_aiderService != null && _setupManager != null)
                {
                    try
                    {
                        // Ensure dependencies are satisfied before sending message
                        var dependenciesSatisfied = await _setupManager.EnsureDependenciesAsync();
                        
                        if (!dependenciesSatisfied)
                        {
                            // User cancelled setup or setup failed
                            var setupErrorResponse = new ChatMessage
                            {
                                Id = Guid.NewGuid().ToString(),
                                Content = "Aider setup is required to use AI assistance. Please complete the setup to continue.",
                                Type = MessageType.System,
                                Timestamp = DateTime.Now
                            };
                            
                            await Dispatcher.InvokeAsync(() =>
                            {
                                AddMessageWithMemoryManagement(setupErrorResponse);
                                ScrollToBottom();
                            });
                            return;
                        }

                        // Initialize Aider service if needed
                        await _aiderService.InitializeAsync();
                        
                        var response = await _aiderService.SendMessageAsync(userMessage);
                        
                        if (response != null)
                        {
                            // Ensure UI update happens on UI thread
                            await Dispatcher.InvokeAsync(() =>
                            {
                                AddMessageWithMemoryManagement(response);
                                ScrollToBottom();
                            });
                        }
                    }
                    catch (InvalidOperationException setupEx) when (setupEx.Message.Contains("not initialized") || setupEx.Message.Contains("not connected"))
                    {
                        // Dependency or connection issue - offer to run setup again
                        var setupRetryResponse = new ChatMessage
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = "Aider is not properly configured. Would you like to run the setup again? Click here to open setup.",
                            Type = MessageType.System,
                            Timestamp = DateTime.Now
                        };
                        
                        await Dispatcher.InvokeAsync(() =>
                        {
                            AddMessageWithMemoryManagement(setupRetryResponse);
                            ScrollToBottom();
                        });
                    }
                    catch (Exception serviceEx)
                    {
                        // Add error response message
                        var errorResponse = new ChatMessage
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = $"Error from Aider service: {serviceEx.Message}",
                            Type = MessageType.System,
                            Timestamp = DateTime.Now
                        };
                        
                        // Ensure UI update happens on UI thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            AddMessageWithMemoryManagement(errorResponse);
                            ScrollToBottom();
                        });
                    }
                }
                else
                {
                    // Services not available - show setup guidance
                    var setupRequiredResponse = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "I'm ready to help! However, Aider needs to be set up first. Please restart Visual Studio to complete the setup process.",
                        Type = MessageType.System,
                        Timestamp = DateTime.Now
                    };
                    
                    // Ensure UI update happens on UI thread
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddMessageWithMemoryManagement(setupRequiredResponse);
                        ScrollToBottom();
                    });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = $"Error sending message: {ex.Message}",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    AddMessageWithMemoryManagement(errorMessage);
                    ScrollToBottom();
                });
                
                System.Diagnostics.Debug.WriteLine($"Error sending message: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Opens the Aider setup dialog
        /// </summary>
        public async void ShowSetupDialog()
        {
            if (_setupManager == null)
            {
                MessageBox.Show(
                    "Setup manager is not available. Please restart Visual Studio and try again.",
                    "Setup Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                
                var setupCompleted = await _setupManager.ShowSetupDialogAsync();
                
                if (setupCompleted)
                {
                    // Add confirmation message to chat
                    var confirmationMessage = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "✅ Aider setup completed successfully! You can now use AI assistance.",
                        Type = MessageType.System,
                        Timestamp = DateTime.Now
                    };
                    
                    AddMessageWithMemoryManagement(confirmationMessage);
                    ScrollToBottom();
                    
                    // Try to initialize the Aider service
                    if (_aiderService != null)
                    {
                        await _aiderService.InitializeAsync();
                    }
                }
                else
                {
                    // Add cancellation message to chat
                    var cancelMessage = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = "Setup was cancelled. You can run setup again anytime by clicking the setup button.",
                        Type = MessageType.System,
                        Timestamp = DateTime.Now
                    };
                    
                    AddMessageWithMemoryManagement(cancelMessage);
                    ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = $"Error during setup: {ex.Message}",
                    Type = MessageType.System,
                    Timestamp = DateTime.Now
                };
                
                AddMessageWithMemoryManagement(errorMessage);
                ScrollToBottom();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ClearChat()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the chat history?",
                "Clear Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _messages.Clear();
                _fileReferences.Clear();
                OnPropertyChanged(nameof(HasFileReferences));
                UpdateStatus("Chat cleared");
            }
        }

        private async void SaveChatHistory()
        {
            try
            {
                // Save chat history using Aider service if available
                if (_aiderService != null)
                {
                    await _aiderService.SaveConversationAsync();
                    UpdateStatus("Chat history saved");
                }
                else
                {
                    UpdateStatus("Aider service not available");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving chat: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error saving chat history: {ex}");
            }
        }

        #endregion

        #region Context Menu

        private void ShowContextMenu()
        {
            if (ContextMenuControl != null)
            {
                ContextMenuControl.LoadContextItems();
                ContextMenuPopup.IsOpen = true;
            }
        }

        private async void HandleContextMenuSelection(ContextMenuItem selectedItem)
        {
            if (selectedItem == null) return;
            
            try
            {
                switch (selectedItem.Type)
                {
                    case ContextMenuItemType.Files:
                        await HandleFileSelection(selectedItem);
                        break;
                        
                    case ContextMenuItemType.Clipboard:
                        HandleClipboardSelection();
                        break;
                        
                    case ContextMenuItemType.GitBranches:
                        await HandleGitBranchSelection(selectedItem);
                        break;
                        
                    case ContextMenuItemType.WebSearch:
                        HandleWebSearchSelection(selectedItem);
                        break;
                        
                    case ContextMenuItemType.Documentation:
                        HandleDocumentationSelection(selectedItem);
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error handling context menu: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Context menu error: {ex}");
            }
        }

        private async Task HandleFileSelection(ContextMenuItem selectedItem)
        {
            if (_fileContextService == null) return;
            
            if (selectedItem.Data is string filePath)
            {
                var content = await _fileContextService.GetFileContentAsync(filePath);
                var fileRef = new FileReference
                {
                    FilePath = filePath,
                    Content = content,
                    Type = ReferenceType.File,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    _fileReferences.Add(fileRef);
                    OnPropertyChanged(nameof(HasFileReferences));
                });
            }
        }

        private void HandleClipboardSelection()
        {
            if (_fileContextService == null) return;
            
            var clipboardContent = _fileContextService.GetClipboardContent();
            if (!string.IsNullOrEmpty(clipboardContent))
            {
                var clipboardRef = new FileReference
                {
                    Content = clipboardContent,
                    Type = ReferenceType.Clipboard,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                Dispatcher.Invoke(() =>
                {
                    _fileReferences.Add(clipboardRef);
                    OnPropertyChanged(nameof(HasFileReferences));
                });
            }
        }

        private async Task HandleGitBranchSelection(ContextMenuItem selectedItem)
        {
            if (_fileContextService == null) return;
            
            if (selectedItem.Data is string branchName)
            {
                var gitStatus = await _fileContextService.GetGitStatusAsync();
                var branchRef = new FileReference
                {
                    Content = $"Git branch: {branchName}",
                    Type = ReferenceType.GitBranch,
                    Timestamp = DateTime.Now
                };
                
                // Ensure UI update happens on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    _fileReferences.Add(branchRef);
                    OnPropertyChanged(nameof(HasFileReferences));
                });
            }
        }

        private void HandleWebSearchSelection(ContextMenuItem selectedItem)
        {
            try
            {
                // Create a simple web search dialog
                var searchTerm = Microsoft.VisualStudio.PlatformUI.MessageDialog.Show(
                    "Web Search", 
                    "Enter search terms:", 
                    Microsoft.VisualStudio.PlatformUI.MessageBoxButton.OKCancel);
                    
                if (searchTerm == Microsoft.VisualStudio.PlatformUI.MessageBoxResult.OK)
                {
                    var searchText = "web search";
                    
                    // Create a placeholder search result
                    var searchResult = $"Search results for '{searchText}' would appear here. This feature connects to web search APIs.";
                    
                    // Add search result to message input
                    var messageInput = FindName("MessageInput") as System.Windows.Controls.TextBox;
                    if (messageInput != null)
                    {
                        messageInput.Text += $"\n\n[Web Search: {searchText}]\n{searchResult}";
                        messageInput.Focus();
                    }
                    
                    UpdateStatus("Web search result added to message");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error performing web search: {ex.Message}");
            }
        }

        private void HandleDocumentationSelection(ContextMenuItem selectedItem)
        {
            try
            {
                // Get current project context
                var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte?.Solution != null)
                {
                    var projectName = System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                    
                    // Common documentation links based on project type
                    var docLinks = new List<string>
                    {
                        "https://docs.microsoft.com/en-us/dotnet/",
                        "https://docs.microsoft.com/en-us/aspnet/",
                        "https://docs.microsoft.com/en-us/visualstudio/"
                    };
                    
                    var docText = $"Documentation links for {projectName}:\n";
                    foreach (var link in docLinks)
                    {
                        docText += $"- {link}\n";
                    }
                    
                    // Add to message input
                    var messageInput = FindName("MessageInput") as System.Windows.Controls.TextBox;
                    if (messageInput != null)
                    {
                        messageInput.Text += $"\n\n[Documentation]\n{docText}";
                        messageInput.Focus();
                    }
                    
                    UpdateStatus("Documentation links added to message");
                }
                else
                {
                    UpdateStatus("No active solution found for documentation context");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error accessing documentation: {ex.Message}");
            }
        }

        #endregion

        #region Search Functionality

        public List<ChatMessage> SearchMessages(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<ChatMessage>();
            
            return _messages.Where(m => 
                m.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void HighlightSearchResult(ChatMessage message)
        {
            if (message == null) return;
            
            try
            {
                // Find the message in the list
                var messageIndex = _messages.IndexOf(message);
                if (messageIndex >= 0)
                {
                    // Scroll to the message
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (MessagesPanel.ItemContainerGenerator.ContainerFromIndex(messageIndex) is FrameworkElement container)
                        {
                            container.BringIntoView();
                            
                            // Add highlight effect
                            container.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)); // Semi-transparent yellow
                            
                            // Clear highlight after 3 seconds
                            var timer = new DispatcherTimer
                            {
                                Interval = TimeSpan.FromSeconds(3)
                            };
                            timer.Tick += (s, e) =>
                            {
                                container.Background = Brushes.Transparent;
                                timer.Stop();
                            };
                            timer.Start();
                        }
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error highlighting search result: {ex.Message}");
            }
        }

        public void ClearSearchHighlight()
        {
            try
            {
                // Clear all highlights from message containers
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (MessagesPanel.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
                    {
                        container.Background = Brushes.Transparent;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing search highlights: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageScrollViewer.ScrollToBottom();
            }), DispatcherPriority.Background);
        }

        private void UpdateStatus(string message = null)
        {
            var statusMessage = message ?? (IsProcessing ? "Processing..." : (!_isInitialized ? "Initializing..." : "Ready"));
            
            // Update StatusTextBlock if it exists
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = statusMessage;
            }
            
            System.Diagnostics.Debug.WriteLine($"Status update: {statusMessage}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a file reference to the chat input
        /// </summary>
        /// <param name="fileReference">The file reference to add</param>
        public void AddFileReference(FileReference fileReference)
        {
            if (fileReference == null) return;

            try
            {
                // Check if the reference already exists
                var existingRef = _fileReferences.FirstOrDefault(fr => 
                    fr.FilePath == fileReference.FilePath && 
                    fr.StartLine == fileReference.StartLine && 
                    fr.EndLine == fileReference.EndLine);

                if (existingRef == null)
                {
                    // Ensure UI update happens on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        _fileReferences.Add(fileReference);
                        OnPropertyChanged(nameof(HasFileReferences));
                    });
                }

                // Focus the input textbox
                Dispatcher.Invoke(() => InputTextBox.Focus());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding file reference: {ex}");
            }
        }

        /// <summary>
        /// Adds multiple file references to the chat input
        /// </summary>
        /// <param name="fileReferences">The file references to add</param>
        public void AddFileReferences(IEnumerable<FileReference> fileReferences)
        {
            if (fileReferences == null) return;

            try
            {
                foreach (var fileRef in fileReferences)
                {
                    AddFileReference(fileRef);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding file references: {ex}");
            }
        }

        /// <summary>
        /// Sets the chat input text
        /// </summary>
        /// <param name="text">The text to set in the input</param>
        public void SetChatInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            try
            {
                // Ensure UI update happens on UI thread
                Dispatcher.Invoke(() =>
                {
                    if (ChatInput != null)
                    {
                        ChatInput.Text = text;
                        ChatInput.Focus();
                        ChatInput.SelectionStart = ChatInput.Text.Length;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting chat input: {ex}");
            }
        }

        #endregion

        #region Memory Management

        private void AddMessageWithMemoryManagement(ChatMessage message)
        {
            if (message == null) return;

            // Add the new message
            _messages.Add(message);

            // Check if we need to trim old messages
            if (_messages.Count > MAX_MESSAGE_HISTORY)
            {
                var messagesToRemove = _messages.Count - MAX_MESSAGE_HISTORY;
                for (int i = 0; i < messagesToRemove; i++)
                {
                    _messages.RemoveAt(0);
                }

                // Force garbage collection after removing many messages
                if (messagesToRemove > 100)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
            }
        }

        private void AddFileReferenceWithMemoryManagement(FileReference fileRef)
        {
            if (fileRef == null) return;

            // Add the new file reference
            _fileReferences.Add(fileRef);

            // Check if we need to trim old file references
            if (_fileReferences.Count > MAX_FILE_REFERENCES)
            {
                var referencesToRemove = _fileReferences.Count - MAX_FILE_REFERENCES;
                for (int i = 0; i < referencesToRemove; i++)
                {
                    _fileReferences.RemoveAt(0);
                }
            }
        }

        #endregion

        #region Theme Management

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            try
            {
                // Apply theme changes on UI thread
                Dispatcher.InvokeAsync(() => ApplyCurrentTheme());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling theme change: {ex.Message}");
            }
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                if (_themingService == null) return;

                // Update highlight colors for search functionality
                var highlightColor = _themingService.GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.SearchHighlightBackground);
                if (highlightColor.HasValue)
                {
                    // Store the theme color for use in search highlighting
                    Resources["SearchHighlightBrush"] = new SolidColorBrush(highlightColor.Value);
                }

                // Update text selection colors
                var selectionColor = _themingService.GetThemedColor(AiderVSExtension.Interfaces.ThemeResourceKey.TextSelectionBackground);
                if (selectionColor.HasValue)
                {
                    Resources["TextSelectionBrush"] = new SolidColorBrush(selectionColor.Value);
                }

                // Force refresh of all UI elements that use theme resources
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Unregister theme change events
            if (_themingService != null)
            {
                _themingService.ThemeChanged -= OnThemeChanged;
            }
            
            _typingTimer?.Stop();
            _typingTimer = null;
            
            _messages?.Clear();
            _fileReferences?.Clear();
        }

        #endregion
    }
}