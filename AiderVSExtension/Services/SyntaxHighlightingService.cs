using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;
using System.Threading.Tasks;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Service for syntax highlighting in chat and code displays
    /// </summary>
    public class SyntaxHighlightingService : ISyntaxHighlightingService, IDisposable
    {
        private readonly IVSThemingService _themingService;
        private readonly IErrorHandler _errorHandler;
        private readonly Dictionary<string, SyntaxHighlightingTheme> _customThemes = new Dictionary<string, SyntaxHighlightingTheme>();
        private readonly Dictionary<string, SyntaxRuleSet> _languageRules = new Dictionary<string, SyntaxRuleSet>();
        private SyntaxHighlightingTheme _currentTheme;
        private bool _disposed = false;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public SyntaxHighlightingService(IVSThemingService themingService, IErrorHandler errorHandler)
        {
            _themingService = themingService ?? throw new ArgumentNullException(nameof(themingService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            InitializeLanguageRules();
            InitializeDefaultThemes();
            
            // Subscribe to theme changes
            _themingService.ThemeChanged += OnVSThemeChanged;
            _currentTheme = _themingService.GetModels.SyntaxHighlightingTheme();
        }

        /// <summary>
        /// Highlights syntax in a text document
        /// </summary>
        /// <param name="text">Text to highlight</param>
        /// <param name="language">Programming language</param>
        /// <param name="theme">Theme to use (optional)</param>
        /// <returns>Highlighted document</returns>
        public FlowDocument HighlightSyntax(string text, string language = null, Models.SyntaxHighlightingTheme theme = null)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return new FlowDocument();

                theme = theme ?? _currentTheme;
                var document = new FlowDocument();
                
                // Auto-detect language if not specified
                if (string.IsNullOrEmpty(language))
                {
                    language = DetectLanguage(text);
                }

                // Apply syntax highlighting
                var paragraph = new Paragraph();
                var runs = HighlightText(text, language, theme);
                
                foreach (var run in runs)
                {
                    paragraph.Inlines.Add(run);
                }

                document.Blocks.Add(paragraph);
                
                // Apply document styling
                ApplyDocumentStyling(document, theme);
                
                return document;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.HighlightSyntax");
                return CreateErrorDocument(text, ex.Message);
            }
        }

        /// <summary>
        /// Highlights syntax in a text run collection
        /// </summary>
        /// <param name="text">Text to highlight</param>
        /// <param name="language">Programming language</param>
        /// <param name="theme">Theme to use (optional)</param>
        /// <returns>Collection of highlighted runs</returns>
        public IEnumerable<Run> HighlightText(string text, string language = null, SyntaxHighlightingTheme theme = null)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return new List<Run>();

                theme = theme ?? _currentTheme;
                language = language ?? DetectLanguage(text);
                
                var rules = GetLanguageRules(language);
                var runs = new List<Run>();
                
                if (rules == null)
                {
                    // No syntax rules, return plain text
                    runs.Add(new Run(text) { Foreground = new SolidColorBrush(theme.Foreground) });
                    return runs;
                }

                var tokens = TokenizeText(text, rules);
                
                foreach (var token in tokens)
                {
                    var run = new Run(token.Text);
                    var brush = GetTokenBrush(token.Type, theme);
                    
                    run.Foreground = brush;
                    
                    // Apply additional styling
                    ApplyTokenStyling(run, token.Type, theme);
                    
                    runs.Add(run);
                }
                
                return runs;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.HighlightText");
                return new List<Run> { new Run(text) };
            }
        }

        /// <summary>
        /// Registers a custom syntax highlighting theme
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <param name="theme">Theme definition</param>
        public void RegisterTheme(string name, Models.SyntaxHighlightingTheme theme)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || theme == null)
                    return;

                _customThemes[name] = theme;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.RegisterTheme");
            }
        }

        /// <summary>
        /// Gets a registered theme by name
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <returns>Theme or null if not found</returns>
        public Models.SyntaxHighlightingTheme GetTheme(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                return _customThemes.TryGetValue(name, out var theme) ? theme : null;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.GetTheme");
                return null;
            }
        }

        /// <summary>
        /// Gets all available themes
        /// </summary>
        /// <returns>Dictionary of theme names and themes</returns>
        public Dictionary<string, Models.SyntaxHighlightingTheme> GetAllThemes()
        {
            try
            {
                var themes = new Dictionary<string, Models.SyntaxHighlightingTheme>(_customThemes);
                
                // Add VS theme
                themes["Visual Studio"] = _themingService.GetModels.SyntaxHighlightingTheme();
                
                return themes;
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.GetAllThemes");
                return new Dictionary<string, Models.SyntaxHighlightingTheme>();
            }
        }

        /// <summary>
        /// Saves a custom theme to disk
        /// </summary>
        /// <param name="name">Theme name</param>
        /// <param name="theme">Theme to save</param>
        public async Task SaveCustomThemeAsync(string name, Models.SyntaxHighlightingTheme theme)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || theme == null)
                    return;

                var themesPath = GetThemesDirectory();
                var filePath = Path.Combine(themesPath, $"{name}.json");
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(theme, Newtonsoft.Json.Formatting.Indented);
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
                _customThemes[name] = theme;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "SyntaxHighlightingService.SaveCustomThemeAsync");
            }
        }

        /// <summary>
        /// Loads custom themes from disk
        /// </summary>
        public async Task LoadCustomThemesAsync()
        {
            try
            {
                var themesPath = GetThemesDirectory();
                if (!System.IO.Directory.Exists(themesPath))
                    return;

                var files = System.IO.Directory.GetFiles(themesPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = await System.IO.File.ReadAllTextAsync(file);
                        var theme = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.SyntaxHighlightingTheme>(json);
                        var name = System.IO.Path.GetFileNameWithoutExtension(file);
                        
                        if (theme != null)
                        {
                            _customThemes[name] = theme;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _errorHandler.HandleExceptionAsync(ex, $"SyntaxHighlightingService.LoadCustomThemesAsync.{file}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "SyntaxHighlightingService.LoadCustomThemesAsync");
            }
        }

        /// <summary>
        /// Deletes a custom theme
        /// </summary>
        /// <param name="name">Theme name to delete</param>
        public async Task DeleteCustomThemeAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return;

                _customThemes.Remove(name);
                
                var themesPath = GetThemesDirectory();
                var filePath = Path.Combine(themesPath, $"{name}.json");
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "SyntaxHighlightingService.DeleteCustomThemeAsync");
            }
        }

        /// <summary>
        /// Exports theme to various formats
        /// </summary>
        /// <param name="theme">Theme to export</param>
        /// <param name="format">Export format</param>
        /// <returns>Exported theme data</returns>
        public async Task<string> ExportThemeAsync(Models.SyntaxHighlightingTheme theme, string format = "json")
        {
            try
            {
                return format.ToLowerInvariant() switch
                {
                    "json" => Newtonsoft.Json.JsonConvert.SerializeObject(theme, Newtonsoft.Json.Formatting.Indented),
                    "xml" => SerializeToXml(theme),
                    "css" => ConvertToCss(theme),
                    _ => Newtonsoft.Json.JsonConvert.SerializeObject(theme, Newtonsoft.Json.Formatting.Indented)
                };
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "SyntaxHighlightingService.ExportThemeAsync");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the current theme
        /// </summary>
        /// <param name="theme">Theme to set</param>
        public void SetCurrentTheme(Models.SyntaxHighlightingTheme theme)
        {
            try
            {
                if (theme == null)
                    return;

                var oldTheme = _currentTheme;
                _currentTheme = theme;
                
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                {
                    OldTheme = VSTheme.Dark, // Would need to determine from old theme
                    NewTheme = VSTheme.Dark,  // Would need to determine from new theme
                    ChangedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.SetCurrentTheme");
            }
        }

        /// <summary>
        /// Detects the programming language from code text
        /// </summary>
        /// <param name="text">Code text</param>
        /// <returns>Detected language</returns>
        public string DetectLanguage(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return "plaintext";

                // Language detection patterns
                var patterns = new Dictionary<string, string[]>
                {
                    ["csharp"] = new[] { "using System", "namespace ", "class ", "public class", "private ", "protected ", "internal " },
                    ["javascript"] = new[] { "function ", "const ", "let ", "var ", "=>", "console.log", "document." },
                    ["typescript"] = new[] { "interface ", "type ", "export ", "import ", ": string", ": number", ": boolean" },
                    ["python"] = new[] { "def ", "import ", "from ", "class ", "if __name__", "print(" },
                    ["java"] = new[] { "public class", "private ", "protected ", "public static void main", "System.out" },
                    ["cpp"] = new[] { "#include", "using namespace", "int main", "std::", "cout", "cin" },
                    ["html"] = new[] { "<!DOCTYPE", "<html", "<head", "<body", "<div", "<span" },
                    ["css"] = new[] { "{", "}", ":", ";", "px", "em", "rem", "color:", "background:" },
                    ["json"] = new[] { "{", "}", "[", "]", ":", "\"", "null", "true", "false" },
                    ["xml"] = new[] { "<?xml", "<", ">", "</", "xmlns" },
                    ["sql"] = new[] { "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "CREATE TABLE" }
                };

                var scores = new Dictionary<string, int>();
                
                foreach (var lang in patterns.Keys)
                {
                    scores[lang] = 0;
                    foreach (var pattern in patterns[lang])
                    {
                        if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            scores[lang]++;
                        }
                    }
                }

                var bestMatch = scores.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                return bestMatch.Value > 0 ? bestMatch.Key : "plaintext";
            }
            catch (Exception ex)
            {
                _errorHandler?.HandleExceptionAsync(ex, "SyntaxHighlightingService.DetectLanguage");
                return "plaintext";
            }
        }

        /// <summary>
        /// Gets supported languages
        /// </summary>
        /// <returns>List of supported languages</returns>
        public IEnumerable<string> GetSupportedLanguages()
        {
            return _languageRules.Keys.ToList();
        }

        #region Private Methods

        private void InitializeLanguageRules()
        {
            // C# rules
            _languageRules["csharp"] = new SyntaxRuleSet
            {
                Keywords = new[] { "using", "namespace", "class", "interface", "struct", "enum", "public", "private", "protected", "internal", "static", "readonly", "const", "virtual", "override", "abstract", "sealed", "partial", "if", "else", "switch", "case", "default", "for", "foreach", "while", "do", "break", "continue", "return", "throw", "try", "catch", "finally", "new", "this", "base", "typeof", "sizeof", "is", "as", "ref", "out", "params", "var", "dynamic", "async", "await", "yield", "from", "where", "select", "group", "into", "orderby", "join", "let", "in", "on", "equals", "by", "ascending", "descending" },
                Types = new[] { "int", "long", "short", "byte", "uint", "ulong", "ushort", "sbyte", "float", "double", "decimal", "bool", "char", "string", "object", "void", "DateTime", "TimeSpan", "Guid", "List", "Dictionary", "Array", "IEnumerable", "ICollection", "IList", "IDictionary", "Task", "Action", "Func", "Predicate", "EventHandler", "Exception" },
                StringDelimiters = new[] { "\"", "@\"", "$\"" },
                CommentPatterns = new[] { "//", "/*", "*/" },
                NumberPattern = @"\b\d+(\.\d+)?\b",
                OperatorPattern = @"[+\-*/%=<>!&|^~?:]+",
                IdentifierPattern = @"\b[a-zA-Z_][a-zA-Z0-9_]*\b"
            };

            // JavaScript rules
            _languageRules["javascript"] = new SyntaxRuleSet
            {
                Keywords = new[] { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue", "return", "throw", "try", "catch", "finally", "new", "this", "typeof", "instanceof", "in", "of", "delete", "void", "null", "undefined", "true", "false", "async", "await", "yield", "class", "extends", "super", "static", "import", "export", "from", "as", "default" },
                Types = new[] { "Object", "Array", "String", "Number", "Boolean", "Date", "RegExp", "Error", "Promise", "Map", "Set", "WeakMap", "WeakSet", "Symbol", "BigInt" },
                StringDelimiters = new[] { "\"", "'", "`" },
                CommentPatterns = new[] { "//", "/*", "*/" },
                NumberPattern = @"\b\d+(\.\d+)?\b",
                OperatorPattern = @"[+\-*/%=<>!&|^~?:]+",
                IdentifierPattern = @"\b[a-zA-Z_$][a-zA-Z0-9_$]*\b"
            };

            // Python rules
            _languageRules["python"] = new SyntaxRuleSet
            {
                Keywords = new[] { "def", "class", "if", "elif", "else", "for", "while", "break", "continue", "return", "yield", "import", "from", "as", "try", "except", "finally", "with", "lambda", "global", "nonlocal", "and", "or", "not", "in", "is", "None", "True", "False", "pass", "del", "raise", "assert", "async", "await" },
                Types = new[] { "int", "float", "str", "bool", "list", "dict", "tuple", "set", "frozenset", "bytes", "bytearray", "complex", "object", "type", "classmethod", "staticmethod", "property" },
                StringDelimiters = new[] { "\"", "'", "\"\"\"", "'''" },
                CommentPatterns = new[] { "#" },
                NumberPattern = @"\b\d+(\.\d+)?\b",
                OperatorPattern = @"[+\-*/%=<>!&|^~]+",
                IdentifierPattern = @"\b[a-zA-Z_][a-zA-Z0-9_]*\b"
            };

            // Add more languages as needed...
        }

        private void InitializeDefaultThemes()
        {
            // Dark theme
            _customThemes["Dark"] = new Models.SyntaxHighlightingTheme
            {
                Background = Color.FromRgb(30, 30, 30),
                Foreground = Color.FromRgb(220, 220, 220),
                Keyword = Color.FromRgb(86, 156, 214),
                String = Color.FromRgb(214, 157, 133),
                Comment = Color.FromRgb(106, 153, 85),
                Number = Color.FromRgb(181, 206, 168),
                Operator = Color.FromRgb(220, 220, 220),
                Identifier = Color.FromRgb(156, 220, 254),
                Error = Color.FromRgb(244, 71, 71),
                Warning = Color.FromRgb(255, 197, 61),
                Information = Color.FromRgb(86, 156, 214)
            };

            // Light theme
            _customThemes["Light"] = new Models.SyntaxHighlightingTheme
            {
                Background = Color.FromRgb(255, 255, 255),
                Foreground = Color.FromRgb(0, 0, 0),
                Keyword = Color.FromRgb(0, 0, 255),
                String = Color.FromRgb(163, 21, 21),
                Comment = Color.FromRgb(0, 128, 0),
                Number = Color.FromRgb(0, 0, 0),
                Operator = Color.FromRgb(0, 0, 0),
                Identifier = Color.FromRgb(43, 145, 175),
                Error = Color.FromRgb(255, 0, 0),
                Warning = Color.FromRgb(255, 140, 0),
                Information = Color.FromRgb(0, 0, 255)
            };

            // Monokai theme
            _customThemes["Monokai"] = new Models.SyntaxHighlightingTheme
            {
                Background = Color.FromRgb(39, 40, 34),
                Foreground = Color.FromRgb(248, 248, 242),
                Keyword = Color.FromRgb(249, 38, 114),
                String = Color.FromRgb(230, 219, 116),
                Comment = Color.FromRgb(117, 113, 94),
                Number = Color.FromRgb(174, 129, 255),
                Operator = Color.FromRgb(249, 38, 114),
                Identifier = Color.FromRgb(166, 226, 46),
                Error = Color.FromRgb(249, 38, 114),
                Warning = Color.FromRgb(230, 219, 116),
                Information = Color.FromRgb(102, 217, 239)
            };
        }

        private SyntaxRuleSet GetLanguageRules(string language)
        {
            if (string.IsNullOrEmpty(language))
                return null;

            _languageRules.TryGetValue(language.ToLowerInvariant(), out var rules);
            return rules;
        }

        private List<SyntaxToken> TokenizeText(string text, SyntaxRuleSet rules)
        {
            var tokens = new List<SyntaxToken>();
            var lines = text.Split('\n');
            
            foreach (var line in lines)
            {
                TokenizeLine(line, rules, tokens);
                if (line != lines.Last())
                {
                    tokens.Add(new SyntaxToken { Text = "\n", Type = TokenType.Text });
                }
            }
            
            return tokens;
        }

        private void TokenizeLine(string line, SyntaxRuleSet rules, List<SyntaxToken> tokens)
        {
            var position = 0;
            
            while (position < line.Length)
            {
                var token = GetNextToken(line, position, rules);
                if (token == null)
                    break;
                    
                tokens.Add(token);
                position += token.Text.Length;
            }
        }

        private SyntaxToken GetNextToken(string line, int position, SyntaxRuleSet rules)
        {
            if (position >= line.Length)
                return null;

            var remaining = line.Substring(position);
            
            // Check for comments
            foreach (var comment in rules.CommentPatterns)
            {
                if (remaining.StartsWith(comment))
                {
                    return new SyntaxToken
                    {
                        Text = remaining,
                        Type = TokenType.Comment
                    };
                }
            }
            
            // Check for strings
            foreach (var delimiter in rules.StringDelimiters)
            {
                if (remaining.StartsWith(delimiter))
                {
                    var endPos = remaining.IndexOf(delimiter, delimiter.Length);
                    if (endPos > 0)
                    {
                        return new SyntaxToken
                        {
                            Text = remaining.Substring(0, endPos + delimiter.Length),
                            Type = TokenType.String
                        };
                    }
                }
            }
            
            // Check for numbers
            var numberMatch = Regex.Match(remaining, rules.NumberPattern);
            if (numberMatch.Success && numberMatch.Index == 0)
            {
                return new SyntaxToken
                {
                    Text = numberMatch.Value,
                    Type = TokenType.Number
                };
            }
            
            // Check for operators
            var operatorMatch = Regex.Match(remaining, rules.OperatorPattern);
            if (operatorMatch.Success && operatorMatch.Index == 0)
            {
                return new SyntaxToken
                {
                    Text = operatorMatch.Value,
                    Type = TokenType.Operator
                };
            }
            
            // Check for identifiers/keywords
            var identifierMatch = Regex.Match(remaining, rules.IdentifierPattern);
            if (identifierMatch.Success && identifierMatch.Index == 0)
            {
                var text = identifierMatch.Value;
                var tokenType = TokenType.Identifier;
                
                if (rules.Keywords.Contains(text))
                    tokenType = TokenType.Keyword;
                else if (rules.Types.Contains(text))
                    tokenType = TokenType.Type;
                
                return new SyntaxToken
                {
                    Text = text,
                    Type = tokenType
                };
            }
            
            // Default to single character
            return new SyntaxToken
            {
                Text = remaining.Substring(0, 1),
                Type = TokenType.Text
            };
        }

        private Brush GetTokenBrush(TokenType tokenType, Models.SyntaxHighlightingTheme theme)
        {
            return tokenType switch
            {
                TokenType.Keyword => new SolidColorBrush(theme.Keyword),
                TokenType.Type => new SolidColorBrush(theme.Keyword),
                TokenType.String => new SolidColorBrush(theme.String),
                TokenType.Comment => new SolidColorBrush(theme.Comment),
                TokenType.Number => new SolidColorBrush(theme.Number),
                TokenType.Operator => new SolidColorBrush(theme.Operator),
                TokenType.Identifier => new SolidColorBrush(theme.Identifier),
                _ => new SolidColorBrush(theme.Foreground)
            };
        }

        private void ApplyTokenStyling(Run run, TokenType tokenType, Models.SyntaxHighlightingTheme theme)
        {
            switch (tokenType)
            {
                case TokenType.Keyword:
                case TokenType.Type:
                    run.FontWeight = FontWeights.Bold;
                    break;
                case TokenType.Comment:
                    run.FontStyle = FontStyles.Italic;
                    break;
            }
        }

        private void ApplyDocumentStyling(FlowDocument document, Models.SyntaxHighlightingTheme theme)
        {
            document.Background = new SolidColorBrush(theme.Background);
            document.FontFamily = new FontFamily("Consolas, 'Courier New', monospace");
            document.FontSize = 14;
            document.LineHeight = 16;
        }

        private FlowDocument CreateErrorDocument(string text, string error)
        {
            var document = new FlowDocument();
            var paragraph = new Paragraph();
            
            paragraph.Inlines.Add(new Run(text) { Foreground = Brushes.Red });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run($"Error: {error}") { Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
            
            document.Blocks.Add(paragraph);
            return document;
        }

        private async void OnVSThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            try
            {
                _currentTheme = _themingService.GetModels.SyntaxHighlightingTheme();
                ThemeChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleExceptionAsync(ex, "SyntaxHighlightingService.OnVSThemeChanged");
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _themingService.ThemeChanged -= OnVSThemeChanged;
                _customThemes.Clear();
                _languageRules.Clear();
                _disposed = true;
            }
        }
    }
}