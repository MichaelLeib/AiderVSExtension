using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiderVSExtension.Interfaces;
using AiderVSExtension.Models;

namespace AiderVSExtension.Services
{
    /// <summary>
    /// Concrete implementation of ICompletionProvider for AI-powered code completion
    /// </summary>
    public class CompletionProviderService : ICompletionProvider, IDisposable
    {
        private readonly IAIModelManager _aiModelManager;
        private readonly IConfigurationService _configurationService;
        private readonly IErrorHandler _errorHandler;
        private bool _disposed = false;

        public CompletionProviderService(
            IAIModelManager aiModelManager,
            IConfigurationService configurationService,
            IErrorHandler errorHandler)
        {
            _aiModelManager = aiModelManager ?? throw new ArgumentNullException(nameof(aiModelManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CompletionItem>> GetCompletionsAsync(
            CompletionContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                // Check if AI completion is enabled
                var config = await _configurationService.GetConfigurationAsync().ConfigureAwait(false);
                if (!config.IsAICompletionEnabled)
                {
                    return GetFallbackCompletions(context);
                }

                // Generate AI-powered completions
                var aiCompletions = await GenerateAICompletionsAsync(context, cancellationToken).ConfigureAwait(false);
                
                // Add fallback completions if AI returns few results
                if (aiCompletions.Count() < 5)
                {
                    var fallbackCompletions = GetFallbackCompletions(context);
                    aiCompletions = aiCompletions.Concat(fallbackCompletions);
                }

                return aiCompletions.Take(10); // Limit to top 10 completions
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex, "Error generating completions");
                return GetFallbackCompletions(context);
            }
        }

        private bool _isEnabled = true;
        
        /// <inheritdoc/>
        public bool IsEnabled
        {
            get
            {
                try
                {
                    if (!_isEnabled) return false;
                    var config = _configurationService.GetConfiguration();
                    return config.IsAICompletionEnabled;
                }
                catch
                {
                    return false;
                }
            }
            set => _isEnabled = value;
        }

        /// <inheritdoc/>
        public int Priority => 100; // High priority for AI completions

        /// <summary>
        /// Generates AI-powered completions for the given context
        /// </summary>
        private async Task<IEnumerable<CompletionItem>> GenerateAICompletionsAsync(
            CompletionContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                // Build prompt for AI completion
                var prompt = BuildCompletionPrompt(context);
                
                // Get completion from AI model
                var response = await _aiModelManager.GenerateCompletionAsync(prompt, cancellationToken).ConfigureAwait(false);
                
                // Parse AI response into completion items
                return ParseAIResponse(response, context);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex, "AI completion generation failed");
                return Enumerable.Empty<CompletionItem>();
            }
        }

        /// <summary>
        /// Builds a prompt for AI completion based on the context
        /// </summary>
        private string BuildCompletionPrompt(CompletionContext context)
        {
            var prompt = $@"Complete the following {context.Language} code. Provide only the completion text, no explanations.

File: {context.FilePath}
Context:
{context.PrecedingText}

Current line: {context.CurrentLine}
Cursor position: {context.Position}

Suggest completions for the code at the cursor position. Return up to 5 suggestions, one per line.";

            return prompt;
        }

