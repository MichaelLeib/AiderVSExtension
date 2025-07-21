using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using AiderVSExtension.UI.Chat;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service implementation for rendering chat messages with syntax highlighting and formatting
    /// </summary>
    public class MessageRendererService : IMessageRenderer, IDisposable
    {
        private RenderTheme _theme;
        private bool _disposed = false;

        public MessageRendererService()
        {
            // Initialize with default theme
            _theme = new RenderTheme
            {
                BackgroundColor = "#FFFFFF",
                ForegroundColor = "#000000",
                AccentColor = "#0078D4",
                CodeBackgroundColor = "#F8F8F8",
                LinkColor = "#0066CC",
                FontFamily = "Segoe UI",
                FontSize = 14
            };
        }

        /// <inheritdoc/>
        public event EventHandler<FileReferenceClickedEventArgs> FileReferenceClicked;

        /// <inheritdoc/>
        public RenderTheme Theme
        {
            get => _theme;
            set => _theme = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public UIElement RenderMessage(ChatMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Create the message renderer control
                var messageRenderer = new MessageRenderer();
                messageRenderer.Message = message;

                // Apply theme
                ApplyThemeToControl(messageRenderer);

                return messageRenderer;
            }
            catch (Exception ex)
            {
                // Return error display on failure
                return CreateErrorDisplay($"Error rendering message: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public UIElement RenderCode(string code, string language)
        {
            if (string.IsNullOrEmpty(code))
                return new TextBlock { Text = "No code content" };

            try
            {
                var border = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.CodeBackgroundColor)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 4, 0, 4)
                };

                var stackPanel = new StackPanel();

                // Add language label if provided
                if (!string.IsNullOrEmpty(language))
                {
                    var languageLabel = new TextBlock
                    {
                        Text = language,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    stackPanel.Children.Add(languageLabel);
                }

                // Add code content
                var codeText = new TextBlock
                {
                    Text = code,
                    FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                    FontSize = 13,
                    TextWrapping = TextWrapping.NoWrap,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.ForegroundColor))
                };

                // Apply basic syntax highlighting based on language
                ApplySyntaxHighlighting(codeText, language);

                stackPanel.Children.Add(codeText);
                border.Child = stackPanel;

                return border;
            }
            catch (Exception ex)
            {
                return CreateErrorDisplay($"Error rendering code: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public UIElement RenderFileReference(FileReference fileReference)
        {
            if (fileReference == null)
                throw new ArgumentNullException(nameof(fileReference));

            try
            {
                var hyperlink = new TextBlock();
                var link = new Hyperlink(new Run(GetDisplayPath(fileReference.FilePath)))
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.LinkColor)),
                    TextDecorations = TextDecorations.Underline
                };

                link.Click += (sender, e) => OnFileReferenceClicked(fileReference);
                
                hyperlink.Inlines.Add(link);

                // Add line information if available
                if (fileReference.StartLine > 0)
                {
                    string lineInfo = fileReference.EndLine > fileReference.StartLine
                        ? $" (lines {fileReference.StartLine}-{fileReference.EndLine})"
                        : $" (line {fileReference.StartLine})";
                    
                    hyperlink.Inlines.Add(new Run(lineInfo)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        FontSize = 12
                    });
                }

                return hyperlink;
            }
            catch (Exception ex)
            {
                return CreateErrorDisplay($"Error rendering file reference: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public UIElement RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return new TextBlock { Text = "No content" };

            try
            {
                var flowDocument = new FlowDocument();
                var paragraph = new Paragraph();

                // Parse markdown content
                ParseMarkdownContent(markdown, paragraph);

                flowDocument.Blocks.Add(paragraph);

                var richTextBox = new RichTextBox
                {
                    Document = flowDocument,
                    IsReadOnly = true,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    FontFamily = new FontFamily(_theme.FontFamily),
                    FontSize = _theme.FontSize,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.ForegroundColor))
                };

                return richTextBox;
            }
            catch (Exception ex)
            {
                return CreateErrorDisplay($"Error rendering markdown: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies theme styling to a control
        /// </summary>
        private void ApplyThemeToControl(FrameworkElement control)
        {
            try
            {
                control.FontFamily = new FontFamily(_theme.FontFamily);
                control.FontSize = _theme.FontSize;
                
                if (control.Background == null)
                {
                    control.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.BackgroundColor));
                }
                
                if (control.Foreground == null)
                {
                    control.SetValue(Control.ForegroundProperty, 
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.ForegroundColor)));
                }
            }
            catch (Exception)
            {
                // Ignore theme application errors
            }
        }

        /// <summary>
        /// Applies basic syntax highlighting to code text
        /// </summary>
        private void ApplySyntaxHighlighting(TextBlock textBlock, string language)
        {
            // Basic syntax highlighting - can be enhanced with proper syntax highlighter
            if (string.IsNullOrEmpty(language))
                return;

            try
            {
                var text = textBlock.Text;
                textBlock.Inlines.Clear();

                // Simple keyword highlighting for common languages
                var keywords = GetLanguageKeywords(language);
                if (keywords.Length == 0)
                {
                    textBlock.Inlines.Add(new Run(text));
                    return;
                }

                var pattern = @"\b(" + string.Join("|", keywords) + @")\b";
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    // Add text before keyword
                    if (match.Index > lastIndex)
                    {
                        textBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                    }

                    // Add highlighted keyword
                    var keywordRun = new Run(match.Value)
                    {
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.AccentColor)),
                        FontWeight = FontWeights.Bold
                    };
                    textBlock.Inlines.Add(keywordRun);

                    lastIndex = match.Index + match.Length;
                }

                // Add remaining text
                if (lastIndex < text.Length)
                {
                    textBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
                }
            }
            catch (Exception)
            {
                // Fallback to plain text
                textBlock.Inlines.Clear();
                textBlock.Inlines.Add(new Run(textBlock.Text));
            }
        }

        /// <summary>
        /// Gets keywords for syntax highlighting
        /// </summary>
        private string[] GetLanguageKeywords(string language)
        {
            return language?.ToLowerInvariant() switch
            {
                "csharp" or "cs" => new[] { "class", "public", "private", "protected", "internal", "static", "void", "string", "int", "bool", "var", "if", "else", "for", "foreach", "while", "return", "new", "this", "base", "using", "namespace" },
                "javascript" or "js" => new[] { "function", "var", "let", "const", "if", "else", "for", "while", "return", "class", "extends", "import", "export", "async", "await", "try", "catch" },
                "typescript" or "ts" => new[] { "function", "var", "let", "const", "if", "else", "for", "while", "return", "class", "extends", "import", "export", "async", "await", "try", "catch", "interface", "type", "enum" },
                "python" => new[] { "def", "class", "if", "else", "elif", "for", "while", "return", "import", "from", "try", "except", "with", "as", "lambda", "and", "or", "not" },
                _ => new string[0]
            };
        }

        /// <summary>
        /// Parses markdown content into WPF inlines
        /// </summary>
        private void ParseMarkdownContent(string markdown, Paragraph paragraph)
        {
            var lines = markdown.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            bool inCodeBlock = false;
            var codeBlockContent = new System.Text.StringBuilder();
            string codeBlockLanguage = null;

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        // End code block
                        var codeElement = RenderCode(codeBlockContent.ToString(), codeBlockLanguage);
                        paragraph.Inlines.Add(new InlineUIContainer(codeElement));
                        codeBlockContent.Clear();
                        codeBlockLanguage = null;
                        inCodeBlock = false;
                    }
                    else
                    {
                        // Start code block
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
                    ProcessMarkdownLine(line, paragraph);
                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            // Handle unclosed code block
            if (inCodeBlock && codeBlockContent.Length > 0)
            {
                var codeElement = RenderCode(codeBlockContent.ToString(), codeBlockLanguage);
                paragraph.Inlines.Add(new InlineUIContainer(codeElement));
            }
        }

        /// <summary>
        /// Processes a single markdown line
        /// </summary>
        private void ProcessMarkdownLine(string line, Paragraph paragraph)
        {
            // Handle inline code
            var codePattern = @"`([^`]+)`";
            var codeMatches = Regex.Matches(line, codePattern);

            int lastIndex = 0;
            foreach (Match match in codeMatches)
            {
                // Add text before code
                if (match.Index > lastIndex)
                {
                    var textBefore = line.Substring(lastIndex, match.Index - lastIndex);
                    ProcessFormattedText(textBefore, paragraph);
                }

                // Add inline code
                var codeRun = new Run(match.Groups[1].Value)
                {
                    FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_theme.CodeBackgroundColor)),
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

        /// <summary>
        /// Processes formatted text (bold, italic, etc.)
        /// </summary>
        private void ProcessFormattedText(string text, Paragraph paragraph)
        {
            // Handle bold text
            var boldPattern = @"\*\*([^*]+)\*\*";
            var boldMatches = Regex.Matches(text, boldPattern);

            int lastIndex = 0;
            foreach (Match match in boldMatches)
            {
                // Add text before bold
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

        /// <summary>
        /// Gets a display-friendly file path
        /// </summary>
        private string GetDisplayPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "Unknown file";

            try
            {
                return System.IO.Path.GetFileName(filePath);
            }
            catch
            {
                return filePath;
            }
        }

        /// <summary>
        /// Creates an error display element
        /// </summary>
        private UIElement CreateErrorDisplay(string message)
        {
            return new TextBlock
            {
                Text = message,
                Foreground = Brushes.Red,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(4)
            };
        }

        /// <summary>
        /// Raises the FileReferenceClicked event
        /// </summary>
        private void OnFileReferenceClicked(FileReference fileReference)
        {
            FileReferenceClicked?.Invoke(this, new FileReferenceClickedEventArgs { FileReference = fileReference });
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                FileReferenceClicked = null;
                _disposed = true;
            }
        }
    }
}