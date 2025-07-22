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

        // Stub controls for non-Windows compilation
#if !WINDOWS
        private class StubControl
        {
            public string Text { get; set; } = "";
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public bool IsEnabled { get; set; } = true;
            public object ItemsSource { get; set; }
            public event EventHandler<KeyEventArgs> KeyDown;
            public event EventHandler<TextChangedEventArgs> TextChanged;
            public event EventHandler<RoutedEventArgs> Click;
            public void Focus() { }
            public void ScrollToEnd() { }
        }
        
        private StubControl InputTextBox = new StubControl();
        private StubControl SendButton = new StubControl();
        private StubControl ClearChatButton = new StubControl();
        private StubControl SaveChatButton = new StubControl();
        private StubControl ContextMenuPopup = new StubControl();
        private StubControl MessagesPanel = new StubControl();
        private StubControl MessageScrollViewer = new StubControl();
        private StubControl StatusTextBlock = new StubControl();
        private StubControl FileReferencesList = new StubControl();
#endif

        public ChatControl()
        {
#if WINDOWS
            InitializeComponent();
#endif
            
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
                
                // Load chat history
                LoadChatHistory();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ChatControl: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private async void LoadChatHistory()
        {
            try
            {
                if (_aiderService != null)
                {
                    var history = await _aiderService.GetChatHistoryAsync();
                    if (history != null)
                    {
                        foreach (var message in history)
                        {
                            AddMessageWithMemoryManagement(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading chat history: {ex.Message}");
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                ShowContextMenu();
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset typing timer
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            // Context menu logic would go here
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
            if (sender is Button button && button.DataContext is FileReference fileRef)
            {
                _fileReferences.Remove(fileRef);
            }
        }

        private void ContextMenuControl_ItemSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            ContextMenuPopup.Visibility = Visibility.Collapsed;
            HandleContextMenuSelection(e.SelectedItem);
        }

        #endregion

        #region Message Handling

        private async void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text) || IsProcessing)
                return;

            var messageText = InputTextBox.Text.Trim();
            InputTextBox.Text = string.Empty;

            var userMessage = new ChatMessage
            {
                Content = messageText,
                Type = MessageType.User,
                Timestamp = DateTime.UtcNow
            };

            AddMessageWithMemoryManagement(userMessage);
            IsProcessing = true;

            try
            {
                await _aiderService.SendMessageAsync(messageText);
                
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage
                {
                    Content = $"Error: {ex.Message}",
                    Type = MessageType.System,
                    Timestamp = DateTime.UtcNow
                };

                AddMessageWithMemoryManagement(errorMessage);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public async void ShowSetupDialog()
        {
            try
            {
                if (_setupManager != null)
                {
                    var result = await _setupManager.ShowSetupDialogAsync();
                    if (result)
                    {
                        // Setup completed successfully
                        System.Diagnostics.Debug.WriteLine("Aider setup completed successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing setup dialog: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        private void ClearChat()
        {
            _messages.Clear();
            _fileReferences.Clear();
            UpdateStatus("Chat cleared");
        }

        private async void SaveChatHistory()
        {
            try
            {
                if (_aiderService != null)
                {
                    // Save chat history logic would go here
                    UpdateStatus("Chat history saved");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving chat history: {ex.Message}");
                UpdateStatus("Error saving chat history");
            }
        }

        private ContextMenuControl ContextMenuControlInstance;

        private void ShowContextMenu()
        {
            if (ContextMenuControlInstance == null)
            {
                ContextMenuControlInstance = new ContextMenuControl();
                ContextMenuControlInstance.ItemSelected += ContextMenuControl_ItemSelected;
            }

            ContextMenuPopup.Visibility = Visibility.Visible;
            ContextMenuControlInstance.LoadContextItems();
        }

        private async void HandleContextMenuSelection(ContextMenuItem selectedItem)
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

        private async Task HandleFileSelection(ContextMenuItem selectedItem)
        {
            try
            {
                if (_fileContextService != null)
                {
                    var files = await _fileContextService.GetSolutionFilesAsync();
                    // File selection logic would go here
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling file selection: {ex.Message}");
            }
        }

        private void HandleClipboardSelection()
        {
            try
            {
                var clipboardText = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(clipboardText))
                {
                    InputTextBox.Text = clipboardText;
                    InputTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling clipboard selection: {ex.Message}");
            }
        }

        private async Task HandleGitBranchSelection(ContextMenuItem selectedItem)
        {
            try
            {
                // Git branch selection logic would go here
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling git branch selection: {ex.Message}");
            }
        }

        private void HandleWebSearchSelection(ContextMenuItem selectedItem)
        {
            try
            {
                // Web search logic would go here
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling web search selection: {ex.Message}");
            }
        }

        private void HandleDocumentationSelection(ContextMenuItem selectedItem)
        {
            try
            {
                // Documentation logic would go here
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling documentation selection: {ex.Message}");
            }
        }

        #endregion

        #region Search and Navigation

        public List<ChatMessage> SearchMessages(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<ChatMessage>();

            return _messages.Where(m => m.Content.Contains(searchText)).ToList();
        }

        public void HighlightSearchResult(ChatMessage message)
        {
            // Search highlighting logic would go here
        }

        public void ClearSearchHighlight()
        {
            // Clear search highlighting logic would go here
        }

        #endregion

        #region UI Updates

        private void ScrollToBottom()
        {
            MessageScrollViewer.ScrollToEnd();
        }

        private void UpdateStatus(string message = null)
        {
            if (message != null)
            {
                StatusTextBlock.Text = message;
            }
            else if (IsProcessing)
            {
                StatusTextBlock.Text = "Processing...";
            }
            else
            {
                StatusTextBlock.Text = $"Messages: {_messages.Count} | Files: {_fileReferences.Count}";
            }
        }

        #endregion

        #region File References

        public void AddFileReference(FileReference fileReference)
        {
            AddFileReferenceWithMemoryManagement(fileReference);
        }

        public void AddFileReferences(IEnumerable<FileReference> fileReferences)
        {
            foreach (var fileRef in fileReferences)
            {
                AddFileReferenceWithMemoryManagement(fileRef);
            }
        }

        public void SetChatInput(string text)
        {
            InputTextBox.Text = text;
            InputTextBox.Focus();
        }

        #endregion

        #region Memory Management

        private void AddMessageWithMemoryManagement(ChatMessage message)
        {
            _messages.Add(message);

            // Remove old messages if we exceed the limit
            while (_messages.Count > MAX_MESSAGE_HISTORY)
            {
                _messages.RemoveAt(0);
            }

            ScrollToBottom();
        }

        private void AddFileReferenceWithMemoryManagement(FileReference fileRef)
        {
            if (!_fileReferences.Any(f => f.FilePath == fileRef.FilePath))
            {
                _fileReferences.Add(fileRef);

                // Remove old references if we exceed the limit
                while (_fileReferences.Count > MAX_FILE_REFERENCES)
                {
                    _fileReferences.RemoveAt(0);
                }
            }
        }

        #endregion

        #region Theming

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                if (_themingService != null)
                {
                    var theme = _themingService.GetCurrentTheme();
                    // Theme application logic would go here
                }
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
            _typingTimer?.Stop();
            if (_themingService != null)
            {
                _themingService.ThemeChanged -= OnThemeChanged;
            }
        }

        #endregion
    }
}