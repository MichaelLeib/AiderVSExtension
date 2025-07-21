using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace AiderVSExtension.UI.Chat
{
    /// <summary>
    /// Interaction logic for MessageRenderer.xaml
    /// </summary>
    public partial class MessageRenderer : UserControl, IMessageRenderer
    {
        private ChatMessage _message;
        private DTE _dte;
        private IVSThemingService _themingService;

        public MessageRenderer()
        {
            InitializeComponent();
            
            // Get DTE service for file navigation
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            
            // Get theming service
            var serviceContainer = ServiceContainer.Instance;
            _themingService = serviceContainer?.GetService<IVSThemingService>();
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(ChatMessage), typeof(MessageRenderer),
                new PropertyMetadata(null, OnMessageChanged));

        public ChatMessage Message
        {
            get => (ChatMessage)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageRenderer renderer)
            {
                renderer.UpdateMessage(e.NewValue as ChatMessage);
            }
        }

        private void UpdateMessage(ChatMessage message)
        {
            _message = message;
            if (message == null) return;

            // Set the data context
            DataContext = message;

            // Apply message-specific styling
            ApplyMessageStyle(message.Type);

            // Render the message content
            RenderMessageContent(message.Content);

            // Update file references
            UpdateFileReferences();
        }

        private void ApplyMessageStyle(MessageType messageType)
        {
            Style style = messageType switch
            {
                MessageType.User => (Style)FindResource("UserMessageStyle"),
                MessageType.Assistant => (Style)FindResource("AssistantMessageStyle"),
                MessageType.System => (Style)FindResource("SystemMessageStyle"),
                _ => (Style)FindResource("AssistantMessageStyle")
            };

            MessageBorder.Style = style;
        }

        private void RenderMessageContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                MessageContent.Document = new FlowDocument();
                return;
            }

            var document = new FlowDocument();
            var paragraph = new Paragraph();

            // Parse markdown-like content
            ParseAndRenderContent(content, paragraph);

            document.Blocks.Add(paragraph);
            MessageContent.Document = document;
        }

        private void ParseAndRenderContent(string content, Paragraph paragraph)
        {
            // Split content into lines for processing
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            bool inCodeBlock = false;
            var codeBlockContent = new System.Text.StringBuilder();
            string codeBlockLanguage = null;

            foreach (var line in lines)
            {
                // Check for code block markers
                if (line.Trim().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        // End of code block
                        AddCodeBlock(paragraph, codeBlockContent.ToString(), codeBlockLanguage);
                        codeBlockContent.Clear();
                        codeBlockLanguage = null;
                        inCodeBlock = false;
                    }
                    else
                    {
                        // Start of code block
                        codeBlockLanguage = line.Trim().Substring(3).Trim();
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeBlockContent.AppendLine(line);
                }
                else
                {
                    // Process regular line
                    ProcessTextLine(line, paragraph);
                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            // Handle unclosed code block
            if (inCodeBlock && codeBlockContent.Length > 0)
            {
                AddCodeBlock(paragraph, codeBlockContent.ToString(), codeBlockLanguage);
            }
        }

        private void ProcessTextLine(string line, Paragraph paragraph)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            // Process inline code
            var codePattern = @"`([^`]+)`";
            var matches = Regex.Matches(line, codePattern);
            
            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Add text before the code
                if (match.Index > lastIndex)
                {
                    var textBefore = line.Substring(lastIndex, match.Index - lastIndex);
                    ProcessFormattedText(textBefore, paragraph);
                }

                // Add inline code
                var codeRun = new Run(match.Groups[1].Value)
                {
                    FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                    Background = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBackground),
                    FontSize = 13
                };
                paragraph.Inlines.Add(codeRun);

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < line.Length)
            {
                var remainingText = line.Substring(lastIndex);
                ProcessFormattedText(remainingText, paragraph);
            }
        }

        private void ProcessFormattedText(string text, Paragraph paragraph)
        {
            // Process bold text
            var boldPattern = @"\*\*([^*]+)\*\*";
            var matches = Regex.Matches(text, boldPattern);
            
            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Add text before the bold
                if (match.Index > lastIndex)
                {
                    var textBefore = text.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Inlines.Add(new Run(textBefore));
                }

                // Add bold text
                var boldRun = new Run(match.Groups[1].Value)
                {
                    FontWeight = FontWeights.Bold
                };
                paragraph.Inlines.Add(boldRun);

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                paragraph.Inlines.Add(new Run(remainingText));
            }
        }

        private void AddCodeBlock(Paragraph paragraph, string code, string language)
        {
            var codeBlock = new Paragraph
            {
                FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                FontSize = 13,
                Background = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBackground),
                BorderBrush = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBorder),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 4)
            };

            // Add language label if specified
            if (!string.IsNullOrEmpty(language))
            {
                var languageRun = new Run($"({language})")
                {
                    FontSize = 11,
                    Foreground = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.GrayText)
                };
                codeBlock.Inlines.Add(languageRun);
                codeBlock.Inlines.Add(new LineBreak());
            }

            // Add code content
            var codeRun = new Run(code.TrimEnd());
            codeBlock.Inlines.Add(codeRun);

            paragraph.Inlines.Add(new InlineUIContainer(new Border
            {
                Child = new RichTextBox
                {
                    Document = new FlowDocument(codeBlock),
                    IsReadOnly = true,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent
                },
                Background = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBackground),
                BorderBrush = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 4, 0, 4)
            }));
        }

        private void UpdateFileReferences()
        {
            // The file references are handled by the XAML data binding
            // This method can be used for additional processing if needed
        }

        private void FileLink_Click(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hyperlink && hyperlink.Tag is FileReference fileRef)
            {
                NavigateToFile(fileRef);
            }
        }

        private void NavigateToFile(FileReference fileRef)
        {
            try
            {
                if (_dte != null && !string.IsNullOrEmpty(fileRef.FilePath))
                {
                    // Open the file in Visual Studio
                    var window = _dte.ItemOperations.OpenFile(fileRef.FilePath);
                    
                    if (window?.Document?.Selection is TextSelection selection)
                    {
                        // Navigate to the specific line if specified
                        if (fileRef.StartLine > 0)
                        {
                            selection.GotoLine(fileRef.StartLine);
                            
                            // Select the range if end line is specified
                            if (fileRef.EndLine > fileRef.StartLine)
                            {
                                selection.GotoLine(fileRef.EndLine, true);
                            }
                        }
                    }
                    
                    // Activate the window
                    window?.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to file {fileRef.FilePath}: {ex.Message}");
            }
        }

        #region IMessageRenderer Implementation

        public UIElement RenderMessage(ChatMessage message)
        {
            Message = message;
            return this;
        }

        public void ClearMessage()
        {
            Message = null;
        }

        public void HighlightText(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            try
            {
                var contentPresenter = FindName("ContentPresenter") as ContentPresenter;
                if (contentPresenter?.Content is TextBlock textBlock)
                {
                    var originalText = textBlock.Text;
                    var highlightBrush = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.SearchHighlightBackground);
                    
                    // Create new TextBlock with highlighted text
                    var newTextBlock = new TextBlock
                    {
                        TextWrapping = textBlock.TextWrapping,
                        Margin = textBlock.Margin
                    };
                    
                    var searchIndex = 0;
                    while (searchIndex < originalText.Length)
                    {
                        var foundIndex = originalText.IndexOf(searchText, searchIndex, StringComparison.OrdinalIgnoreCase);
                        
                        if (foundIndex == -1)
                        {
                            // Add remaining text
                            if (searchIndex < originalText.Length)
                            {
                                newTextBlock.Inlines.Add(new Run(originalText.Substring(searchIndex)));
                            }
                            break;
                        }
                        
                        // Add text before highlight
                        if (foundIndex > searchIndex)
                        {
                            newTextBlock.Inlines.Add(new Run(originalText.Substring(searchIndex, foundIndex - searchIndex)));
                        }
                        
                        // Add highlighted text
                        var highlightRun = new Run(originalText.Substring(foundIndex, searchText.Length))
                        {
                            Background = highlightBrush,
                            FontWeight = FontWeights.Bold
                        };
                        newTextBlock.Inlines.Add(highlightRun);
                        
                        searchIndex = foundIndex + searchText.Length;
                    }
                    
                    contentPresenter.Content = newTextBlock;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error highlighting text: {ex.Message}");
            }
        }

        public void ClearHighlight()
        {
            try
            {
                var contentPresenter = FindName("ContentPresenter") as ContentPresenter;
                if (contentPresenter?.Content is TextBlock textBlock && Message != null)
                {
                    // Restore original text without highlighting
                    var newTextBlock = new TextBlock
                    {
                        Text = Message.Content,
                        TextWrapping = textBlock.TextWrapping,
                        Margin = textBlock.Margin
                    };
                    
                    contentPresenter.Content = newTextBlock;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing highlight: {ex.Message}");
            }
        }

        #region Theme Helper Methods

        private SolidColorBrush GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey themeKey)
        {
            try
            {
                if (_themingService != null)
                {
                    var color = _themingService.GetThemedColor(themeKey);
                    if (color.HasValue)
                    {
                        return new SolidColorBrush(color.Value);
                    }
                }

                // Fallback colors based on theme key
                return themeKey switch
                {
                    AiderVSExtension.Interfaces.ThemeResourceKey.CodeBackground => new SolidColorBrush(Color.FromRgb(248, 248, 248)),
                    AiderVSExtension.Interfaces.ThemeResourceKey.CodeBorder => new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    AiderVSExtension.Interfaces.ThemeResourceKey.GrayText => new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    AiderVSExtension.Interfaces.ThemeResourceKey.SearchHighlightBackground => new SolidColorBrush(Colors.Yellow),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting themed brush: {ex.Message}");
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public UIElement RenderCode(string code, string language)
        {
            var textBlock = new TextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                Background = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBackground),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4),
                TextWrapping = TextWrapping.Wrap
            };
            
            var border = new Border
            {
                Child = textBlock,
                BorderBrush = GetThemedBrush(AiderVSExtension.Interfaces.ThemeResourceKey.CodeBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3)
            };
            
            return border;
        }

        public UIElement RenderFileReference(FileReference fileReference)
        {
            var hyperlink = new Hyperlink();
            hyperlink.Inlines.Add(new Run(fileReference.FilePath));
            hyperlink.Click += (s, e) => 
            {
                FileReferenceClicked?.Invoke(this, new FileReferenceClickedEventArgs 
                { 
                    FileReference = fileReference 
                });
            };
            
            var textBlock = new TextBlock();
            textBlock.Inlines.Add(hyperlink);
            
            return textBlock;
        }

        public UIElement RenderMarkdown(string markdown)
        {
            // Basic markdown rendering - would need a proper markdown library for full support
            var textBlock = new TextBlock
            {
                Text = markdown,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4)
            };
            
            return textBlock;
        }

        public event EventHandler<FileReferenceClickedEventArgs> FileReferenceClicked;

        private RenderTheme _theme = RenderTheme.Default;
        public RenderTheme Theme 
        { 
            get => _theme;
            set 
            {
                _theme = value;
                // Apply theme changes to the UI
                ApplyTheme();
            }
        }

        private void ApplyTheme()
        {
            // Apply theme changes to the current UI
            if (_themingService != null)
            {
                _themingService.ApplyTheme(this);
            }
        }

        #endregion

        #endregion
    }
}