        /// <summary>
        /// Parses AI response into completion items
        /// </summary>
        private IEnumerable<CompletionItem> ParseAIResponse(string response, CompletionContext context)
        {
            if (string.IsNullOrWhiteSpace(response))
                return Enumerable.Empty<CompletionItem>();

            var completions = new List<CompletionItem>();
            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(lines.Length, 5); i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    completions.Add(new CompletionItem
                    {
                        Text = line,
                        DisplayText = line,
                        Description = "AI-generated completion",
                        InsertText = line,
                        Kind = CompletionItemKind.Text,
                        Priority = 100 - i, // Higher priority for first suggestions
                        Source = "AI",
                        IsFromAI = true
                    });
                }
            }

            return completions;
        }

        /// <summary>
        /// Gets fallback completions when AI is unavailable
        /// </summary>
        private IEnumerable<CompletionItem> GetFallbackCompletions(CompletionContext context)
        {
            var completions = new List<CompletionItem>();

            // Add common language keywords based on context
            var keywords = GetLanguageKeywords(context.Language);
            foreach (var keyword in keywords.Where(k => k.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase)))
            {
                completions.Add(new CompletionItem
                {
                    Text = keyword,
                    DisplayText = keyword,
                    Description = $"{context.Language} keyword",
                    InsertText = keyword,
                    Kind = CompletionItemKind.Keyword,
                    Priority = 50,
                    Source = "Fallback",
                    IsFromAI = false
                });
            }

            // Add common snippets
            var snippets = GetCommonSnippets(context.Language);
            foreach (var snippet in snippets.Where(s => s.Trigger.StartsWith(context.CurrentWord, StringComparison.OrdinalIgnoreCase)))
            {
                completions.Add(new CompletionItem
                {
                    Text = snippet.Trigger,
                    DisplayText = snippet.Trigger,
                    Description = snippet.Description,
                    InsertText = snippet.Content,
                    Kind = CompletionItemKind.Snippet,
                    Priority = 40,
                    Source = "Fallback",
                    IsFromAI = false
                });
            }

            return completions.Take(10);
        }

        /// <summary>
        /// Gets language keywords for fallback completions
        /// </summary>
        private string[] GetLanguageKeywords(string language)
        {
            return language?.ToLowerInvariant() switch
            {
                "csharp" or "cs" => new[]
                {
                    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                    "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                    "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                    "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                    "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                    "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                    "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
                    "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
                    "void", "volatile", "while", "async", "await", "var"
                },
                "javascript" or "js" => new[]
                {
                    "abstract", "arguments", "boolean", "break", "byte", "case", "catch", "char", "class",
                    "const", "continue", "debugger", "default", "delete", "do", "double", "else", "enum",
                    "eval", "export", "extends", "false", "final", "finally", "float", "for", "function",
                    "goto", "if", "implements", "import", "in", "instanceof", "int", "interface", "let",
                    "long", "native", "new", "null", "package", "private", "protected", "public", "return",
                    "short", "static", "super", "switch", "synchronized", "this", "throw", "throws",
                    "transient", "true", "try", "typeof", "var", "void", "volatile", "while", "with", "yield"
                },
                "typescript" or "ts" => new[]
                {
                    "abstract", "any", "as", "boolean", "break", "case", "catch", "class", "const", "continue",
                    "declare", "default", "delete", "do", "else", "enum", "export", "extends", "false",
                    "finally", "for", "from", "function", "if", "implements", "import", "in", "instanceof",
                    "interface", "let", "module", "namespace", "new", "null", "number", "package", "private",
                    "protected", "public", "readonly", "return", "static", "string", "super", "switch",
                    "this", "throw", "true", "try", "type", "typeof", "undefined", "var", "void", "while", "with", "yield"
                },
                _ => new string[0]
            };
        }

        /// <summary>
        /// Gets common code snippets for the language
        /// </summary>
        private CodeSnippet[] GetCommonSnippets(string language)
        {
            return language?.ToLowerInvariant() switch
            {
                "csharp" or "cs" => new[]
                {
                    new CodeSnippet("for", "for loop", "for (int i = 0; i < length; i++)\n{\n    \n}"),
                    new CodeSnippet("foreach", "foreach loop", "foreach (var item in collection)\n{\n    \n}"),
                    new CodeSnippet("if", "if statement", "if (condition)\n{\n    \n}"),
                    new CodeSnippet("try", "try-catch block", "try\n{\n    \n}\ncatch (Exception ex)\n{\n    \n}"),
                    new CodeSnippet("class", "class definition", "public class ClassName\n{\n    \n}"),
                    new CodeSnippet("method", "method definition", "public void MethodName()\n{\n    \n}"),
                    new CodeSnippet("prop", "property", "public Type PropertyName { get; set; }")
                },
                "javascript" or "js" => new[]
                {
                    new CodeSnippet("function", "function declaration", "function functionName() {\n    \n}"),
                    new CodeSnippet("for", "for loop", "for (let i = 0; i < array.length; i++) {\n    \n}"),
                    new CodeSnippet("foreach", "forEach loop", "array.forEach((item) => {\n    \n});"),
                    new CodeSnippet("if", "if statement", "if (condition) {\n    \n}"),
                    new CodeSnippet("try", "try-catch block", "try {\n    \n} catch (error) {\n    \n}"),
                    new CodeSnippet("class", "class definition", "class ClassName {\n    constructor() {\n        \n    }\n}")
                },
                _ => new CodeSnippet[0]
            };
        }

        /// <summary>
        /// Represents a code snippet for completion
        /// </summary>
        private class CodeSnippet
        {
            public string Trigger { get; }
            public string Description { get; }
            public string Content { get; }

            public CodeSnippet(string trigger, string description, string content)
            {
                Trigger = trigger;
                Description = description;
                Content = content;
            }
        }

        public async Task<CompletionDetails> GetCompletionDetailsAsync(CompletionItem item, CancellationToken cancellationToken)
        {
            try
            {
                if (item == null)
                    return null;

                return new CompletionDetails
                {
                    DetailedDescription = item.Description ?? $"AI completion: {item.Label}",
                    Documentation = item.Documentation ?? "AI-generated code completion",
                    Parameters = new List<ParameterInfo>(),
                    Examples = new List<string>(),
                    ReturnType = item.Kind.ToString()
                };
            }
            catch (Exception ex)
            {
                _errorHandler?.LogErrorAsync($"Error getting completion details: {ex.Message}", ex, "CompletionProviderService.GetCompletionDetailsAsync");
                return null;
            }
        }

        public bool ShouldTriggerCompletion(CompletionContext context)
        {
            try
            {
                if (context == null || !IsEnabled)
                    return false;

                // Trigger on certain characters or when typing identifiers
                return context.TriggerKind == CompletionTriggerKind.TriggerCharacter ||
                       context.TriggerKind == CompletionTriggerKind.Invoked ||
                       (context.TriggerKind == CompletionTriggerKind.TriggerForIncompleteCompletions && 
                        !string.IsNullOrWhiteSpace(context.TextBeforeCursor));
            }
            catch (Exception ex)
            {
                _errorHandler?.LogErrorAsync($"Error checking completion trigger: {ex.Message}", ex, "CompletionProviderService.ShouldTriggerCompletion");
                return false;
            }
        }

        public IEnumerable<char> TriggerCharacters => new[] { '.', ':', '>', ' ', '(', '[', '<', '"', '\'' };


        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}