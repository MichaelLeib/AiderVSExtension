using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AiderVSExtension.UI.Chat
{
    /// <summary>
    /// Interaction logic for ContextMenuControl.xaml
    /// </summary>
    public partial class ContextMenuControl : UserControl
    {
        private readonly ObservableCollection<ContextMenuItem> _contextItems;
        private IFileContextService _fileContextService;
        private List<ContextMenuItem> _allItems;
        private bool _isFileSearchMode = false;

        // Stub controls for cross-platform compilation
#if !WINDOWS
        private class StubControl
        {
            public object ItemsSource { get; set; }
            public string Text { get; set; } = "";
            public Visibility Visibility { get; set; } = Visibility.Visible;
            public bool IsEnabled { get; set; } = true;
            public event EventHandler<RoutedEventArgs> Click;
            public void Focus() { }
            public void Clear() { }
        }
        
        private StubControl ContextItemsList = new StubControl();
        private StubControl SearchTextBox = new StubControl();
#endif

        // Stub for InitializeComponent - normally generated from XAML
        private void InitializeComponent()
        {
#if WINDOWS
            // This method is normally auto-generated from XAML
#endif
        }

        public ContextMenuControl()
        {
            InitializeComponent();
            
            _contextItems = new ObservableCollection<ContextMenuItem>();
            _allItems = new List<ContextMenuItem>();
            
            ContextItemsList.ItemsSource = _contextItems;
            
            // Initialize services
            InitializeServices();
        }

        public event EventHandler<ContextMenuItemSelectedEventArgs> ItemSelected;

        private void InitializeServices()
        {
            try
            {
                // Get services from the global service provider
                var serviceProvider = Package.GetGlobalService(typeof(IServiceProvider)) as IServiceProvider;
                if (serviceProvider != null)
                {
                    var serviceContainer = serviceProvider.GetService(typeof(ServiceContainer)) as ServiceContainer;
                    if (serviceContainer != null)
                    {
                        _fileContextService = serviceContainer.GetService<IFileContextService>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing context menu services: {ex.Message}");
            }
        }

        public async void LoadContextItems()
        {
            try
            {
                _contextItems.Clear();
                _allItems.Clear();
                
                // Add main context menu items
                var mainItems = new List<ContextMenuItem>
                {
                    new ContextMenuItem
                    {
                        Type = ContextMenuItemType.Files,
                        Title = "Files",
                        Subtitle = "Reference files from your solution",
                        Icon = "📁",
                        KeyboardShortcut = "F"
                    },
                    new ContextMenuItem
                    {
                        Type = ContextMenuItemType.Clipboard,
                        Title = "Clipboard",
                        Subtitle = "Reference clipboard content",
                        Icon = "📋",
                        KeyboardShortcut = "C"
                    },
                    new ContextMenuItem
                    {
                        Type = ContextMenuItemType.GitBranches,
                        Title = "Git Branches",
                        Subtitle = "Reference Git branch information",
                        Icon = "🌿",
                        KeyboardShortcut = "G"
                    },
                    new ContextMenuItem
                    {
                        Type = ContextMenuItemType.WebSearch,
                        Title = "Web Search",
                        Subtitle = "Search the web for information",
                        Icon = "🔍",
                        KeyboardShortcut = "W"
                    },
                    new ContextMenuItem
                    {
                        Type = ContextMenuItemType.Documentation,
                        Title = "Documentation",
                        Subtitle = "Search documentation resources",
                        Icon = "📚",
                        KeyboardShortcut = "D"
                    }
                };

                _allItems.AddRange(mainItems);
                
                foreach (var item in mainItems)
                {
                    _contextItems.Add(item);
                }
                
                // Pre-select the first item
                if (_contextItems.Count > 0)
                {
                    ContextItemsList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading context items: {ex.Message}");
            }
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isFileSearchMode) return;
            
            var searchText = SearchTextBox.Text?.Trim();
            await FilterFiles(searchText);
        }

        private async Task FilterFiles(string searchText)
        {
            try
            {
                if (_fileContextService == null) return;
                
                _contextItems.Clear();
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Show recent files or all files
                    var files = await _fileContextService.GetSolutionFilesAsync();
                    var fileItems = files.Take(20).Select(f => new ContextMenuItem
                    {
                        Type = ContextMenuItemType.Files,
                        Title = f.FileName,
                        Subtitle = f.RelativePath,
                        Icon = GetFileIcon(f.FileType),
                        Data = f.FilePath
                    });
                    
                    foreach (var item in fileItems)
                    {
                        _contextItems.Add(item);
                    }
                }
                else
                {
                    // Search files
                    var searchResults = await _fileContextService.SearchFilesAsync(searchText);
                    var fileItems = searchResults.Take(20).Select(f => new ContextMenuItem
                    {
                        Type = ContextMenuItemType.Files,
                        Title = f.FileName,
                        Subtitle = f.RelativePath,
                        Icon = GetFileIcon(f.FileType),
                        Data = f.FilePath
                    });
                    
                    foreach (var item in fileItems)
                    {
                        _contextItems.Add(item);
                    }
                }
                
                // Pre-select the first item
                if (_contextItems.Count > 0)
                {
                    ContextItemsList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering files: {ex.Message}");
            }
        }

        private string GetFileIcon(string fileExtension)
        {
            if (fileExtension == null) return "📄";
            
            var ext = fileExtension.ToLower();
            switch (ext)
            {
                case ".cs":
                    return "🔷";
                case ".js":
                    return "🟨";
                case ".ts":
                    return "🔷";
                case ".html":
                    return "🌐";
                case ".css":
                    return "🎨";
                case ".json":
                    return "📋";
                case ".xml":
                    return "📄";
                case ".txt":
                    return "📝";
                case ".md":
                    return "📖";
                case ".py":
                    return "🐍";
                case ".java":
                    return "☕";
                case ".cpp":
                case ".c":
                    return "⚡";
                case ".h":
                    return "📋";
                case ".sql":
                    return "🗃️";
                case ".xaml":
                    return "🎨";
                default:
                    return "📄";
            }
        }

        private void ContextItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }

        private void ContextItemsList_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    SelectCurrentItem();
                    e.Handled = true;
                    break;
                    
                case Key.Escape:
                    CancelSelection();
                    e.Handled = true;
                    break;
                    
                case Key.F when !_isFileSearchMode:
                    EnterFileSearchMode();
                    e.Handled = true;
                    break;
                    
                case Key.C when !_isFileSearchMode:
                    SelectClipboard();
                    e.Handled = true;
                    break;
                    
                case Key.G when !_isFileSearchMode:
                    SelectGitBranches();
                    e.Handled = true;
                    break;
                    
                case Key.W when !_isFileSearchMode:
                    SelectWebSearch();
                    e.Handled = true;
                    break;
                    
                case Key.D when !_isFileSearchMode:
                    SelectDocumentation();
                    e.Handled = true;
                    break;
            }
        }

        private void SelectCurrentItem()
        {
            var selectedItem = ContextItemsList.SelectedItem as ContextMenuItem;
            if (selectedItem != null)
            {
                if (selectedItem.Type == ContextMenuItemType.Files && selectedItem.Data == null)
                {
                    // Enter file search mode
                    EnterFileSearchMode();
                }
                else
                {
                    // Select the item
                    OnItemSelected(selectedItem);
                }
            }
        }

        private async void EnterFileSearchMode()
        {
            _isFileSearchMode = true;
            SearchTextBox.Visibility = Visibility.Visible;
            SearchTextBox.Focus();
            
            // Load files
            await FilterFiles(string.Empty);
        }

        private void SelectClipboard()
        {
            var clipboardItem = _allItems.FirstOrDefault(i => i.Type == ContextMenuItemType.Clipboard);
            if (clipboardItem != null)
            {
                OnItemSelected(clipboardItem);
            }
        }

        private async void SelectGitBranches()
        {
            try
            {
                if (_fileContextService == null) return;
                
                var branches = await _fileContextService.GetGitBranchesAsync();
                var branchItems = branches.Take(10).Select(b => new ContextMenuItem
                {
                    Type = ContextMenuItemType.GitBranches,
                    Title = b.Name,
                    Subtitle = b.IsCurrentBranch ? "Current branch" : $"Last commit: {b.LastCommitDate:yyyy-MM-dd}",
                    Icon = b.IsCurrentBranch ? "🌿" : "🌱",
                    Data = b.Name
                });
                
                _contextItems.Clear();
                foreach (var item in branchItems)
                {
                    _contextItems.Add(item);
                }
                
                if (_contextItems.Count > 0)
                {
                    ContextItemsList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Git branches: {ex.Message}");
            }
        }

        private void SelectWebSearch()
        {
            var webSearchItem = _allItems.FirstOrDefault(i => i.Type == ContextMenuItemType.WebSearch);
            if (webSearchItem != null)
            {
                OnItemSelected(webSearchItem);
            }
        }

        private void SelectDocumentation()
        {
            var docItem = _allItems.FirstOrDefault(i => i.Type == ContextMenuItemType.Documentation);
            if (docItem != null)
            {
                OnItemSelected(docItem);
            }
        }

        private void CancelSelection()
        {
            OnItemSelected(null);
        }

        private void OnItemSelected(ContextMenuItem selectedItem)
        {
            ItemSelected?.Invoke(this, new ContextMenuItemSelectedEventArgs(selectedItem));
        }
    }

    public class ContextMenuItem
    {
        public ContextMenuItemType Type { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Icon { get; set; }
        public string KeyboardShortcut { get; set; }
        public object Data { get; set; }
        
        public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
        public bool HasKeyboardShortcut => !string.IsNullOrEmpty(KeyboardShortcut);
    }

    public enum ContextMenuItemType
    {
        Files,
        Clipboard,
        GitBranches,
        WebSearch,
        Documentation
    }

    public class ContextMenuItemSelectedEventArgs : EventArgs
    {
        public ContextMenuItem SelectedItem { get; }
        
        public ContextMenuItemSelectedEventArgs(ContextMenuItem selectedItem)
        {
            SelectedItem = selectedItem;
        }
    }
}